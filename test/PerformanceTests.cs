using System.Collections.Concurrent;
using System.Diagnostics;
using Bogus;
using FluentAssertions;
using StackExchange.Redis;

namespace R8.RedisHashMap.Tests;

public class PerformanceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Faker<TestUser> _userFaker;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
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
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(5000)]
    [InlineData(10000)]
    public void Write_Performance_ShouldCompleteWithinReasonableTime(int count)
    {
        // Arrange
        var users = _userFaker.Generate(count);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var results = users.Select(u => TestCacheContext.Default.TestUser.GetHashEntries(u)).ToList();
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(count);

        var averageMs = stopwatch.ElapsedMilliseconds / (double)count;
        _output.WriteLine($"Processed {count} users in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per user: {averageMs:F4}ms");

        // Should process at least 1000 items per second
        var itemsPerSecond = count / stopwatch.Elapsed.TotalSeconds;
        _output.WriteLine($"Throughput: {itemsPerSecond:F0} items/second");

        itemsPerSecond.Should().BeGreaterThan(1000, "should maintain good throughput");
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(5000)]
    [InlineData(10000)]
    public void Read_Performance_ShouldCompleteWithinReasonableTime(int count)
    {
        // Arrange
        var users = _userFaker.Generate(count);
        var hashEntriesList = users.Select(u => TestCacheContext.Default.TestUser.GetHashEntries(u)).ToList();

        var stopwatch = Stopwatch.StartNew();

        // Act
        var results = hashEntriesList.Select(entries =>
            TestCacheContext.Default.TestUser.FromHashEntries(entries)
        ).ToList();

        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(count);

        var averageMs = stopwatch.ElapsedMilliseconds / (double)count;
        _output.WriteLine($"Deserialized {count} users in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per user: {averageMs:F4}ms");

        var itemsPerSecond = count / stopwatch.Elapsed.TotalSeconds;
        _output.WriteLine($"Throughput: {itemsPerSecond:F0} items/second");

        itemsPerSecond.Should().BeGreaterThan(1000, "should maintain good throughput");
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(5000)]
    public void RoundTrip_Performance_ShouldMaintainThroughput(int count)
    {
        // Arrange
        var users = _userFaker.Generate(count);
        var stopwatch = Stopwatch.StartNew();

        // Act - Write + Read
        var results = users.Select(user =>
        {
            var entries = TestCacheContext.Default.TestUser.GetHashEntries(user);
            return TestCacheContext.Default.TestUser.FromHashEntries(entries);
        }).ToList();

        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(count);

        var averageMs = stopwatch.ElapsedMilliseconds / (double)count;
        _output.WriteLine($"Round-tripped {count} users in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per user: {averageMs:F4}ms");

        var itemsPerSecond = count / stopwatch.Elapsed.TotalSeconds;
        _output.WriteLine($"Throughput: {itemsPerSecond:F0} items/second");

        itemsPerSecond.Should().BeGreaterThan(500, "round-trip should maintain reasonable throughput");
    }

    [Fact]
    public void Memory_Write_ShouldNotExcessivelyAllocate()
    {
        // Arrange
        var users = _userFaker.Generate(1000);

        // Force GC to get baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var beforeMemory = GC.GetTotalMemory(false);

        // Act
        var results = users.Select(u => TestCacheContext.Default.TestUser.GetHashEntries(u)).ToList();

        var afterMemory = GC.GetTotalMemory(false);
        var allocatedMB = (afterMemory - beforeMemory) / 1024.0 / 1024.0;

        // Assert
        results.Should().HaveCount(1000);
        _output.WriteLine($"Allocated memory: {allocatedMB:F2} MB for 1000 users");
        _output.WriteLine($"Average per user: {allocatedMB * 1024 / 1000:F2} KB");

        // Should not allocate more than 10 MB for 1000 users
        allocatedMB.Should().BeLessThan(10, "should use efficient memory allocation");
    }

    [Fact]
    public void ParallelProcessing_ShouldScaleEfficiently()
    {
        // Arrange
        var users = _userFaker.Generate(10000);

        // Sequential processing
        var sequentialStopwatch = Stopwatch.StartNew();
        var sequentialResults = users.Select(u => TestCacheContext.Default.TestUser.GetHashEntries(u)).ToList();
        sequentialStopwatch.Stop();

        // Parallel processing
        var parallelStopwatch = Stopwatch.StartNew();
        var parallelResults = new ConcurrentBag<HashEntry[]>();
        Parallel.ForEach(users, user =>
        {
            var entries = TestCacheContext.Default.TestUser.GetHashEntries(user);
            parallelResults.Add(entries);
        });
        parallelStopwatch.Stop();

        // Assert
        sequentialResults.Should().HaveCount(10000);
        parallelResults.Should().HaveCount(10000);

        _output.WriteLine($"Sequential: {sequentialStopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Parallel: {parallelStopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Speedup: {sequentialStopwatch.ElapsedMilliseconds / (double)parallelStopwatch.ElapsedMilliseconds:F2}x");

        // Parallel should be faster (or at least not significantly slower)
        parallelStopwatch.ElapsedMilliseconds.Should().BeLessThan(
            (long)(sequentialStopwatch.ElapsedMilliseconds * 1.5),
            "parallel processing should provide some benefit"
        );
    }

    [Fact]
    public void StressTest_ShouldHandleExtremeLoad()
    {
        // Arrange
        const int totalUsers = 50000;
        var batchSize = 5000;
        var batches = totalUsers / batchSize;

        var stopwatch = Stopwatch.StartNew();
        var totalProcessed = 0;

        // Act
        for (var i = 0; i < batches; i++)
        {
            var batch = _userFaker.Generate(batchSize);
            var results = batch.Select(u => TestCacheContext.Default.TestUser.GetHashEntries(u)).ToList();
            totalProcessed += results.Count;
        }

        stopwatch.Stop();

        // Assert
        totalProcessed.Should().Be(totalUsers);

        _output.WriteLine($"Processed {totalUsers} users in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average: {stopwatch.ElapsedMilliseconds / (double)totalUsers:F4}ms per user");

        var itemsPerSecond = totalUsers / stopwatch.Elapsed.TotalSeconds;
        _output.WriteLine($"Throughput: {itemsPerSecond:F0} items/second");

        // Should complete in reasonable time
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMinutes(1), "should handle extreme load efficiently");
    }

    [Fact]
    public void ConsistencyTest_ShouldProduceSameResultsForSameInput()
    {
        // Arrange
        var user = _userFaker.Generate();

        // Act - Generate hash entries multiple times
        var results = Enumerable.Range(0, 100)
            .Select(_ => TestCacheContext.Default.TestUser.GetHashEntries(user))
            .ToList();

        // Assert
        var firstResult = results[0];
        foreach (var result in results.Skip(1))
            result.Should().BeEquivalentTo(firstResult,
                "same input should always produce identical output");
    }

    [Theory]
    [InlineData(100, 10)] // 100 users, 10 properties each
    [InlineData(1000, 10)] // 1000 users, 10 properties each
    public void NoCollisions_ShouldEnsureUniqueFieldNames(int userCount, int expectedMinFields)
    {
        // Arrange
        var users = _userFaker.Generate(userCount);

        // Act
        var results = users.Select(u => new
        {
            User = u,
            Entries = TestCacheContext.Default.TestUser.GetHashEntries(u)
        }).ToList();

        // Assert
        foreach (var result in results)
        {
            // Each result should have unique field names
            var fieldNames = result.Entries.Select(e => e.Name.ToString()).ToList();
            fieldNames.Should().OnlyHaveUniqueItems("no field name collisions within same object");

            // Should have reasonable number of fields
            fieldNames.Count.Should().BeGreaterThanOrEqualTo(expectedMinFields - 2,
                "should serialize most properties (allowing for some nulls)");
        }
    }
}