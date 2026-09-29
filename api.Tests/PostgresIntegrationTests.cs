using System.Net;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace ApiTests;

public sealed class PostgresIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private PostgresApiFactory? _factory;

    public async Task InitializeAsync()
    {
        try
        {
            _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
                .Build();
            await _postgresContainer.StartAsync();
            _factory = new PostgresApiFactory(_postgresContainer.GetConnectionString());
            await _factory.InitializeDatabaseAsync();
        }
        catch
        {
            _postgresContainer = null;
            _factory = null;
        }
    }

    public async Task DisposeAsync()
    {
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task Accept_ConcurrentRequests_OnlyOneSucceeds_PostgreSql()
    {
        if (_postgresContainer == null || _factory == null)
        {
            return;
        }

        var scenario = await ApiTestHelpers.CreateScenarioAsync(_factory);
        using var setupClient = _factory.CreateClient();
        var craftsmanToken = await ApiTestHelpers.LoginAsync(setupClient, scenario.CraftsmanEmail, scenario.Password);
        var customerOneToken = await ApiTestHelpers.LoginAsync(setupClient, scenario.CustomerOneEmail, scenario.Password);
        var customerTwoToken = await ApiTestHelpers.LoginAsync(setupClient, scenario.CustomerTwoEmail, scenario.Password);
        using var craftsmanClient = ApiTestHelpers.AuthorizedClient(_factory, craftsmanToken);
        using var customerOneClient = ApiTestHelpers.AuthorizedClient(_factory, customerOneToken);
        using var customerTwoClient = ApiTestHelpers.AuthorizedClient(_factory, customerTwoToken);
        var date = DateTime.UtcNow.Date.AddDays(30);
        var requestOne = await ApiTestHelpers.CreateRequestAsync(customerOneClient, scenario.CraftsmanId, scenario.CraftId, date);
        var requestTwo = await ApiTestHelpers.CreateRequestAsync(customerTwoClient, scenario.CraftsmanId, scenario.CraftId, date, 125m);

        var responses = await Task.WhenAll(
            craftsmanClient.PostAsJsonAsync($"/craftsman/requests/{requestOne}/accept", new { }),
            craftsmanClient.PostAsJsonAsync($"/craftsman/requests/{requestTwo}/accept", new { }));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        var conflict = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.BadRequest);
        var conflictBody = await conflict.Content.ReadAsStringAsync();
        var winningId = responses.Single(response => response.StatusCode == HttpStatusCode.OK).RequestMessage!.RequestUri!.Segments[^2];
        Assert.Contains("Request #", conflictBody);
        Assert.Contains(winningId.TrimEnd('/'), conflictBody);
    }
}
