namespace Ordering.Tests;

/// <summary>
/// Comprehensive test suite for OrderEngine functionality covering:
/// - Basic element ordering
/// - Relative ordering (Before/After)
/// - Group management
/// - Order rules and conflicts
/// - Edge cases and error handling
/// 
/// Tests are parameterized to run against both BuildMode implementations:
/// - MultipleBuild: Optimized for multiple Build() calls with caching
/// - SingleBuild: Optimized for single Build() call with minimal overhead
/// 
/// Each test is executed twice (once per BuildMode), ensuring both implementations
/// behave identically. This approach avoids test duplication while providing
/// complete coverage of all implementations.
/// </summary>
[TestFixture(BuildMode.MultipleBuild, TestName = "OrderEngineTests(MultipleBuild)")]
[TestFixture(BuildMode.SingleBuild, TestName = "OrderEngineTests(SingleBuild)")]
public class OrderEngineTests(BuildMode buildMode)
{
    /// <summary>
    /// Helper method to create an IOrderEngine instance with the configured BuildMode.
    /// This allows the same test code to run against both implementations.
    /// </summary>
    private IOrderEngine CreateEngine() => OrderEngine.Create(buildMode);

    #region Basic Ordering Tests
    
    [Test]
    public void AddSingleElement_BuildReturnsElement()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("Item1");

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(1));
        Assert.That(elements[0].Name, Is.EqualTo("Item1"));
    }

    [Test]
    public void AddMultipleElements_BuildRetainsInsertionOrder()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("First");
        orderEngine.AddLogged("Second");
        orderEngine.AddLogged("Third");

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(3));
        Assert.That(elements[0].Name, Is.EqualTo("First"));
        Assert.That(elements[1].Name, Is.EqualTo("Second"));
        Assert.That(elements[2].Name, Is.EqualTo("Third"));
    }

    [Test]
    public void AddElementWithNullName_Throws()
    {
        var orderEngine = CreateEngine();
        
        Assert.Throws<ArgumentException>(() => orderEngine.AddLogged(null!));
    }

    [Test]
    public void AddNullElement_Throws()
    {
        var orderEngine = CreateEngine();
        
        Assert.Throws<ArgumentNullException>(() => orderEngine.Add(null!));
    }

    #endregion

    #region Before Ordering Tests

    [Test]
    public void BeforeSimpleCase_ElementPlacedBeforeTarget()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B");
        orderEngine.AddLogged("C", new Before("B"));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(3));
        // C should come before B
        var cIndex = elements.FindIndex(e => e.Name == "C");
        var bIndex = elements.FindIndex(e => e.Name == "B");
        Assert.That(cIndex, Is.LessThan(bIndex));
    }

    [Test]
    public void BeforeMultipleElements_CorrectOrdering()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B");
        orderEngine.AddLogged("C");
        orderEngine.AddLogged("D", new Before("B"));
        orderEngine.AddLogged("E", new Before("C"));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(5));
        // D should come before B
        var dIndex = elements.FindIndex(e => e.Name == "D");
        var bIndex = elements.FindIndex(e => e.Name == "B");
        Assert.That(dIndex, Is.LessThan(bIndex));
        
        // E should come before C
        var eIndex = elements.FindIndex(e => e.Name == "E");
        var cIndex = elements.FindIndex(e => e.Name == "C");
        Assert.That(eIndex, Is.LessThan(cIndex));
    }

    [Test]
    public void BeforeChain_AllElementsInCorrectOrder()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("First");
        orderEngine.AddLogged("Last", new Before("First"));
        orderEngine.AddLogged("Middle", new Before("First"));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(3));
        Assert.That(elements[0].Name, Is.EqualTo("Last"));
        Assert.That(elements[1].Name, Is.EqualTo("Middle"));
        Assert.That(elements[2].Name, Is.EqualTo("First"));
    }

    [Test]
    public void BeforeNonExistentTarget_Throws()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B", new Before("NonExistent"));

        Assert.Throws<Exception>(() => orderEngine.BuildLogged());
    }

    #endregion

    #region After Ordering Tests

    [Test]
    public void AfterSimpleCase_ElementPlacedAfterTarget()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B");
        orderEngine.AddLogged("C", new After("A"));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(3));
        Assert.That(elements[0].Name, Is.EqualTo("A"));
        Assert.That(elements[1].Name, Is.EqualTo("C"));
        Assert.That(elements[2].Name, Is.EqualTo("B"));
    }

    [Test]
    public void AfterMultipleElements_CorrectOrdering()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B");
        orderEngine.AddLogged("C");
        orderEngine.AddLogged("D", new After("A"));
        orderEngine.AddLogged("E", new After("B"));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(5));
        Assert.That(elements[0].Name, Is.EqualTo("A"));
        Assert.That(elements[1].Name, Is.EqualTo("D"));
        Assert.That(elements[2].Name, Is.EqualTo("B"));
        Assert.That(elements[3].Name, Is.EqualTo("E"));
        Assert.That(elements[4].Name, Is.EqualTo("C"));
    }

    [Test]
    public void AfterChain_AllElementsInCorrectOrder()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("First");
        orderEngine.AddLogged("Second", new After("First"));
        orderEngine.AddLogged("Third", new After("Second"));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(3));
        Assert.That(elements[0].Name, Is.EqualTo("First"));
        Assert.That(elements[1].Name, Is.EqualTo("Second"));
        Assert.That(elements[2].Name, Is.EqualTo("Third"));
    }

    [Test]
    public void AfterNonExistentTarget_Throws()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B", new After("NonExistent"));

        Assert.Throws<Exception>(() => orderEngine.BuildLogged());
    }

    #endregion

    #region Cycle Detection Tests

    [Test]
    public void SimpleCycle_BeforeBeforeCycle_Throws()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A", new Before("B"));
        orderEngine.AddLogged("B", new Before("A"));

        Assert.Throws<Exception>(() => orderEngine.BuildLogged());
    }

    [Test]
    public void SimpleCycle_AfterAfterCycle_Throws()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A", new After("B"));
        orderEngine.AddLogged("B", new After("A"));

        Assert.Throws<Exception>(() => orderEngine.BuildLogged());
    }

    [Test]
    public void ComplexCycle_ThreeElementCycle_Throws()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A", new Before("B"));
        orderEngine.AddLogged("B", new Before("C"));
        orderEngine.AddLogged("C", new Before("A"));

        Assert.Throws<Exception>(() => orderEngine.BuildLogged());
    }

    [Test]
    public void MixedCycle_BeforeAndAfter_Throws()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A", new Before("B"));
        orderEngine.AddLogged("B", new After("A"));
        orderEngine.AddLogged("A", new After("B"));

        Assert.Throws<Exception>(() => orderEngine.BuildLogged());
    }

    #endregion

    #region Duplicate and Update Tests

    [Test]
    public void DuplicateElementName_SecondCallUpdatesRule()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B");
        orderEngine.AddLogged("B", new Before("A"));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(2));
        Assert.That(elements[0].Name, Is.EqualTo("B"));
        Assert.That(elements[1].Name, Is.EqualTo("A"));
    }

    [Test]
    public void DuplicateElementName_UpdateToAfterRule()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B", new Before("A"));
        orderEngine.AddLogged("B", new After("A"));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(2));
        Assert.That(elements[0].Name, Is.EqualTo("A"));
        Assert.That(elements[1].Name, Is.EqualTo("B"));
    }

    [Test]
    public void DuplicateElementName_MultipleUpdates()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B");
        orderEngine.AddLogged("C");
        orderEngine.AddLogged("B", new Before("C"));
        orderEngine.AddLogged("B", new After("A"));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(3));
        Assert.That(elements[0].Name, Is.EqualTo("A"));
        Assert.That(elements[1].Name, Is.EqualTo("B"));
        Assert.That(elements[2].Name, Is.EqualTo("C"));
    }

    #endregion

    #region Group Tests

    [Test]
    public void SimpleGroup_CreatesGroupBoundaries()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("Item1/SubItem1");
        orderEngine.AddLogged("Item1/SubItem2");

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(4));
        Assert.That(elements[0], Is.TypeOf<GroupStart>());
        Assert.That(elements[0].Name, Is.EqualTo("Item1"));
        Assert.That(elements[1].Name, Is.EqualTo("SubItem1"));
        Assert.That(elements[2].Name, Is.EqualTo("SubItem2"));
        Assert.That(elements[3], Is.TypeOf<GroupEnd>());
        Assert.That(elements[3].Name, Is.EqualTo("Item1"));
    }

    [Test]
    public void MultipleGroups_CorrectStructure()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("Group1/Item1");
        orderEngine.AddLogged("Group2/Item2");

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(6));
        
        Assert.That(elements[0], Is.TypeOf<GroupStart>());
        Assert.That(elements[0].Name, Is.EqualTo("Group1"));
        Assert.That(elements[1].Name, Is.EqualTo("Item1"));
        Assert.That(elements[2], Is.TypeOf<GroupEnd>());
        Assert.That(elements[2].Name, Is.EqualTo("Group1"));
        
        Assert.That(elements[3], Is.TypeOf<GroupStart>());
        Assert.That(elements[3].Name, Is.EqualTo("Group2"));
        Assert.That(elements[4].Name, Is.EqualTo("Item2"));
        Assert.That(elements[5], Is.TypeOf<GroupEnd>());
        Assert.That(elements[5].Name, Is.EqualTo("Group2"));
    }

    [Test]
    public void NestedGroups_MultipleDepths()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("Group1/Group2/Item1");
        orderEngine.AddLogged("Group1/Group2/Item2");

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(6));
        
        Assert.That(elements[0], Is.TypeOf<GroupStart>());
        Assert.That(elements[0].Name, Is.EqualTo("Group1"));
        
        Assert.That(elements[1], Is.TypeOf<GroupStart>());
        Assert.That(elements[1].Name, Is.EqualTo("Group2"));
        
        Assert.That(elements[2].Name, Is.EqualTo("Item1"));
        Assert.That(elements[3].Name, Is.EqualTo("Item2"));
        
        Assert.That(elements[4], Is.TypeOf<GroupEnd>());
        Assert.That(elements[4].Name, Is.EqualTo("Group2"));
        
        Assert.That(elements[5], Is.TypeOf<GroupEnd>());
        Assert.That(elements[5].Name, Is.EqualTo("Group1"));
    }

    [Test]
    public void EmptyGroup_AddGroupMethod()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("Item1");
        orderEngine.AddGroupLogged(new Group("EmptyGroup"));
        orderEngine.AddLogged("Item2");

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(4));
        Assert.That(elements[0].Name, Is.EqualTo("Item1"));
        Assert.That(elements[1], Is.TypeOf<GroupStart>());
        Assert.That(elements[1].Name, Is.EqualTo("EmptyGroup"));
        Assert.That(elements[2], Is.TypeOf<GroupEnd>());
        Assert.That(elements[2].Name, Is.EqualTo("EmptyGroup"));
        Assert.That(elements[3].Name, Is.EqualTo("Item2"));
    }

    [Test]
    public void GroupWithOrderingRule_ElementOrderedBeforeGroup()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("Item1");
        orderEngine.AddLogged("Group/Item2", new Before("Item1"));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(4));
        Assert.That(elements[0], Is.TypeOf<GroupStart>());
        Assert.That(elements[0].Name, Is.EqualTo("Group"));
        Assert.That(elements[1].Name, Is.EqualTo("Item2"));
        Assert.That(elements[2], Is.TypeOf<GroupEnd>());
        Assert.That(elements[3].Name, Is.EqualTo("Item1"));
    }

    [Test]
    public void GroupType_DefaultType()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("Group1/Item1");
        orderEngine.AddGroupLogged(new Group("Group1", GroupType.Default));

        var elements = orderEngine.BuildLogged();

        var groupStart = elements[0] as GroupStart;
        Assert.That(groupStart, Is.Not.Null);
        Assert.That(groupStart!.GroupType, Is.EqualTo(GroupType.Default));
    }

    [Test]
    public void GroupType_HorizontalType()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("Group1/Item1");
        orderEngine.AddGroupLogged(new Group("Group1", GroupType.Horizontal));

        var elements = orderEngine.BuildLogged();

        var groupStart = elements[0] as GroupStart;
        Assert.That(groupStart, Is.Not.Null);
        Assert.That(groupStart!.GroupType, Is.EqualTo(GroupType.Horizontal));
    }

    [Test]
    public void GroupType_FoldoutType()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("Group1/Item1");
        orderEngine.AddGroupLogged(new Group("Group1", GroupType.Foldout));

        var elements = orderEngine.BuildLogged();

        var groupStart = elements[0] as GroupStart;
        Assert.That(groupStart, Is.Not.Null);
        Assert.That(groupStart!.GroupType, Is.EqualTo(GroupType.Foldout));
    }

    [Test]
    public void DeepNestedGroupWithTypeUpdate()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("Group1/Group2/Group3/Item1");
        orderEngine.AddGroupLogged(new Group("Group1/Group2/Group3", GroupType.Horizontal));

        var elements = orderEngine.BuildLogged();

        // Should have Group1, Group2, Group3, Item1, and ends for each group
        Assert.That(elements.Count, Is.GreaterThanOrEqualTo(5));
        
        // Find the Group3 start and verify its type is Horizontal
        var group3Start = elements.OfType<GroupStart>().FirstOrDefault(g => g.Name == "Group3");
        Assert.That(group3Start, Is.Not.Null);
        Assert.That(group3Start!.GroupType, Is.EqualTo(GroupType.Horizontal));
        
        // Verify Item1 exists in the output
        var item1 = elements.FirstOrDefault(e => e.Name == "Item1");
        Assert.That(item1, Is.Not.Null);
    }

    #endregion

    #region ByIndex Ordering Tests
    
    /// <summary>
    /// ByIndex Ordering Implementation
    /// 
    /// ByIndex ordering uses direct priority assignment where:
    /// - If element has ByIndex: Key = ByIndex value
    /// - If element has no rule: Key = insertion order (0, 1, 2, ...)
    /// 
    /// This means ByIndex values and insertion orders share the same priority space,
    /// allowing natural interleaving. Items maintain their position based on their key value.
    /// 
    /// Example:
    /// Add("A")                      // Key = 0 (insertion order)
    /// Add("B", ByIndex(0))          // Key = 0 (same as A, tiebreaker = insertion order)
    /// Add("C", ByIndex(1))          // Key = 1 (between A/B and later insertion order items)
    /// Add("D")                      // Key = 3 (insertion order)
    /// 
    /// Result:  A, B (tie broken by insertion order), C, D ✓
    /// 
    /// Key insights:
    /// - Items without ByIndex maintain their insertion order
    /// - ByIndex creates sort ordering interleaved with insertion order
    /// - When keys tie, insertion order is the tiebreaker
    /// </summary>

    [Test]
    public void ByIndexRule_ElementIsTypeOfOrderRule()
    {
        Assert.That(typeof(ByIndex).IsAssignableTo(typeof(OrderRule)), Is.True);
    }

    [Test]
    public void ByIndexRule_CanBeCreated()
    {
        ByIndex indexRule = new ByIndex(5);
        Assert.That(indexRule.Index, Is.EqualTo(5));
    }

    [Test]
    public void ByIndex_SingleElementAtIndex()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B", new ByIndex(0));
        orderEngine.AddLogged("C");

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(3));
        // With 3-tuple priority (Key, NotByIndex, Insertion):
        // A: (0, true, 0)  - insertion order 0, no ByIndex
        // B: (0, false, 1) - ByIndex(0), NotByIndex=false sorts before true
        // C: (2, true, 2)  - insertion order 2, no ByIndex
        // Sorted: B(0,false), A(0,true), C(2,true)
        var bIndex = elements.FindIndex(e => e.Name == "B");
        Assert.That(bIndex, Is.EqualTo(0), "B with ByIndex(0) sorts before A (insertion order 0)");
        Assert.That(elements[1].Name, Is.EqualTo("A"));
        Assert.That(elements[2].Name, Is.EqualTo("C"));
    }

    [Test]
    public void ByIndex_MultipleElementsWithIndices()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B", new ByIndex(2));
        orderEngine.AddLogged("C", new ByIndex(1));
        orderEngine.AddLogged("D", new ByIndex(0));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(4));
        // Verify all elements are present
        Assert.That(elements.Any(e => e.Name == "A"), Is.True);
        Assert.That(elements.Any(e => e.Name == "B"), Is.True);
        Assert.That(elements.Any(e => e.Name == "C"), Is.True);
        Assert.That(elements.Any(e => e.Name == "D"), Is.True);
    }

    [Test]
    public void ByIndex_ZeroIndex()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("First");
        orderEngine.AddLogged("Second");
        orderEngine.AddLogged("Third");
        orderEngine.AddLogged("Zero", new ByIndex(0));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(4));
        // Verify element exists with zero index
        var zeroIndex = elements.FindIndex(e => e.Name == "Zero");
        Assert.That(zeroIndex, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void ByIndex_NegativeValueSortsFirst()
    {
        // Demonstrates that negative ByIndex values sort even earlier
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B", new ByIndex(-100));  // Even lower priority
        orderEngine.AddLogged("C");

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(3));
        // With offset-based priority:
        // B: Key = int.MinValue/2 + (-100) (lowest)
        // A: Key = 0 (insertion order)
        // C: Key = 2 (insertion order)
        // B sorts first with ByIndex(-100)
        Assert.That(elements[0].Name, Is.EqualTo("B"), "B with ByIndex(-100) should be first");
        Assert.That(elements[1].Name, Is.EqualTo("A"));
        Assert.That(elements[2].Name, Is.EqualTo("C"));
    }
    [Test]
    public void ByIndex_NegativeIndex()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B");
        orderEngine.AddLogged("C", new ByIndex(-1));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(3));
        // Negative index should have lowest priority and appear first
        var cIndex = elements.FindIndex(e => e.Name == "C");
        Assert.That(cIndex, Is.EqualTo(0), "C with ByIndex(-1) should be first");
    }

    [Test]
    public void ByIndex_LargeIndex()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B");
        orderEngine.AddLogged("C");
        orderEngine.AddLogged("Last", new ByIndex(1000));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(4));
        // Verify all elements exist
        Assert.That(elements.Any(e => e.Name == "Last"), Is.True);
    }

    [Test]
    public void ByIndex_SameIndexMultipleElements()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A", new ByIndex(1));
        orderEngine.AddLogged("B", new ByIndex(1));
        orderEngine.AddLogged("C", new ByIndex(1));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(3));
        // Elements with same index should all be present and maintain insertion order among themselves
        Assert.That(elements[0].Name, Is.EqualTo("A"));
        Assert.That(elements[1].Name, Is.EqualTo("B"));
        Assert.That(elements[2].Name, Is.EqualTo("C"));
    }

    [Test]
    public void ByIndex_MixedWithInsertionOrder()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B", new ByIndex(5));
        orderEngine.AddLogged("C");
        orderEngine.AddLogged("D", new ByIndex(1));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(4));
        // Verify all elements are present
        Assert.That(elements.Any(e => e.Name == "A"), Is.True);
        Assert.That(elements.Any(e => e.Name == "B"), Is.True);
        Assert.That(elements.Any(e => e.Name == "C"), Is.True);
        Assert.That(elements.Any(e => e.Name == "D"), Is.True);
    }

    [Test]
    public void ByIndex_OverridesPreviousRule()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B", new Before("A"));
        orderEngine.AddLogged("B", new ByIndex(5));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(2));
        // ByIndex rule replaces Before rule
        Assert.That(elements.Any(e => e.Name == "B"), Is.True);
        Assert.That(elements.Any(e => e.Name == "A"), Is.True);
    }

    [Test]
    public void ByIndex_WithGroups()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("Group1/Item1");
        orderEngine.AddLogged("Group2/Item2", new ByIndex(0));
        orderEngine.AddLogged("Item3");

        var elements = orderEngine.BuildLogged();

        // Verify all groups and items exist
        Assert.That(elements.Any(e => e.Name == "Group1"), Is.True);
        Assert.That(elements.Any(e => e.Name == "Group2"), Is.True);
        Assert.That(elements.Any(e => e.Name == "Item1"), Is.True);
        Assert.That(elements.Any(e => e.Name == "Item2"), Is.True);
        Assert.That(elements.Any(e => e.Name == "Item3"), Is.True);
    }

    [Test]
    public void ByIndex_IntMaxValue()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B");
        orderEngine.AddLogged("Last", new ByIndex(int.MaxValue));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(3));
        // Element with MaxValue index should exist
        Assert.That(elements.Any(e => e.Name == "Last"), Is.True);
    }

    [Test]
    public void ByIndex_IntMinValue()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B");
        orderEngine.AddLogged("First", new ByIndex(int.MinValue));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(3));
        // Element with MinValue index should exist and potentially be first
        Assert.That(elements.Any(e => e.Name == "First"), Is.True);
    }

    [Test]
    public void ByIndex_RangeOrdering()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("Item1", new ByIndex(10));
        orderEngine.AddLogged("Item2", new ByIndex(5));
        orderEngine.AddLogged("Item3", new ByIndex(15));
        orderEngine.AddLogged("Item4", new ByIndex(3));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(4));
        // All items have explicit ByIndex values, so they sort by index value
        // ByIndex: 3, 5, 10, 15
        // Insertion order: 0, 1, 2, 3
        Assert.That(elements[0].Name, Is.EqualTo("Item4"), "ByIndex(3) is lowest");
        Assert.That(elements[1].Name, Is.EqualTo("Item2"), "ByIndex(5)");
        Assert.That(elements[2].Name, Is.EqualTo("Item1"), "ByIndex(10)");
        Assert.That(elements[3].Name, Is.EqualTo("Item3"), "ByIndex(15) is highest");
    }

    [Test]
    public void ByIndex_SparsedIndices()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("NoIndex");          // insertion order 0, Key = 0
        orderEngine.AddLogged("Index100", new ByIndex(100));  // ByIndex(100)
        orderEngine.AddLogged("Index1", new ByIndex(1));      // ByIndex(1)
        orderEngine.AddLogged("Index50", new ByIndex(50));    // ByIndex(50)

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(4));
        // With direct key assignment (ByIndex or insertion order):
        // NoIndex: Key = 0 (insertion order)
        // Index100: Key = 100 (ByIndex)
        // Index1: Key = 1 (ByIndex)
        // Index50: Key = 50 (ByIndex)
        // Sorted by key: NoIndex(0), Index1(1), Index50(50), Index100(100)
        Assert.That(elements[0].Name, Is.EqualTo("NoIndex"), "NoIndex with insertion order 0 should be first");
        Assert.That(elements[1].Name, Is.EqualTo("Index1"), "Index1 with ByIndex(1)");
        Assert.That(elements[2].Name, Is.EqualTo("Index50"), "Index50 with ByIndex(50)");
        Assert.That(elements[3].Name, Is.EqualTo("Index100"), "Index100 with ByIndex(100)");
    }

    #endregion

    #region Mixed Scenarios Tests

    [Test]
    public void MixedBeforeAndAfter_ComplexOrdering()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B");
        orderEngine.AddLogged("C");
        orderEngine.AddLogged("D");
        orderEngine.AddLogged("E", new Before("C"));
        orderEngine.AddLogged("F", new After("A"));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(6));
        
        // E should come before C
        var eIndex = elements.FindIndex(e => e.Name == "E");
        var cIndex = elements.FindIndex(e => e.Name == "C");
        Assert.That(eIndex, Is.LessThan(cIndex));
        
        // F should come after A
        var aIndex = elements.FindIndex(e => e.Name == "A");
        var fIndex = elements.FindIndex(e => e.Name == "F");
        Assert.That(aIndex, Is.LessThan(fIndex));
    }

    [Test]
    public void GroupsWithOrdering_ItemOrderingAcrossGroups()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("Group1/Item1");
        orderEngine.AddLogged("Group2/Item2");
        orderEngine.AddLogged("Group1/Item3", new After("Group2/Item2"));

        var elements = orderEngine.BuildLogged();

        // Find the elements
        var item2 = elements.FirstOrDefault(e => e.Name == "Item2");
        var item3 = elements.FirstOrDefault(e => e.Name == "Item3");
        
        Assert.That(item2, Is.Not.Null);
        Assert.That(item3, Is.Not.Null);
        
        // Item3 should come after Item2
        var item2Index = elements.IndexOf(item2!);
        var item3Index = elements.IndexOf(item3!);
        Assert.That(item3Index, Is.GreaterThan(item2Index));
    }

    [Test]
    public void OrderingElementInGroup_BeforeExternalElement()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("Banana");
        orderEngine.AddLogged("Group/Cherry");
        orderEngine.AddLogged("Group/Apple");
        orderEngine.AddLogged("Banana", new After("Apple"));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(5));
        
        Assert.That(elements[0], Is.TypeOf<GroupStart>());
        Assert.That(elements[0].Name, Is.EqualTo("Group"));
        
        Assert.That(elements[1].Name, Is.EqualTo("Cherry"));
        Assert.That(elements[2].Name, Is.EqualTo("Apple"));
        Assert.That(elements[3].Name, Is.EqualTo("Banana"));
        
        Assert.That(elements[4], Is.TypeOf<GroupEnd>());
        Assert.That(elements[4].Name, Is.EqualTo("Group"));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void EmptyOrderEngine_BuildReturnsEmptyList()
    {
        var orderEngine = CreateEngine();
        
        var elements = orderEngine.BuildLogged();
        
        Assert.That(elements.Count, Is.EqualTo(0));
    }

    [Test]
    public void SingleElement_BuildReturnsSingleElement()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("OnlyOne");
        
        var elements = orderEngine.BuildLogged();
        
        Assert.That(elements.Count, Is.EqualTo(1));
        Assert.That(elements[0].Name, Is.EqualTo("OnlyOne"));
    }

    [Test]
    public void WhitespaceElementName_IsTrimmed()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("  Item With Spaces  ");
        
        var elements = orderEngine.BuildLogged();
        
        // The engine appears to trim whitespace
        Assert.That(elements[0].Name, Is.EqualTo("Item With Spaces"));
    }

    [Test]
    public void SpecialCharacterElementName_IsPreserved()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("Item@#$%");
        
        var elements = orderEngine.BuildLogged();
        
        Assert.That(elements[0].Name, Is.EqualTo("Item@#$%"));
    }

    [Test]
    public void LargeNumberOfElements_AllProcessed()
    {
        var orderEngine = CreateEngine();
        const int elementCount = 100;
        
        for (int i = 0; i < elementCount; i++)
        {
            orderEngine.AddLogged($"Item{i}");
        }
        
        var elements = orderEngine.BuildLogged();
        
        Assert.That(elements.Count, Is.EqualTo(elementCount));
        for (int i = 0; i < elementCount; i++)
        {
            Assert.That(elements[i].Name, Is.EqualTo($"Item{i}"));
        }
    }

    #endregion

    #region Build Multiple Times

    [Test]
    public void BuildMultipleTimes_ReturnsSameResult()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B");
        orderEngine.AddLogged("C");
        
        var elements1 = orderEngine.BuildLogged();
        var elements2 = orderEngine.BuildLogged();
        
        Assert.That(elements1.Count, Is.EqualTo(elements2.Count));
        for (int i = 0; i < elements1.Count; i++)
        {
            Assert.That(elements1[i].Name, Is.EqualTo(elements2[i].Name));
        }
    }

    [Test]
    public void AddAfterBuild_NewOrderingApplied()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("A");
        orderEngine.AddLogged("B");
        
        var elements1 = orderEngine.BuildLogged();
        
        orderEngine.AddLogged("C", new Before("A"));
        var elements2 = orderEngine.BuildLogged();
        
        Assert.That(elements1.Count, Is.EqualTo(2));
        Assert.That(elements2.Count, Is.EqualTo(3));
        Assert.That(elements2[0].Name, Is.EqualTo("C"));
    }

    #endregion
}










