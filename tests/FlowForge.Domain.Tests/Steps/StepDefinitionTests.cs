using FlowForge.Domain.Common;
using FlowForge.Domain.Handlers;
using FlowForge.Domain.Steps;
using FlowForge.Domain.Tests.Handlers;

namespace FlowForge.Domain.Tests.Steps;

public static class StepDefinitionTests
{
    private static StepId ValidId() => StepId.From("approve-submission");
    private static DisplayName ValidName() => new("Submission Approval");
    private static HandlerKey ValidKey() => HandlerKey.From("submission.human-approval");

    private static StepDefinition ValidStep() => StepDefinition.Create(ValidId(), ValidName(), ValidKey());

    public sealed class CreateTests
    {
        [Fact]
        public void Create_WithValidValues_ShouldCreateStepDefinition()
        {
            // Arrange
            const string name = "Submission Approval";
            const string step = "approve-submission";
            const string key = "submission.human-approval";

            var displayName = new DisplayName(name);
            var stepId = StepId.From(step);
            var handlerKey = HandlerKey.From(key);

            // Act
            var stepDefinition = StepDefinition.Create(stepId, displayName, handlerKey);

            // Assert
            Assert.Equal(name,  stepDefinition.Name.Value);
            Assert.Equal(step,  stepDefinition.Id.Value);
            Assert.Equal(key,  stepDefinition.HandlerKey.Value);
        }

        [Fact]
        public void Create_WithNullStepId_ShouldThrowIdNullException()
        {
            var ex = Assert.Throws<DomainException>(() => StepDefinition.Create(
                id: null!, ValidName(), ValidKey()));

            Assert.Equal(StepDefinition.Errors.IdNull.Code, ex.Error.Code);
        }

        [Fact]
        public void Create_WithNullDisplayName_ShouldThrowNameNullException()
        {
            var ex = Assert.Throws<DomainException>(() => StepDefinition.Create(
                ValidId(), null!, ValidKey()));

            Assert.Equal(StepDefinition.Errors.NameNull.Code, ex.Error.Code);
        }

        [Fact]
        public void Create_WithNullHandlerKey_ShouldThrowHandlerKeyNullException()
        {
            var ex = Assert.Throws<DomainException>(() => StepDefinition.Create(
                ValidId(), ValidName(), null!));

            Assert.Equal(StepDefinition.Errors.HandlerKeyNull.Code, ex.Error.Code);
        }
    }

    public sealed class RehydrateTests
    {
        [Fact]
        public void Rehydrate_WithValidValues_ShouldRehydrateStepDefinition()
        {
            // Arrange
            const string name = "Submission Approval";
            const string step = "approve-submission";
            const string key = "submission.human-approval";

            var displayName = new DisplayName(name);
            var stepId = StepId.From(step);
            var handlerKey = HandlerKey.From(key);

            // Act
            var stepDefinition = StepDefinition.Rehydrate(stepId, displayName, handlerKey);

            // Assert
            Assert.Equal(name,  stepDefinition.Name.Value);
            Assert.Equal(step,  stepDefinition.Id.Value);
            Assert.Equal(key,  stepDefinition.HandlerKey.Value);
        }

        [Fact]
        public void Rehydrate_WithNullStepId_ShouldThrowIdNullException()
        {
            var ex = Assert.Throws<DomainException>(() => StepDefinition.Rehydrate(
                id: null!, ValidName(), ValidKey()));

            Assert.Equal(StepDefinition.Errors.IdNull.Code, ex.Error.Code);
        }

        [Fact]
        public void Rehydrate_WithNullDisplayName_ShouldThrowNameNullException()
        {
            var ex = Assert.Throws<DomainException>(() => StepDefinition.Rehydrate(
                ValidId(), null!, ValidKey()));

            Assert.Equal(StepDefinition.Errors.NameNull.Code, ex.Error.Code);
        }

        [Fact]
        public void Rehydrate_WithNullHandlerKey_ShouldThrowHandlerKeyNullException()
        {
            var ex = Assert.Throws<DomainException>(() => StepDefinition.Rehydrate(
                ValidId(), ValidName(), null!));

            Assert.Equal(StepDefinition.Errors.HandlerKeyNull.Code, ex.Error.Code);
        }

