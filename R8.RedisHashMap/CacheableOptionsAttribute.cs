using System;

namespace R8.RedisHashMap
{
    /// <summary>
    /// An attribute to define options for objects implementing <see cref="ICacheable"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class CacheableOptionsAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether to include fields or not.
        /// </summary>
        public bool IncludeFields { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include private fields/properties or not.
        /// </summary>
        public bool IncludePrivate { get; set; }
    }
}