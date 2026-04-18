namespace L6R.OrderEngine
{
    public record Element(string Name)
    {
        public static implicit operator Element(string name) => new(name);
    }

    public record Group(string Name, GroupType GroupType = GroupType.Default);
}

