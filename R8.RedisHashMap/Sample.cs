using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace R8.RedisHashMap
{
    public partial class Sample
    {
        private int _propsLength = 32;

        private global::System.Collections.Immutable.ImmutableArray<string> _props = global::System.Collections.Immutable.ImmutableArray.Create(new[] { nameof(_stringField), nameof(StringProp), nameof(EnumProp), nameof(CharProp), nameof(Int64Prop), nameof(Int64NullableProp), nameof(UInt64Prop), nameof(UInt32Prop), nameof(Int32Prop), nameof(UInt16Prop), nameof(Int16Prop), nameof(DecimalProp), nameof(DateTimeProp), nameof(DateTimeNullableProp), nameof(TimeSpanProp), nameof(TimeNullableSpanProp), nameof(DoubleProp), nameof(BoolProp), nameof(FloatProp), nameof(CharArrayProp), nameof(CharListProp), nameof(CharCollectionProp), nameof(CProp), nameof(CArrayProp), nameof(CListProp), nameof(CCollectionProp), nameof(CImmutableArrayProp), nameof(ByteArrayProp), nameof(ByteMemoryProp), nameof(ByteReadOnlyMemoryProp), nameof(IntMemoryProp), nameof(IntReadOnlyMemoryProp) });

        public void Save()
        {
            
        }
        //
        // public void Init(global::StackExchange.Redis.RedisValue[] keys, global::StackExchange.Redis.RedisValue[] values)
        // {
        //     for (int i = 0; i < _propsLength; i++)
        //     {
        //         var key = keys[i];
        //         var value = values[i];
        //
        //         switch (key)
        //         {
        //             case nameof(_stringField): { Init_stringField(value); break; }
        //             case nameof(StringProp): { InitStringProp(value); break; }
        //             case nameof(EnumProp): { InitEnumProp(value); break; }
        //             case nameof(CharProp): { InitCharProp(value); break; }
        //             case nameof(Int64Prop): { InitInt64Prop(value); break; }
        //             case nameof(Int64NullableProp): { InitInt64NullableProp(value); break; }
        //             case nameof(UInt64Prop): { InitUInt64Prop(value); break; }
        //             case nameof(UInt32Prop): { InitUInt32Prop(value); break; }
        //             case nameof(Int32Prop): { InitInt32Prop(value); break; }
        //             case nameof(UInt16Prop): { InitUInt16Prop(value); break; }
        //             case nameof(Int16Prop): { InitInt16Prop(value); break; }
        //             case nameof(DecimalProp): { InitDecimalProp(value); break; }
        //             case nameof(DateTimeProp): { InitDateTimeProp(value); break; }
        //             case nameof(DateTimeNullableProp): { InitDateTimeNullableProp(value); break; }
        //             case nameof(TimeSpanProp): { InitTimeSpanProp(value); break; }
        //             case nameof(TimeNullableSpanProp): { InitTimeNullableSpanProp(value); break; }
        //             case nameof(DoubleProp): { InitDoubleProp(value); break; }
        //             case nameof(BoolProp): { InitBoolProp(value); break; }
        //             case nameof(FloatProp): { InitFloatProp(value); break; }
        //             case nameof(CharArrayProp): { InitCharArrayProp(value); break; }
        //             case nameof(CharListProp): { InitCharListProp(value); break; }
        //             case nameof(CharCollectionProp): { InitCharCollectionProp(value); break; }
        //             case nameof(CProp): { InitCProp(value); break; }
        //             case nameof(CArrayProp): { InitCArrayProp(value); break; }
        //             case nameof(CListProp): { InitCListProp(value); break; }
        //             case nameof(CCollectionProp): { InitCCollectionProp(value); break; }
        //             case nameof(CImmutableArrayProp): { InitCImmutableArrayProp(value); break; }
        //             case nameof(ByteArrayProp): { InitByteArrayProp(value); break; }
        //             case nameof(ByteMemoryProp): { InitByteMemoryProp(value); break; }
        //             case nameof(ByteReadOnlyMemoryProp): { InitByteReadOnlyMemoryProp(value); break; }
        //             case nameof(IntMemoryProp): { InitIntMemoryProp(value); break; }
        //             case nameof(IntReadOnlyMemoryProp): { InitIntReadOnlyMemoryProp(value); break; }
        //         }
        //     }
        // }
        //
        // private void Init_stringField(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this._stringField = default;
        //         return;
        //     }
        //
        //     this._stringField = value.ToString();
        // }
        //
        // private void InitStringProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.StringProp = default;
        //         return;
        //     }
        //
        //     this.StringProp = value.ToString();
        // }
        //
        // private void InitEnumProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.EnumProp = default;
        //         return;
        //     }
        //
        //     if (value.IsInteger)
        //     {
        //         this.EnumProp = (R8.RedisHashMap.Test.MyEnum)(int)value;
        //         return;
        //     }
        //     
        //     this.EnumProp = global::System.Enum.Parse<R8.RedisHashMap.Test.MyEnum>(value.ToString());
        // }
        //
        // private void InitCharProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.CharProp = default;
        //         return;
        //     }
        //
        //     this.CharProp = (char)(((ReadOnlyMemory<byte>)value)).Span[0];
        // }
        //
        // private void InitInt64Prop(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.Int64Prop = default;
        //         return;
        //     }
        //
        //     if (value.IsInteger)
        //     {
        //         this.Int64Prop = (long)value;
        //         return;
        //     }
        //     
        //     throw new global::System.InvalidOperationException($"Cannot convert {value} to {typeof(long)}. Targeted property: {nameof(Int64Prop)}");
        // }
        //
        // private void InitInt64NullableProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.Int64NullableProp = null;
        //         return;
        //     }
        //
        //     if (value.IsInteger)
        //     {
        //         this.Int64NullableProp = (long)value;
        //         return;
        //     }
        //     
        //     throw new global::System.InvalidOperationException($"Cannot convert {value} to {typeof(long)}. Targeted property: {nameof(Int64NullableProp)}");
        // }
        //
        // private void InitUInt64Prop(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.UInt64Prop = default;
        //         return;
        //     }
        //
        //     if (value.IsInteger)
        //     {
        //         this.UInt64Prop = (ulong)value;
        //         return;
        //     }
        //     
        //     throw new global::System.InvalidOperationException($"Cannot convert {value} to {typeof(ulong)}. Targeted property: {nameof(UInt64Prop)}");
        // }
        //
        // private void InitUInt32Prop(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.UInt32Prop = default;
        //         return;
        //     }
        //
        //     if (value.IsInteger)
        //     {
        //         this.UInt32Prop = (uint)value;
        //         return;
        //     }
        //     
        //     throw new global::System.InvalidOperationException($"Cannot convert {value} to {typeof(uint)}. Targeted property: {nameof(UInt32Prop)}");
        // }
        //
        // private void InitInt32Prop(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.Int32Prop = default;
        //         return;
        //     }
        //
        //     if (value.IsInteger)
        //     {
        //         this.Int32Prop = (int)value;
        //         return;
        //     }
        //     
        //     throw new global::System.InvalidOperationException($"Cannot convert {value} to {typeof(int)}. Targeted property: {nameof(Int32Prop)}");
        // }
        //
        // private void InitUInt16Prop(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.UInt16Prop = default;
        //         return;
        //     }
        //
        //     if (value.IsInteger)
        //     {
        //         this.UInt16Prop = checked((ushort)(short)value);
        //         return;
        //     }
        //     
        //     throw new global::System.InvalidOperationException($"Cannot convert {value} to {typeof(ushort)}. Targeted property: {nameof(UInt16Prop)}");
        // }
        //
        // private void InitInt16Prop(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.Int16Prop = default;
        //         return;
        //     }
        //
        //     if (value.IsInteger)
        //     {
        //         this.Int16Prop = (short)value;
        //         return;
        //     }
        //
        //     throw new global::System.InvalidOperationException($"Cannot convert {value} to {typeof(short)}. Targeted property: {nameof(Int16Prop)}");
        // }
        //
        // private void InitDecimalProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.DecimalProp = default;
        //         return;
        //     }
        //
        //     if (value.IsInteger)
        //     {
        //         this.DecimalProp = (decimal)value;
        //         return;
        //     }
        //     
        //     throw new global::System.InvalidOperationException($"Cannot convert {value} to {typeof(decimal)}. Targeted property: {nameof(DecimalProp)}");
        // }
        //
        // private void InitDateTimeProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.DateTimeProp = default;
        //         return;
        //     }
        //
        //     if (value.IsInteger)
        //     {
        //         this.DateTimeProp = new global::System.DateTime((long)value);
        //         return;
        //     }
        //     
        //     this.DateTimeProp = global::System.DateTime.Parse(value);
        // }
        //
        // private void InitDateTimeNullableProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.DateTimeNullableProp = default;
        //         return;
        //     }
        //
        //     if (value.IsInteger)
        //     {
        //         this.DateTimeNullableProp = new global::System.DateTime((long)value);
        //         return;
        //     }
        //     
        //     this.DateTimeNullableProp = global::System.DateTime.Parse(value);
        // }
        //
        // private void InitTimeSpanProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.TimeSpanProp = default;
        //         return;
        //     }
        //
        //     if (value.IsInteger)
        //     {
        //         this.TimeSpanProp = global::System.TimeSpan.FromTicks((long)value);
        //         return;
        //     }
        //     
        //     this.TimeSpanProp = global::System.TimeSpan.Parse(value);
        // }
        //
        // private void InitTimeNullableSpanProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.TimeNullableSpanProp = null;
        //         return;
        //     }
        //
        //     if (value.IsInteger)
        //     {
        //         this.TimeNullableSpanProp = global::System.TimeSpan.FromTicks((long)value);
        //         return;
        //     }
        //     
        //     this.TimeNullableSpanProp = global::System.TimeSpan.Parse(value);
        // }
        //
        // private void InitDoubleProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.DoubleProp = default;
        //         return;
        //     }
        //
        //     if (value.IsInteger)
        //     {
        //         this.DoubleProp = (double)value;
        //         return;
        //     }
        //     
        //     throw new global::System.InvalidOperationException($"Cannot convert {value} to {typeof(double)}. Targeted property: {nameof(DoubleProp)}");
        // }
        //
        // private void InitBoolProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.BoolProp = default;
        //         return;
        //     }
        //
        //     this.BoolProp = (bool)value;
        // }
        //
        // private void InitFloatProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.FloatProp = default;
        //         return;
        //     }
        //
        //     if (value.IsInteger)
        //     {
        //         this.FloatProp = (float)value;
        //         return;
        //     }
        //     
        //     throw new global::System.InvalidOperationException($"Cannot convert {value} to {typeof(float)}. Targeted property: {nameof(FloatProp)}");
        // }
        //
        // private void InitCharArrayProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.CharArrayProp = default;
        //         return;
        //     }
        //
        //     this.CharArrayProp = global::System.Text.Json.JsonSerializer.Deserialize<char[]>(((ReadOnlyMemory<byte>)value).Span);
        // }
        //
        // private void InitCharListProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.CharListProp = default;
        //         return;
        //     }
        //
        //     this.CharListProp = global::System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<char>>(((ReadOnlyMemory<byte>)value).Span);
        // }
        //
        // private void InitCharCollectionProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.CharCollectionProp = default;
        //         return;
        //     }
        //
        //     this.CharCollectionProp = global::System.Text.Json.JsonSerializer.Deserialize<System.Collections.ObjectModel.Collection<char>>(((ReadOnlyMemory<byte>)value).Span);
        // }
        //
        // private void InitCProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.CProp = default;
        //         return;
        //     }
        //
        //     this.CProp = global::System.Text.Json.JsonSerializer.Deserialize<R8.RedisHashMap.Test.C>(((ReadOnlyMemory<byte>)value).Span);
        // }
        //
        // private void InitCArrayProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.CArrayProp = default;
        //         return;
        //     }
        //
        //     this.CArrayProp = global::System.Text.Json.JsonSerializer.Deserialize<R8.RedisHashMap.Test.C[]>(((ReadOnlyMemory<byte>)value).Span);
        // }
        //
        // private void InitCListProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.CListProp = default;
        //         return;
        //     }
        //
        //     this.CListProp = global::System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<R8.RedisHashMap.Test.C>>(((ReadOnlyMemory<byte>)value).Span);
        // }
        //
        // private void InitCCollectionProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.CCollectionProp = default;
        //         return;
        //     }
        //
        //     this.CCollectionProp = global::System.Text.Json.JsonSerializer.Deserialize<System.Collections.ObjectModel.Collection<R8.RedisHashMap.Test.C>>(((ReadOnlyMemory<byte>)value).Span);
        // }
        //
        // private void InitCImmutableArrayProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.CImmutableArrayProp = default;
        //         return;
        //     }
        //
        //     throw new global::System.InvalidOperationException($"Cannot convert {value} to {typeof(System.Collections.Immutable.ImmutableArray<R8.RedisHashMap.Test.C>)}. Targeted property: {nameof(CImmutableArrayProp)}");
        // }
        //
        // private void InitByteArrayProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.ByteArrayProp = default;
        //         return;
        //     }
        //
        //     this.ByteArrayProp = (byte[])value;
        // }
        //
        // private void InitByteMemoryProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.ByteMemoryProp = default;
        //         return;
        //     }
        //
        //     throw new global::System.InvalidOperationException($"Cannot convert {value} to {typeof(System.Memory<byte>)}. Targeted property: {nameof(ByteMemoryProp)}");
        // }
        //
        // private void InitByteReadOnlyMemoryProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.ByteReadOnlyMemoryProp = default;
        //         return;
        //     }
        //
        //     this.ByteReadOnlyMemoryProp = (global::System.ReadOnlyMemory<byte>)value;
        // }
        //
        // private void InitIntMemoryProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.IntMemoryProp = default;
        //         return;
        //     }
        //
        //     throw new global::System.InvalidOperationException($"Cannot convert {value} to {typeof(System.Memory<int>)}. Targeted property: {nameof(IntMemoryProp)}");
        // }
        //
        // private void InitIntReadOnlyMemoryProp(global::StackExchange.Redis.RedisValue value)
        // {
        //     if (value.IsNullOrEmpty)
        //     {
        //         this.IntReadOnlyMemoryProp = default;
        //         return;
        //     }
        //
        //     throw new global::System.InvalidOperationException($"Cannot convert {value} to {typeof(System.ReadOnlyMemory<int>)}. Targeted property: {nameof(IntReadOnlyMemoryProp)}");
        // }

    }

    public partial class Sample
    {
        private string _stringField;

        private string StringProp { get; set; }

        public MyEnum EnumProp { get; set; }
        public char CharProp { get; set; }
        public long Int64Prop { get; set; }
        public long? Int64NullableProp { get; set; }
        public ulong UInt64Prop { get; set; }
        public uint UInt32Prop { get; set; }
        public int Int32Prop { get; set; }
        public ushort UInt16Prop { get; set; }
        public short Int16Prop { get; set; }
        public decimal DecimalProp { get; set; }

        public DateTime DateTimeProp { get; set; }
        public DateTime DateTimeNullableProp { get; set; }
        public TimeSpan TimeSpanProp { get; set; }
        public TimeSpan? TimeNullableSpanProp { get; set; }

        public double DoubleProp { get; set; }
        public bool BoolProp { get; set; }
        public float FloatProp { get; set; }

        public char[] CharArrayProp { get; set; }
        public List<char> CharListProp { get; set; }
        public Collection<char> CharCollectionProp { get; set; }

        public C CProp { get; set; }
        public C[] CArrayProp { get; set; }
        public List<C> CListProp { get; set; }
        public Collection<C> CCollectionProp { get; set; }
        public ImmutableArray<C> CImmutableArrayProp { get; set; }

        public byte[] ByteArrayProp { get; set; }
        public Memory<byte> ByteMemoryProp { get; set; }
        public ReadOnlyMemory<byte> ByteReadOnlyMemoryProp { get; set; }
        public Memory<int> IntMemoryProp { get; set; }
        public ReadOnlyMemory<int> IntReadOnlyMemoryProp { get; set; }
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
}