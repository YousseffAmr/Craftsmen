using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using api.Data;
using api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Identity;

namespace ApiTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"craftconnect-tests-{Guid.NewGuid():N}.db");
    public string ConnectionString => $"Data Source={databasePath}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Jwt:Key"] = "test-signing-key-that-is-longer-than-32-characters",
                ["Jwt:Issuer"] = "CraftConnect",
                ["Jwt:Audience"] = "CraftConnect.Client"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(ConnectionString));
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        if (!await db.Users.AnyAsync(user => user.Email == "admin@craftconnect.local"))
        {
            var admin = new User
            {
                Name = "Test Admin",
                Email = "admin@craftconnect.local",
                Role = UserRole.Admin
            };
            admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, "Admin123!");
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { File.Delete(databasePath); } catch { }
    }
}

public sealed record TestScenario(
    int CraftId,
    int CraftsmanId,
    int CustomerOneId,
    int CustomerTwoId,
    string CraftsmanEmail,
    string CustomerOneEmail,
    string CustomerTwoEmail,
    string Password);

public static class ApiTestHelpers
{
    public static async Task<TestScenario> CreateScenarioAsync(ApiFactory factory, bool approved = true, bool addSecondCraft = false)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        const string password = "Password123!";
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = new PasswordHasher<User>();
        var craft = new Craft { Name = $"Test Craft {suffix}" };
        var otherCraft = new Craft { Name = $"Other Craft {suffix}" };
        db.Crafts.Add(craft);
        if (addSecondCraft) db.Crafts.Add(otherCraft);
        var craftsmanUser = NewUser($"craftsman-{suffix}@test.local", "Test Craftsman", UserRole.Craftsman, hasher, password);
        var craftsman = new Craftsman { User = craftsmanUser, Name = craftsmanUser.Name, Status = approved ? CraftsmanStatus.Approved : CraftsmanStatus.Pending };
        var customerOneUser = NewUser($"customer-one-{suffix}@test.local", "Customer One", UserRole.Customer, hasher, password);
        var customerTwoUser = NewUser($"customer-two-{suffix}@test.local", "Customer Two", UserRole.Customer, hasher, password);
        var customerOne = new Customer { User = customerOneUser, Name = customerOneUser.Name };
        var customerTwo = new Customer { User = customerTwoUser, Name = customerTwoUser.Name };
        db.Users.AddRange(craftsmanUser, customerOneUser, customerTwoUser);
        db.Craftsmen.Add(craftsman);
        db.Customers.AddRange(customerOne, customerTwo);
        await db.SaveChangesAsync();
        db.CraftsmanCrafts.Add(new CraftsmanCraft { CraftsmanId = craftsman.Id, CraftId = craft.Id });
        await db.SaveChangesAsync();
        return new TestScenario(craft.Id, craftsman.Id, customerOne.Id, customerTwo.Id, craftsmanUser.Email, customerOneUser.Email, customerTwoUser.Email, password);
    }

    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("token").GetString()!;
    }

    public static HttpClient AuthorizedClient(ApiFactory factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<int> CreateRequestAsync(HttpClient customerClient, int craftsmanId, int craftId, DateTime neededOn, decimal price = 100m)
    {
        var response = await customerClient.PostAsJsonAsync($"/customer/craftsmen/{craftsmanId}/request", new
        {
            craftId,
            neededOn,
            price,
            description = "Integration test request"
        });
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetInt32();
    }

    private static User NewUser(string email, string name, UserRole role, PasswordHasher<User> hasher, string password)
    {
        var user = new User { Email = email, Name = name, Role = role };
        user.PasswordHash = hasher.HashPassword(user, password);
        return user;
    }
}
