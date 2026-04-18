// Polyfills for netstandard2.0 compatibility
#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
namespace System
{
    internal readonly struct Index
    {
        private readonly int _value;
        public Index(int value, bool fromEnd = false)
        {
            _value = fromEnd ? ~value : value;
        }
        public static Index FromEnd(int value) => new Index(value, fromEnd: true);
        public int GetOffset(int length)
        {
            int offset = _value;
            if (_value < 0)
                offset = length + ~_value + 1;
            return offset;
        }
        public bool IsFromEnd => _value < 0;
        public int Value => _value < 0 ? ~_value : _value;
    }
}
#endif