        [Theory]
        [MemberData(nameof(StepIdCases.LegacyFormatCases), MemberType = typeof(StepIdCases))]
        public void Rehydrate_ShouldKeepLegacyStepIdExactly(string codeFromWouldThrow, string legacy)
        {
            // Premise: Today's rules reject this id, i.e. legacy id.
            var ex = Assert.Throws<DomainException>(() => StepId.From(legacy));

            Assert.Equal(codeFromWouldThrow, ex.Error.Code);

            // Stored data loads anyway, untouched.
            var step = StepDefinition.Rehydrate(
                StepId.Restore(legacy),
                new DisplayName("Approve request"),
                HandlerKey.From("billing.charge-card"));

            Assert.Equal(legacy, step.Id.Value);
        }

        [Theory]
        [MemberData(nameof(HandlerKeyCases.LegacyFormatCases), MemberType = typeof(HandlerKeyCases))]
        public void Rehydrate_ShouldKeepLegacyHandlerKeyExactly(string codeFromWouldThrow, string legacy)
        {
            var ex = Assert.Throws<DomainException>(() => HandlerKey.From(legacy));
            Assert.Equal(codeFromWouldThrow, ex.Error.Code);

            var step = StepDefinition.Rehydrate(
                StepId.From("approve-request"),
                new DisplayName("Approve request"),
                HandlerKey.Restore(legacy));

            Assert.Equal(legacy, step.HandlerKey.Value);
        }
    }

    public sealed class WithDisplayNameTests
    {
        [Fact]
        public void WithDisplayName_ShouldReturnNewInstance_AndLeaveOriginalUnchanged()
        {
            var original = ValidStep();

            var renamed = original.WithDisplayName(new DisplayName("Human submission approval"));

            Assert.NotSame(original, renamed);
            Assert.Equal("Human submission approval", renamed.Name.Value);
            Assert.Equal(ValidName().Value, original.Name.Value);
        }

        [Fact]
        public void WithDisplayName_ShouldKeepIdAndHandlerKey()
        {
            var original = ValidStep();

            var renamed = original.WithDisplayName(new DisplayName("Human submission approval"));

            Assert.Equal(original.Id.Value, renamed.Id.Value);
            Assert.Equal(original.HandlerKey.Value, renamed.HandlerKey.Value);
        }

        [Fact]
        public void WithDisplayName_WithNullDisplayName_ShouldThrowNameNullException()
        {
            var step = StepDefinition.Rehydrate(ValidId(), ValidName(), ValidKey());

            var ex = Assert.Throws<DomainException>(() => step.WithDisplayName(null!));

            Assert.Equal(StepDefinition.Errors.NameNull.Code, ex.Error.Code);
        }
    }

    public sealed class WithHandlerKeyTests
    {
        [Fact]
        public void WithHandlerKey_ShouldReturnNewInstance_AndLeaveOriginalUnchanged()
        {
            var original = ValidStep();

            var changed = original.WithHandlerKey(HandlerKey.From("human.submission-approval"));

            Assert.NotSame(original, changed);
            Assert.Equal("human.submission-approval", changed.HandlerKey.Value);
            Assert.Equal(ValidKey().Value, original.HandlerKey.Value);
        }

        [Fact]
        public void WithHandlerKey_ShouldKeepIdAndName()
        {
            var original = ValidStep();

            var changed = original.WithHandlerKey(HandlerKey.From("human.submission-approval"));

            Assert.Equal(original.Id.Value, changed.Id.Value);
            Assert.Equal(original.Name.Value, changed.Name.Value);
        }

        [Fact]
        public void WithHandlerKey_WithNullHandlerKey_ShouldThrowHandlerKeyNullException()
        {
            var step = StepDefinition.Rehydrate(ValidId(), ValidName(), ValidKey());

            var ex = Assert.Throws<DomainException>(() => step.WithHandlerKey(null!));

            Assert.Equal(StepDefinition.Errors.HandlerKeyNull.Code, ex.Error.Code);
        }
    }

