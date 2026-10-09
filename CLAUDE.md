# FlowForge — Claude working agreement

## How we work (read first)

This is a **learning project. I write the code; you are my pairing partner and reviewer.**

- **Do not create or edit files** unless I explicitly ask you to in that message.
  - Exception: this `CLAUDE.md` and files under `docs/` may be updated when I ask you to record a decision.
- Your job: review my changes, explain trade-offs, ask probing questions, point out bugs and design smells, and push back when something will hurt later.
- Snippets are fine when they illustrate an idea (a few lines, a signature, a shape). Do not hand me complete classes or modules unless I ask.
- When I say **"review"**: look at `git diff` (and untracked files), run `dotnet build` and `dotnet test`, then give feedback ordered by importance. Keep nits separate and short.
- Prefer questions that lead me to the answer over giving the answer, when the point is a design decision.
- You may run read-only and verification commands (`git status/diff/log`, `dotnet build`, `dotnet test`, `docker compose ps`) without asking.

## Project goal

A .NET 10 workflow engine, built progressively:

- Workflows are **versioned directed graphs**: steps (nodes) + transitions (edges).
- Steps are executed by **generic handlers referenced by key** (resolved from DI).
- An **event stream is the source of truth**; current instance state is a **projection**.
- Stack: ASP.NET Core, CQRS, DDD, EF Core, **PostgreSQL**.
- Later: background workers/queues, human tasks, retries, audit/history, SignalR, authorization, maybe a visual designer.

## Roadmap

1. **Hardcoded engine, in memory** — core vocabulary: definition, version, step, transition, instance, handler. ← *current phase (domain modeling of definitions first)*
2. **Events as source of truth** — instance emits events, state rebuilt by folding them. Still in memory.
3. **Hand-rolled event store on PostgreSQL via EF Core** — append-only table, optimistic concurrency, inline projection.
4. **CQRS over ASP.NET Core** — commands go through aggregates, queries read projections.
5. **Configurable, versioned definitions** stored as data.
6. Workers/queues, retries, human tasks, SignalR, authz, audit views, designer.

Don't build ahead of the current phase. Flag it if I'm about to.

## Decisions made (do not silently contradict; challenge openly if needed)

