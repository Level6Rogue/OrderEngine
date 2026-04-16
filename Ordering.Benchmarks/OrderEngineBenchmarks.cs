using BenchmarkDotNet.Attributes;

namespace Ordering.Benchmarks;

[MemoryDiagnoser]
public class OrderEngineBenchmarks
{
    // CachedOrderEngine (MultipleBuild)
    private IOrderEngine _flatEngineCached = null!;
    private IOrderEngine _groupedEngineCached = null!;
    private IOrderEngine _flatEngineForAddCached = null!;
    private IOrderEngine _groupedEngineForBuildCached = null!;

    // SimplifiedOrderEngine (SingleBuild)
    private IOrderEngine _flatEngineSimplified = null!;
    private IOrderEngine _groupedEngineSimplified = null!;
    private IOrderEngine _flatEngineForAddSimplified = null!;
    private IOrderEngine _groupedEngineForBuildSimplified = null!;

    [Params(50, 200)]
    public int Size;

    [Params(50, 200, 1000, 5000)]
    public int LargeSize;

    [GlobalSetup]
    public void Setup()
    {
        // CachedOrderEngine
        _flatEngineCached = CreateFlatEngine(Size, BuildMode.MultipleBuild);
        _groupedEngineCached = CreateGroupedEngine(Size, BuildMode.MultipleBuild);
        _flatEngineForAddCached = OrderEngine.Create(BuildMode.MultipleBuild);
        _groupedEngineForBuildCached = CreateGroupedEngine(Size, BuildMode.MultipleBuild);

        // SimplifiedOrderEngine
        _flatEngineSimplified = CreateFlatEngine(Size, BuildMode.SingleBuild);
        _groupedEngineSimplified = CreateGroupedEngine(Size, BuildMode.SingleBuild);
        _flatEngineForAddSimplified = OrderEngine.Create(BuildMode.SingleBuild);
        _groupedEngineForBuildSimplified = CreateGroupedEngine(Size, BuildMode.SingleBuild);
    }

    [Benchmark]
    public void AddOnly()
    {
        for (int i = 0; i < Size; i++)
        {
            string name = $"Item{i}";
            if (i == 0)
            {
                _flatEngineForAddCached.Add(name);
            }
            else
            {
                _flatEngineForAddCached.Add(name, new After($"Item{i - 1}"));
            }
        }
    }

    // CachedOrderEngine Benchmarks
    [Benchmark]
    public int BuildOnlyCached()
    {
        return _groupedEngineForBuildCached.Build().Count;
    }

    [Benchmark]
    public int CachedBuild()
    {
        // Simulate repeated Build calls to measure cache effectiveness
        int total = 0;
        for (int i = 0; i < 3; i++)
        {
            total += _groupedEngineForBuildCached.Build().Count;
        }
        return total;
    }

    [Benchmark]
    public int BuildFlatLargeCached()
    {
        return CreateFlatEngine(LargeSize, BuildMode.MultipleBuild).Build().Count;
    }

    [Benchmark]
    public int BuildGroupedLargeCached()
    {
        return CreateGroupedEngine(LargeSize, BuildMode.MultipleBuild).Build().Count;
    }

    [Benchmark(Baseline = true)]
    public int BuildFlatCached()
    {
        return _flatEngineCached.Build().Count;
    }

    [Benchmark]
    public int BuildGroupedCached()
    {
        return _groupedEngineCached.Build().Count;
    }

    [Benchmark]
    public int AddAndBuildGroupedCached()
    {
        return CreateGroupedEngine(Size, BuildMode.MultipleBuild).Build().Count;
    }

    // SimplifiedOrderEngine Benchmarks (for comparison)
    [Benchmark]
    public int BuildOnlySimplified()
    {
        return _groupedEngineForBuildSimplified.Build().Count;
    }

    [Benchmark]
    public int SimplifiedMultipleBuild()
    {
        // Simulate repeated Build calls with no caching
        int total = 0;
        for (int i = 0; i < 3; i++)
        {
            total += _groupedEngineForBuildSimplified.Build().Count;
        }
        return total;
    }

    [Benchmark]
    public int BuildFlatLargeSimplified()
    {
        return CreateFlatEngine(LargeSize, BuildMode.SingleBuild).Build().Count;
    }

    [Benchmark]
    public int BuildGroupedLargeSimplified()
    {
        return CreateGroupedEngine(LargeSize, BuildMode.SingleBuild).Build().Count;
    }

    [Benchmark]
    public int BuildFlatSimplified()
    {
        return _flatEngineSimplified.Build().Count;
    }

    [Benchmark]
    public int BuildGroupedSimplified()
    {
        return _groupedEngineSimplified.Build().Count;
    }

    [Benchmark]
    public int AddAndBuildGroupedSimplified()
    {
        return CreateGroupedEngine(Size, BuildMode.SingleBuild).Build().Count;
    }

    private static IOrderEngine CreateFlatEngine(int size, BuildMode mode = BuildMode.MultipleBuild)
    {
        IOrderEngine engine = OrderEngine.Create(mode);

        for (int i = 0; i < size; i++)
        {
            string name = $"Item{i}";
            if (i == 0)
            {
                engine.Add(name);
            }
            else
            {
                engine.Add(name, new After($"Item{i - 1}"));
            }
        }

        return engine;
    }

    private static IOrderEngine CreateGroupedEngine(int size, BuildMode mode = BuildMode.MultipleBuild)
    {
        IOrderEngine engine = OrderEngine.Create(mode);

        int groups = Math.Max(1, size / 10);
        int[] lastItemByGroup = new int[groups];
        Array.Fill(lastItemByGroup, -1);

        for (int groupIndex = 0; groupIndex < groups; groupIndex++)
        {
            engine.AddGroup(new Group($"Section{groupIndex}", GroupType.Foldout));
        }

        for (int i = 0; i < size; i++)
        {
            int groupIndex = i % groups;
            string currentPath = $"Section{groupIndex}/Item{i}";
            int previousIndex = lastItemByGroup[groupIndex];

            if (previousIndex < 0)
            {
                engine.Add(currentPath);
                lastItemByGroup[groupIndex] = i;
                continue;
            }

            string previousName = $"Item{previousIndex}";
            string previousPath = $"Section{groupIndex}/{previousName}";

            // Mix leaf and full-path references to exercise target resolution paths.
            engine.Add(currentPath, i % 2 == 0 ? new After(previousName) : new After(previousPath));
            lastItemByGroup[groupIndex] = i;
        }

        return engine;
    }
}
