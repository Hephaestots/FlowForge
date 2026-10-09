using FlowForge.Domain.Common;

namespace FlowForge.Domain.Workflows;

public sealed record WorkflowDefinitionId
{
    public Guid Value { get; }
    
    private WorkflowDefinitionId(Guid value) => Value = value;

    public static WorkflowDefinitionId New() => new(Guid.CreateVersion7());
    public static WorkflowDefinitionId From(Guid value) => new(DomainGuard.AgainstEmpty(value, Errors.Empty));

    public static class Errors
    {
        public static readonly DomainError Empty =
            new("workflow_definition_id.empty", "Workflow definition id must not be empty.");
    }
}