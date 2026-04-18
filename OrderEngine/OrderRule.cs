namespace L6R.OrderEngine
{
    public abstract record OrderRule;

    public record ByIndex(int Index) : OrderRule;

    public abstract record Relative : OrderRule;
    public record Before(string ElementName) : Relative;
    public record After(string ElementName) : Relative;
}

