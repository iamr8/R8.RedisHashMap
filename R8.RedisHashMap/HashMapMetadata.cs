namespace R8.RedisHashMap
{
    public struct HashMapMetadata
    {
        public bool IsNullable { get; set; }
        public bool IsPrimitive { get; set; }
        public bool IsInt16 { get; set; }
        public bool IsInt32 { get; set; }
        public bool IsInt64 { get; set; }
        public bool IsDouble { get; set; }
    }
}