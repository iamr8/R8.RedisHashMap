using Bogus;
using FluentAssertions;
using R8.RedisHashMap.Tests.Models;

namespace R8.RedisHashMap.Tests;

public class AdvancedTypesTests
{
    private readonly Faker<AdvancedTestModel> _advancedFaker;

    public AdvancedTypesTests()
    {
        _advancedFaker = new Faker<AdvancedTestModel>()
            .RuleFor(a => a.Id, f => f.IndexFaker)
            .RuleFor(a => a.Name, f => f.Commerce.ProductName())
            .RuleFor(a => a.RegistrationDate, f => f.Date.Past())
            .RuleFor(a => a.LastLoginDate, f => f.Random.Bool() ? f.Date.Recent() : null)
            .RuleFor(a => a.Duration, f => TimeSpan.FromMinutes(f.Random.Int(1, 120)))
            .RuleFor(a => a.DurationNullable, f => f.Random.Bool() ? TimeSpan.FromSeconds(f.Random.Int(1, 3600)) : null)
            .RuleFor(a => a.Names, f => f.Make(3, () => f.Name.FullName()).ToList())
            .RuleFor(a => a.Keys, f => f.Random.Bytes(16))
            .RuleFor(a => a.Enumerable, f => f.Random.Bytes(8))
            .RuleFor(a => a.RawData, f => new ReadOnlyMemory<byte>(f.Random.Bytes(32)))
            .RuleFor(a => a.StringResult, f => new Result<string> { Value = f.Lorem.Sentence() })
            .RuleFor(a => a.IntResult, f => new Result<int> { Value = f.Random.Int(1, 1000) })
            .RuleFor(a => a.NestedStruct, f => new Nested { Id = f.Random.Long(1, 10000), Name = f.Name.FirstName() })
            .RuleFor(a => a.NullableNestedStruct, f => f.Random.Bool()
                ? new Nested { Id = f.Random.Long(1, 10000), Name = f.Name.FirstName() }
                : null)
            .RuleFor(a => a.Price, f => f.Finance.Amount(1))
            .RuleFor(a => a.OptionalPrice, f => f.Random.Bool() ? f.Finance.Amount(1, 100) : null)
            .RuleFor(a => a.Scores, f => new Dictionary<string, int>
            {
                { "math", f.Random.Int(0, 100) },
                { "science", f.Random.Int(0, 100) },
                { "english", f.Random.Int(0, 100) }
            })
            .RuleFor(a => a.IdToName, f => new Dictionary<int, string>
            {
                { 1, f.Name.FirstName() },
                { 2, f.Name.FirstName() },
                { 3, f.Name.FirstName() }
            });
    }

    [Fact]
    public void GetHashEntries_AdvancedModel_ShouldConvertSuccessfully()
    {
        // Arrange
        var model = _advancedFaker.Generate();

        // Act
        var hashEntries = TestCacheContext.Default.AdvancedTestModel.GetHashEntries(model);

        // Assert
        hashEntries.Should().NotBeNull();
        hashEntries.Should().NotBeEmpty();

        var entryDict = hashEntries.ToDictionary(e => e.Name.ToString(), e => e.Value);
        entryDict.Should().ContainKey("id");
        entryDict.Should().ContainKey("name");
        entryDict.Should().ContainKey("registrationDate");
    }

