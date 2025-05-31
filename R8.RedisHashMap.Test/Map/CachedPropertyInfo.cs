using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace R8.RedisHashMap.Test.Map
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class CachedPropertyInfo : IEquatable<CachedPropertyInfo>
    {
        public CachedPropertyInfo(string name, PropertyInfo property)
        {
            this.Name = name;
            this.Property = property;
            this.IsRequired = property.GetCustomAttribute<RequiredAttribute>() != null;
            this.HasSetMethod = property.GetSetMethod() != null;
            this.PropertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            this.HasJsonIgnore = property.GetCustomAttribute<JsonIgnoreAttribute>() != null;
        }

        public string Name { get; }
        public PropertyInfo Property { get; }
        public bool IsRequired { get; }
        public bool HasSetMethod { get; }
        public bool HasJsonIgnore { get; set; }
        public Type PropertyType { get; }

        public bool Equals(CachedPropertyInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Property, other.Property);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CachedPropertyInfo)obj);
        }

        public override int GetHashCode()
        {
            return (Property != null ? Property.GetHashCode() : 0);
        }

        public static bool operator ==(CachedPropertyInfo left, CachedPropertyInfo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CachedPropertyInfo left, CachedPropertyInfo right)
        {
            return !Equals(left, right);
        }
    }
}