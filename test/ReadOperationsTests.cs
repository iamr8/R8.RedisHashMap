using System.Collections.Concurrent;
using System.Text.Json;
using Bogus;
using FluentAssertions;
using R8.RedisHashMap.Tests.Models;
using StackExchange.Redis;

namespace R8.RedisHashMap.Tests;

public class ReadOperationsTests
{
    private readonly Faker<TestProduct> _productFaker;
    private readonly Faker<TestSession> _sessionFaker;
    private readonly Faker<TestUser> _userFaker;

    public ReadOperationsTests()
    {
        _userFaker = new Faker<TestUser>()
            .RuleFor(u => u.Id, f => f.IndexFaker)
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Age, f => f.Random.Int(18, 80))
            .RuleFor(u => u.CreatedAt, f => f.Date.Past(2))
            .RuleFor(u => u.IsActive, f => f.Random.Bool())
            .RuleFor(u => u.Balance, f => f.Finance.Amount(0, 10000))
            .RuleFor(u => u.Tags, f => f.Make(3, () => f.Lorem.Word()).ToArray())
            .RuleFor(u => u.Metadata, f => new Dictionary<string, string>
            {
                { "city", f.Address.City() },
                { "country", f.Address.Country() }
            });

        _productFaker = new Faker<TestProduct>()
            .RuleFor(p => p.Id, f => f.Random.Guid())
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
            .RuleFor(p => p.Price, f => f.Finance.Amount(1))
            .RuleFor(p => p.StockQuantity, f => f.Random.Int(0, 500))
            .RuleFor(p => p.Category, f => f.Commerce.Categories(1)[0])
            .RuleFor(p => p.LastRestocked, f => f.Date.Recent(30))
            .RuleFor(p => p.Rating, f => f.Random.Double(1, 5))
            .RuleFor(p => p.IsAvailable, f => f.Random.Bool())
            .RuleFor(p => p.ImageData, f => f.Random.Bytes(100))
            .RuleFor(p => p.Specifications, f => JsonDocument.Parse($$"""{"cpu": "{{f.Random.AlphaNumeric(10)}}", "ram": {{f.Random.Int(4, 64)}}}"""))
            .RuleFor(p => p.RelatedProducts, f => f.Make(3, () => f.Commerce.Product()))
            .RuleFor(p => p.VariantMap, f => new Dictionary<int, string>
            {
                { 1, f.Commerce.Color() },
                { 2, f.Commerce.Color() }
            })
            .RuleFor(p => p.Thumbnail, f => new ReadOnlyMemory<byte>(f.Random.Bytes(64)))
            .RuleFor(p => p.StockResult, f => new Result<int> { Value = f.Random.Int(0, 1000) })
            .RuleFor(p => p.ProductCode, f => new Nested { Id = f.Random.Long(1000, 9999), Name = f.Commerce.Ean8() });