    [Fact]
    public void FromHashEntries_AdvancedModel_ShouldDeserializeSuccessfully()
    {
        // Arrange
        var original = _advancedFaker.Generate();
        var hashEntries = TestCacheContext.Default.AdvancedTestModel.GetHashEntries(original);

        // Act
        var deserialized = TestCacheContext.Default.AdvancedTestModel.FromHashEntries(hashEntries);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Id.Should().Be(original.Id);
        deserialized.Name.Should().Be(original.Name);
        deserialized.RegistrationDate.Should().BeCloseTo(original.RegistrationDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RoundTrip_TimeSpanProperties_ShouldPreserveValues()
    {
        // Arrange
        var model = new AdvancedTestModel
        {
            Id = 1,
            Name = "Test",
            RegistrationDate = DateTime.UtcNow,
            Duration = TimeSpan.FromMinutes(45),
            DurationNullable = TimeSpan.FromHours(2.5),
            StringResult = new Result<string> { Value = "test" },
            IntResult = new Result<int> { Value = 42 },
            NestedStruct = new Nested { Id = 100, Name = "Nested" },
            Price = 99.99m
        };

        // Act
        var hashEntries = TestCacheContext.Default.AdvancedTestModel.GetHashEntries(model);
        var roundTrip = TestCacheContext.Default.AdvancedTestModel.FromHashEntries(hashEntries);

        // Assert
        roundTrip.Duration.Should().Be(model.Duration);
        roundTrip.DurationNullable.Should().Be(model.DurationNullable);
    }

    [Fact]
    public void RoundTrip_DecimalProperties_ShouldPreservePrecision()
    {
        // Arrange
        var model = new AdvancedTestModel
        {
            Id = 1,
            Name = "Test",
            RegistrationDate = DateTime.UtcNow,
            Price = 123.456789m,
            OptionalPrice = 987.654321m,
            StringResult = new Result<string> { Value = "test" },
            IntResult = new Result<int> { Value = 42 },
            NestedStruct = new Nested { Id = 100, Name = "Nested" }
        };

        // Act
        var hashEntries = TestCacheContext.Default.AdvancedTestModel.GetHashEntries(model);
        var roundTrip = TestCacheContext.Default.AdvancedTestModel.FromHashEntries(hashEntries);

        // Assert
        roundTrip.Price.Should().Be(model.Price);
        roundTrip.OptionalPrice.Should().Be(model.OptionalPrice);
    }

    [Fact]
    public void RoundTrip_Collections_ShouldPreserveData()
    {
        // Arrange
        var model = new AdvancedTestModel
        {
            Id = 1,
            Name = "Test",
            RegistrationDate = DateTime.UtcNow,
            Names = new List<string> { "Alice", "Bob", "Charlie" },
            Keys = new byte[] { 1, 2, 3, 4, 5 },
            Scores = new Dictionary<string, int> { { "math", 95 }, { "science", 87 } },
            IdToName = new Dictionary<int, string> { { 1, "John" }, { 2, "Jane" } },
            StringResult = new Result<string> { Value = "test" },
            IntResult = new Result<int> { Value = 42 },
            NestedStruct = new Nested { Id = 100, Name = "Nested" }
        };

        // Act
        var hashEntries = TestCacheContext.Default.AdvancedTestModel.GetHashEntries(model);
        var roundTrip = TestCacheContext.Default.AdvancedTestModel.FromHashEntries(hashEntries);

        // Assert
        roundTrip.Names.Should().BeEquivalentTo(model.Names);
        roundTrip.Keys.Should().BeEquivalentTo(model.Keys);
        roundTrip.Scores.Should().BeEquivalentTo(model.Scores);
        roundTrip.IdToName.Should().BeEquivalentTo(model.IdToName);
    }

    [Fact]
    public void RoundTrip_StructTypes_ShouldPreserveValues()
    {
        // Arrange
        var model = new AdvancedTestModel
        {
            Id = 1,
            Name = "Test",
            RegistrationDate = DateTime.UtcNow,
            NestedStruct = new Nested { Id = 999, Name = "TestNested" },
            NullableNestedStruct = new Nested { Id = 888, Name = "NullableNested" },
            StringResult = new Result<string> { Value = "test" },
            IntResult = new Result<int> { Value = 42 }
        };

        // Act
        var hashEntries = TestCacheContext.Default.AdvancedTestModel.GetHashEntries(model);
        var roundTrip = TestCacheContext.Default.AdvancedTestModel.FromHashEntries(hashEntries);

        // Assert
        roundTrip.NestedStruct.Id.Should().Be(model.NestedStruct.Id);
        roundTrip.NestedStruct.Name.Should().Be(model.NestedStruct.Name);
        roundTrip.NullableNestedStruct.Should().NotBeNull();
        roundTrip.NullableNestedStruct!.Value.Id.Should().Be(model.NullableNestedStruct!.Value.Id);
    }

    [Fact]
    public void RoundTrip_GenericTypes_ShouldPreserveValues()
    {
        // Arrange
        var model = new AdvancedTestModel
        {
            Id = 1,
            Name = "Test",
            RegistrationDate = DateTime.UtcNow,
            StringResult = new Result<string> { Value = "Hello World" },
            IntResult = new Result<int> { Value = 12345 },
            NestedStruct = new Nested { Id = 100, Name = "Nested" }
        };

        // Act
        var hashEntries = TestCacheContext.Default.AdvancedTestModel.GetHashEntries(model);
        var roundTrip = TestCacheContext.Default.AdvancedTestModel.FromHashEntries(hashEntries);

        // Assert
        roundTrip.StringResult.Should().NotBeNull();
        roundTrip.StringResult.Value.Should().Be(model.StringResult.Value);
        roundTrip.IntResult.Should().NotBeNull();
        roundTrip.IntResult.Value.Should().Be(model.IntResult.Value);
    }

    [Fact]
    public void RoundTrip_MemoryTypes_ShouldPreserveData()
    {
        // Arrange
        var rawBytes = new byte[] { 10, 20, 30, 40, 50 };
        var model = new AdvancedTestModel
        {
            Id = 1,
            Name = "Test",
            RegistrationDate = DateTime.UtcNow,
            RawData = new ReadOnlyMemory<byte>(rawBytes),
            Enumerable = new byte[] { 60, 70, 80 },
            StringResult = new Result<string> { Value = "test" },
            IntResult = new Result<int> { Value = 42 },
            NestedStruct = new Nested { Id = 100, Name = "Nested" }
        };

        // Act
        var hashEntries = TestCacheContext.Default.AdvancedTestModel.GetHashEntries(model);
        var roundTrip = TestCacheContext.Default.AdvancedTestModel.FromHashEntries(hashEntries);

        // Assert
        roundTrip.RawData.ToArray().Should().BeEquivalentTo(rawBytes);
        roundTrip.Enumerable.Should().BeEquivalentTo(model.Enumerable);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void GetHashEntries_MultipleAdvancedModels_ShouldHandleLoad(int count)
    {
        // Arrange
        var models = _advancedFaker.Generate(count);

        // Act
        var results = models.Select(m =>
            TestCacheContext.Default.AdvancedTestModel.GetHashEntries(m)
        ).ToList();

        // Assert
        results.Should().HaveCount(count);
        results.Should().OnlyContain(entries => entries.Length > 0);
    }

    [Fact]
    public void GetHashEntries_NullableProperties_ShouldHandleNulls()
    {
        // Arrange
        var model = new AdvancedTestModel
        {
            Id = 1,
            Name = "Test",
            RegistrationDate = DateTime.UtcNow,
            LastLoginDate = null,
            DurationNullable = null,
            NullableNestedStruct = null,
            OptionalPrice = null,
            StringResult = new Result<string> { Value = "test" },
            IntResult = new Result<int> { Value = 42 },
            NestedStruct = new Nested { Id = 100, Name = "Nested" }
        };

        // Act
        var hashEntries = TestCacheContext.Default.AdvancedTestModel.GetHashEntries(model);
        var roundTrip = TestCacheContext.Default.AdvancedTestModel.FromHashEntries(hashEntries);

        // Assert
        roundTrip.LastLoginDate.Should().BeNull();
        roundTrip.DurationNullable.Should().BeNull();
        roundTrip.NullableNestedStruct.Should().BeNull();
        roundTrip.OptionalPrice.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_ComplexScenario_ShouldMaintainIntegrity()
    {
        // Arrange - Create a fully populated model
        var model = _advancedFaker.Generate();

        // Act
        var hashEntries = TestCacheContext.Default.AdvancedTestModel.GetHashEntries(model);
        var roundTrip = TestCacheContext.Default.AdvancedTestModel.FromHashEntries(hashEntries);

        // Assert - Verify critical properties
        roundTrip.Id.Should().Be(model.Id);
        roundTrip.Name.Should().Be(model.Name);
        roundTrip.Price.Should().Be(model.Price);
        roundTrip.NestedStruct.Id.Should().Be(model.NestedStruct.Id);
        roundTrip.StringResult.Value.Should().Be(model.StringResult.Value);
        roundTrip.IntResult.Value.Should().Be(model.IntResult.Value);
    }
}