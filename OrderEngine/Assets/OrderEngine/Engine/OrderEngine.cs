#nullable enable
using System;
using System.Collections.Generic;

namespace Ordering
{
    /// <summary>
    /// Specifies the optimization strategy for the OrderEngine.
    /// 
    /// MultipleBuild: Optimizes for multiple Build() calls without modifications.
    /// - Caches expensive lookups (target resolution, group hierarchies)
    /// - Faster subsequent Build() calls
    /// - Cache invalidation cost on Add/AddGroup
    /// - Best when: You call Build() multiple times after adding elements
    /// 
    /// SingleBuild: Optimizes for a single Build() call after adding all elements.
    /// - No caching overhead - caches are not maintained
    /// - Minimal memory usage
    /// - No invalidation cost on Add/AddGroup
    /// - Best when: You add elements once and call Build() once
    /// </summary>
    public enum BuildMode
    {
        MultipleBuild,  // Optimize for multiple Build() calls (with caching)
        SingleBuild     // Optimize for one Build() call (no caching)
    }

    /// <summary>
    /// Common interface for order engines with different optimization strategies.
    /// Use OrderEngine.Create(BuildMode) to instantiate the appropriate implementation.
    /// </summary>
    public interface IOrderEngine
    {
        void Add(Element element, OrderRule? orderRule = null);
        void AddGroup(Group group, OrderRule? orderRule = null);
        List<Output> Build();
    }

    /// <summary>
    /// Factory for creating optimized OrderEngine instances.
    /// 
    /// Usage:
    /// - IOrderEngine engine = OrderEngine.Create(BuildMode.MultipleBuild);  // For multiple Build() calls
    /// - IOrderEngine engine = OrderEngine.Create(BuildMode.SingleBuild);    // For single Build() call
    /// 
    /// Both implementations share the same interface, so tests and client code are identical.
    /// </summary>
    public static class OrderEngine
    {
        /// <summary>
        /// Creates an OrderEngine optimized for the specified BuildMode.
        /// 
        /// Parameters:
        /// - mode: BuildMode.MultipleBuild for caching optimization, BuildMode.SingleBuild for minimal overhead
        /// 
        /// Returns:
        /// - IOrderEngine interface that works identically regardless of mode
        /// </summary>
        public static IOrderEngine Create(BuildMode mode = BuildMode.MultipleBuild) =>
            mode switch
            {
                BuildMode.MultipleBuild => new CachedOrderEngine(),
                BuildMode.SingleBuild => new SimplifiedOrderEngine(),
                _ => throw new ArgumentException($"Unknown BuildMode: {mode}", nameof(mode))
            };
    }
}


