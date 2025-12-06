using System.Collections.Concurrent;
using System.Text.Json;
using Bogus;
using FluentAssertions;
using R8.RedisHashMap.Tests.Models;
using StackExchange.Redis;

namespace R8.RedisHashMap.Tests;

public class WriteOperationsTests
{
    private readonly Faker<TestProduct> _productFaker;
    private readonly Faker<TestSession> _sessionFaker;
    private readonly Faker<TestUser> _userFaker;

    public WriteOperationsTests()
    {
        // Setup Bogus fakers for generating test data
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
                { "country", f.Address.Country() },
                { "zipCode", f.Address.ZipCode() }
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
    public void GetHashEntries_SingleUser_ShouldConvertSuccessfully()
    {
        // Arrange
        var user = _userFaker.Generate();

        // Act
        var hashEntries = TestCacheContext.Default.TestUser.GetHashEntries(user);

        // Assert
        hashEntries.Should().NotBeNull();
        hashEntries.Should().NotBeEmpty();
        hashEntries.Length.Should().BeGreaterThan(0);

        // Verify all non-null properties are included
        var entryDict = hashEntries.ToDictionary(e => e.Name.ToString(), e => e.Value);
        entryDict.Should().ContainKey("id");
        entryDict.Should().ContainKey("firstName");
        entryDict.Should().ContainKey("lastName");
        entryDict.Should().ContainKey("email");
        entryDict.Should().ContainKey("age");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void GetHashEntries_MultipleUsers_ShouldHandleLoad(int count)
    {
        // Arrange
        var users = _userFaker.Generate(count);

        // Act
        var results = users.Select(user =>
            TestCacheContext.Default.TestUser.GetHashEntries(user)
        ).ToList();

        // Assert
        results.Should().HaveCount(count);
        results.Should().OnlyContain(entries => entries.Length > 0);

        // Check for uniqueness - each user should have unique email
        var emails = results
            .SelectMany(entries => entries)
            .Where(e => e.Name == "email")
            .Select(e => e.Value.ToString())
            .ToList();

        emails.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GetHashEntries_UserWithNullValues_ShouldSkipNullProperties()
    {
        // Arrange
        var user = new TestUser
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Age = 30,
            CreatedAt = DateTime.Now,
            IsActive = true,
            Balance = 100.50m,
            Tags = null!, // Null array
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var hashEntries = TestCacheContext.Default.TestUser.GetHashEntries(user);

        // Assert
        hashEntries.Should().NotBeNull();
        var entryDict = hashEntries.ToDictionary(e => e.Name.ToString(), e => e.Value);

        // Null/empty collections should not be included
        entryDict.Should().NotContainKey("tags");
    }

    [Fact]
    public void GetHashEntries_Product_ShouldHandleGuidAndBytes()
    {
        // Arrange
        var product = _productFaker.Generate();

        // Act
        var hashEntries = TestCacheContext.Default.TestProduct.GetHashEntries(product);

        // Assert
        hashEntries.Should().NotBeNull();
        hashEntries.Should().NotBeEmpty();

        var entryDict = hashEntries.ToDictionary(e => e.Name.ToString(), e => e.Value);
        entryDict.Should().ContainKey("id");
        entryDict.Should().ContainKey("name");
        entryDict.Should().ContainKey("price");

        // Verify byte array is included
        if (product.ImageData?.Length > 0) entryDict.Should().ContainKey("imageData");

        // Verify advanced properties are included
        if (product.Specifications != null) entryDict.Should().ContainKey("specifications");

        if (product.RelatedProducts is { Count: > 0 }) entryDict.Should().ContainKey("relatedProducts");

        if (product.VariantMap is { Count: > 0 }) entryDict.Should().ContainKey("variantMap");

        if (product.Thumbnail.Length > 0) entryDict.Should().ContainKey("thumbnail");

        if (product.StockResult != null) entryDict.Should().ContainKey("stockResult");

        if (product.ProductCode != null) entryDict.Should().ContainKey("productCode");
    }

    [Theory]
    [InlineData(50)]
    [InlineData(500)]
    public void GetHashEntries_MultipleProducts_ShouldMaintainDataIntegrity(int count)
    {
        // Arrange
        var products = _productFaker.Generate(count);

        // Act
        var results = products.Select(p => new
        {
            Product = p,
            HashEntries = TestCacheContext.Default.TestProduct.GetHashEntries(p)
        }).ToList();

        // Assert
        results.Should().HaveCount(count);

        foreach (var result in results)
        {
            var entryDict = result.HashEntries.ToDictionary(e => e.Name.ToString(), e => e.Value);

            // Verify critical fields
            entryDict.Should().ContainKey("id");
            entryDict["name"].ToString().Should().Be(result.Product.Name);

            // Verify price conversion
            decimal.Parse(entryDict["price"].ToString()).Should().Be(result.Product.Price);
        }
    }

    [Fact]
    public void GetHashEntries_Session_ShouldHandleTimeSpanConverter()
    {
        // Arrange
        var session = _sessionFaker.Generate();

        // Act
        var hashEntries = TestCacheContext.Default.TestSession.GetHashEntries(session);

        // Assert
        hashEntries.Should().NotBeNull();
        var entryDict = hashEntries.ToDictionary(e => e.Name.ToString(), e => e.Value);

        entryDict.Should().ContainKey("duration");

        // Verify TimeSpan is converted to milliseconds
        var milliseconds = (long)entryDict["duration"];
        milliseconds.Should().Be((long)session.Duration.TotalMilliseconds);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    public void GetHashEntries_MixedLoad_ShouldHandleMultipleTypes(int countPerType)
    {
        // Arrange
        var users = _userFaker.Generate(countPerType);
        var products = _productFaker.Generate(countPerType);
        var sessions = _sessionFaker.Generate(countPerType);

        // Act
        var userResults = users.Select(u => TestCacheContext.Default.TestUser.GetHashEntries(u)).ToList();
        var productResults = products.Select(p => TestCacheContext.Default.TestProduct.GetHashEntries(p)).ToList();
        var sessionResults = sessions.Select(s => TestCacheContext.Default.TestSession.GetHashEntries(s)).ToList();

        // Assert
        userResults.Should().HaveCount(countPerType);
        productResults.Should().HaveCount(countPerType);
        sessionResults.Should().HaveCount(countPerType);

        // All conversions should succeed
        userResults.Should().OnlyContain(entries => entries.Length > 0);
        productResults.Should().OnlyContain(entries => entries.Length > 0);
        sessionResults.Should().OnlyContain(entries => entries.Length > 0);
    }

    [Fact]
    public void GetHashEntries_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var users = _userFaker.Generate(100);
        var results = new ConcurrentBag<HashEntry[]>();

        // Act
        Parallel.ForEach(users, user =>
        {
            var entries = TestCacheContext.Default.TestUser.GetHashEntries(user);
            results.Add(entries);
        });

        // Assert
        results.Should().HaveCount(100);
        results.Should().OnlyContain(entries => entries.Length > 0);
    }

    [Fact]
    public void GetHashEntries_PropertyNaming_ShouldUseCamelCase()
    {
        // Arrange
        var user = _userFaker.Generate();

        // Act
        var hashEntries = TestCacheContext.Default.TestUser.GetHashEntries(user);

        // Assert
        var fieldNames = hashEntries.Select(e => e.Name.ToString()).ToList();

        // Verify camelCase naming
        fieldNames.Should().Contain("firstName");
        fieldNames.Should().Contain("lastName");
        fieldNames.Should().Contain("isActive");
        fieldNames.Should().Contain("createdAt");

        // Should NOT contain PascalCase
        fieldNames.Should().NotContain("FirstName");
        fieldNames.Should().NotContain("LastName");
    }

    [Fact]
    public void GetHashEntries_ProductAdvancedTypes_ShouldSerializeAllComplexTypes()
    {
        // Arrange - Create a product with all advanced types populated
        var product = new TestProduct
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Description = "Test Description",
            Category = "Test Category",
            Price = 99.99m,
            StockQuantity = 100,
            LastRestocked = DateTime.UtcNow,
            Rating = 4.5,
            IsAvailable = true,
            ImageData = new byte[] { 1, 2, 3, 4, 5 },
            Specifications = JsonDocument.Parse("""{"cpu": "Intel i7", "ram": 16}"""),
            RelatedProducts = new List<string> { "Product1", "Product2", "Product3" },
            VariantMap = new Dictionary<int, string>
            {
                { 1, "Red" },
                { 2, "Blue" },
                { 3, "Green" }
            },
            Thumbnail = new ReadOnlyMemory<byte>(new byte[] { 10, 20, 30, 40, 50 }),
            StockResult = new Result<int> { Value = 250 },
            ProductCode = new Nested { Id = 12345, Name = "SKU-12345" }
        };

        // Act
        var hashEntries = TestCacheContext.Default.TestProduct.GetHashEntries(product);

        // Assert
        hashEntries.Should().NotBeNull();
        hashEntries.Should().NotBeEmpty();

        var entryDict = hashEntries.ToDictionary(e => e.Name.ToString(), e => e.Value);

        // Verify basic properties
        entryDict.Should().ContainKey("id");
        entryDict.Should().ContainKey("name");
        entryDict.Should().ContainKey("price");

        // Verify advanced properties are serialized
        entryDict.Should().ContainKey("specifications");
        entryDict.Should().ContainKey("relatedProducts");
        entryDict.Should().ContainKey("variantMap");
        entryDict.Should().ContainKey("thumbnail");
        entryDict.Should().ContainKey("stockResult");
        entryDict.Should().ContainKey("productCode");
    }
}