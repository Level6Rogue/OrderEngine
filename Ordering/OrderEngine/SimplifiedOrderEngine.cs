namespace Ordering;
/// <summary>
/// OrderEngine optimized for a single Build() call.
/// 
/// Use this when you add elements and call Build() once.
/// 
/// Memory: Lower (no cached dictionaries)
/// Speed: Faster Add/AddGroup (no cache invalidation), single Build() call
/// Multiple Build() calls: Works but recomputes lookups each time
/// </summary>
public class SimplifiedOrderEngine : OrderEngineBase
{
    protected override Dictionary<string, ItemEntry> GetOrCreateTargetLookup()
    {
        return CreateTargetLookup();
    }
    protected override Dictionary<string, List<GroupEntry>> GetOrCreateChildGroupsByParent()
    {
        return CreateChildGroupsByParent();
    }
    protected override Dictionary<string, List<ElementEntry>> GetOrCreateChildElementsByParent()
    {
        return CreateChildElementsByParent();
    }
}
