namespace FlowForge.Domain.Common;

public sealed record VersionStatus
{
    public static readonly VersionStatus Discarded = new("Discarded");
    public static readonly VersionStatus Draft = new("Draft");
    public static readonly VersionStatus Published = new("Published");
    public static readonly VersionStatus Retired = new("Retired");

    private static readonly VersionStatus[] All = [Discarded, Draft, Published, Retired];
    
    public string Name { get; }
    
    private VersionStatus(string name) => Name = name;

    public bool IsEditable => this == Draft;
    
    public bool CanStartInstance => this == Published;

    public bool CanTransitionTo(VersionStatus next) =>
        (this == Draft && (next == Discarded || next == Published)) ||
        (this == Published && next == Retired) ||
        (this == Retired && next == Published);
    
    public static VersionStatus FromName(string name) =>
        All.SingleOrDefault(vs => vs.Name == name) ??
        throw new DomainException($"Unknown version status '{name}'.");
    
    public override string ToString() => Name;
}