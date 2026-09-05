using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiTests;

public sealed class Step10IntegrationTests : IAsyncLifetime
{
    private readonly ApiFactory factory = new();

    public async Task InitializeAsync() => await factory.InitializeDatabaseAsync();
    public Task DisposeAsync() { factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Accept_ConcurrentRequests_OnlyOneSucceeds()
    {
        var scenario = await ApiTestHelpers.CreateScenarioAsync(factory);
        using var setupClient = factory.CreateClient();
        var craftsmanToken = await ApiTestHelpers.LoginAsync(setupClient, scenario.CraftsmanEmail, scenario.Password);
        var customerOneToken = await ApiTestHelpers.LoginAsync(setupClient, scenario.CustomerOneEmail, scenario.Password);
        var customerTwoToken = await ApiTestHelpers.LoginAsync(setupClient, scenario.CustomerTwoEmail, scenario.Password);
        using var craftsmanClient = ApiTestHelpers.AuthorizedClient(factory, craftsmanToken);
        using var customerOneClient = ApiTestHelpers.AuthorizedClient(factory, customerOneToken);
        using var customerTwoClient = ApiTestHelpers.AuthorizedClient(factory, customerTwoToken);
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

    [Fact]
    public async Task Accept_SameRequestTwice_SecondIsRejected()
    {
        var scenario = await ApiTestHelpers.CreateScenarioAsync(factory);
        using var setupClient = factory.CreateClient();
        var craftsmanClient = ApiTestHelpers.AuthorizedClient(factory, await ApiTestHelpers.LoginAsync(setupClient, scenario.CraftsmanEmail, scenario.Password));
        var customerClient = ApiTestHelpers.AuthorizedClient(factory, await ApiTestHelpers.LoginAsync(setupClient, scenario.CustomerOneEmail, scenario.Password));
        var requestId = await ApiTestHelpers.CreateRequestAsync(customerClient, scenario.CraftsmanId, scenario.CraftId, DateTime.UtcNow.Date.AddDays(31));
        var first = await craftsmanClient.PostAsJsonAsync($"/craftsman/requests/{requestId}/accept", new { });
        var second = await craftsmanClient.PostAsJsonAsync($"/craftsman/requests/{requestId}/accept", new { });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Contains("already been answered", await second.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateRequest_CraftsmanNotOfferingCraft_Returns400()
    {
        var scenario = await ApiTestHelpers.CreateScenarioAsync(factory, addSecondCraft: true);
        using var setupClient = factory.CreateClient();
        var customerClient = ApiTestHelpers.AuthorizedClient(factory, await ApiTestHelpers.LoginAsync(setupClient, scenario.CustomerOneEmail, scenario.Password));
        var otherCraftId = await GetOtherCraftIdAsync();
        var response = await customerClient.PostAsJsonAsync($"/customer/craftsmen/{scenario.CraftsmanId}/request", new { craftId = otherCraftId, neededOn = DateTime.UtcNow.Date.AddDays(32), price = 100m, description = "Not offered" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("doesn't offer", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateRequest_CraftsmanNotApproved_Returns400()
    {
        var scenario = await ApiTestHelpers.CreateScenarioAsync(factory, approved: false);
        using var setupClient = factory.CreateClient();
        var customerClient = ApiTestHelpers.AuthorizedClient(factory, await ApiTestHelpers.LoginAsync(setupClient, scenario.CustomerOneEmail, scenario.Password));
        var response = await customerClient.PostAsJsonAsync($"/customer/craftsmen/{scenario.CraftsmanId}/request", new { craftId = scenario.CraftId, neededOn = DateTime.UtcNow.Date.AddDays(33), price = 100m, description = "Pending craftsman" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("not approved", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetOwnResource_WrongOwnerValidToken_Returns403()
    {
        var scenario = await ApiTestHelpers.CreateScenarioAsync(factory);
        using var setupClient = factory.CreateClient();
        var owner = ApiTestHelpers.AuthorizedClient(factory, await ApiTestHelpers.LoginAsync(setupClient, scenario.CustomerOneEmail, scenario.Password));
        var other = ApiTestHelpers.AuthorizedClient(factory, await ApiTestHelpers.LoginAsync(setupClient, scenario.CustomerTwoEmail, scenario.Password));
        var requestId = await ApiTestHelpers.CreateRequestAsync(owner, scenario.CraftsmanId, scenario.CraftId, DateTime.UtcNow.Date.AddDays(34));
        var response = await other.PostAsJsonAsync($"/customers/requests/{requestId}/withdraw", new { });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<int> GetOtherCraftIdAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<api.Data.AppDbContext>();
        return await db.Crafts.OrderByDescending(craft => craft.Id).Select(craft => craft.Id).FirstAsync();
    }
}
