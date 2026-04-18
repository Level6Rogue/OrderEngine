# OrderEngine

A flexible .NET library for ordering and organizing elements with support for relative positioning, groups, and hierarchical structures.

[![NuGet](https://img.shields.io/nuget/v/Level6Rogue.OrderEngine)](https://www.nuget.org/packages/Level6Rogue.OrderEngine/)

## Installation

```bash
dotnet add package Level6Rogue.OrderEngine
```

## Quick Example

```csharp
using OrderEngine;

IOrderEngine engine = OrderEngine.Create();

engine.Add("A");
engine.Add("B");
engine.Add("C", new Before("B"));

List<Output> result = engine.Build();
// Result: A, C, B
```

## Documentation

See the [OrderEngine README](OrderEngine/README.md) for full documentation.

## Projects

| Project | Description |
|---------|-------------|
| **OrderEngine** | Core library |
| **OrderEngine.Tests** | Unit tests |
| **OrderEngine.Benchmarks** | Performance benchmarks |

## License

MIT License

