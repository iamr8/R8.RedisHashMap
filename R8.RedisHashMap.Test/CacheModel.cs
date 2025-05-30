using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using R8.RedisHashMap;
using StackExchange.Redis;

namespace R8.RedisHashMap.Test;

[CacheableOptions(IncludeFields = false, IncludePrivate = false)]
public partial class CacheModel : ICacheable
{
    private string _stringField;
    private readonly int _intReadOnlyField;
    public string _publicStringField;

    private string StringProp { get; set; }
    private string? StringNullableProp { get; set; }
    private string[] StringArrayProp { get; set; }
    private List<string> StringListProp { get; set; }
    private Dictionary<string, string> StringDictionaryProp { get; set; }
    public int IntProp { get; set; }
    public int? IntNullableProp { get; set; }

    [CacheablePropertyName("enumProp")] public MyEnum EnumProp { get; set; }

    [JsonPropertyName("enumNullableProp")]
    public MyEnum? EnumNullableProp { get; set; }
    public MyEnum[] EnumArrayProp { get; set; }
    public CacheModel? CacheModelProp { get; set; }

    // public byte ByteProp { get; set; } // TODO: Source-Generator Bug

    // public char CharProp { get; set; }
    // public char? CharNullableProp { get; set; }
    // public char[] CharArrayProp { get; set; }
    // public List<char> CharListProp { get; set; }
    // public Collection<char> CharCollectionProp { get; set; }
    // public ImmutableArray<char> CharImmutableArrayProp { get; set; }
    //
    // public long Int64Prop { get; set; }
    // public long? Int64NullableProp { get; set; }
    // public long[] Int64ArrayProp { get; set; }
    // public long?[] Int64NullableArrayProp { get; set; }
    // public ulong UInt64Prop { get; set; }
    // public ulong? UInt64NullableProp { get; set; }
    // public ulong[] UInt64ArrayProp { get; set; }
    // public ulong?[] UInt64NullableArrayProp { get; set; }
    //
    // public uint UInt32Prop { get; set; }
    // public uint? UInt32NullableProp { get; set; }
    // public int Int32Prop { get; set; }
    // public int? Int32NullableProp { get; set; }
    //
    // public ushort UInt16Prop { get; set; }
    // public ushort? UInt16NullableProp { get; set; }
    // public short Int16Prop { get; set; }
    // public short? Int16NullableProp { get; set; }
    //
    // public decimal DecimalProp { get; set; }
    // public decimal? DecimalNullableProp { get; set; }
    // public double DoubleProp { get; set; }
    // public double? DoubleNullableProp { get; set; }
    // public float FloatProp { get; set; }
    //
    // [CacheableConverter(typeof(TimeSpanCacheableConverter))]
    // public float? FloatNullableProp { get; set; }
    //
    // public DateTime DateTimeProp { get; set; }
    // public DateTime? DateTimeNullableProp { get; set; }
    //
    // [CacheableConverter(typeof(TimeSpanCacheableConverter))]
    // public TimeSpan TimeSpanProp { get; set; }
    //
    // [CacheableConverter(typeof(TimeSpanCacheableConverter))]
    // public TimeSpan? TimeNullableSpanProp { get; set; }
    //
    // public Dictionary<string, DateTime> DateTimeDictionaryProp { get; set; }
    //
    // public bool BoolProp { get; set; }
    // public bool? BoolNullableProp { get; set; }
    //
    // public C CProp { get; set; }
    // public C[] CArrayProp { get; set; }
    // public List<C> CListProp { get; set; }
    // public Collection<C> CCollectionProp { get; set; }
    // public ImmutableArray<C> CImmutableArrayProp { get; set; }
    // public Dictionary<int, C> CDictionaryProp { get; set; }
    // public Dictionary<string, C> CStringDictionaryProp { get; set; }
    //
    // public byte[] ByteArrayProp { get; set; }
    // public Memory<byte> ByteMemoryProp { get; set; }
    // public ReadOnlyMemory<byte> ByteReadOnlyMemoryProp { get; set; }
    // public Memory<int> IntMemoryProp { get; set; }
    // public ReadOnlyMemory<int> IntReadOnlyMemoryProp { get; set; }
    // public IEnumerable<byte> ByteIEnumerableProp { get; set; }
    //
    // public RedisValue RedisValueProp { get; set; }
    // public RedisValue? RedisValueNullableProp { get; set; }
    // public RedisValue[] RedisValueArrayProp { get; set; } // Should Check Serialization for RedisValue[]
    // public List<RedisValue> RedisValueListProp { get; set; } // Should Check Serialization for List<RedisValue>
    //
    // public JsonDocument JsonDocumentProp { get; set; }
    // public JsonDocument? JsonDocumentNullableProp { get; set; }
    // public JsonElement JsonElementProp { get; set; }
    // public JsonElement? JsonElementNullableProp { get; set; }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class CacheablePropertyNameAttribute : Attribute
{
    public string Name { get; }

    public CacheablePropertyNameAttribute(string name)
    {
        Name = name;
    }
}

public class C
{
    public string StringProp { get; set; }
}

public enum MyEnum
{
    Value1,
    Value2,
    Value3
}