        _sessionFaker = new Faker<TestSession>()
            .RuleFor(s => s.SessionId, f => f.Random.Guid().ToString())
            .RuleFor(s => s.UserId, f => f.Random.Int(1, 10000))
            .RuleFor(s => s.StartTime, f => f.Date.Recent())
            .RuleFor(s => s.EndTime, f => f.Date.Recent(0))
            .RuleFor(s => s.Duration, f => TimeSpan.FromMinutes(f.Random.Int(1, 120)))
            .RuleFor(s => s.IpAddress, f => f.Internet.Ip())
            .RuleFor(s => s.UserAgent, f => f.Internet.UserAgent())
            .RuleFor(s => s.IsExpired, f => f.Random.Bool());
    }

    [Fact]
    public void FromHashEntries_SingleUser_ShouldDeserializeSuccessfully()
    {
        // Arrange
        var originalUser = _userFaker.Generate();
        var hashEntries = TestCacheContext.Default.TestUser.GetHashEntries(originalUser);

        // Act
        var deserializedUser = TestCacheContext.Default.TestUser.FromHashEntries(hashEntries);

        // Assert
        deserializedUser.Should().NotBeNull();
        deserializedUser.Id.Should().Be(originalUser.Id);
        deserializedUser.FirstName.Should().Be(originalUser.FirstName);
        deserializedUser.LastName.Should().Be(originalUser.LastName);
        deserializedUser.Email.Should().Be(originalUser.Email);
        deserializedUser.Age.Should().Be(originalUser.Age);
        deserializedUser.IsActive.Should().Be(originalUser.IsActive);
        deserializedUser.Balance.Should().Be(originalUser.Balance);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void FromHashEntries_MultipleUsers_ShouldHandleLoad(int count)
    {
        // Arrange
        var originalUsers = _userFaker.Generate(count);
        var hashEntriesList = originalUsers
            .Select(u => new { Original = u, Entries = TestCacheContext.Default.TestUser.GetHashEntries(u) })
            .ToList();

        // Act
        var deserializedUsers = hashEntriesList
            .Select(x => TestCacheContext.Default.TestUser.FromHashEntries(x.Entries))
            .ToList();

        // Assert
        deserializedUsers.Should().HaveCount(count);

        for (var i = 0; i < count; i++)
        {
            deserializedUsers[i].Should().NotBeNull();
            deserializedUsers[i].Id.Should().Be(originalUsers[i].Id);
            deserializedUsers[i].Email.Should().Be(originalUsers[i].Email);
        }
    }

    [Fact]
    public void RoundTrip_User_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var originalUser = _userFaker.Generate();

        // Act - Write
        var hashEntries = TestCacheContext.Default.TestUser.GetHashEntries(originalUser);

        // Act - Read
        var roundTrippedUser = TestCacheContext.Default.TestUser.FromHashEntries(hashEntries);

        // Assert
        roundTrippedUser.Should().NotBeNull();
        roundTrippedUser.Id.Should().Be(originalUser.Id);
        roundTrippedUser.FirstName.Should().Be(originalUser.FirstName);
        roundTrippedUser.LastName.Should().Be(originalUser.LastName);
        roundTrippedUser.Email.Should().Be(originalUser.Email);
        roundTrippedUser.Age.Should().Be(originalUser.Age);
        roundTrippedUser.IsActive.Should().Be(originalUser.IsActive);
        roundTrippedUser.Balance.Should().Be(originalUser.Balance);
        roundTrippedUser.CreatedAt.Should().BeCloseTo(originalUser.CreatedAt, TimeSpan.FromMilliseconds(1));

        // Complex types
        if (originalUser.Tags?.Length > 0) roundTrippedUser.Tags.Should().BeEquivalentTo(originalUser.Tags);

        if (originalUser.Metadata?.Count > 0) roundTrippedUser.Metadata.Should().BeEquivalentTo(originalUser.Metadata);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(500)]
    public void RoundTrip_MultipleUsers_ShouldPreserveAllData(int count)
    {
        // Arrange
        var originalUsers = _userFaker.Generate(count);

        // Act
        var results = originalUsers.Select(original =>
        {
            var entries = TestCacheContext.Default.TestUser.GetHashEntries(original);
            var deserialized = TestCacheContext.Default.TestUser.FromHashEntries(entries);
            return new { Original = original, Deserialized = deserialized };
        }).ToList();

        // Assert
        results.Should().HaveCount(count);

        foreach (var result in results)
        {
            result.Deserialized.Should().NotBeNull();
            result.Deserialized.Id.Should().Be(result.Original.Id);
            result.Deserialized.FirstName.Should().Be(result.Original.FirstName);
            result.Deserialized.LastName.Should().Be(result.Original.LastName);
            result.Deserialized.Email.Should().Be(result.Original.Email);
            result.Deserialized.Age.Should().Be(result.Original.Age);
            result.Deserialized.Balance.Should().Be(result.Original.Balance);
        }
    }

    [Fact]
    public void RoundTrip_Product_ShouldHandleGuidAndComplexTypes()
    {
        // Arrange
        var originalProduct = _productFaker.Generate();

        // Act
        var hashEntries = TestCacheContext.Default.TestProduct.GetHashEntries(originalProduct);
        var roundTrippedProduct = TestCacheContext.Default.TestProduct.FromHashEntries(hashEntries);

        // Assert
        roundTrippedProduct.Should().NotBeNull();
        roundTrippedProduct.Id.Should().Be(originalProduct.Id);
        roundTrippedProduct.Name.Should().Be(originalProduct.Name);
        roundTrippedProduct.Description.Should().Be(originalProduct.Description);
        roundTrippedProduct.Price.Should().Be(originalProduct.Price);
        roundTrippedProduct.StockQuantity.Should().Be(originalProduct.StockQuantity);
        roundTrippedProduct.Category.Should().Be(originalProduct.Category);
        roundTrippedProduct.Rating.Should().Be(originalProduct.Rating);
        roundTrippedProduct.IsAvailable.Should().Be(originalProduct.IsAvailable);

        if (originalProduct.ImageData?.Length > 0) roundTrippedProduct.ImageData.Should().BeEquivalentTo(originalProduct.ImageData);

        if (originalProduct.LastRestocked.HasValue)
            roundTrippedProduct.LastRestocked.Should().BeCloseTo(
                originalProduct.LastRestocked.Value,
                TimeSpan.FromMilliseconds(1)
            );

        // Test advanced properties
        if (originalProduct.Specifications != null)
        {
            roundTrippedProduct.Specifications.Should().NotBeNull();
            roundTrippedProduct.Specifications!.RootElement.ToString()
                .Should().Be(originalProduct.Specifications.RootElement.ToString());
        }

        if (originalProduct.RelatedProducts != null && originalProduct.RelatedProducts.Count > 0)
        {
            roundTrippedProduct.RelatedProducts.Should().NotBeNull();
            roundTrippedProduct.RelatedProducts.Should().BeEquivalentTo(originalProduct.RelatedProducts);
        }

        if (originalProduct.VariantMap != null && originalProduct.VariantMap.Count > 0)
        {
            roundTrippedProduct.VariantMap.Should().NotBeNull();
            roundTrippedProduct.VariantMap.Should().BeEquivalentTo(originalProduct.VariantMap);
        }

        if (originalProduct.Thumbnail.Length > 0) roundTrippedProduct.Thumbnail.ToArray().Should().BeEquivalentTo(originalProduct.Thumbnail.ToArray());

        if (originalProduct.StockResult != null)
        {
            roundTrippedProduct.StockResult.Should().NotBeNull();
            roundTrippedProduct.StockResult!.Value.Should().Be(originalProduct.StockResult.Value);
        }

        if (originalProduct.ProductCode != null)
        {
            roundTrippedProduct.ProductCode.Should().NotBeNull();
            roundTrippedProduct.ProductCode!.Value.Id.Should().Be(originalProduct.ProductCode.Value.Id);
            roundTrippedProduct.ProductCode!.Value.Name.Should().Be(originalProduct.ProductCode.Value.Name);
        }
    }

    [Fact]
    public void RoundTrip_Session_ShouldHandleTimeSpanConversion()
    {
        // Arrange
        var originalSession = _sessionFaker.Generate();

        // Act
        var hashEntries = TestCacheContext.Default.TestSession.GetHashEntries(originalSession);
        var roundTrippedSession = TestCacheContext.Default.TestSession.FromHashEntries(hashEntries);

        // Assert
        roundTrippedSession.Should().NotBeNull();
        roundTrippedSession.SessionId.Should().Be(originalSession.SessionId);
        roundTrippedSession.UserId.Should().Be(originalSession.UserId);
        roundTrippedSession.IpAddress.Should().Be(originalSession.IpAddress);
        roundTrippedSession.UserAgent.Should().Be(originalSession.UserAgent);
        roundTrippedSession.IsExpired.Should().Be(originalSession.IsExpired);

        // TimeSpan should be preserved (with millisecond precision)
        roundTrippedSession.Duration.TotalMilliseconds.Should().Be(originalSession.Duration.TotalMilliseconds);

        roundTrippedSession.StartTime.Should().BeCloseTo(originalSession.StartTime, TimeSpan.FromMilliseconds(1));

        if (originalSession.EndTime.HasValue)
            roundTrippedSession.EndTime.Should().BeCloseTo(
                originalSession.EndTime.Value,
                TimeSpan.FromMilliseconds(1)
            );
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    public void RoundTrip_MixedTypes_ShouldMaintainIntegrity(int countPerType)
    {
        // Arrange
        var users = _userFaker.Generate(countPerType);
        var products = _productFaker.Generate(countPerType);
        var sessions = _sessionFaker.Generate(countPerType);

        // Act - Users
        var userResults = users.Select(u =>
        {
            var entries = TestCacheContext.Default.TestUser.GetHashEntries(u);
            var deserialized = TestCacheContext.Default.TestUser.FromHashEntries(entries);
            return new { Original = u, Deserialized = deserialized };
        }).ToList();

        // Act - Products
        var productResults = products.Select(p =>
        {
            var entries = TestCacheContext.Default.TestProduct.GetHashEntries(p);
            var deserialized = TestCacheContext.Default.TestProduct.FromHashEntries(entries);
            return new { Original = p, Deserialized = deserialized };
        }).ToList();

        // Act - Sessions
        var sessionResults = sessions.Select(s =>
        {
            var entries = TestCacheContext.Default.TestSession.GetHashEntries(s);
            var deserialized = TestCacheContext.Default.TestSession.FromHashEntries(entries);
            return new { Original = s, Deserialized = deserialized };
        }).ToList();

        // Assert - Users
        foreach (var result in userResults)
        {
            result.Deserialized.Email.Should().Be(result.Original.Email);
            result.Deserialized.Id.Should().Be(result.Original.Id);
        }

        // Assert - Products
        foreach (var result in productResults)
        {
            result.Deserialized.Id.Should().Be(result.Original.Id);
            result.Deserialized.Name.Should().Be(result.Original.Name);
        }

        // Assert - Sessions
        foreach (var result in sessionResults)
        {
            result.Deserialized.SessionId.Should().Be(result.Original.SessionId);
            result.Deserialized.Duration.TotalMilliseconds.Should().Be(result.Original.Duration.TotalMilliseconds);
        }
    }

    [Fact]
    public void FromHashEntries_EmptyArray_ShouldReturnNull()
    {
        // Arrange
        var emptyEntries = Array.Empty<HashEntry>();

        // Act
        var result = TestCacheContext.Default.TestUser.FromHashEntries(emptyEntries);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var users = _userFaker.Generate(100);
        var results = new ConcurrentBag<bool>();

        // Act
        Parallel.ForEach(users, user =>
        {
            var entries = TestCacheContext.Default.TestUser.GetHashEntries(user);
            var deserialized = TestCacheContext.Default.TestUser.FromHashEntries(entries);

            // Verify data integrity
            var isValid = deserialized != null &&
                          deserialized.Id == user.Id &&
                          deserialized.Email == user.Email;

            results.Add(isValid);
        });

        // Assert
        results.Should().HaveCount(100);
        results.Should().OnlyContain(isValid => isValid);
    }

    [Fact]
    public void RoundTrip_SpecialCharacters_ShouldBePreserved()
    {
        // Arrange
        var user = new TestUser
        {
            Id = 1,
            FirstName = "João",
            LastName = "O'Brien",
            Email = "test+special@example.com",
            Age = 30,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Balance = 1234.56m,
            Tags = new[] { "tag-1", "tag_2", "tag.3", "tag@4" },
            Metadata = new Dictionary<string, string>
            {
                { "key-with-dash", "value" },
                { "key_with_underscore", "value" },
                { "unicode", "日本語" }
            }
        };

        // Act
        var entries = TestCacheContext.Default.TestUser.GetHashEntries(user);
        var deserialized = TestCacheContext.Default.TestUser.FromHashEntries(entries);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.FirstName.Should().Be("João");
        deserialized.LastName.Should().Be("O'Brien");
        deserialized.Email.Should().Be("test+special@example.com");
        deserialized.Tags.Should().BeEquivalentTo(user.Tags);
        deserialized.Metadata["unicode"].Should().Be("日本語");
    }

    [Fact]
    public void RoundTrip_ProductAdvancedTypes_ShouldPreserveAllComplexTypes()
    {
        // Arrange - Create a product with all advanced types populated
        var originalProduct = new TestProduct
        {
            Id = Guid.NewGuid(),
            Name = "Advanced Test Product",
            Description = "Test Description with special chars: <>\"'&",
            Category = "Electronics",
            Price = 999.99m,
            StockQuantity = 150,
            LastRestocked = DateTime.UtcNow,
            Rating = 4.8,
            IsAvailable = true,
            ImageData = new byte[] { 0x01, 0x02, 0x03, 0xFF, 0xFE },
            Specifications = JsonDocument.Parse("""
                                                {
                                                    "cpu": "Intel i9-13900K",
                                                    "ram": 32,
                                                    "storage": "2TB NVMe SSD",
                                                    "features": ["WiFi 6E", "Bluetooth 5.3", "Thunderbolt 4"]
                                                }
                                                """),
            RelatedProducts = new List<string>
            {
                "Product-A",
                "Product-B",
                "Product-C with spaces",
                "Product_D"
            },
            VariantMap = new Dictionary<int, string>
            {
                { 1, "Midnight Black" },
                { 2, "Silver" },
                { 3, "Deep Purple" },
                { 100, "Limited Edition Gold" }
            },
            Thumbnail = new ReadOnlyMemory<byte>(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 }),
            StockResult = new Result<int> { Value = 42 },
            ProductCode = new Nested { Id = 987654321, Name = "SKU-ADVANCED-2025" }
        };

        // Act
        var hashEntries = TestCacheContext.Default.TestProduct.GetHashEntries(originalProduct);
        var roundTrippedProduct = TestCacheContext.Default.TestProduct.FromHashEntries(hashEntries);

        // Assert - Basic properties
        roundTrippedProduct.Should().NotBeNull();
        roundTrippedProduct.Id.Should().Be(originalProduct.Id);
        roundTrippedProduct.Name.Should().Be(originalProduct.Name);
        roundTrippedProduct.Description.Should().Be(originalProduct.Description);
        roundTrippedProduct.Category.Should().Be(originalProduct.Category);
        roundTrippedProduct.Price.Should().Be(originalProduct.Price);
        roundTrippedProduct.StockQuantity.Should().Be(originalProduct.StockQuantity);
        roundTrippedProduct.Rating.Should().Be(originalProduct.Rating);
        roundTrippedProduct.IsAvailable.Should().Be(originalProduct.IsAvailable);

        // Assert - DateTime
        roundTrippedProduct.LastRestocked.Should().NotBeNull();
        roundTrippedProduct.LastRestocked!.Value.Should().BeCloseTo(
            originalProduct.LastRestocked!.Value,
            TimeSpan.FromMilliseconds(1)
        );

        // Assert - Byte array
        roundTrippedProduct.ImageData.Should().BeEquivalentTo(originalProduct.ImageData);

        // Assert - JsonDocument
        roundTrippedProduct.Specifications.Should().NotBeNull();
        var originalJson = originalProduct.Specifications!.RootElement.ToString();
        var roundTrippedJson = roundTrippedProduct.Specifications!.RootElement.ToString();
        roundTrippedJson.Should().Be(originalJson);

        // Assert - List<string>
        roundTrippedProduct.RelatedProducts.Should().NotBeNull();
        roundTrippedProduct.RelatedProducts.Should().HaveCount(4);
        roundTrippedProduct.RelatedProducts.Should().BeEquivalentTo(originalProduct.RelatedProducts);

        // Assert - Dictionary<int, string>
        roundTrippedProduct.VariantMap.Should().NotBeNull();
        roundTrippedProduct.VariantMap.Should().HaveCount(4);
        roundTrippedProduct.VariantMap.Should().BeEquivalentTo(originalProduct.VariantMap);

        // Assert - ReadOnlyMemory<byte>
        roundTrippedProduct.Thumbnail.Length.Should().Be(originalProduct.Thumbnail.Length);
        roundTrippedProduct.Thumbnail.ToArray().Should().BeEquivalentTo(originalProduct.Thumbnail.ToArray());

        // Assert - Result<int> (generic class)
        roundTrippedProduct.StockResult.Should().NotBeNull();
        roundTrippedProduct.StockResult!.Value.Should().Be(originalProduct.StockResult!.Value);

        // Assert - Nested (struct)
        roundTrippedProduct.ProductCode.Should().NotBeNull();
        roundTrippedProduct.ProductCode!.Value.Id.Should().Be(originalProduct.ProductCode!.Value.Id);
        roundTrippedProduct.ProductCode!.Value.Name.Should().Be(originalProduct.ProductCode!.Value.Name);
    }
}