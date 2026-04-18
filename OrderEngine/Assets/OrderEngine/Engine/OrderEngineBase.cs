#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ordering
{
    /// <summary>
    /// Base class containing all shared logic for order engines.
    /// Subclasses differ only in their caching strategies.
    /// </summary>
    public abstract class OrderEngineBase : IOrderEngine
    {
        protected readonly Dictionary<string, ElementEntry> ElementsByPath = new();
        protected readonly Dictionary<string, GroupEntry> GroupsByPath = new();
        protected readonly List<NodeEntry> Nodes = new();
        private int _nextNodeId;

        public void Add(Element element, OrderRule? orderRule = null)
        {
            if (element is null) throw new ArgumentNullException(nameof(element));

            InvalidateCaches();

            ParseElementPath(element.Name, out string fullPath, out string leafName, out string? parentGroupPath);

            if (parentGroupPath is not null) 
                EnsureGroupPath(parentGroupPath);

            if (ElementsByPath.TryGetValue(fullPath, out ElementEntry? existing))
            {
                if (orderRule is not null) 
                    existing.OrderRule = orderRule;
                return;
            }

            NodeEntry node = CreateNode(NodeKind.Element, leafName);
            ElementsByPath[fullPath] = new ElementEntry(fullPath, leafName, parentGroupPath, node.Id)
            {
                OrderRule = orderRule
            };
        }

        public void AddGroup(Group group, OrderRule? orderRule = null)
        {
            if (group is null) throw new ArgumentNullException(nameof(group));

            InvalidateCaches();

            string fullPath = NormalizePath(group.Name, "Group name");
            GroupEntry entry = EnsureGroupPath(fullPath);
            entry.GroupType = group.GroupType;
            if (orderRule is not null) 
                entry.OrderRule = orderRule;
        }

        /// <summary>
        /// Hook for subclasses to invalidate their caches.
        /// Override to clear any caching structures.
        /// </summary>
        protected virtual void InvalidateCaches() { }

        /// <summary>
        /// Hook for subclasses to provide cached or recomputed target lookup.
        /// </summary>
        protected abstract Dictionary<string, ItemEntry> GetOrCreateTargetLookup();

        /// <summary>
        /// Hook for subclasses to provide cached or recomputed child groups lookup.
        /// </summary>
        protected abstract Dictionary<string, List<GroupEntry>> GetOrCreateChildGroupsByParent();

        /// <summary>
        /// Hook for subclasses to provide cached or recomputed child elements lookup.
        /// </summary>
        protected abstract Dictionary<string, List<ElementEntry>> GetOrCreateChildElementsByParent();

        /// <summary>
        /// Builds the ordered list of elements by performing a topological sort on the dependency graph.
        /// This is the main orchestration method that:
        /// 1. Creates a directed acyclic graph (DAG) from elements, groups, and ordering rules
        /// 2. Detects cycles in dependencies and throws if found
        /// 3. Returns elements in dependency-respecting order using Kahn's algorithm
        /// 
        /// The method uses caching to avoid recomputing expensive lookups when Build() is called
        /// multiple times without modifications. All caches are invalidated when Add/AddGroup are called.
        /// </summary>
        /// <returns>
        /// A list of Output items in topological order that respects all ordering rules.
        /// Returns empty list if no elements have been added.
        /// </returns>
        public List<Output> Build()
        {
            if (Nodes.Count == 0)
                return new List<Output>();

            // Create adjacency list for the DAG: edges[nodeId] = set of nodes that depend on nodeId
            HashSet<int>[] edges = new HashSet<int>[_nextNodeId];
            // Track in-degree (number of dependencies) for each node - used for topological sort
            int[] indegree = new int[_nextNodeId];
            // Lazy-initialized dictionary to store ByIndex ordering overrides (only if ByIndex rules exist)
            Dictionary<int, int>? byIndexOverride = null;
        
            // Use hook method to get lookups (cached or recomputed based on subclass)
            Dictionary<string, ItemEntry> targetLookup = GetOrCreateTargetLookup();

            // Initialize HashSets for edge tracking
            foreach (NodeEntry node in Nodes) 
                edges[node.Id] = new HashSet<int>();

            void AddEdge(int from, int to)
            {
                if (from == to)
                    throw new Exception("Ordering rule creates a self-dependency.");

                if (edges[from].Add(to))
                    indegree[to]++;
            }

            // Use hook methods to get lookups (cached or recomputed based on subclass)
            Dictionary<string, List<GroupEntry>> childGroupsByParent = GetOrCreateChildGroupsByParent();
            Dictionary<string, List<ElementEntry>> childElementsByParent = GetOrCreateChildElementsByParent();

            foreach (GroupEntry group in GroupsByPath.Values)
            {
                bool hasAnyChild = false;

                if (childGroupsByParent.TryGetValue(group.Path, out List<GroupEntry>? childGroups))
                {
                    foreach (GroupEntry childGroup in childGroups)
                    {
                        AddEdge(group.StartNodeId, childGroup.StartNodeId);
                        AddEdge(childGroup.EndNodeId, group.EndNodeId);
                        hasAnyChild = true;
                    }
                }

                if (childElementsByParent.TryGetValue(group.Path, out List<ElementEntry>? childElements))
                {
                    foreach (ElementEntry childElement in childElements)
                    {
                        AddEdge(group.StartNodeId, childElement.NodeId);
                        AddEdge(childElement.NodeId, group.EndNodeId);
                        hasAnyChild = true;
                    }
                }

                if (!hasAnyChild) 
                    AddEdge(group.StartNodeId, group.EndNodeId);
            }

            foreach (ElementEntry element in ElementsByPath.Values) 
                ApplyOrderRule(element, element.OrderRule, targetLookup, AddEdge, ref byIndexOverride);

            foreach (GroupEntry group in GroupsByPath.Values) 
                ApplyOrderRule(group, group.OrderRule, targetLookup, AddEdge, ref byIndexOverride);

            // KAHN'S ALGORITHM: Topological sort using in-degree method
            // Step 1: Find all nodes with no dependencies (in-degree == 0) - these are safe to process first
            int estimatedInitialNodes = Math.Max(Nodes.Count / 4, 1);
            PriorityQueue<int, (int Key, bool NotByIndex, int Insertion)> available = new(estimatedInitialNodes);
            foreach (NodeEntry node in Nodes)
            {
                if (indegree[node.Id] == 0) 
                    available.Enqueue(node.Id, GetNodePriority(node.Id, byIndexOverride));
            }

            // Step 2: Process nodes in order, removing edges as we go
            List<int> ordered = new(Nodes.Count);

            while (available.TryDequeue(out int current, out _))
            {
                ordered.Add(current);

                foreach (int next in edges[current])
                {
                    indegree[next]--;
                    if (indegree[next] == 0) 
                        available.Enqueue(next, GetNodePriority(next, byIndexOverride));
                }
            }

            // Step 3: Check for cycles
            if (ordered.Count != Nodes.Count)
                throw new Exception("Ordering graph contains a cycle.");

            // Pre-size dictionary for group types
            Dictionary<int, GroupType> groupTypeByStartNode = new(GroupsByPath.Count);
            foreach (GroupEntry group in GroupsByPath.Values) 
                groupTypeByStartNode[group.StartNodeId] = group.GroupType;

            List<Output> result = new(ordered.Count);
            foreach (int nodeId in ordered)
            {
                NodeEntry node = Nodes[nodeId];
                result.Add(node.Kind switch
                {
                    NodeKind.Element => new OutputElement(node.Name),
                    NodeKind.GroupStart => new GroupStart(node.Name, groupTypeByStartNode.GetValueOrDefault(node.Id, GroupType.Default)),
                    NodeKind.GroupEnd => new GroupEnd(node.Name),
                    _ => throw new InvalidOperationException("Unknown node type.")
                });
            }

            return result;
        
        
            (int Key, bool NotByIndex, int Insertion) GetNodePriority(int nodeId, Dictionary<int, int>? byIndexOverride)
            {
                int insertion = Nodes[nodeId].InsertionOrder;
                if (byIndexOverride?.TryGetValue(nodeId, out int byIndex) ?? false)
                    return (byIndex, false, insertion);
                else
                    return (insertion, true, insertion);
            }
        }

        /// <summary>
        /// Converts an ordering rule (Before, After, or ByIndex) into edges in the dependency graph.
        /// </summary>
        private void ApplyOrderRule(
            ItemEntry item,
            OrderRule? orderRule,
            Dictionary<string, ItemEntry> targetLookup,
            Action<int, int> addEdge,
            ref Dictionary<int, int>? byIndexOverride)
        {
            if (orderRule is null)
                return;

            switch (orderRule)
            {
                case Before before:
                {
                    ItemEntry target = ResolveTarget(before.ElementName, targetLookup);
                    int from = GetBeforeAnchor(item, target);
                    int to = GetTargetForBefore(target);
                    addEdge(from, to);
                    byIndexOverride ??= new Dictionary<int, int>();
                    byIndexOverride.TryAdd(from, int.MinValue / 2);
                    break;
                }
                case After after:
                {
                    ItemEntry target = ResolveTarget(after.ElementName, targetLookup);
                    int from = GetTargetForAfter(target);
                    int to = GetAfterAnchor(item, target);
                    addEdge(from, to);
                    if (ShareContainerScope(item, target))
                    {
                        byIndexOverride ??= new Dictionary<int, int>();
                        byIndexOverride.TryAdd(to, Nodes[from].InsertionOrder);
                    }
                    break;
                }
                case ByIndex byIndex:
                {
                    byIndexOverride ??= new Dictionary<int, int>();
                    byIndexOverride[GetByIndexAnchor(item)] = byIndex.Index;
                    break;
                }
                default:
                    throw new Exception($"Unsupported order rule: {orderRule.GetType().Name}");
            }
        
            ItemEntry ResolveTarget(string name, Dictionary<string, ItemEntry> targetLookup)
            {
                if (targetLookup.TryGetValue(name, out ItemEntry? target))
                    return target;

                if (HasAmbiguousTarget(name))
                    throw new Exception($"Ambiguous ordering target '{name}'.");

                throw new Exception($"Unknown ordering target '{name}'.");
            }
        }

        protected Dictionary<string, ItemEntry> CreateTargetLookup()
        {
            int estimatedCapacity = (ElementsByPath.Count + GroupsByPath.Count) * 2;
            Dictionary<string, ItemEntry> unique = new(estimatedCapacity, StringComparer.Ordinal);
            HashSet<string> ambiguous = new(StringComparer.Ordinal);

            foreach (ElementEntry element in ElementsByPath.Values)
            {
                AddTargetCandidate(unique, ambiguous, element.Path, element);
                AddTargetCandidate(unique, ambiguous, element.Name, element);
            }

            foreach (GroupEntry group in GroupsByPath.Values)
            {
                AddTargetCandidate(unique, ambiguous, group.Path, group);
                AddTargetCandidate(unique, ambiguous, group.Name, group);
            }

            foreach (string name in ambiguous)
                unique.Remove(name);

            return unique;
        }

        private static void AddTargetCandidate(
            Dictionary<string, ItemEntry> unique,
            HashSet<string> ambiguous,
            string key,
            ItemEntry candidate)
        {
            if (ambiguous.Contains(key))
                return;

            if (unique.TryGetValue(key, out ItemEntry? existing))
            {
                if (!ReferenceEquals(existing, candidate))
                    ambiguous.Add(key);
                return;
            }

            unique[key] = candidate;
        }

        private bool HasAmbiguousTarget(string name)
        {
            int matches = 0;

            if (ElementsByPath.ContainsKey(name))
                matches++;

            if (GroupsByPath.ContainsKey(name))
                matches++;

            foreach (ElementEntry element in ElementsByPath.Values)
            {
                if (element.Name == name)
                    matches++;
            }

            foreach (GroupEntry group in GroupsByPath.Values)
            {
                if (group.Name == name)
                    matches++;
            }

            return matches > 1;
        }

        protected Dictionary<string, List<GroupEntry>> CreateChildGroupsByParent()
        {
            Dictionary<string, List<GroupEntry>> childrenByParent = new(StringComparer.Ordinal);

            foreach (GroupEntry group in GroupsByPath.Values)
            {
                if (group.ParentPath is null)
                    continue;

                if (!childrenByParent.TryGetValue(group.ParentPath, out List<GroupEntry>? children))
                {
                    children = new List<GroupEntry>(capacity: 1);
                    childrenByParent[group.ParentPath] = children;
                }

                children.Add(group);
            }

            return childrenByParent;
        }

        protected Dictionary<string, List<ElementEntry>> CreateChildElementsByParent()
        {
            Dictionary<string, List<ElementEntry>> childrenByParent = new(StringComparer.Ordinal);

            foreach (ElementEntry element in ElementsByPath.Values)
            {
                if (element.ParentGroupPath is null)
                    continue;

                if (!childrenByParent.TryGetValue(element.ParentGroupPath, out List<ElementEntry>? children))
                {
                    children = new List<ElementEntry>(capacity: 2);
                    childrenByParent[element.ParentGroupPath] = children;
                }

                children.Add(element);
            }

            return childrenByParent;
        }

        private int GetBeforeAnchor(ItemEntry subject, ItemEntry target)
        {
            if (subject is GroupEntry group)
                return group.EndNodeId;

            ElementEntry element = (ElementEntry)subject;
            if (element.ParentGroupPath is not null && !IsInsideGroup(target, element.ParentGroupPath))
                return GroupsByPath[element.ParentGroupPath].EndNodeId;

            return element.NodeId;
        }

        private int GetAfterAnchor(ItemEntry subject, ItemEntry target)
        {
            if (subject is GroupEntry group)
                return group.StartNodeId;

            ElementEntry element = (ElementEntry)subject;
            if (element.ParentGroupPath is not null && !IsInsideGroup(target, element.ParentGroupPath))
                return GroupsByPath[element.ParentGroupPath].StartNodeId;

            return element.NodeId;
        }

        private static int GetTargetForBefore(ItemEntry target) =>
            target switch
            {
                GroupEntry group => group.StartNodeId,
                ElementEntry element => element.NodeId,
                _ => throw new Exception("Unknown target type.")
            };

        private static int GetTargetForAfter(ItemEntry target) =>
            target switch
            {
                GroupEntry group => group.EndNodeId,
                ElementEntry element => element.NodeId,
                _ => throw new Exception("Unknown target type.")
            };

        private static int GetByIndexAnchor(ItemEntry item) =>
            item switch
            {
                GroupEntry group => group.StartNodeId,
                ElementEntry element => element.NodeId,
                _ => throw new Exception("Unknown item type.")
            };

        private static bool IsInsideGroup(ItemEntry target, string groupPath) =>
            target switch
            {
                ElementEntry element => IsPathInside(element.ParentGroupPath, groupPath),
                GroupEntry group => IsPathInside(group.Path, groupPath),
                _ => false
            };

        private static bool IsPathInside(string? candidatePath, string groupPath) =>
            candidatePath switch
            {
                null => false,
                _ => candidatePath == groupPath || candidatePath.StartsWith(groupPath + "/", StringComparison.Ordinal)
            };

        private static bool ShareContainerScope(ItemEntry first, ItemEntry second) => 
            GetContainerPath(first) == GetContainerPath(second);

        private static string? GetContainerPath(ItemEntry item) =>
            item switch
            {
                ElementEntry element => element.ParentGroupPath,
                GroupEntry group => group.ParentPath,
                _ => null
            };

        protected GroupEntry EnsureGroupPath(string? fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException("Group path cannot be null or empty.", nameof(fullPath));

            string normalizedFullPath = NormalizePath(fullPath, "Group path");

            GroupEntry? parent = null;
            string[] segments = normalizedFullPath.Split('/');
            string currentPath = string.Empty;

            for (int i = 0; i < segments.Length; i++)
            {
                currentPath = i == 0 ? segments[i] : currentPath + "/" + segments[i];

                if (GroupsByPath.TryGetValue(currentPath, out GroupEntry? existing))
                {
                    parent = existing;
                    continue;
                }

                NodeEntry start = CreateNode(NodeKind.GroupStart, segments[i]);
                NodeEntry end = CreateNode(NodeKind.GroupEnd, segments[i]);

                GroupEntry created = new(currentPath, segments[i], parent?.Path, start.Id, end.Id)
                {
                    GroupType = GroupType.Default
                };

                GroupsByPath[currentPath] = created;
                parent = created;
            }

            return GroupsByPath[normalizedFullPath];
        }

        private static void ParseElementPath(string rawName, out string fullPath, out string leafName, out string? parentGroupPath)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                throw new ArgumentException("Element name cannot be null or empty.", nameof(rawName));

            string[] segments = rawName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                throw new ArgumentException("Element name cannot be empty.", nameof(rawName));

            fullPath = string.Join("/", segments);
            leafName = segments[segments.Length - 1];
            parentGroupPath = segments.Length > 1 ? string.Join("/", segments.Take(segments.Length - 1)) : null;
        }

        private static string NormalizePath(string rawName, string label)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                throw new ArgumentException($"{label} cannot be null or empty.", nameof(rawName));

            string[] segments = rawName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                throw new ArgumentException($"{label} cannot be empty.", nameof(rawName));

            return string.Join("/", segments);
        }

        protected NodeEntry CreateNode(NodeKind kind, string name)
        {
            NodeEntry node = new(_nextNodeId, Nodes.Count, kind, name);
            Nodes.Add(node);
            _nextNodeId++;
            return node;
        }

        protected enum NodeKind
        {
            Element,
            GroupStart,
            GroupEnd
        }

        protected sealed record NodeEntry(int Id, int InsertionOrder, NodeKind Kind, string Name);

        protected abstract class ItemEntry
        {
            protected ItemEntry(string path, string name)
            {
                Path = path;
                Name = name;
            }

            public string Path { get; }
            public string Name { get; }
            public OrderRule? OrderRule { get; set; }
        }

        protected sealed class ElementEntry : ItemEntry
        {
            public ElementEntry(string path, string name, string? parentGroupPath, int nodeId) : base(path, name)
            {
                ParentGroupPath = parentGroupPath;
                NodeId = nodeId;
            }

            public string? ParentGroupPath { get; }
            public int NodeId { get; }
        }

        protected sealed class GroupEntry : ItemEntry
        {
            public GroupEntry(string path, string name, string? parentPath, int startNodeId, int endNodeId) : base(path, name)
            {
                ParentPath = parentPath;
                StartNodeId = startNodeId;
                EndNodeId = endNodeId;
            }

            public string? ParentPath { get; }
            public int StartNodeId { get; }
            public int EndNodeId { get; }
            public GroupType GroupType { get; set; }
        }
    }
}



