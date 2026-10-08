using FlowForge.Domain.Common;

namespace FlowForge.Domain.Workflows;

public sealed class WorkflowVersion
{
    public int VersionNumber { get; }
    public VersionStatus Status { get; }
}