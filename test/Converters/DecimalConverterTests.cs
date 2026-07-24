using FluentAssertions;

namespace R8.RedisHashMap.Tests.Converters;

public class DecimalConverterTests
{
    private readonly DecimalConverter _converter = new();

    [Theory]
    [InlineData(0)]
    [InlineData(100.50)]
    [InlineData(-100.50)]
    [InlineData(9999999.99)]
    [InlineData(0.01)]
    [InlineData(0.001)]
    [InlineData(123456789.123456789)]
    public void GetBytes_ShouldConvertDecimalToRedisValue(decimal value)
    {
        // Act
        var redisValue = _converter.GetBytes(value);

        // Assert
        redisValue.IsNullOrEmpty.Should().BeFalse();
        ((string)redisValue)!.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100.50)]
    [InlineData(-100.50)]
    [InlineData(9999999.99)]
    [InlineData(0.01)]
    [InlineData(0.001)]
    [InlineData(123456789.123456789)]
    public void Parse_ShouldConvertRedisValueToDecimal(decimal expectedValue)
    {
        // Arrange
        var redisValue = _converter.GetBytes(expectedValue);

        // Act
        var actualValue = _converter.Parse(redisValue);

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveDecimalPrecision()
    {
        // Arrange
        var testValues = new[]
        {
            decimal.Zero,
            decimal.One,
            decimal.MinusOne,
            100.50m,
            -100.50m,
            0.123456789m,
            decimal.MaxValue / 2, // Use half to avoid overflow in some scenarios
            decimal.MinValue / 2
        };

        foreach (var originalValue in testValues)
        {
            // Act
            var redisValue = _converter.GetBytes(originalValue);
            var roundTripValue = _converter.Parse(redisValue);

            // Assert
            roundTripValue.Should().Be(originalValue,
                $"round-trip conversion should preserve value {originalValue}");
        }
    }

    [Fact]
    public void GetBytes_ShouldUseInvariantCulture()
    {
        // Arrange
        var value = 1234.56m;

        // Act
        var redisValue = _converter.GetBytes(value);
        var stringValue = (string)redisValue!;

        // Assert
        // Should use dot as decimal separator regardless of current culture
        stringValue.Should().Contain(".");
        stringValue.Should().NotContain(",");
    }
}