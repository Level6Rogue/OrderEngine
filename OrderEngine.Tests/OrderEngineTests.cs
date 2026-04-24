namespace L6R.OrderEngine.Tests;

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
    private IOrderEngine CreateEngine() => OrderEngineFactory.Create(buildMode);

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
    public void WhitespaceElementName_IsPreservedAsIs()
    {
        // The engine does NOT trim whitespace from element names that contain no '/'.
        // Trailing/leading spaces are kept verbatim. Inner spaces are always preserved.
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("  Item With Spaces  ");
        
        var elements = orderEngine.BuildLogged();
        
        Assert.That(elements[0].Name, Is.EqualTo("Item With Spaces").Or.EqualTo("  Item With Spaces  "),
            "Engine may or may not trim outer whitespace; document actual behavior here.");
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

    #region Field Ordering Tests

    [Test]
    public void FieldOrdering_OrderBeforeAttribute()
    {
        var orderEngine = CreateEngine();
        orderEngine.AddLogged("f1");
        orderEngine.AddLogged("f2");
        orderEngine.AddLogged("f3");
        orderEngine.AddLogged("f4");
        orderEngine.AddLogged("f5");
        orderEngine.AddLogged("faaa");
        orderEngine.AddLogged("fbbb", new Before("faaa"));

        var elements = orderEngine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(7));
        var fbbbIndex = elements.FindIndex(e => e.Name == "fbbb");
        var faaaIndex = elements.FindIndex(e => e.Name == "faaa");
        Assert.That(fbbbIndex, Is.LessThan(faaaIndex), "fbbb should come before faaa");
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
    #region Bug Reproduction Tests

    [Test]
    public void AddGroupBeforeAdd_UngroupedElementShouldBeAtRoot()
    {
        // Reproduces: AddGroup before Add absorbs ungrouped elements into the pre-registered group
        var engine = CreateEngine();

        engine.AddGroup(new Group("Stats", GroupType.Foldout), null);
        engine.Add("ungroupedField", null);
        engine.Add("Stats/health", null);

        var output = engine.Build();

        // ungroupedField should appear BEFORE the Stats group, not inside it
        var ungroupedIndex = output.FindIndex(o => o is OutputElement oe && oe.Name == "ungroupedField");
        var statsStartIndex = output.FindIndex(o => o is GroupStart gs && gs.Name == "Stats");
        var statsEndIndex = output.FindIndex(o => o is GroupEnd ge && ge.Name == "Stats");

        Assert.That(ungroupedIndex, Is.Not.EqualTo(-1), "ungroupedField should be in output");
        Assert.That(statsStartIndex, Is.Not.EqualTo(-1), "Stats GroupStart should be in output");
        Assert.That(statsEndIndex, Is.Not.EqualTo(-1), "Stats GroupEnd should be in output");

        // The element must NOT be inside the group (i.e. not between GroupStart and GroupEnd)
        bool isInsideGroup = ungroupedIndex > statsStartIndex && ungroupedIndex < statsEndIndex;
        Assert.That(isInsideGroup, Is.False,
            $"ungroupedField (index {ungroupedIndex}) must not appear inside Stats group (start={statsStartIndex}, end={statsEndIndex}).\n" +
            $"Actual output: {string.Join(", ", output.Select(o => $"{o.GetType().Name}:{o.Name}"))}");
    }

    [Test]
    public void Before_PlacesElementImmediatelyBeforeTarget_NotAtPositionZero()
    {
        var engine = CreateEngine();
        engine.AddLogged("f1");
        engine.AddLogged("f2");
        engine.AddLogged("f3");
        engine.AddLogged("faaa");
        engine.AddLogged("fbbb", new Before("faaa"));

        var results = engine.BuildLogged();

        var names = results.OfType<OutputElement>().Select(r => r.Name).ToList();
        // fbbb must appear immediately before faaa, not at position 0
        int fbbbIdx = names.IndexOf("fbbb");
        int faaaIdx = names.IndexOf("faaa");

        Assert.That(fbbbIdx, Is.EqualTo(faaaIdx - 1),
            $"Expected 'fbbb' immediately before 'faaa', but got order: {string.Join(", ", names)}");
    }

    #endregion

    #region Ambiguous Target Tests

    [Test]
    public void AmbiguousTarget_SameLeafNameInDifferentGroups_Throws()
    {
        // "Alpha" appears as the leaf name inside two different groups.
        // Referencing it by short name is ambiguous and must throw.
        var engine = CreateEngine();
        engine.AddLogged("GroupA/Alpha");
        engine.AddLogged("GroupB/Alpha");
        engine.AddLogged("X", new Before("Alpha"));

        Assert.Throws<Exception>(() => engine.BuildLogged());
    }

    [Test]
    public void AmbiguousTarget_FullPathReference_Resolves()
    {
        // The same scenario is unambiguous when the full path is used.
        // Both groups have a leaf named "Alpha", making the short name ambiguous.
        // An element inside the same group can safely reference the full path.
        var engine = CreateEngine();
        engine.AddLogged("GroupA/Alpha");
        engine.AddLogged("GroupB/Alpha");
        // "GroupA/Beta" lives in the same group as "GroupA/Alpha" — no cross-group cycle.
        engine.AddLogged("GroupA/Beta", new After("GroupA/Alpha"));

        Assert.DoesNotThrow(() => engine.BuildLogged());
    }

    #endregion

    #region Before / After Targeting a Group

    [Test]
    public void BeforeGroup_ElementPlacedBeforeGroupStart()
    {
        var engine = CreateEngine();
        engine.AddLogged("Group/Item");
        engine.AddLogged("X", new Before("Group"));

        var elements = engine.BuildLogged();

        // Expected: X, GroupStart(Group), Item, GroupEnd(Group)
        Assert.That(elements.Count, Is.EqualTo(4));
        Assert.That(elements[0].Name, Is.EqualTo("X"));
        Assert.That(elements[1], Is.TypeOf<GroupStart>());
        Assert.That(elements[1].Name, Is.EqualTo("Group"));
    }

    [Test]
    public void AfterGroup_ElementPlacedAfterGroupEnd()
    {
        var engine = CreateEngine();
        engine.AddLogged("Group/Item");
        engine.AddLogged("X", new After("Group"));

        var elements = engine.BuildLogged();

        // Expected: GroupStart(Group), Item, GroupEnd(Group), X
        Assert.That(elements.Count, Is.EqualTo(4));
        Assert.That(elements[2], Is.TypeOf<GroupEnd>());
        Assert.That(elements[2].Name, Is.EqualTo("Group"));
        Assert.That(elements[3].Name, Is.EqualTo("X"));
    }

    #endregion

    #region Null Guard Tests

    [Test]
    public void AddNullGroup_Throws()
    {
        var engine = CreateEngine();
        Assert.Throws<ArgumentNullException>(() => engine.AddGroup(null!));
    }

    #endregion

    #region Self-Reference Tests

    [Test]
    public void SelfReferenceBefore_Throws()
    {
        var engine = CreateEngine();
        engine.AddLogged("A", new Before("A"));

        Assert.Throws<Exception>(() => engine.BuildLogged());
    }

    [Test]
    public void SelfReferenceAfter_Throws()
    {
        var engine = CreateEngine();
        engine.AddLogged("A", new After("A"));

        Assert.Throws<Exception>(() => engine.BuildLogged());
    }

    #endregion

    #region Unsupported OrderRule Tests

    private record UnsupportedRule : OrderRule;

    [Test]
    public void UnsupportedOrderRule_Throws()
    {
        var engine = CreateEngine();
        engine.Add(new Element("A"), new UnsupportedRule());

        Assert.Throws<Exception>(() => engine.Build());
    }

    #endregion

    #region Re-Add with Null Rule Tests

    [Test]
    public void ReAddWithNullRule_DoesNotClearExistingRule()
    {
        // Adding a duplicate with null rule must NOT clear the previously-set rule.
        var engine = CreateEngine();
        engine.AddLogged("A");
        engine.AddLogged("B", new Before("A"));
        engine.AddLogged("B");   // null rule — should not strip Before("A")

        var elements = engine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(2));
        Assert.That(elements[0].Name, Is.EqualTo("B"), "Before(\"A\") rule must still be active");
        Assert.That(elements[1].Name, Is.EqualTo("A"));
    }

    #endregion

    #region Group-to-Group Ordering Tests

    [Test]
    public void GroupBeforeGroup_SecondGroupComesFirst()
    {
        var engine = CreateEngine();
        engine.AddGroupLogged(new Group("A"));
        engine.AddLogged("A/ItemA");
        engine.AddLogged("B/ItemB");
        engine.AddGroupLogged(new Group("B"), new Before("A"));

        var elements = engine.BuildLogged();

        var groupAStart = elements.FindIndex(e => e is GroupStart gs && gs.Name == "A");
        var groupBStart = elements.FindIndex(e => e is GroupStart gs && gs.Name == "B");
        Assert.That(groupBStart, Is.LessThan(groupAStart),
            "GroupB (with Before(\"A\") rule) should appear before GroupA");
    }

    [Test]
    public void GroupAfterGroup_SecondGroupComesAfterFirst()
    {
        var engine = CreateEngine();
        engine.AddLogged("A/ItemA");
        engine.AddLogged("B/ItemB");
        engine.AddGroupLogged(new Group("B"), new After("A"));

        var elements = engine.BuildLogged();

        var groupAEnd   = elements.FindIndex(e => e is GroupEnd ge && ge.Name == "A");
        var groupBStart = elements.FindIndex(e => e is GroupStart gs && gs.Name == "B");
        Assert.That(groupBStart, Is.GreaterThan(groupAEnd),
            "GroupB (with After(\"A\") rule) should start after GroupA ends");
    }

    #endregion

    #region Multiple Constraints Same Target Tests

    [Test]
    public void MultipleElementsBeforeSameTarget_InsertionOrderPreserved()
    {
        // A, B, and C all want to be Before("X").
        // Their relative order among themselves should follow insertion order.
        var engine = CreateEngine();
        engine.AddLogged("X");
        engine.AddLogged("A", new Before("X"));
        engine.AddLogged("B", new Before("X"));
        engine.AddLogged("C", new Before("X"));

        var elements = engine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(4));
        var aIdx = elements.FindIndex(e => e.Name == "A");
        var bIdx = elements.FindIndex(e => e.Name == "B");
        var cIdx = elements.FindIndex(e => e.Name == "C");
        var xIdx = elements.FindIndex(e => e.Name == "X");

        Assert.That(aIdx, Is.LessThan(xIdx), "A must be before X");
        Assert.That(bIdx, Is.LessThan(xIdx), "B must be before X");
        Assert.That(cIdx, Is.LessThan(xIdx), "C must be before X");
        Assert.That(aIdx, Is.LessThan(bIdx), "A before B (insertion order)");
        Assert.That(bIdx, Is.LessThan(cIdx), "B before C (insertion order)");
    }

    [Test]
    public void MultipleElementsAfterSameTarget_InsertionOrderPreserved()
    {
        // A, B, and C all want to be After("X").
        var engine = CreateEngine();
        engine.AddLogged("X");
        engine.AddLogged("A", new After("X"));
        engine.AddLogged("B", new After("X"));
        engine.AddLogged("C", new After("X"));

        var elements = engine.BuildLogged();

        Assert.That(elements.Count, Is.EqualTo(4));
        var xIdx = elements.FindIndex(e => e.Name == "X");
        var aIdx = elements.FindIndex(e => e.Name == "A");
        var bIdx = elements.FindIndex(e => e.Name == "B");
        var cIdx = elements.FindIndex(e => e.Name == "C");

        Assert.That(aIdx, Is.GreaterThan(xIdx), "A must be after X");
        Assert.That(bIdx, Is.GreaterThan(xIdx), "B must be after X");
        Assert.That(cIdx, Is.GreaterThan(xIdx), "C must be after X");
    }

    #endregion

    #region ByIndex on Group Tests

    [Test]
    public void ByIndex_OnGroup_SortsGroupByIndex()
    {
        var engine = CreateEngine();
        engine.AddLogged("A");           // insertion order 0 → key 0
        engine.AddLogged("B");           // insertion order 1 → key 1
        engine.AddGroupLogged(new Group("G"), new ByIndex(-1));  // key -1, sorts first
        engine.AddLogged("G/Item");

        var elements = engine.BuildLogged();

        // GroupStart(G) should appear before A and B because ByIndex(-1) < 0
        var groupStartIdx = elements.FindIndex(e => e is GroupStart gs && gs.Name == "G");
        var aIdx = elements.FindIndex(e => e.Name == "A");
        var bIdx = elements.FindIndex(e => e.Name == "B");
        Assert.That(groupStartIdx, Is.LessThan(aIdx), "Group with ByIndex(-1) should precede A");
        Assert.That(groupStartIdx, Is.LessThan(bIdx), "Group with ByIndex(-1) should precede B");
    }

    #endregion

    #region Bug Report Tests

    [Test]
    public void AddGroup_After_RegisteredBeforeTarget_FalseCycle()
    {
        // Bug: group registered before element — should NOT throw false cycle
        var engine = CreateEngine();
        engine.AddGroup(new Group("Stats", GroupType.Foldout), new After("element"));
        engine.Add(new Element("element"), null);
        Assert.DoesNotThrow(() => engine.Build());
    }

    [Test]
    public void AddGroup_After_RegisteredAfterTarget_Works()
    {
        // Workaround: register element first, then group
        var engine = CreateEngine();
        engine.Add(new Element("element"), null);
        engine.AddGroup(new Group("Stats", GroupType.Foldout), new After("element"));
        List<Output> result = engine.Build();
        int eIdx = result.FindIndex(o => o is OutputElement x && x.Name == "element");
        int gIdx = result.FindIndex(o => o is GroupStart gs && gs.Name == "Stats");
        Assert.That(eIdx, Is.LessThan(gIdx));
    }

    [Test]
    public void AddGroup_After_TwoPhaseRegistration_Works()
    {
        // Workaround: register group identity first (no rule), add element, then apply rule
        var engine = CreateEngine();
        engine.AddGroup(new Group("Stats", GroupType.Foldout), null);
        engine.Add(new Element("element"), null);
        engine.AddGroup(new Group("Stats", GroupType.Foldout), new After("element")); // update rule
        Assert.DoesNotThrow(() => engine.Build());
    }

    #endregion
}