### Versioning
- **Instances are pinned** to the definition version they started on: they record `DefinitionId` + `DefinitionVersion`, ideally inside the `WorkflowStarted` event.
- **Published versions are immutable.** Changes always produce a new version (draft N+1).
- **Steps have stable IDs** separate from display names (renames don't change identity; future migration mapping keys on them).
- **Migration of running instances** to a newer version is a *wanted later feature*, not now. It will be "re-pin + mapping rules + an `InstanceMigrated` event".

### Version lifecycle (decided 2026-10-07)
- **`VersionStatus`** is a closed set: `Draft`, `Published`, `Retired`, `Discarded`. Allowed transitions: `Draft → Published`, `Draft → Discarded`, `Published → Retired`, **`Retired → Published`**. `Discarded` is terminal. Only `Draft` is editable; only `Published` can start instances.
- **`Retired → Published` (reactivation) is intentional.** Use case: a new version turns out to be problematic, so retire it temporarily and re-publish the previous one (rollback). The graph never changes, so immutability still holds.
- **At most one `Published` version per definition.** Enforced twice:
  - **Aggregate/Application (to be implemented):** `Publish` and reactivate must check that no other version of the same definition is `Published`. `VersionStatus.CanTransitionTo` only validates a single status's own transitions; it cannot see sibling versions.
  - **Database (phase 3+):** partial unique index on `DefinitionId` filtered to `Status = 'Published'`, as the backstop against races. Detect violations via `PostgresException.SqlState == "23505"`, like the event-store concurrency check.
- **Rollback order matters:** retire the problematic version *before* publishing the previous one, in the same transaction. Publishing first would violate the invariant.
- **Running instances on a retired version continue**; retiring only blocks starting new ones. We want to see which instances are still pinned to a retired version: that is a **read-model/query concern** (projection keyed on `DefinitionId` + `DefinitionVersion`, phase 4), not something the version aggregate tracks.
- **Reactivation (`Retired → Published`) re-runs graph validation**, same as the first publish. Cheap, and it doesn't rely on trusting old data.
- **One `Draft` at a time (for now).** A second cross-version invariant, enforced like the Published one: the aggregate rejects creating a draft while one exists, and phase 3+ adds a partial unique index on `DefinitionId` filtered to `Status = 'Draft'`. Easier to relax later than to add. Consequence: "edit" always targets the single draft, and "create draft N+1" fails if a draft exists.

### Model split
- `WorkflowDefinition` — **the aggregate root**: identity + metadata (name, description; mutable) + the collection of its versions. It owns the cross-version invariants (one `Published`, one `Draft`, version numbering N+1).
- `WorkflowDefinitionVersion` — **a child entity inside `WorkflowDefinition`, not its own aggregate** (decided 2026-10-07). Holds the graph (steps, transitions) and status; immutable once published.
  - **The version owns its own graph validation.** `Version.Publish()` validates the graph, then flips the status (same on reactivation). The definition checks the cross-version rule first, then delegates. If the version can't validate itself, it can't be published.
  - **A version holds no reference to the definition or its siblings.** Its state-changing methods are `internal`, callable only by the definition.
  - **The definition never reads a sibling version's graph.** Cross-version invariants depend only on number and status; old graphs are inert payload. If loading ever hurts, split along that line (lightweight version entries in the aggregate, graph loaded separately or keyed by `(DefinitionId, Version)`). Never half-load children behind the aggregate's back.
  - **Planned persistence (phase 3+):** graph as one `jsonb` column (EF owned type mapped to JSON) on the version row, so loading all versions is a single query.
- **`StepDefinition` and `Transition` are immutable records**; edits are done by replacement. Consequence: creating draft N+1 from version N copies the *collection* (a new list), and sharing the immutable instances is safe (no deep copy). Tests: after editing v2 (add a node, remove the start node), v1's graph equals its snapshot from before the edit (records give value equality); and a v2 transition to a step that exists only in v1 fails v2's validation. (EF owned types can't have two owners, so persistence will need separate instances per version — a phase 3 concern.)
  - **Guarded factory, closed against `with`.** `StepDefinition` (and `Transition` as needed) is a `sealed record` with a private constructor and **get-only properties, no `init` accessor**, so `step with { ... }` does not compile and can't bypass validation. Edits go through explicit methods (e.g. `step.WithHandlerKey(key)`) that route through the same factory and return a new instance. Factory enforces: valid `StepId` slug, valid `HandlerKey`, non-null display name.
  - **`HandlerKey` is a value object** (own type, like `DisplayName`), distinct from `StepId`. Rules are in "Identifiers".
  - **Graph-level checks** are split between mutation time and `Validate()`; see "Graph validation".
- **CQRS direction:** keep the child-entity model for writes; add a read-side query that loads one version directly for execution (phase 4+, don't build ahead). The executor loads by **pinned `(DefinitionId, Version)`**, not "the published one": running instances stay on retired versions. "The published one" is only for starting a new instance. Published graphs are immutable, so a loaded version is cacheable forever under that key.
- `WorkflowInstance` — one run; separate aggregate; pinned to (DefinitionId, Version).

### Graph validation (decided 2026-10-08)
- **A draft may be broken.** An empty draft is the natural starting state; graph problems are *reported*, not prevented, until `Publish()`.
- **Start step is a pointer**: `WorkflowDefinitionVersion.StartStepId`, nullable, set explicitly with `SetStart(stepId)` (no auto-assign of the first step; can be added later if a designer UI wants it). It is not a flag on `StepDefinition`.
- **`Validate()` returns a list of issues; `Publish()` throws.** `Publish()` throws `ValidationException : DomainException` carrying `IReadOnlyList<Issue>`, so callers and tests assert on codes, not message text. Construction invariants keep the single-`Error` `DomainException` contract (see "Errors").
- **`Issue(DomainError Error, Subject Subject)` (decided 2026-10-09): built on `DomainError`, one family, not two systems.** The shared vocabulary is a stable code plus a message; the issue only adds *where*. The code identifies the *rule*; one rule can produce several issues.
  - **`Subject` is a small closed discriminated union** (replaces the earlier "two nullable properties" and `TransitionRef(From, Key, To?)` ideas), one case per *kind of thing an issue is about*. Type-safe, no free-form string path. A UI switches on the code and the subject case.
    - `Version`: about the version as a whole (`StartStepMissing`, `NoEndStep`).
    - `Step(StepId)`: about one step (`StepUnreachable`, `StepCannotReachEnd`).
    - `Edge(From, Key, To)`: about one concrete transition (`TransitionSourceNotFound`, `TransitionTargetNotFound`). `Key` is part of the identity: without it two edges between the same steps are indistinguishable.
    - `Transition(From, Key)`: about the *group* of transitions leaving `From` under `Key` (`AmbiguousOutcome`). Separate case on purpose: a nullable `To` meaning "not applicable for this code" would be a hidden rule every consumer must know.
  - **Invariant: each code always uses the same subject kind.** `AmbiguousOutcome` is always `Transition`, `TransitionTargetNotFound` is always `Edge`, and so on. One test per code asserts the subject kind, and consumers (API, UI) may rely on it.
  - **Still open:** which subject `StartStepNotFound` uses (`Version`, since the pointer is a property of the version, or `Step(missingId)`); and the case name `Transition` collides with the domain `Transition` record (namespace or rename, e.g. `Outcome`).
- **Issue codes are closed codes, not closed message instances.** The code is the identity; messages and subjects carry the specifics, so parameterized errors (like `VersionStatus`'s `Unknown(name)`) still work. Codes follow the one convention in "Errors".
- **`Validate()` checks relations only.** Format rules guard new input through the value-object factories (`From`); relation rules guard publishing. A legacy ID loaded through `Restore` never blocks publishing for its format.
- **Rejected at mutation time (not issues):**
  - A duplicate `StepId` in `AddStep` (a keyed collection cannot hold two). So "unique step IDs" is *not* a `Validate()` rule. It is a normal single fail-fast `DomainError` with the stable code `step.duplicate`. **The rule of thumb (decided 2026-10-09):** invariants that can never hold in any state are rejected at mutation, so the aggregate can never contain them; `Validate()` covers states that are legal while drafting but not publishable (no start step, unreachable steps, ambiguous outcomes). A duplicate in `Validate()` could never fire: dead code that looks like coverage.
  - **Replay caveat:** if an aggregate is ever rebuilt by replaying events through `AddStep`, a duplicate in the stream would throw. Fine as long as events are written only after the command passed; add a replay test if that path appears.
  - Any edit to a non-draft version (invariant 6).
- **Reported by `Validate()`** (a draft can hold all of these):
  - `StartStepMissing`: no start pointer set.
  - `StartStepNotFound`: the start pointer references a step that does not exist.
  - `TransitionSourceNotFound` and `TransitionTargetNotFound`: separate codes on purpose, as precise as possible.
  - `NoEndStep`: no step without outgoing transitions.
  - `StepUnreachable`: a step not reachable from the start.
  - `StepCannotReachEnd`: a step from which no end step can be reached (needed because cycles are allowed).
  - `AmbiguousOutcome`: two or more transitions with the same `(From, TransitionKey)`, regardless of target (routing is a function of those two; even when both lead to the same step, one is redundant). **One issue per `(From, key)` group**, subject `Transition` with `To == null`. An exact duplicate edge is just another case; no special handling. `Validate()` only, never rejected at add-time.
- **`RemoveStep` may leave things dangling** (the start pointer, transitions pointing at the removed step); `Validate()` reports it. No cascade, no rejection.
- **An empty draft reports both `StartStepMissing` and `NoEndStep`.** That noise is accepted.
- **Skip derived issues when the root cause is already reported:** `StepUnreachable` is not reported while the start is missing or dangling. `Validate()` is therefore not a set of fully independent checks; ordering matters.
- **Cycles are allowed** (e.g. rejected → edit → resubmit). Because of that, "at least one end step" is not enough: every step must be able to reach an end step (`StepCannotReachEnd`).
- **Self-loops (`A → A`) are not allowed.** Enforced in the `Transition` factory at create time (needs no graph context), so it is not a `Validate()` code. `A → B → A` stays legal. Retrying the same step is the handler's `Failed(reason, retryable)`, not a graph edge.
- **Transitions are stored in a `List`** (not a set keyed by `(From, Key)`, since a draft may hold ambiguous edges). Removal is by full value (`From`, `Key`, `To`), which works because records have value equality; `List.Remove` drops the first match and identical edges are interchangeable. No `TransitionId` until something must reference one edge from elsewhere.
- **More skip rules (derived issues are not reported when the root cause is):**
  - `StepCannotReachEnd` is skipped while `NoEndStep` is reported.
  - Dangling edges (source or target not found) are ignored as traversable edges, and `StepCannotReachEnd` is suppressed for a step whose outgoing edge is dangling: `TransitionTargetNotFound` already reports it, and a second issue would break the `Assert.Single` style of test.
- **`Rehydrate` throws on duplicate `StepId`s** (structural guard: a keyed collection cannot hold them). It never runs `Validate()`, because stored drafts may legitimately be broken and must still load.
- **`StepId` is frozen from creation.** To change one, remove the step and add a new one, rewriting its edges by hand. No `RenameStep` until the designer needs it; display names are what get renamed.
- **Reactivation re-runs `Validate()`** like the first publish (see "Version lifecycle").

### Errors (decided 2026-10-09)
- **`DomainError(string Code, string Message)`** is a record in `Domain/Common`. **`DomainException(DomainError)`** exposes it as `.Error`; the exception message is `Error.Message`. Tests assert on `ex.Error.Code`, never on message text.
- **`DomainGuard` takes a `DomainError`**, not a parameter name (`CallerArgumentExpression` gave useless messages like "value cannot be null"). Each type owns its errors in a nested `Errors` class of `static readonly DomainError` fields; parameterized errors are static methods (e.g. `VersionStatus.Errors.Unknown(name)`).
- **Errors are owned per type, deliberately (decided 2026-10-09).** Each type keeps its own `Errors` class and codes, even when two types share a rule (`StepId` and `TransitionKey`): they are separate types so the rules can diverge. Centralize only truly agnostic behavior (e.g. `DomainGuard`'s null/whitespace/empty checks), never rule-specific checks or messages.
- **Two jobs, same shape.** Construction invariants (`StepId.From`) fail fast with **one** `DomainException`. `Validate()` collects relational issues across the graph and `Publish()` throws a plural `ValidationException : DomainException` (see "Graph validation").
- **One code convention: `type.rule`**, lowercase with underscores inside each part (e.g. `step_id.too_long`, `display_name.empty`), across construction errors and validation issues, so a code is never ambiguous. Codes are stable and may be persisted or exposed. (Event type names are a separate namespace and stay kebab-case, e.g. `workflow.step-completed`.)
- **Test structure (decided 2026-10-09):** value-object tests use nested classes per concern (`FromTests`, `RestoreTests`, `EqualityTests`, `ToStringTests`) inside a static class named after the type, with shared data in a `...Cases` class. Keep this for `TransitionKey`, `HandlerKey` and later types; older flat test files are left as they are.

### Naming and uniqueness
- **`DisplayName` value object** (Domain/Common) holds every display name: trimmed, never null or whitespace, **casing preserved as entered**.
- **Names compare case-insensitively** (`OrdinalIgnoreCase` in `Equals`/`GetHashCode`); assert on `.Value` when a test needs exact text.
- **Definition-name uniqueness is an Application/DB concern (phase 3+).** Plan: the domain computes a normalized key (`Value.ToUpperInvariant()`) stored next to the name, with a plain unique index on that column — not `lower(name)` or `citext`, whose case folding can disagree with .NET.
- **`Description` is free text, so it is the one tolerant place** (decided 2026-10-08): trim; null or whitespace-only becomes empty (never `null`); never change case; cap at 1000 characters and reject beyond it. A plain `string` property, not a value object (no invariants worth a type). Not used in equality or routing.
- **Within a version, only step IDs are unique.** Step display names may repeat; no invariant checks them (decided 2026-10-07).

### Identifiers
- **Strongly-typed IDs as `sealed record` classes** (not structs: `default(T)` would bypass the guard). Private constructor, get-only `Value`; no public parameterless constructor.
- **Guid-backed IDs** (`WorkflowDefinitionId`, later `InstanceId`): `New()` generates `Guid.CreateVersion7()`; `From(Guid)` is the guarded entry point (rejects `Guid.Empty`) used for rehydration, JSON and the API edge.
- **`StepId` is a human-chosen slug** (e.g. `approve-request`), not a Guid. One validating factory, no generator. Distinct from the handler key.
  - **Slug rule (decided 2026-10-08):** lowercase `a-z`, digits and `-` as the separator; must start with a letter; length 1 to 64; must not end with `-` or contain `--`. Illustration: `^[a-z](?:[a-z0-9]|-(?=[a-z0-9])){0,63}$`.
  - **Strict rejection, no normalization:** `"Approve-Request"` or `" approve "` throws, with no trimming or lowercasing. Equality is plain ordinal. Frozen from creation (see "Graph validation").
  - **API shape (decided 2026-10-08):** `StepId.From(string?)` is the strict, guarded entry point (same name as `WorkflowDefinitionId.From`, for consistency across IDs). `StepId.Restore(string?)` is the lenient path for stored data: it rejects only null, empty and whitespace, and does no trimming or normalization. Both build the same record type; equality is `Value` ordinal and there is deliberately **no "was restored" flag** (it would break value equality, and a restored value can only equal a strict one if it is a valid slug anyway).
  - **Rule placement (revised 2026-10-09, replaces the earlier "regex with `\z`, no loop" decision):** `From` is a **sequence of separate checks, one `DomainError` per rule**, with a hand-rolled character loop (`All(...)`) instead of a regex. A regex cannot say *which* rule failed; specific errors are the goal. This also removes the `$`-before-trailing-`\n` trap. Check order is a **tested contract** (`PrecedenceCases`): empty/null/whitespace, too long (so huge inputs never reach the character loop), must start with a lowercase letter, invalid character, consecutive `--`, trailing `-`. The length cap applies in `From` only, never in `Restore`.
  - **`StepId.ToString()` returns `Value`** (like `DisplayName`), so messages and logs show the slug, not a record dump. `From` and `Restore` both take `string?`.
  - Error codes: `step_id.empty`, `step_id.too_long`, `step_id.must_start_with_letter`, `step_id.invalid_character`, `step_id.consecutive_hyphens`, `step_id.trailing_hyphen`; exposed through a public nested `StepId.Errors`.
- **`HandlerKey` rules (decided 2026-10-08):** lowercase `a-z`, digits and `-`, with `.` allowed as a namespace separator (e.g. `billing.charge-card`; the handler registry is global). Must start with a letter, 1 to 100 characters, no empty segments (`a..b`), no leading or trailing separators. Strict rejection, no normalization: `"Send-Email"` throws. `Rehydrate` stays lenient.
- **Entity signatures take value objects, not `string`** (decided 2026-10-08): constructors, `Create`, `Rehydrate` and mutators take `DisplayName`, `StepId`, `HandlerKey` and `TransitionKey`. Validation happens once, in the value object's constructor/factory, and entities do not re-check it. Raw strings stay at the edges (API, EF converters) and are parsed into value objects immediately.
- **`TransitionKey` is a value object**, a separate type from `StepId` that currently shares its rule (lowercase, digits, `-`; same strict rejection). Separate types so either rule can change later without touching the other, and so one can't be passed where the other belongs. No `_` until a real outcome name needs it. **No reserved `"default"` and no fallback routing for now**: that is a new semantic that interacts with `AmbiguousOutcome` and needs its own decision. `Rehydrate` stays lenient for both (see below).
- **No generic `Id<T>` base yet.** Revisit at the third Guid-backed ID or in phase 3 (one EF/JSON converter for all IDs).
- **Entities have two named entry points**: `Create(...)` (generates the ID) and `Rehydrate(id, ...)` (takes a typed ID, never reads the clock or generates). Constructors are private; `Rehydrate` rejects a null ID.
- **`Rehydrate` is deliberately more lenient than `Create`** (decided 2026-10-07): a rule tightened later (slug format, handler-key format) must never make stored data unloadable, so `Create` enforces the current rules and `Rehydrate` does not re-apply them. Keep only the structural guards needed to avoid a broken object (e.g. null checks). This applies to `StepDefinition`, `Transition` and their value objects (`StepId`, `HandlerKey`), so those need a lenient rehydration path separate from the validating factory.

### Time
- **No services in the domain.** Entities never receive a `TimeProvider`; they receive a `DateTimeOffset`.
- The private constructor **normalizes to UTC** (`ToUniversalTime()`), for `Create` and `Rehydrate` alike: the offset carries no meaning here, and normalizing never makes a stored row fail to load. Tests assert on `.Offset`, since `DateTimeOffset` equality ignores it.
- **The Application layer supplies the time** via `TimeProvider.GetUtcNow()` (wired up in phase 4; `FakeTimeProvider` in Application tests).
- **No `DateTime` for instants anywhere** (code, tests, DTOs, EF mappings). The domain can't detect `DateTimeKind.Unspecified` after the implicit conversion, so the rule is enforced at the callers. **Enforced at build time (decided 2026-10-08):** `Microsoft.CodeAnalysis.BannedApiAnalyzers` with a `BannedSymbols.txt` in the Domain project (and Application once it has code), so a violation is a build error, not a convention. Also ban `DateTimeOffset.UtcNow`/`Now`. Time enters the domain as a `DateTimeOffset` parameter (e.g. `Publish(at)`), never read from a clock. A `TimeProvider` (Application layer) supplies the value from outside; it is never injected into the domain. In tests, use `FakeTimeProvider` (Microsoft.Extensions.TimeProvider.Testing) only where a `TimeProvider` exists (Application).

### Execution
- **Async-capable handler contract from day one**: a handler returns an outcome — `Completed(TransitionKey, output)` (a `TransitionKey` value object, not a raw string; a handler that builds an invalid key has a bug, which surfaces at runtime as a failed node rather than a silent mismatch; takes effect when the executor is built in phase 1, nothing earlier depends on it), `Waiting(waitKey)` (human task/signal/timer), or `Failed(reason, retryable)`.
- **Inline execution first**; queues/workers come later and must only change the executor, not the domain or handlers.
- **Handlers must be idempotent** (assume at-least-once execution once retries/queues exist).

### Event store (hand-rolled, PostgreSQL)
- Events table: `GlobalPosition` (bigint identity), `StreamId` (one stream per instance), `StreamVersion`, `EventType`, `Data` (jsonb), `Metadata` (jsonb: correlation/causation id, user, timestamp).
- **Unique index on (StreamId, StreamVersion)** for optimistic concurrency; detect conflicts via Npgsql `PostgresException.SqlState == "23505"`.
- **Stable event type names** (e.g. `workflow.step-completed`) mapped explicitly to CLR types — never store CLR type names.
- Stored events are forever: evolve via new event types or upcasting on read.
- **Projections inline** (same transaction) to start. Known future trap for async projections: identity values are assigned at insert, not commit, so positions can become visible out of order.
- No snapshots until proven necessary.
- Abstraction (`IEventStore`) in Application/Domain; Postgres implementation in Infrastructure.

### Architecture
- Clean Architecture: Domain (no dependencies) ← Application ← Infrastructure; Api references Application + Infrastructure for DI wiring only. Keep EF Core and Npgsql out of Domain.

## Current exercise

Model the definition graph in **Domain only** (no persistence, no engine):

- `WorkflowDefinitionVersion` with version number and `VersionStatus` (`Draft`, `Published`, `Retired`, `Discarded`)
- `StepDefinition` with stable `StepId`, display name, handler key
- `Transition`: from step → to step + key/label (e.g. `approved` / `rejected`)
- `Publish()` validates the graph, then freezes it

Invariants (test-first, one failing test each). `Publish()` throws if `Validate()` reports any issue; codes are in "Graph validation":
1. Start step set and existing (`StartStepMissing`, `StartStepNotFound`: two tests)
2. Step IDs unique within a version (rejected by `AddStep`, not a `Validate()` rule)
3. Every transition references existing steps (`TransitionSourceNotFound`, `TransitionTargetNotFound`: two tests)
4. At least one end step (`NoEndStep`)
5. Every step reachable from the start step (`StepUnreachable`; skipped while the start is missing or dangling)
6. Modifying steps/transitions on a non-draft version throws
7. Every step can reach an end step (`StepCannotReachEnd`; needed because cycles are allowed; skipped while `NoEndStep` is reported or for steps with a dangling outgoing edge)
8. No two transitions share `(From, TransitionKey)` (`AmbiguousOutcome`, one issue per group; parameterize over same and different targets)
9. A transition cannot loop on its own step (rejected by the `Transition` factory, not a `Validate()` code)

Open questions I must answer (ask me about them in reviews):
- ~~Is `WorkflowDefinitionVersion` its own aggregate?~~ Decided: child entity inside `WorkflowDefinition` (see "Model split").
- ~~Are cycles allowed?~~ Decided: yes; every step must be able to reach an end step (see "Graph validation").
- ~~Self-loops~~ Decided: not allowed, rejected in the `Transition` factory.
- ~~Duplicate transition keys~~ Decided: `AmbiguousOutcome`, one issue per `(From, key)` group.
- ~~`StepCannotReachEnd` vs `NoEndStep`~~ Decided: skipped (see "Graph validation").
- ~~`Rehydrate` with duplicate `StepId`s~~ Decided: throws.
- ~~Where does "create draft N+1 from version N" live?~~ Decided: on `WorkflowDefinition` (it enforces the one-draft and numbering rules).

## Outstanding review items on existing code

Last reviewed 2026-10-08 (71 tests passing; one CS8618 warning from the `WorkflowDefinitionVersion` stub). Ask me about the open decisions in reviews.

### Open decisions
- ~~**`StepId` slug rules**~~ Decided 2026-10-08 (see "Identifiers"): strict, frozen from creation.
- ~~**`HandlerKey` rules**~~ Decided 2026-10-08 (see "Identifiers").
- ~~**`DateTime` ban enforcement**~~ Decided 2026-10-08: BannedApiAnalyzers (see "Time"). Not set up yet.
- ~~**Entity signatures**~~ Decided 2026-10-08: value objects, not `string` (see "Identifiers"). `WorkflowDefinition` still takes `string`.
- ~~**`Description` normalization**~~ Decided 2026-10-08 (see "Naming and uniqueness"). Not implemented yet.
- ~~**xUnit v3**~~ Decided 2026-10-08: migrate to `xunit.v3`. Not done yet.

### To do
- **Next: `TransitionKey`, test-first** (same rule as `StepId`, separate type; reuse the `StepId` shape: `From`/`Restore`, `Errors` with `transition_key.*` codes, nested test classes). **Decided: duplicate the checks, no shared helper** (errors owned per type; see "Errors"). Then `HandlerKey`, `StepDefinition`, `Transition`, then the version graph and `Validate()`.
- Phase 4: inject `TimeProvider` in Application and pass `GetUtcNow()` into `Create`.
- Switch `WorkflowDefinition` constructors/`Create`/`Rehydrate`/`Rename` to take `DisplayName`, and update the tests.
- `Description`: trim, null/whitespace becomes empty, cap at 1000. The property changes from `string?` to `string`, so the existing nullable-description tests change.
- Add `Microsoft.CodeAnalysis.BannedApiAnalyzers` and `BannedSymbols.txt` (Domain, and Application when it has code).
- Migrate `FlowForge.Domain.Tests` to `xunit.v3` (executable output type, Microsoft.Testing.Platform); follow the official migration guide rather than copying v2 setup.

### Nits
- Trailing whitespace on blank lines in a few files; missing final newline in `WorkflowDefinitionId.cs`.
- Template comments in `Program.cs`; `FlowForge.Api.http` is still tracked.

### Done
- `StepId` (2026-10-09): `From`/`Restore`, per-rule `DomainError` codes, tested (135 tests passing, 0 warnings).
- `DomainError` + `DomainException.Error` + `DomainGuard(…, DomainError)` rolled out to `DisplayName`, `VersionStatus`, `WorkflowDefinition`, `WorkflowDefinitionId` (see "Errors").
- Renamed `Workflow` → `WorkflowDefinition`.
- `Guid.Empty` guard on `Id`, tested.
- `DomainException` replaces `ArgumentException` in the domain.
- `DisplayName` value object; name normalization decided (see "Naming and uniqueness").
- Test naming (`_Should...`); tests use `[Theory]` where data-driven and mirror the source folders (`Common/`, `Workflows/`).
- Removed the redundant Infrastructure → Domain reference.
- `WorkflowDefinitionId` (typed, Guid v7) with `New()`/`From()`; `WorkflowDefinition` uses it, with `Create`/`Rehydrate` and a null-ID guard (see "Identifiers").
- `CreatedAtUtc` → `CreatedAt` (`DateTimeOffset`), normalized to UTC, tested for both entry points (see "Time").
- `DomainGuard`: `paramName` now used in messages; added `AgainstNull`.
- Wiring tests assert on `.Name.Value`.
- `VersionStatus` (closed set: `Draft`, `Published`, `Retired`, `Discarded`) with `CanTransitionTo`, `IsEditable`, `CanStartInstance` and `FromName`; tested (see "Version lifecycle").
- Domain classes use explicit private constructors, no primary constructors (parameters would stay in scope and could silently shadow the properties).
- `WorkflowVersion` renamed to `WorkflowDefinitionVersion` (class and file), matching the docs.

## Environment

- Ubuntu 24.04 (WSL), .NET 10, solution file `FlowForge.slnx`
- Projects: `src/FlowForge.{Api,Application,Domain,Infrastructure}`, `tests/FlowForge.Domain.Tests` (xUnit v2 today; moving to `xunit.v3`)
- PostgreSQL planned (likely via Docker) — not set up yet
