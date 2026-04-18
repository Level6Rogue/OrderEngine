using System.Runtime.CompilerServices;

namespace L6R.OrderEngine.Tests;

public static class OrderEngineDiagnosticsExtensions
{
    private sealed class TraceState
    {
        public List<string> Inputs { get; } = new();
    }

    private static readonly ConditionalWeakTable<IOrderEngine, TraceState> TraceByEngine = new();

    public static IOrderEngine AddLogged(this IOrderEngine engine, string elementName, OrderRule? orderRule = null)
    {
        if (orderRule is null)
        {
            engine.Add(elementName);
            GetState(engine).Inputs.Add($"Add(\"{elementName}\")");
        }
        else
        {
            engine.Add(elementName, orderRule);
            GetState(engine).Inputs.Add($"Add(\"{elementName}\", {FormatRule(orderRule)})");
        }

        return engine;
    }

    public static IOrderEngine AddGroupLogged(this IOrderEngine engine, Group group, OrderRule? orderRule = null)
    {
        if (orderRule is null)
        {
            engine.AddGroup(group);
            GetState(engine).Inputs.Add($"AddGroup(\"{group.Name}\", {group.GroupType})");
        }
        else
        {
            engine.AddGroup(group, orderRule);
            GetState(engine).Inputs.Add($"AddGroup(\"{group.Name}\", {group.GroupType}, {FormatRule(orderRule)})");
        }

        return engine;
    }

    public static List<Output> BuildLogged(this IOrderEngine engine)
    {
        string testName = TestContext.CurrentContext.Test.Name;
        TraceState state = GetState(engine);

        TestContext.Progress.WriteLine($"=== {testName} ===");
        TestContext.Progress.WriteLine("Inputs:");

        if (state.Inputs.Count == 0)
        {
            TestContext.Progress.WriteLine("  (none)");
        }
        else
        {
            for (int i = 0; i < state.Inputs.Count; i++)
            {
                TestContext.Progress.WriteLine($"  {i + 1:00}. {state.Inputs[i]}");
            }
        }

        try
        {
            List<Output> output = engine.Build();

            TestContext.Progress.WriteLine("Output:");
            if (output.Count == 0)
            {
                TestContext.Progress.WriteLine("  (empty)");
            }
            else
            {
                for (int i = 0; i < output.Count; i++)
                {
                    TestContext.Progress.WriteLine($"  {i + 1:00}. {FormatElement(output[i])}");
                }

                TestContext.Progress.WriteLine("Tree:");
                foreach (string line in BuildTreeLines(output))
                {
                    TestContext.Progress.WriteLine($"  {line}");
                }
            }

            return output;
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"Output: threw {ex.GetType().Name}: {ex.Message}");
            throw;
        }
        finally
        {
            TestContext.Progress.WriteLine(string.Empty);
        }
    }

    private static TraceState GetState(IOrderEngine engine)
    {
        return TraceByEngine.GetValue(engine, _ => new TraceState());
    }

    private static string FormatRule(OrderRule orderRule)
    {
        return orderRule switch
        {
            Before before => $"Before(\"{before.ElementName}\")",
            After after => $"After(\"{after.ElementName}\")",
            ByIndex byIndex => $"ByIndex({byIndex.Index})",
            _ => orderRule.GetType().Name
        };
    }

    private static string FormatElement(Output output)
    {
        return output switch
        {
            GroupStart groupStart => $"GroupStart(Name=\"{groupStart.Name}\", Type={groupStart.GroupType})",
            GroupEnd groupEnd => $"GroupEnd(Name=\"{groupEnd.Name}\")",
            OutputElement element => $"Element(Name=\"{element.Name}\")",
            _ => $"Output(Name=\"{output.Name}\")"
        };
    }

    private static List<string> BuildTreeLines(List<Output> output)
    {
        List<TreeNode> root = new();
        Stack<TreeNode> openGroups = new();

        foreach (Output item in output)
        {
            List<TreeNode> currentContainer = openGroups.Count == 0
                ? root
                : openGroups.Peek().Children;

            switch (item)
            {
                case GroupStart groupStart:
                {
                    TreeNode groupNode = new($"{groupStart.Name}/ ({groupStart.GroupType})");
                    currentContainer.Add(groupNode);
                    openGroups.Push(groupNode);
                    break;
                }
                case GroupEnd:
                {
                    if (openGroups.Count > 0)
                    {
                        openGroups.Pop();
                    }

                    break;
                }
                default:
                    currentContainer.Add(new TreeNode(item.Name));
                    break;
            }
        }

        List<string> lines = new();
        AppendTreeLines(root, string.Empty, lines);

        if (lines.Count == 0)
        {
            lines.Add("(empty)");
        }

        return lines;
    }

    private static void AppendTreeLines(List<TreeNode> nodes, string prefix, List<string> lines)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            TreeNode node = nodes[i];
            bool isLast = i == nodes.Count - 1;
            string connector = isLast ? "\\-" : "+-";
            lines.Add($"{prefix}{connector} {node.Label}");

            if (node.Children.Count > 0)
            {
                string childPrefix = prefix + (isLast ? "   " : "|  ");
                AppendTreeLines(node.Children, childPrefix, lines);
            }
        }
    }

    private sealed class TreeNode
    {
        public TreeNode(string label)
        {
            Label = label;
        }

        public string Label { get; }
        public List<TreeNode> Children { get; } = new();
    }
}


