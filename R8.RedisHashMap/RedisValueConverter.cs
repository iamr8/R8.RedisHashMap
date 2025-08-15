using StackExchange.Redis;

namespace R8.RedisHashMap
{
    /// <summary>
    ///     Represents an abstract base class for converting values between
    ///     application-defined types and Redis-compatible representations.
    /// </summary>
    public abstract class RedisValueConverter
    {
    }

    /// <summary>
    ///     Serves as the base class for converting application-defined types to and from
    ///     Redis-compatible values, enabling seamless interaction with Redis data structures.
    /// </summary>
    /// <typeparam name="T">The type of the value to be converted.</typeparam>
    public abstract class RedisValueConverter<T> : RedisValueConverter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RedisValueConverter{T}" /> class.
        /// </summary>
        public RedisValueConverter()
        {
        }

        /// <summary>
        ///     Converts a value of type <typeparamref name="T" /> to a Redis-compatible <see cref="RedisValue" />.
        /// </summary>
        /// <param name="value">The value of type <typeparamref name="T" /> to be converted to a Redis-compatible format.</param>
        /// <returns>A <see cref="RedisValue" /> representation of the given value.</returns>
        public abstract RedisValue GetBytes(T value);

        /// <summary>
        ///     Converts a given <see cref="RedisValue" /> to its corresponding value of type <typeparamref name="T" />.
        /// </summary>
        /// <param name="value">The <see cref="RedisValue" /> to be converted to a value of type <typeparamref name="T" />.</param>
        /// <returns>The converted value of type <typeparamref name="T" />.</returns>
        public abstract T Parse(RedisValue value);
    }
}