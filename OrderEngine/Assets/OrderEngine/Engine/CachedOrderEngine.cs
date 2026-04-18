#nullable enable
using System.Collections.Generic;

namespace Ordering
{
    /// <summary>
    /// OrderEngine optimized for multiple Build() calls with caching.
    /// 
    /// Use this when you call Build() multiple times without modifications between calls.
    /// 
    /// Memory: Higher (maintains 3 cached dictionaries)
    /// Speed: Faster subsequent Build() calls
    /// Add/AddGroup: Includes cache invalidation overhead
    /// </summary>
    public class CachedOrderEngine : OrderEngineBase
    {
        // Cache for target lookups
        private Dictionary<string, ItemEntry>? _cachedTargetLookup;
        // Cache for child lookups
        private Dictionary<string, List<GroupEntry>>? _cachedChildGroupsByParent;
        private Dictionary<string, List<ElementEntry>>? _cachedChildElementsByParent;

        protected override void InvalidateCaches()
        {
            _cachedTargetLookup = null;
            _cachedChildGroupsByParent = null;
            _cachedChildElementsByParent = null;
        }

        protected override Dictionary<string, ItemEntry> GetOrCreateTargetLookup() => _cachedTargetLookup ??= CreateTargetLookup();
        protected override Dictionary<string, List<GroupEntry>> GetOrCreateChildGroupsByParent() => _cachedChildGroupsByParent ??= CreateChildGroupsByParent();
        protected override Dictionary<string, List<ElementEntry>> GetOrCreateChildElementsByParent() => _cachedChildElementsByParent ??= CreateChildElementsByParent();
    }
}


