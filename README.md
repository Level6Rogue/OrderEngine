# OrderEngine

A flexible .NET library for ordering and organizing elements with support for relative positioning, groups, and hierarchical structures.

## Features

- **Relative Ordering** - Position elements before or after other elements
- **Index-Based Ordering** - Assign explicit priority indices to elements
- **Grouping** - Organize elements into hierarchical groups with different display types
- **Cycle Detection** - Automatically detects and reports circular dependencies
- **Multiple Build Modes** - Optimized for either single or multiple build scenarios

## Installation

Add a reference to the `Ordering` project in your solution.

## Quick Start

```csharp
using Ordering;

// Create an order engine
IOrderEngine engine = OrderEngine.Create();

// Add elements
engine.Add("Item1");
engine.Add("Item2");
engine.Add("Item3");

// Build and get the ordered output
List<Output> result = engine.Build();
```

## Ordering Rules

### No Rule (Insertion Order)

Elements without ordering rules maintain their insertion order:

```csharp
engine.Add("First");
engine.Add("Second");
engine.Add("Third");
// Result: First, Second, Third
```

### Before

Position an element before another element:

```csharp
engine.Add("A");
engine.Add("B");
engine.Add("C", new Before("B"));
// Result: A, C, B
```

### After

Position an element after another element:

```csharp
engine.Add("A");
engine.Add("B");
engine.Add("C", new After("A"));
// Result: A, C, B
```

### ByIndex

Assign explicit priority indices for fine-grained control:

```csharp
engine.Add("Item1", new ByIndex(10));
engine.Add("Item2", new ByIndex(5));
engine.Add("Item3", new ByIndex(15));
// Result: Item2, Item1, Item3 (sorted by index)
```

Negative indices are supported and sort before positive indices:

```csharp
engine.Add("A");
engine.Add("B", new ByIndex(-100));
// Result: B, A
```

## Groups

### Implicit Groups (Path-Based)

Create groups using path separators (`/`):

```csharp
engine.Add("Settings/Audio/Volume");
engine.Add("Settings/Audio/Mute");
engine.Add("Settings/Video/Resolution");
```

Output structure:
```
GroupStart: Settings
  GroupStart: Audio
    Volume
    Mute
  GroupEnd: Audio
  GroupStart: Video
    Resolution
  GroupEnd: Video
GroupEnd: Settings
```

### Explicit Groups

Create empty groups or specify group types:

```csharp
engine.AddGroup(new Group("MyGroup"));
engine.AddGroup(new Group("FoldableGroup", GroupType.Foldout));
engine.AddGroup(new Group("HorizontalGroup", GroupType.Horizontal));
```

### Group Types

| Type | Description |
|------|-------------|
| `GroupType.Default` | Standard group container |
| `GroupType.Foldout` | Collapsible group |
| `GroupType.Horizontal` | Horizontal layout group |

## Build Modes

Choose the optimization strategy based on your usage pattern:

```csharp
// For multiple Build() calls (with caching)
IOrderEngine engine = OrderEngineFactory.Create(BuildMode.MultipleBuild);

// For a single Build() call (minimal overhead)
IOrderEngine engine = OrderEngineFactory.Create(BuildMode.SingleBuild);
```

| Mode | Best For | Characteristics |
|------|----------|-----------------|
| `MultipleBuild` | Calling `Build()` multiple times after adding elements | Caches lookups, faster subsequent builds |
| `SingleBuild` | Adding elements once, calling `Build()` once | No caching overhead, minimal memory |

## Output Types

The `Build()` method returns a list of `Output` objects:

| Type | Description |
|------|-------------|
| `OutputElement` | A regular element |
| `GroupStart` | Marks the beginning of a group |
| `GroupEnd` | Marks the end of a group |

```csharp
foreach (var output in engine.Build())
{
    switch (output)
    {
        case GroupStart gs:
            Console.WriteLine($"Group Start: {gs.Name} ({gs.GroupType})");
            break;
        case GroupEnd ge:
            Console.WriteLine($"Group End: {ge.Name}");
            break;
        case OutputElement el:
            Console.WriteLine($"Element: {el.Name}");
            break;
    }
}
```

## Updating Elements

Adding an element with the same name updates its ordering rule:

```csharp
engine.Add("A");
engine.Add("B", new Before("A"));  // B is before A
engine.Add("B", new After("A"));   // B is now after A
// Result: A, B
```

## Error Handling

### Circular Dependencies

The engine detects and throws an exception for circular dependencies:

```csharp
engine.Add("A", new Before("B"));
engine.Add("B", new Before("A"));
engine.Build(); // Throws exception: circular dependency detected
```

### Invalid References

Referencing a non-existent element throws an exception:

```csharp
engine.Add("A", new Before("NonExistent"));
engine.Build(); // Throws exception: target not found
```

## API Reference

### IOrderEngine Interface

```csharp
public interface IOrderEngine
{
    void Add(Element element, OrderRule? orderRule = null);
    void AddGroup(Group group, OrderRule? orderRule = null);
    List<Output> Build();
}
```

### Element

```csharp
// Create explicitly
var element = new Element("MyElement");

// Or use implicit conversion from string
engine.Add("MyElement");
```

### OrderRule Types

```csharp
public abstract record OrderRule;
public record ByIndex(int Index) : OrderRule;
public record Before(string ElementName) : OrderRule;
public record After(string ElementName) : OrderRule;
```

## License

See LICENSE file for details.

