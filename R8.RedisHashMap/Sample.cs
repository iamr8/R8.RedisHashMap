// using System;
// using System.CodeDom.Compiler;
// using System.Collections.Generic;
// using System.Text.Json;
// using System.Text.Json.Serialization.Metadata;
// using R8.RedisHashMap;
// using StackExchange.Redis;
//
// namespace R8.RedisHashMap
// {
//     [global::System.CodeDom.Compiler.GeneratedCode("R8.RedisHashMap", "1.0.0")]
//     public partial class SampleCacheableContext
//     {
//         private SampleCacheableContext()
//         {
//         }
//
//         private static readonly Lazy<SampleCacheableContext> Instance = new Lazy<SampleCacheableContext>(() => new SampleCacheableContext());
//         public static readonly SampleCacheableContext Default = Instance.Value;
//
//         // public readonly DictionaryStringSampleCacheableTypeResolver DictionaryResolver = DictionaryStringSampleCacheableTypeResolver.Default;
//
//         // public readonly ListStringCacheableTypeResolver ListResolver = ListStringCacheableTypeResolver.Default;
//
//         // public readonly SampleModelCacheableContext SampleResolver = SampleModelCacheableContext.Default;
//     }
//
//     // PropertyNames.cs
//     public partial class SampleCacheableContext
//     {
//         private const string PropName_Id = "Id";
//         private const string PropName_FirstName = "FirstName";
//         private const string PropName_LastName = "LastName";
//         private const string PropName_Email = "Email";
//         private const string PropName_Mobile = "Mobile";
//         private const string PropName_Roles = "Roles";
//         private const string PropName_Tags = "Tags";
//         private const string PropName_Data = "Data";
//         private const string PropName_Nested = "Nested";
//     }
//
//     // GetCacheableTypeInfo.cs
//     public partial class SampleCacheableContext : ICacheableContext
//     {
//         CacheableTypeInfo? ICacheableContext.GetTypeInfo(Type type)
//         {
//             if (type == typeof(Sample))
//             {
//                 return Create_Sample();
//             }
//
//             return null;
//         }
//     }
//
//     // Sample.cs
//     public partial class SampleCacheableContext
//     {
//         private CacheableTypeInfo<Sample>? _Sample;
//
//         public CacheableTypeInfo<Sample> Sample
//         {
//             get => _Sample ??= (CacheableTypeInfo<Sample>)((ICacheableContext)this).GetTypeInfo(typeof(Sample));
//         }
//
//         private CacheableTypeInfo<Sample> Create_Sample()
//         {
//             var typeInfo = new CacheableTypeInfo<Sample>
//             {
//                 ObjectCreator = () => new Sample(),
//                 PropertyMetadataInitializer = _ => SamplePropInit(),
//                 OriginatingResolver = this
//             };
//
//             return typeInfo;
//         }
//
//         private CacheablePropertyInfo[] SamplePropInit()
//         {
//             var jsonTypeInfoResolver = this as IJsonTypeInfoResolver;
//             var properties = new CacheablePropertyInfo[9];
//
//             var info0 = new CacheablePropertyInfoValues<int>
//             {
//                 DeclaringType = typeof(Sample),
//                 DefaultCreator = () => default(int),
//                 Getter = obj => ((Sample)obj).Id,
//                 Setter = (obj, value) => ((Sample)obj).Id = value!,
//                 Generator = value => (RedisValue)value,
//                 Parser = value => (int)value,
//                 Converter = null,
//                 PropertyName = PropName_Id,
//             };
//
//             properties[0] = CacheableMetadataServices.CreatePropertyInfo<int>(info0);
//
//             var info1 = new CacheablePropertyInfoValues<string>
//             {
//                 DeclaringType = typeof(Sample),
//                 DefaultCreator = () => default(string),
//                 Getter = obj => ((Sample)obj).FirstName,
//                 Setter = (obj, value) => ((Sample)obj).FirstName = value!,
//                 Generator = value => (RedisValue)value,
//                 Parser = value => (string)value,
//                 Converter = null,
//                 PropertyName = PropName_FirstName,
//             };
//
//             properties[1] = CacheableMetadataServices.CreatePropertyInfo<string>(info1);
//
//             var info2 = new CacheablePropertyInfoValues<string>
//             {
//                 DeclaringType = typeof(Sample),
//                 DefaultCreator = () => default(string),
//                 Getter = obj => ((Sample)obj).LastName,
//                 Setter = (obj, value) => ((Sample)obj).LastName = value!,
//                 Generator = value => (RedisValue)value,
//                 Parser = value => (string)value,
//                 Converter = null,
//                 PropertyName = PropName_LastName,
//             };
//
//             properties[2] = CacheableMetadataServices.CreatePropertyInfo<string>(info2);
//
//             var info3 = new CacheablePropertyInfoValues<string>
//             {
//                 DeclaringType = typeof(Sample),
//                 DefaultCreator = () => default(string?),
//                 Getter = obj => ((Sample)obj).Email,
//                 Setter = (obj, value) => ((Sample)obj).Email = value!,
//                 Generator = value => (RedisValue)value,
//                 Parser = value => (string)value,
//                 Converter = null,
//                 PropertyName = PropName_Email,
//             };
//
//             properties[3] = CacheableMetadataServices.CreatePropertyInfo<string>(info3);
//
//             var info4 = new CacheablePropertyInfoValues<string>
//             {
//                 DeclaringType = typeof(Sample),
//                 DefaultCreator = () => default(string?),
//                 Getter = obj => ((Sample)obj).Mobile,
//                 Setter = (obj, value) => ((Sample)obj).Mobile = value!,
//                 Generator = value => (RedisValue)value,
//                 Parser = value => (string)value,
//                 Converter = null,
//                 PropertyName = PropName_Mobile,
//             };
//
//             properties[4] = CacheableMetadataServices.CreatePropertyInfo<string>(info4);
//
//             var info5 = new CacheablePropertyInfoValues<UserRoleType[]>
//             {
//                 DeclaringType = typeof(Sample),
//                 DefaultCreator = Array.Empty<UserRoleType>,
//                 Getter = obj => ((Sample)obj).Roles,
//                 Setter = (obj, value) => ((Sample)obj).Roles = value!,
//                 Generator = value => jsonType != null ? JsonSerializer.SerializeToUtf8Bytesvalue : JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions.Default),
//                 Parser = value => jsonType != null ? JsonSerializer.Deserialize<UserRoleType[]>(((ReadOnlyMemory<byte>)value).Span, jsonType)! : JsonSerializer.Deserialize<UserRoleType[]>(((ReadOnlyMemory<byte>)value).Span, JsonSerializerOptions.Default)!,
//                 Converter = null,
//                 PropertyName = PropName_Roles,
//             };
//
//             properties[5] = CacheableMetadataServices.CreatePropertyInfo<UserRoleType[]>(info5);
//
//             var info6 = new CacheablePropertyInfoValues<string[]>
//             {
//                 DeclaringType = typeof(Sample),
//                 DefaultCreator = Array.Empty<string>,
//                 Getter = obj => ((Sample)obj).Tags,
//                 Setter = (obj, value) => ((Sample)obj).Tags = value!,
//                 Generator = value => jsonType != null ? JsonSerializer.SerializeToUtf8Bytesvalue : JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions.Default),
//                 Parser = value => jsonType != null ? JsonSerializer.Deserialize<string[]>(((ReadOnlyMemory<byte>)value).Span, jsonType)! : JsonSerializer.Deserialize<string[]>(((ReadOnlyMemory<byte>)value).Span, JsonSerializerOptions.Default)!,
//                 Converter = null,
//                 PropertyName = PropName_Tags,
//             };
//
//             properties[6] = CacheableMetadataServices.CreatePropertyInfo<string[]>(info6);
//
//             var info7 = new CacheablePropertyInfoValues<Dictionary<string, string>>
//             {
//                 DeclaringType = typeof(Sample),
//                 DefaultCreator = () => new Dictionary<string, string>(),
//                 Getter = obj => ((Sample)obj).Data,
//                 Setter = (obj, value) => ((Sample)obj).Data = value!,
//                 Generator = value => jsonType != null ? JsonSerializer.SerializeToUtf8Bytesvalue : JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions.Default),
//                 Parser = value => jsonType != null ? JsonSerializer.Deserialize<Dictionary<string, string>>(((ReadOnlyMemory<byte>)value).Span, jsonType)! : JsonSerializer.Deserialize<Dictionary<string, string>>(((ReadOnlyMemory<byte>)value).Span, JsonSerializerOptions.Default)!,
//                 Converter = null,
//                 PropertyName = PropName_Data,
//             };
//
//             properties[7] = CacheableMetadataServices.CreatePropertyInfo<Dictionary<string, string>>(info7);
//
//             var info8 = new CacheablePropertyInfoValues<List<Nested>>
//             {
//                 DeclaringType = typeof(Sample),
//                 DefaultCreator = () => new List<Nested>(),
//                 Getter = obj => ((Sample)obj).Nested,
//                 Setter = (obj, value) => ((Sample)obj).Nested = value!,
//                 Generator = value => jsonType != null ? JsonSerializer.SerializeToUtf8Bytesvalue : JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions.Default),
//                 Parser = value => jsonType != null ? JsonSerializer.Deserialize<List<Nested>>(((ReadOnlyMemory<byte>)value).Span, jsonType)! : JsonSerializer.Deserialize<List<Nested>>(((ReadOnlyMemory<byte>)value).Span, JsonSerializerOptions.Default)!,
//                 Converter = null,
//                 PropertyName = PropName_Nested,
//             };
//
//             properties[8] = CacheableMetadataServices.CreatePropertyInfo<List<Nested>>(info8);
//
//             var info9 = new CacheablePropertyInfoValues<UserRoleType>
//             {
//                 DeclaringType = typeof(Sample),
//                 DefaultCreator = () => default(UserRoleType),
//                 Getter = obj => ((Sample)obj).EnumProp,
//                 Setter = (obj, value) => ((Sample)obj).EnumProp = value!,
//                 Generator = value => (RedisValue)(int)value,
//                 Parser = value => (UserRoleType)(int)value,
//                 Converter = null,
//                 PropertyName = PropName_Nested,
//             };
//
//             properties[9] = CacheableMetadataServices.CreatePropertyInfo<UserRoleType>(info9);
//
//             var info10 = new CacheablePropertyInfoValues<JsonDocument>
//             {
//                 DeclaringType = typeof(Sample),
//                 DefaultCreator = () => default(JsonDocument),
//                 Getter = obj => ((Sample)obj).JsonDocumentProp,
//                 Setter = (obj, value) => ((Sample)obj).JsonDocumentProp = value!,
//                 Generator = value => (RedisValue)value.RootElement.GetBytesFromBase64(),
//                 Parser = value => JsonDocument.Parse((ReadOnlyMemory<byte>)value),
//                 Converter = null,
//                 PropertyName = PropName_Nested,
//             };
//
//             properties[10] = CacheableMetadataServices.CreatePropertyInfo<JsonDocument>(info10);
//
//             return properties;
//         }
//     }
//
//     [Cacheable(typeof(List<Sample>))]
//     [Cacheable(typeof(Sample))]
//     [Cacheable(typeof(Dictionary<string, Sample>))]
//     public class Sample
//     {
//         public int Id { get; set; }
//         public string FirstName { get; set; }
//         public string LastName { get; set; }
//         public string? Email { get; set; }
//         public string? Mobile { get; set; }
//         public UserRoleType[] Roles { get; set; }
//         public string[] Tags { get; set; }
//         public Dictionary<string, string> Data { get; set; }
//         public List<Nested> Nested { get; set; }
//         public JsonDocument? JsonDocumentProp { get; set; }
//         public UserRoleType EnumProp { get; set; }
//     }
//
//     public class Nested
//     {
//     }
//
//     public enum UserRoleType
//     {
//         User = 0,
//         Admin = 1,
//     }
// }