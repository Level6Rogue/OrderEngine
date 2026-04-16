using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;

// Bypass the problematic SDK validator that causes NullReferenceException on .NET 10
var config = DefaultConfig.Instance
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
