namespace R8.RedisHashMap
{
    public static class CacheableMetadataServices
    {
        public static CacheablePropertyInfo<T> CreatePropertyInfo<T>(CacheablePropertyInfoValues<T> propertyInfoValues)
        {
            var propertyInfo = new CacheablePropertyInfo<T>(propertyInfoValues.DeclaringType, propertyInfoValues.ValueType)
            {
                PropertyName = propertyInfoValues.PropertyName,
                Get = propertyInfoValues.Getter!,
                Set = propertyInfoValues.Setter,
                SetDefault = propertyInfoValues.DefaultCreator,
                Converter = propertyInfoValues.Converter,
                Generate = propertyInfoValues.Generator,
                Parse = propertyInfoValues.Parser,
            };

            return propertyInfo;
        }
    }
}