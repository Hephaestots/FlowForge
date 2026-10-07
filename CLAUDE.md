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

### Model split
- `WorkflowDefinition` — identity + metadata (name, description). Metadata is mutable.
- `WorkflowDefinitionVersion` — the graph (steps, transitions), status `Draft` → `Published`, immutable once published.
- `WorkflowInstance` — one run; separate aggregate; pinned to (DefinitionId, Version).

### Naming and uniqueness
- **`DisplayName` value object** (Domain/Common) holds every display name: trimmed, never null or whitespace, **casing preserved as entered**.
- **Names compare case-insensitively** (`OrdinalIgnoreCase` in `Equals`/`GetHashCode`); assert on `.Value` when a test needs exact text.
- **Definition-name uniqueness is an Application/DB concern (phase 3+).** Plan: the domain computes a normalized key (`Value.ToUpperInvariant()`) stored next to the name, with a plain unique index on that column — not `lower(name)` or `citext`, whose case folding can disagree with .NET.
- **Within a version, only step IDs are unique.** Step display names may repeat; no invariant checks them (decided 2026-10-07).

### Identifiers
- **Strongly-typed IDs as `sealed record` classes** (not structs: `default(T)` would bypass the guard). Private constructor, get-only `Value`; no public parameterless constructor.
- **Guid-backed IDs** (`WorkflowDefinitionId`, later `InstanceId`): `New()` generates `Guid.CreateVersion7()`; `From(Guid)` is the guarded entry point (rejects `Guid.Empty`) used for rehydration, JSON and the API edge.
- **`StepId` is a human-chosen slug** (e.g. `approve-request`), not a Guid. One validating factory, no generator. Distinct from the handler key. Slug format rule and when it freezes are still open.
- **No generic `Id<T>` base yet.** Revisit at the third Guid-backed ID or in phase 3 (one EF/JSON converter for all IDs).
- **Entities have two named entry points**: `Create(...)` (generates the ID) and `Rehydrate(id, ...)` (takes a typed ID, never reads the clock or generates). Constructors are private; `Rehydrate` rejects a null ID.

### Time
- **No services in the domain.** Entities never receive a `TimeProvider`; they receive a `DateTimeOffset`.
- The private constructor **normalizes to UTC** (`ToUniversalTime()`), for `Create` and `Rehydrate` alike: the offset carries no meaning here, and normalizing never makes a stored row fail to load. Tests assert on `.Offset`, since `DateTimeOffset` equality ignores it.
- **The Application layer supplies the time** via `TimeProvider.GetUtcNow()` (wired up in phase 4; `FakeTimeProvider` in Application tests).
- **No `DateTime` for instants anywhere** (code, tests, DTOs, EF mappings). The domain can't detect `DateTimeKind.Unspecified` after the implicit conversion, so the rule is enforced at the callers. Enforcement mechanism (BannedApiAnalyzers vs convention) is still open.

### Execution
- **Async-capable handler contract from day one**: a handler returns an outcome — `Completed(transitionKey, output)`, `Waiting(waitKey)` (human task/signal/timer), or `Failed(reason, retryable)`.
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

- `WorkflowDefinitionVersion` with version number and `Draft`/`Published` status
- `StepDefinition` with stable `StepId`, display name, handler key
- `Transition`: from step → to step + key/label (e.g. `approved` / `rejected`)
- `Publish()` validates the graph, then freezes it

Invariants enforced by `Publish()` (test-first, one failing test each):
1. Exactly one start step
2. Step IDs unique within a version
3. Every transition references existing steps
4. At least one end step (no outgoing transitions)
5. Every step reachable from the start step
6. Modifying steps/transitions on a published version throws

Open questions I must answer (ask me about them in reviews):
- Is `WorkflowDefinitionVersion` its own aggregate or an entity inside `WorkflowDefinition`? (consistency boundary; size after many versions)
- Are cycles allowed (e.g. rejected → edit → resubmit)? If yes, how does that affect invariant 4?
- Where does "create draft N+1 from version N" live?

## Outstanding review items on existing code

Last reviewed 2026-10-07 (build clean, 34 tests passing). Ask me about the open decisions in reviews.

### Open decisions
- **`StepId` slug rules**: allowed characters/length (leaning lowercase kebab-case so equality is plain ordinal); frozen from creation or only after `Publish()`? Should rehydration use the same validation as creation (a tightened rule must not make old stored data unloadable)?
- **`DateTime` ban enforcement**: BannedApiAnalyzers (`BannedSymbols.txt`) on Domain/Application, or convention only?
- **Entity signatures**: the constructor and `Rename` take `string` while `Name` returns `DisplayName`. Pick one style before `StepDefinition` copies it.
- **`Description` normalization**: `""` and `"   "` are stored as given. Does blank collapse to `null`?
- **xUnit v3**: still on v2 (2.9.3). Migrate or stay?

### To do
- Phase 4: inject `TimeProvider` in Application and pass `GetUtcNow()` into `Create`.
- Primary-constructor parameters stay in scope in `WorkflowDefinition`; reading `name` instead of `Name` in a method would silently use stale data. Keep in mind as entities grow.

### Nits
- Trailing whitespace on blank lines in a few files; missing final newline in `WorkflowDefinitionId.cs`.
- Template comments in `Program.cs`; `FlowForge.Api.http` is still tracked.

### Done
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

## Environment

- Ubuntu 24.04 (WSL), .NET 10, solution file `FlowForge.slnx`
- Projects: `src/FlowForge.{Api,Application,Domain,Infrastructure}`, `tests/FlowForge.Domain.Tests` (xUnit v2)
- PostgreSQL planned (likely via Docker) — not set up yet