    public sealed class EqualityTests
    {
        private const string OtherName = "Human submission approval";

        [Fact]
        public void Equals_IdenticalValues_ShouldBeEqual()
        {
            // ValidStep() builds every part from scratch, so nothing is shared by reference.
            var first = ValidStep();
            var second = ValidStep();

            Assert.NotSame(first, second);
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.True(first == second);
            Assert.False(first != second);
        }

        [Fact]
        public void Equals_DifferentName_ShouldBeEqual()
        {
            var first = ValidStep();
            var second = first.WithDisplayName(new DisplayName(OtherName));

            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.True(first == second);
            Assert.False(first != second);

            // Equality is name-blind, so prove the rename really happened.
            Assert.NotEqual(first.Name.Value, second.Name.Value);
            Assert.Equal(OtherName, second.Name.Value);
        }

        [Fact]
        public void Equals_DifferentHandlerKey_ShouldNotBeEqual()
        {
            var first = ValidStep();
            var second = first.WithHandlerKey(HandlerKey.From("human.submission-approval"));

            Assert.NotEqual(first, second);
            Assert.False(first == second);
            Assert.True(first != second);

            // Only the handler key changed.
            Assert.Equal(first.Id.Value, second.Id.Value);
            Assert.Equal(first.Name.Value, second.Name.Value);
        }

        [Fact]
        public void Equals_DifferentStepId_ShouldNotBeEqual()
        {
            var first = ValidStep();
            var second = StepDefinition.Create(
                StepId.From("reject-submission"),
                first.Name,
                first.HandlerKey);

            Assert.NotEqual(first.Id.Value, second.Id.Value);
            Assert.NotEqual(first, second);
            Assert.False(first == second);
            Assert.True(first != second);

            // Only the id changed.
            Assert.Equal(first.Name.Value, second.Name.Value);
            Assert.Equal(first.HandlerKey.Value, second.HandlerKey.Value);
        }

        [Fact]
        public void Equals_Null_ShouldBeFalse()
        {
            var step = ValidStep();

            // A bare `null` binds to Equals(StepDefinition?); the cast reaches Equals(object?).
            Assert.False(step.Equals((StepDefinition?)null));
            Assert.False(step.Equals((object?)null));
        }

        [Fact]
        public void Equals_AsObject_ShouldMatchTypedEquals()
        {
            var step = ValidStep();

            Assert.True(step.Equals((object)ValidStep()));
            Assert.False(step.Equals((object)step.WithHandlerKey(HandlerKey.From("human.submission-approval"))));
        }

        [Fact]
        public void Operators_WithNull_ShouldNotThrowAndShouldBeUnequal()
        {
            var step = ValidStep();
            StepDefinition? none = null;

            Assert.False(step == none);
            Assert.True(step != none);
            Assert.False(none == step);
            Assert.True(none != step);
            Assert.Null(none);
        }

        [Fact]
        public void HashSet_WithRenamedStep_ShouldCollapseToOneElementAndKeepTheFirst()
        {
            var original = ValidStep();
            var renamed = original.WithDisplayName(new DisplayName(OtherName));

            var set = new HashSet<StepDefinition> { original };

            Assert.False(set.Add(renamed));
            Assert.Single(set);
            Assert.Contains(renamed, set);

            // Which one survives: the first added. Pins what a keyed collection would do on a rename.
            Assert.Equal(ValidName().Value, set.Single().Name.Value);
        }

        [Fact]
        public void HashSet_WithDifferentIdOrHandlerKey_ShouldKeepStepsApart()
        {
            var original = ValidStep();
            
            var otherId = StepDefinition.Create(
                StepId.From("reject-submission"), 
                original.Name, 
                original.HandlerKey);
            
            var otherKey = original.WithHandlerKey(HandlerKey.From("human.submission-approval"));

            var set = new HashSet<StepDefinition> { original };

            Assert.True(set.Add(otherId));
            Assert.True(set.Add(otherKey));
            Assert.Equal(3, set.Count);
        }
    }
}
