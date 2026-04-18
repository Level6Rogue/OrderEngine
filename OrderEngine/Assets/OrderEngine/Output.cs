namespace Ordering
{
    /// <summary>
    /// Base type for output items returned by Build().
    /// Can be an element, group start marker, or group end marker.
    /// </summary>
    public abstract record Output(string Name);

    /// <summary>
    /// Represents a regular element in the output.
    /// </summary>
    public record OutputElement(string Name) : Output(Name);

    /// <summary>
    /// Marks the start of a group in the output hierarchy.
    /// </summary>
    public record GroupStart(string Name, GroupType GroupType) : Output(Name);

    /// <summary>
    /// Marks the end of a group in the output hierarchy.
    /// </summary>
    public record GroupEnd(string Name) : Output(Name);

    public enum GroupType
    {
        Default,
        Foldout,
        Horizontal
    }
}

