using System.Net;
using System.Text;
using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Infrastructure.Data;
using KjcBusinessHub.Infrastructure.PublicHolidays;
using KjcBusinessHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace KjcBusinessHub.Integration.Tests.PublicHolidays;

public sealed class DagsmartApiPublicHolidayImporterTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly OffDayRepository _repository;

    public DagsmartApiPublicHolidayImporterTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _repository = new OffDayRepository(_db);
    }

    [Fact]
    public async Task ImportAsync_inserts_updates_and_skips_vacation_entries()
    {
        await _repository.AddAsync(new OffDay
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            Date = new DateOnly(2025, 1, 1),
            OffDayType = OffDayType.PublicHoliday,
            Description = "Old name",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await _repository.AddAsync(new OffDay
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            Date = new DateOnly(2025, 6, 20),
            OffDayType = OffDayType.Vacation,
            Description = "Vacation",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await _repository.SaveChangesAsync();

        using var httpClient = CreateHttpClient("""
[
  { "date": "2025-01-01", "name": { "sv": "Nyårsdagen" } },
  { "date": "2025-06-06", "name": { "sv": "Sveriges nationaldag" } },
  { "date": "2025-06-20", "name": { "sv": "Midsommarafton" } }
]
""");
        var sut = new DagsmartApiPublicHolidayImporter(httpClient, _repository, NullLogger<DagsmartApiPublicHolidayImporter>.Instance);

        var result = await sut.ImportAsync(2025);
        var offDays = await _repository.GetByYearAsync(2025);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Added);
        Assert.Equal(1, result.Updated);
        Assert.Equal(3, offDays.Count);
        Assert.Contains(offDays, d => d.Date == new DateOnly(2025, 1, 1) && d.Description == "Nyårsdagen");
        Assert.Contains(offDays, d => d.Date == new DateOnly(2025, 6, 6) && d.OffDayType == OffDayType.PublicHoliday);
        Assert.Contains(offDays, d => d.Date == new DateOnly(2025, 6, 20) && d.OffDayType == OffDayType.Vacation && d.Description == "Vacation");
    }

    [Fact]
    public async Task ImportAsync_skips_rows_with_invalid_dates()
    {
        using var httpClient = CreateHttpClient("""
[
  { "date": "not-a-date", "name": { "sv": "Broken row" } },
  { "date": "2025-12-25", "name": { "sv": "Juldagen" } }
]
""");
        var sut = new DagsmartApiPublicHolidayImporter(httpClient, _repository, NullLogger<DagsmartApiPublicHolidayImporter>.Instance);

        var result = await sut.ImportAsync(2025);
        var offDays = await _repository.GetByYearAsync(2025);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Added);
        Assert.Equal(0, result.Updated);
        Assert.Single(offDays);
        Assert.Equal(new DateOnly(2025, 12, 25), offDays[0].Date);
    }

    [Fact]
    public async Task ImportAsync_returns_error_when_request_fails()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler((_, _) => throw new HttpRequestException("boom")));
        var sut = new DagsmartApiPublicHolidayImporter(httpClient, _repository, NullLogger<DagsmartApiPublicHolidayImporter>.Instance);

        var result = await sut.ImportAsync(2025);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.Added);
        Assert.Equal(0, result.Updated);
        Assert.Contains("Could not reach the Dagsmart API", result.ErrorMessage);
        Assert.Empty(await _repository.GetByYearAsync(2025));
    }

    [Fact]
    public async Task ImportAsync_returns_error_when_api_returns_null_payload()
    {
        using var httpClient = CreateHttpClient("null");
        var sut = new DagsmartApiPublicHolidayImporter(httpClient, _repository, NullLogger<DagsmartApiPublicHolidayImporter>.Instance);

        var result = await sut.ImportAsync(2025);

        Assert.False(result.IsSuccess);
        Assert.Equal("The Dagsmart API returned an empty response.", result.ErrorMessage);
        Assert.Empty(await _repository.GetByYearAsync(2025));
    }

    public void Dispose() => _db.Dispose();

    private static HttpClient CreateHttpClient(string json)
    {
        return new HttpClient(new StubHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://api.dagsmart.se/holidays?year=2025", request.RequestUri?.ToString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }));
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request, cancellationToken));
    }
}
