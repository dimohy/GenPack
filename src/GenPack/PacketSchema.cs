using System.ComponentModel;


namespace GenPack { 
    public class PacketSchema
    {
        public UnitEndian DefaultEndian { get; init; }
        public StringEncoding DefaultStringEncoding { get; init; }
    }
}

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
