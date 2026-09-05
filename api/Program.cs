using System.ComponentModel.DataAnnotations;
using api.Data;
using api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var seedRequested = args.Contains("--seed", StringComparer.OrdinalIgnoreCase);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=crafts.db");
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key must be configured.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CraftConnect";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CraftConnect.Client";
var jwtLifetimeMinutes = builder.Configuration.GetValue("Jwt:LifetimeMinutes", 60);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            RoleClaimType = "role",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("AngularDev");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment() || seedRequested)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    const string adminEmail = "admin@craftconnect.local";
    if (!await db.Users.AnyAsync(user => user.Email == adminEmail))
    {
        var admin = new User
        {
            Name = "System Admin",
            Email = adminEmail,
            Role = UserRole.Admin
        };

        admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, "Admin123!");
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }

    if (seedRequested)
    {
        await SeedDemoDataAsync(db);
        Console.WriteLine("Demo seed complete: two pending requests share one craftsman and date.");
        return;
    }
}

app.MapGet("/admin/crafts", [Microsoft.AspNetCore.Authorization.Authorize] async (int? page, int? pageSize, AppDbContext db) =>
{
    var paging = GetPaging(page, pageSize);
    var crafts = await db.Crafts
        .AsNoTracking()
        .OrderBy(c => c.Id)
        .Skip(paging.Skip)
        .Take(paging.Take)
        .ToListAsync();

    return Results.Json(crafts);
});

app.MapPost("/admin/crafts", [Microsoft.AspNetCore.Authorization.Authorize] async (HttpContext http, AppDbContext db) =>
{
    CraftInput? input;

    try
    {
        input = await http.Request.ReadFromJsonAsync<CraftInput>();
    }
    catch (Exception)
    {
        return ErrorResult(400, "Craft name is required.", "name");
    }

    var name = input?.Name?.Trim();

    if (string.IsNullOrWhiteSpace(name))
    {
        return ErrorResult(400, "Craft name is required.", "name");
    }

    if (await db.Crafts.AnyAsync(c => c.Name == name))
    {
        return ErrorResult(400, "A craft with this name already exists.", "name");
    }

    var craft = new Craft
    {
        Name = name,
        Description = input?.Description?.Trim()
    };

    db.Crafts.Add(craft);
    await db.SaveChangesAsync();

    return Results.Created($"/admin/crafts/{craft.Id}", craft);
});

app.MapPut("/admin/crafts/{id:int}", [Microsoft.AspNetCore.Authorization.Authorize] async (int id, HttpContext http, AppDbContext db) =>
{
    CraftInput? input;

    try
    {
        input = await http.Request.ReadFromJsonAsync<CraftInput>();
    }
    catch (Exception)
    {
        return ErrorResult(400, "Craft name is required.", "name");
    }

    var name = input?.Name?.Trim();

    if (string.IsNullOrWhiteSpace(name))
    {
        return ErrorResult(400, "Craft name is required.", "name");
    }

    var craft = await db.Crafts.FindAsync(id);
    if (craft is null)
    {
        return ErrorResult(404, "Craft not found.");
    }

    if (await db.Crafts.AnyAsync(c => c.Id != id && c.Name == name))
    {
        return ErrorResult(400, "A craft with this name already exists.", "name");
    }

    craft.Name = name;
    craft.Description = input?.Description?.Trim();
    await db.SaveChangesAsync();

    return Results.Json(craft);
});

app.MapDelete("/admin/crafts/{id:int}", [Microsoft.AspNetCore.Authorization.Authorize] async (int id, AppDbContext db) =>
{
    var craft = await db.Crafts.FindAsync(id);
    if (craft is null)
    {
        return ErrorResult(404, "Craft not found.");
    }

    if (await db.CraftsmanCrafts.AnyAsync(cc => cc.CraftId == id) ||
        await db.Requests.AnyAsync(request => request.CraftId == id))
    {
        return ErrorResult(400, "This craft is still in use and can't be removed.");
    }

    db.Crafts.Remove(craft);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapGet("/admin/craftsmen/pending", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")] async (int? page, int? pageSize, AppDbContext db) =>
{
    var paging = GetPaging(page, pageSize);
    var craftsmen = await db.Craftsmen
        .AsNoTracking()
        .Include(c => c.User)
        .Where(c => c.Status == CraftsmanStatus.Pending)
        .OrderByDescending(c => c.Id)
        .Skip(paging.Skip)
        .Take(paging.Take)
        .Select(c => new
        {
            id = c.Id,
            name = c.Name,
            email = c.User!.Email,
            status = c.Status.ToString()
        })
        .ToListAsync();

    return Results.Ok(craftsmen);
});

app.MapPost("/admin/craftsmen/{id:int}/approve", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")] async (int id, AppDbContext db) =>
{
    var craftsman = await db.Craftsmen.FindAsync(id);
    if (craftsman is null)
    {
        return ErrorResult(404, "Craftsman not found.");
    }

    if (craftsman.Status != CraftsmanStatus.Pending)
    {
        return ErrorResult(400, "This craftsman has already been decided.");
    }

    craftsman.Status = CraftsmanStatus.Approved;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        id = craftsman.Id,
        status = craftsman.Status.ToString()
    });
});

app.MapPost("/admin/craftsmen/{id:int}/refuse", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")] async (int id, AppDbContext db) =>
{
    var craftsman = await db.Craftsmen.FindAsync(id);
    if (craftsman is null)
    {
        return ErrorResult(404, "Craftsman not found.");
    }

    if (craftsman.Status != CraftsmanStatus.Pending)
    {
        return ErrorResult(400, "This craftsman has already been decided.");
    }

    craftsman.Status = CraftsmanStatus.Refused;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        id = craftsman.Id,
        status = craftsman.Status.ToString()
    });
});

app.MapGet("/craftsman/crafts", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Craftsman")] async (int? page, int? pageSize, ClaimsPrincipal user, AppDbContext db) =>
{
    var paging = GetPaging(page, pageSize);
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
    {
        return ErrorResult(403, "Craftsman profile could not be found.");
    }

    var craftsman = await db.Craftsmen.AsNoTracking().SingleOrDefaultAsync(c => c.UserId == userId);
    if (craftsman is null)
    {
        return ErrorResult(403, "Craftsman profile could not be found.");
    }

    var selectedCraftIds = await db.CraftsmanCrafts
        .Where(cc => cc.CraftsmanId == craftsman.Id)
        .Select(cc => cc.CraftId)
        .ToListAsync();

    var crafts = await db.Crafts.AsNoTracking()
        .OrderBy(c => c.Name)
        .Skip(paging.Skip)
        .Take(paging.Take)
        .Select(c => new
        {
            id = c.Id,
            name = c.Name,
            description = c.Description,
            selected = selectedCraftIds.Contains(c.Id)
        })
        .ToListAsync();

    return Results.Ok(new
    {
        status = craftsman.Status.ToString(),
        crafts
    });
});

app.MapPost("/craftsman/crafts/{craftId:int}", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Craftsman")] async (int craftId, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
    {
        return ErrorResult(403, "Craftsman profile could not be found.");
    }

    var craftsman = await db.Craftsmen.SingleOrDefaultAsync(c => c.UserId == userId);
    if (craftsman is null || craftsman.Status != CraftsmanStatus.Approved)
    {
        return ErrorResult(403, "Only approved craftsmen can manage their crafts.");
    }

    if (!await db.Crafts.AnyAsync(c => c.Id == craftId))
    {
        return ErrorResult(404, "Craft not found.");
    }

    if (await db.CraftsmanCrafts.AnyAsync(cc => cc.CraftsmanId == craftsman.Id && cc.CraftId == craftId))
    {
        return ErrorResult(400, "This craft is already selected.", "craftId");
    }

    db.CraftsmanCrafts.Add(new CraftsmanCraft { CraftsmanId = craftsman.Id, CraftId = craftId });
    await db.SaveChangesAsync();
    return Results.Ok(new { craftId });
});

app.MapDelete("/craftsman/crafts/{craftId:int}", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Craftsman")] async (int craftId, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
    {
        return ErrorResult(403, "Craftsman profile could not be found.");
    }

    var craftsman = await db.Craftsmen.SingleOrDefaultAsync(c => c.UserId == userId);
    if (craftsman is null || craftsman.Status != CraftsmanStatus.Approved)
    {
        return ErrorResult(403, "Only approved craftsmen can manage their crafts.");
    }

    var relationship = await db.CraftsmanCrafts
        .SingleOrDefaultAsync(cc => cc.CraftsmanId == craftsman.Id && cc.CraftId == craftId);
    if (relationship is null)
    {
        return ErrorResult(404, "Selected craft not found.");
    }

    db.CraftsmanCrafts.Remove(relationship);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/craftsman/requests", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Craftsman")] async (int? page, int? pageSize, ClaimsPrincipal user, AppDbContext db) =>
{
    var paging = GetPaging(page, pageSize);
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
    {
        return ErrorResult(403, "Craftsman profile could not be found.");
    }

    var craftsman = await db.Craftsmen.AsNoTracking().SingleOrDefaultAsync(c => c.UserId == userId);
    if (craftsman is null || craftsman.Status != CraftsmanStatus.Approved)
    {
        return ErrorResult(403, "Only approved craftsmen can view requests.");
    }

    var requests = await db.Requests
        .AsNoTracking()
        .Where(r => r.CraftsmanId == craftsman.Id)
        .OrderByDescending(r => r.CreatedAt)
        .Skip(paging.Skip)
        .Take(paging.Take)
        .Select(r => new
        {
            id = r.Id,
            customerName = r.Customer.Name,
            craftName = r.Craft.Name,
            neededOn = r.NeededOn,
            price = r.Price,
            description = r.Description,
            status = r.Status.ToString(),
            createdAt = r.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(requests);
});

app.MapPost("/craftsman/requests/{id:int}/accept", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Craftsman")] async (int id, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
    {
        return ErrorResult(403, "Craftsman profile could not be found.");
    }

    var craftsman = await db.Craftsmen.SingleOrDefaultAsync(c => c.UserId == userId);
    if (craftsman is null || craftsman.Status != CraftsmanStatus.Approved)
    {
        return ErrorResult(403, "Only approved craftsmen can answer requests.");
    }

    var request = await db.Requests
        .Include(r => r.Customer)
        .Include(r => r.Craft)
        .SingleOrDefaultAsync(r => r.Id == id);
    if (request is null)
    {
        return ErrorResult(404, "Request not found.");
    }

    if (request.CraftsmanId != craftsman.Id)
    {
        return ErrorResult(403, "You cannot answer another craftsman's request.");
    }

    if (request.Status != RequestStatus.Pending)
    {
        return ErrorResult(400, "This request has already been answered.");
    }

    var conflict = await db.Requests
        .AsNoTracking()
        .Where(r => r.CraftsmanId == craftsman.Id && r.NeededOn == request.NeededOn && r.Status == RequestStatus.Accepted)
        .Select(r => (int?)r.Id)
        .FirstOrDefaultAsync();
    if (conflict.HasValue)
    {
        return ErrorResult(400, $"You already have an accepted job on this date (Request #{conflict.Value}).");
    }

    request.Status = RequestStatus.Accepted;
    request.RespondedAt = DateTime.UtcNow;

    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: 19 })
    {
        var winningRequestId = await db.Requests
            .AsNoTracking()
            .Where(r => r.CraftsmanId == craftsman.Id && r.NeededOn == request.NeededOn && r.Status == RequestStatus.Accepted)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync();
        var suffix = winningRequestId.HasValue ? $" (Request #{winningRequestId.Value})" : string.Empty;
        return ErrorResult(400, $"You already have an accepted job on this date{suffix}.");
    }

    return Results.Ok(new
    {
        id = request.Id,
        customerName = request.Customer.Name,
        craftName = request.Craft.Name,
        neededOn = request.NeededOn,
        price = request.Price,
        description = request.Description,
        status = request.Status.ToString(),
        createdAt = request.CreatedAt,
        respondedAt = request.RespondedAt
    });
});

app.MapPost("/craftsman/requests/{id:int}/decline", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Craftsman")] async (int id, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
    {
        return ErrorResult(403, "Craftsman profile could not be found.");
    }

    var craftsman = await db.Craftsmen.SingleOrDefaultAsync(c => c.UserId == userId);
    if (craftsman is null || craftsman.Status != CraftsmanStatus.Approved)
    {
        return ErrorResult(403, "Only approved craftsmen can answer requests.");
    }

    var request = await db.Requests
        .Include(r => r.Customer)
        .Include(r => r.Craft)
        .SingleOrDefaultAsync(r => r.Id == id);
    if (request is null)
    {
        return ErrorResult(404, "Request not found.");
    }

    if (request.CraftsmanId != craftsman.Id)
    {
        return ErrorResult(403, "You cannot answer another craftsman's request.");
    }

    if (request.Status != RequestStatus.Pending)
    {
        return ErrorResult(400, "This request has already been answered.");
    }

    request.Status = RequestStatus.Declined;
    request.RespondedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        id = request.Id,
        customerName = request.Customer.Name,
        craftName = request.Craft.Name,
        neededOn = request.NeededOn,
        price = request.Price,
        description = request.Description,
        status = request.Status.ToString(),
        createdAt = request.CreatedAt,
        respondedAt = request.RespondedAt
    });
});

app.MapGet("/craftsman/board", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Craftsman")] async (ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
    {
        return ErrorResult(403, "Craftsman profile could not be found.");
    }

    var craftsman = await db.Craftsmen.AsNoTracking().SingleOrDefaultAsync(c => c.UserId == userId);
    if (craftsman is null || craftsman.Status != CraftsmanStatus.Approved)
    {
        return ErrorResult(403, "Only approved craftsmen can view the board.");
    }

    var requestValues = await db.Requests
        .AsNoTracking()
        .Where(r => r.CraftsmanId == craftsman.Id)
        .Select(r => new { r.Status, r.Price })
        .ToListAsync();

    var groups = requestValues
        .GroupBy(request => request.Status)
        .Select(group => new
        {
            status = group.Key.ToString(),
            count = group.Count(),
            total = group.Sum(request => request.Price)
        })
        .ToList();

    var requestCount = groups.Sum(group => group.count);
    var totalValue = groups.Sum(group => group.total);

    return Results.Ok(new
    {
        groups = Enum.GetValues<RequestStatus>()
            .Select(status => groups.FirstOrDefault(group => group.status == status.ToString()) ?? new
            {
                status = status.ToString(),
                count = 0,
                total = 0m
            }),
        totals = new { requestCount, totalValue }
    });
});

app.MapGet("/craftsman/day-sheet", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Craftsman")] async (DateTime? date, int? page, int? pageSize, ClaimsPrincipal user, AppDbContext db) =>
{
    var paging = GetPaging(page, pageSize);
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
    {
        return ErrorResult(403, "Craftsman profile could not be found.");
    }

    var craftsman = await db.Craftsmen.AsNoTracking().SingleOrDefaultAsync(c => c.UserId == userId);
    if (craftsman is null || craftsman.Status != CraftsmanStatus.Approved)
    {
        return ErrorResult(403, "Only approved craftsmen can view the day sheet.");
    }

    var selectedDate = (date ?? DateTime.UtcNow).Date;
    var requests = await db.Requests
        .AsNoTracking()
        .Where(r => r.CraftsmanId == craftsman.Id && r.NeededOn == selectedDate && r.Status == RequestStatus.Accepted)
        .OrderBy(r => r.CreatedAt)
        .Skip(paging.Skip)
        .Take(paging.Take)
        .Select(r => new
        {
            id = r.Id,
            customerName = r.Customer.Name,
            craftName = r.Craft.Name,
            neededOn = r.NeededOn,
            price = r.Price,
            description = r.Description,
            status = r.Status.ToString(),
            createdAt = r.CreatedAt,
            respondedAt = r.RespondedAt
        })
        .ToListAsync();

    return Results.Ok(new { date = selectedDate, requests });
});

app.MapPost("/craftsman/day-sheet/{requestId:int}/complete", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Craftsman")] async (int requestId, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
    {
        return ErrorResult(403, "Craftsman profile could not be found.");
    }

    var craftsman = await db.Craftsmen.SingleOrDefaultAsync(c => c.UserId == userId);
    if (craftsman is null || craftsman.Status != CraftsmanStatus.Approved)
    {
        return ErrorResult(403, "Only approved craftsmen can complete jobs.");
    }

    var request = await db.Requests
        .Include(r => r.Customer)
        .Include(r => r.Craft)
        .SingleOrDefaultAsync(r => r.Id == requestId);
    if (request is null)
    {
        return ErrorResult(404, "Request not found.");
    }

    if (request.CraftsmanId != craftsman.Id)
    {
        return ErrorResult(403, "You cannot complete another craftsman's request.");
    }

    if (request.Status != RequestStatus.Accepted)
    {
        return ErrorResult(400, "Only accepted jobs can be marked done.");
    }

    if (request.NeededOn.Date > DateTime.UtcNow.Date)
    {
        return ErrorResult(400, "Only jobs on or before today can be marked done.", "neededOn");
    }

    request.Status = RequestStatus.Completed;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        id = request.Id,
        customerName = request.Customer.Name,
        craftName = request.Craft.Name,
        neededOn = request.NeededOn,
        price = request.Price,
        description = request.Description,
        status = request.Status.ToString(),
        createdAt = request.CreatedAt,
        respondedAt = request.RespondedAt
    });
});

app.MapGet("/craftsman/notifications", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Craftsman")] async (int? page, int? pageSize, ClaimsPrincipal user, AppDbContext db) =>
{
    var paging = GetPaging(page, pageSize);
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
    {
        return ErrorResult(403, "Craftsman profile could not be found.");
    }

    await using var transaction = await db.Database.BeginTransactionAsync();
    var craftsman = await db.Craftsmen.SingleOrDefaultAsync(c => c.UserId == userId);
    if (craftsman is null || craftsman.Status != CraftsmanStatus.Approved)
    {
        return ErrorResult(403, "Only approved craftsmen can view notifications.");
    }

    var previousCheckedAt = craftsman.LastCheckedAt;
    var notifications = await db.Requests
        .AsNoTracking()
        .Where(r => r.CraftsmanId == craftsman.Id &&
            (!previousCheckedAt.HasValue || r.CreatedAt > previousCheckedAt.Value))
        .OrderByDescending(r => r.CreatedAt)
        .Skip(paging.Skip)
        .Take(paging.Take)
        .Select(r => new
        {
            id = r.Id,
            customerName = r.Customer.Name,
            craftName = r.Craft.Name,
            neededOn = r.NeededOn,
            price = r.Price,
            description = r.Description,
            status = r.Status.ToString(),
            createdAt = r.CreatedAt
        })
        .ToListAsync();

    craftsman.LastCheckedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    await transaction.CommitAsync();

    return Results.Ok(notifications);
});

app.MapGet("/customers/notifications", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Customer")] async (int? page, int? pageSize, ClaimsPrincipal user, AppDbContext db) =>
{
    var paging = GetPaging(page, pageSize);
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
    {
        return ErrorResult(403, "Customer profile could not be found.");
    }

    await using var transaction = await db.Database.BeginTransactionAsync();
    var customer = await db.Customers.SingleOrDefaultAsync(c => c.UserId == userId);
    if (customer is null)
    {
        return ErrorResult(403, "Customer profile could not be found.");
    }

    var previousCheckedAt = customer.LastCheckedAt;
    var notifications = await db.Requests
        .AsNoTracking()
        .Where(r => r.CustomerId == customer.Id && r.RespondedAt.HasValue &&
            (r.Status == RequestStatus.Accepted || r.Status == RequestStatus.Declined) &&
            (!previousCheckedAt.HasValue || r.RespondedAt > previousCheckedAt.Value))
        .OrderByDescending(r => r.RespondedAt)
        .Skip(paging.Skip)
        .Take(paging.Take)
        .Select(r => new
        {
            id = r.Id,
            craftsmanName = r.Craftsman.Name,
            craftName = r.Craft.Name,
            neededOn = r.NeededOn,
            price = r.Price,
            description = r.Description,
            status = r.Status.ToString(),
            respondedAt = r.RespondedAt
        })
        .ToListAsync();

    customer.LastCheckedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    await transaction.CommitAsync();

    return Results.Ok(notifications);
});

app.MapGet("/customer/craftsmen", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Customer")] async (int? craftId, int? page, int? pageSize, AppDbContext db) =>
{
    var paging = GetPaging(page, pageSize);
    if (!craftId.HasValue)
    {
        return ErrorResult(400, "A craft must be selected.", "craftId");
    }

    if (!await db.Crafts.AnyAsync(c => c.Id == craftId.Value))
    {
        return ErrorResult(404, "Craft not found.");
    }

    var craftsmen = await db.CraftsmanCrafts
        .AsNoTracking()
        .Where(cc => cc.CraftId == craftId.Value && cc.Craftsman.Status == CraftsmanStatus.Approved)
        .OrderBy(cc => cc.Craftsman.Name)
        .Skip(paging.Skip)
        .Take(paging.Take)
        .Select(cc => new
        {
            id = cc.Craftsman.Id,
            name = cc.Craftsman.Name
        })
        .ToListAsync();

    return Results.Ok(craftsmen);
});

app.MapPost("/customer/craftsmen/{craftsmanId:int}/request", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Customer")] async (int craftsmanId, ClaimsPrincipal user, HttpContext http, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
    {
        return ErrorResult(403, "Customer profile could not be found.");
    }

    var customer = await db.Customers.SingleOrDefaultAsync(c => c.UserId == userId);
    if (customer is null)
    {
        return ErrorResult(403, "Customer profile could not be found.");
    }

    RequestInput? input;
    try
    {
        input = await http.Request.ReadFromJsonAsync<RequestInput>();
    }
    catch (Exception)
    {
        return ErrorResult(400, "Please provide a valid service request.", "description");
    }

    if (input is null)
    {
        return ErrorResult(400, "Please provide a valid service request.", "description");
    }

    if (input.NeededOn == default || input.NeededOn.Date < DateTime.UtcNow.Date)
    {
        return ErrorResult(400, "The requested date cannot be in the past.", "neededOn");
    }

    if (input.Price <= 0)
    {
        return ErrorResult(400, "Price must be greater than zero.", "price");
    }

    var description = input.Description?.Trim();
    if (string.IsNullOrWhiteSpace(description))
    {
        return ErrorResult(400, "Request details are required.", "description");
    }

    var craft = await db.Crafts.FindAsync(input.CraftId);
    if (craft is null)
    {
        return ErrorResult(404, "Craft not found.");
    }

    var craftsman = await db.Craftsmen.SingleOrDefaultAsync(c => c.Id == craftsmanId);
    if (craftsman is null)
    {
        return ErrorResult(404, "Craftsman not found.");
    }

    if (craftsman.UserId == userId)
    {
        return ErrorResult(400, "You cannot submit a request to yourself.");
    }

    if (craftsman.Status != CraftsmanStatus.Approved)
    {
        return ErrorResult(400, "This craftsman is not approved.", "craftsmanId");
    }

    if (!await db.CraftsmanCrafts.AnyAsync(cc => cc.CraftsmanId == craftsmanId && cc.CraftId == input.CraftId))
    {
        return ErrorResult(400, "This craftsman doesn't offer that craft.", "craftId");
    }

    var neededOn = input.NeededOn.Date;
    if (await db.Requests.AnyAsync(r =>
        r.CustomerId == customer.Id &&
        r.CraftsmanId == craftsmanId &&
        r.CraftId == input.CraftId &&
        r.NeededOn == neededOn &&
        (r.Status == RequestStatus.Pending || r.Status == RequestStatus.Accepted)))
    {
        return ErrorResult(400, "You already have an active request for this craftsman and date.", "neededOn");
    }

    var request = new Request
    {
        CustomerId = customer.Id,
        CraftsmanId = craftsmanId,
        CraftId = input.CraftId,
        NeededOn = neededOn,
        Price = decimal.Round(input.Price, 2),
        Description = description,
        Status = RequestStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };

    db.Requests.Add(request);
    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        return ErrorResult(400, "You already have an active request for this craftsman and date.", "neededOn");
    }

    return Results.Created($"/customer/requests/{request.Id}", new
    {
        id = request.Id,
        customerId = request.CustomerId,
        craftsmanId = request.CraftsmanId,
        craftId = request.CraftId,
        neededOn = request.NeededOn,
        price = request.Price,
        description = request.Description,
        status = request.Status.ToString(),
        createdAt = request.CreatedAt
    });
});

app.MapGet("/customers/requests", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Customer")] async (int? page, int? pageSize, ClaimsPrincipal user, AppDbContext db) =>
{
    var paging = GetPaging(page, pageSize);
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
    {
        return ErrorResult(403, "Customer profile could not be found.");
    }

    var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.UserId == userId);
    if (customer is null)
    {
        return ErrorResult(403, "Customer profile could not be found.");
    }

    var requests = await db.Requests.AsNoTracking()
        .Where(r => r.CustomerId == customer.Id)
        .OrderByDescending(r => r.CreatedAt)
        .Skip(paging.Skip)
        .Take(paging.Take)
        .Select(r => new
        {
            id = r.Id,
            craftsmanName = r.Craftsman.Name,
            craftName = r.Craft.Name,
            neededOn = r.NeededOn,
            price = r.Price,
            description = r.Description,
            status = r.Status.ToString(),
            createdAt = r.CreatedAt,
            respondedAt = r.RespondedAt
        })
        .ToListAsync();

    return Results.Ok(requests);
});

app.MapPost("/customers/requests/{id:int}/withdraw", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Customer")] async (int id, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
    {
        return ErrorResult(403, "Customer profile could not be found.");
    }

    var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.UserId == userId);
    if (customer is null)
    {
        return ErrorResult(403, "Customer profile could not be found.");
    }

    var request = await db.Requests.Include(r => r.Craftsman).Include(r => r.Craft)
        .SingleOrDefaultAsync(r => r.Id == id);
    if (request is null)
    {
        return ErrorResult(404, "Request not found.");
    }

    if (request.CustomerId != customer.Id)
    {
        return ErrorResult(403, "You cannot withdraw another customer's request.");
    }

    if (request.Status == RequestStatus.Accepted)
    {
        return ErrorResult(400, "This request has already been accepted and can't be withdrawn.");
    }

    if (request.Status == RequestStatus.Withdrawn)
    {
        return ErrorResult(400, "This request has already been withdrawn.");
    }

    request.Status = RequestStatus.Withdrawn;
    await db.SaveChangesAsync();
    return Results.Ok(new
    {
        id = request.Id,
        craftsmanName = request.Craftsman.Name,
        craftName = request.Craft.Name,
        neededOn = request.NeededOn,
        price = request.Price,
        description = request.Description,
        status = request.Status.ToString(),
        createdAt = request.CreatedAt,
        respondedAt = request.RespondedAt
    });
});

app.MapGet("/craftsman/profile", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Craftsman")] async (ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)) return ErrorResult(403, "Craftsman profile could not be found.");
    var craftsman = await db.Craftsmen.AsNoTracking().SingleOrDefaultAsync(c => c.UserId == userId);
    return craftsman is null ? ErrorResult(403, "Craftsman profile could not be found.") : Results.Ok(new { id = craftsman.Id, name = craftsman.Name, contactInfo = craftsman.ContactInfo, dailyRate = craftsman.DailyRate, status = craftsman.Status.ToString() });
});

app.MapPut("/craftsman/profile", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Craftsman")] async (ClaimsPrincipal user, HttpContext http, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)) return ErrorResult(403, "Craftsman profile could not be found.");
    var craftsman = await db.Craftsmen.SingleOrDefaultAsync(c => c.UserId == userId);
    if (craftsman is null) return ErrorResult(403, "Craftsman profile could not be found.");
    using var document = await JsonDocument.ParseAsync(http.Request.Body);
    var allowed = new[] { "name", "contactInfo", "dailyRate" };
    if (document.RootElement.EnumerateObject().Any(property => !allowed.Contains(property.Name, StringComparer.OrdinalIgnoreCase))) return ErrorResult(400, "Status and crafts can't be changed here.");
    var root = document.RootElement;
    var name = root.TryGetProperty("name", out var nameValue) ? nameValue.GetString()?.Trim() : craftsman.Name;
    if (string.IsNullOrWhiteSpace(name)) return ErrorResult(400, "Name is required.", "name");
    craftsman.Name = name;
    craftsman.ContactInfo = root.TryGetProperty("contactInfo", out var contactValue) ? contactValue.GetString()?.Trim() : craftsman.ContactInfo;
    if (root.TryGetProperty("dailyRate", out var rateValue) && (!rateValue.TryGetDecimal(out var rate) || rate < 0)) return ErrorResult(400, "Daily rate must be zero or greater.", "dailyRate");
    if (root.TryGetProperty("dailyRate", out rateValue)) craftsman.DailyRate = decimal.Round(rateValue.GetDecimal(), 2);
    await db.SaveChangesAsync();
    return Results.Ok(new { id = craftsman.Id, name = craftsman.Name, contactInfo = craftsman.ContactInfo, dailyRate = craftsman.DailyRate, status = craftsman.Status.ToString() });
});

app.MapGet("/customers/profile", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Customer")] async (ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)) return ErrorResult(403, "Customer profile could not be found.");
    var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.UserId == userId);
    return customer is null ? ErrorResult(403, "Customer profile could not be found.") : Results.Ok(new { id = customer.Id, name = customer.Name, contactInfo = customer.ContactInfo });
});

app.MapPut("/customers/profile", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Customer")] async (ClaimsPrincipal user, HttpContext http, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)) return ErrorResult(403, "Customer profile could not be found.");
    var customer = await db.Customers.SingleOrDefaultAsync(c => c.UserId == userId);
    if (customer is null) return ErrorResult(403, "Customer profile could not be found.");
    using var document = await JsonDocument.ParseAsync(http.Request.Body);
    var allowed = new[] { "name", "contactInfo" };
    if (document.RootElement.EnumerateObject().Any(property => !allowed.Contains(property.Name, StringComparer.OrdinalIgnoreCase))) return ErrorResult(400, "Only name and contact information can be changed here.");
    var root = document.RootElement;
    var name = root.TryGetProperty("name", out var nameValue) ? nameValue.GetString()?.Trim() : customer.Name;
    if (string.IsNullOrWhiteSpace(name)) return ErrorResult(400, "Name is required.", "name");
    customer.Name = name;
    customer.ContactInfo = root.TryGetProperty("contactInfo", out var contactValue) ? contactValue.GetString()?.Trim() : customer.ContactInfo;
    await db.SaveChangesAsync();
    return Results.Ok(new { id = customer.Id, name = customer.Name, contactInfo = customer.ContactInfo });
});

app.MapPost("/helper/ask", [Microsoft.AspNetCore.Authorization.Authorize] async (ClaimsPrincipal user, HttpContext http) =>
{
    using var document = await JsonDocument.ParseAsync(http.Request.Body);
    if (!document.RootElement.TryGetProperty("question", out var questionValue) || string.IsNullOrWhiteSpace(questionValue.GetString())) return ErrorResult(400, "Question is required.", "question");
    var question = questionValue.GetString()!.ToLowerInvariant();
    var role = user.FindFirstValue("role");
    if (role == "Craftsman")
    {
        if (question.Contains("request")) return Results.Ok(new { answer = "Review incoming work from your requests screen.", link = "/craftsman/requests" });
        if (question.Contains("board")) return Results.Ok(new { answer = "Your board summarizes your current requests.", link = "/craftsman/board" });
        return Results.Ok(new { answer = "You can manage requests, your board, and your day sheet from the craftsman area." });
    }
    if (role == "Customer")
    {
        if (question.Contains("request")) return Results.Ok(new { answer = "Your requests and their answers are available in your customer area.", link = "/customer/requests" });
        return Results.Ok(new { answer = "Browse craftsmen, send requests, and review their answers from the customer area." });
    }
    return Results.Ok(new { answer = "Use the administrator area to manage the craft catalog and approvals.", link = "/home" });
});

app.MapPost("/auth/signup", async (HttpContext http, AppDbContext db) =>
{
    SignupRequest? input;

    try
    {
        input = await http.Request.ReadFromJsonAsync<SignupRequest>();
    }
    catch (Exception)
    {
        return ErrorResult(400, "Please provide a valid signup request.", "name");
    }

    var name = input?.Name?.Trim();
    if (string.IsNullOrWhiteSpace(name))
    {
        return ErrorResult(400, "Name is required.", "name");
    }

    var email = input?.Email?.Trim();
    if (string.IsNullOrWhiteSpace(email))
    {
        return ErrorResult(400, "Email is required.", "email");
    }

    if (!new EmailAddressAttribute().IsValid(email))
    {
        return ErrorResult(400, "Please enter a valid email address.", "email");
    }

    var password = input?.Password;
    if (string.IsNullOrWhiteSpace(password))
    {
        return ErrorResult(400, "Password is required.", "password");
    }

    if (password.Length < 8)
    {
        return ErrorResult(400, "Password must be at least 8 characters long.", "password");
    }

    email = email.ToLowerInvariant();

    var requestedRole = input?.Role?.Trim();
    var effectiveRole = UserRole.Customer;

    if (!string.IsNullOrWhiteSpace(requestedRole))
    {
        if (!Enum.TryParse<UserRole>(requestedRole, ignoreCase: true, out var parsedRole))
        {
            return ErrorResult(400, "Role is invalid. Only Customer or Craftsman can sign up publicly.", "role");
        }

        effectiveRole = parsedRole;
    }

    if (effectiveRole == UserRole.Admin)
    {
        return ErrorResult(400, "Public signup cannot create an Admin account.", "role");
    }

    if (await db.Users.AnyAsync(u => u.Email.ToLower() == email))
    {
        return ErrorResult(400, "An account with this email already exists.", "email");
    }

    var hasher = new PasswordHasher<User>();
    var user = new User
    {
        Name = name,
        Email = email,
        Role = effectiveRole,
        PasswordHash = hasher.HashPassword(new User(), password)
    };

    db.Users.Add(user);
    if (effectiveRole == UserRole.Craftsman)
    {
        db.Craftsmen.Add(new Craftsman
        {
            User = user,
            Name = user.Name,
            Status = CraftsmanStatus.Pending
        });
    }
    else
    {
        db.Customers.Add(new Customer
        {
            User = user,
            Name = user.Name
        });
    }
    await db.SaveChangesAsync();

    return Results.Created($"/auth/users/{user.Id}", new
    {
        id = user.Id,
        name = user.Name,
        email = user.Email,
        role = user.Role.ToString()
    });
});

app.MapPost("/auth/login", async (HttpContext http, AppDbContext db) =>
{
    LoginRequest? input;

    try
    {
        input = await http.Request.ReadFromJsonAsync<LoginRequest>();
    }
    catch (Exception)
    {
        return ErrorResult(401, "Email or password is incorrect.");
    }

    var email = input?.Email?.Trim().ToLowerInvariant();
    var user = string.IsNullOrWhiteSpace(email)
        ? null
        : await db.Users.SingleOrDefaultAsync(candidate => candidate.Email.ToLower() == email);

    var hasher = new PasswordHasher<User>();
    if (user is null || string.IsNullOrWhiteSpace(input?.Password) ||
        hasher.VerifyHashedPassword(user, user.PasswordHash, input.Password) == PasswordVerificationResult.Failed)
    {
        return ErrorResult(401, "Email or password is incorrect.");
    }

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim("role", user.Role.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };
    var credentials = new SigningCredentials(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(jwtLifetimeMinutes),
        signingCredentials: credentials);

    return Results.Ok(new
    {
        token = new JwtSecurityTokenHandler().WriteToken(token),
        user = new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            role = user.Role.ToString()
        }
    });
});

app.Run();

static (int Skip, int Take) GetPaging(int? page, int? pageSize)
{
    var safePage = Math.Max(page ?? 1, 1);
    var safePageSize = Math.Clamp(pageSize ?? 20, 1, 20);
    return ((safePage - 1) * safePageSize, safePageSize);
}

static async Task SeedDemoDataAsync(AppDbContext db)
{
    const string markerEmail = "seed-customer-1@craftconnect.local";
    if (await db.Users.AnyAsync(user => user.Email == markerEmail))
    {
        return;
    }

    var hasher = new PasswordHasher<User>();
    var crafts = new[]
    {
        new Craft { Name = "Seed Carpentry", Description = "Woodwork and joinery" },
        new Craft { Name = "Seed Plumbing", Description = "Pipes, fixtures, and repairs" },
        new Craft { Name = "Seed Electrical", Description = "Domestic electrical work" }
    };
    db.Crafts.AddRange(crafts);

    var craftsmen = new[]
    {
        new Craftsman { Name = "Seed Carpenter", Status = CraftsmanStatus.Approved, DailyRate = 180m },
        new Craftsman { Name = "Seed Plumber", Status = CraftsmanStatus.Approved, DailyRate = 160m },
        new Craftsman { Name = "Seed Electrician", Status = CraftsmanStatus.Approved, DailyRate = 175m }
    };
    var craftsmansUsers = craftsmen.Select((craftsman, index) =>
    {
        var user = new User
        {
            Name = craftsman.Name,
            Email = $"seed-craftsman-{index + 1}@craftconnect.local",
            Role = UserRole.Craftsman
        };
        user.PasswordHash = hasher.HashPassword(user, "Seed123!");
        craftsman.User = user;
        return user;
    }).ToArray();
    db.Users.AddRange(craftsmansUsers);
    db.Craftsmen.AddRange(craftsmen);

    var customers = new[]
    {
        new Customer { Name = "Seed Customer One" },
        new Customer { Name = "Seed Customer Two" }
    };
    var customerUsers = new[]
    {
        new User { Name = customers[0].Name, Email = markerEmail, Role = UserRole.Customer },
        new User { Name = customers[1].Name, Email = "seed-customer-2@craftconnect.local", Role = UserRole.Customer }
    };
    foreach (var user in customerUsers) user.PasswordHash = hasher.HashPassword(user, "Seed123!");
    customers[0].User = customerUsers[0];
    customers[1].User = customerUsers[1];
    db.Users.AddRange(customerUsers);
    db.Customers.AddRange(customers);
    await db.SaveChangesAsync();

    db.CraftsmanCrafts.AddRange(
        new CraftsmanCraft { CraftsmanId = craftsmen[0].Id, CraftId = crafts[0].Id },
        new CraftsmanCraft { CraftsmanId = craftsmen[1].Id, CraftId = crafts[1].Id },
        new CraftsmanCraft { CraftsmanId = craftsmen[2].Id, CraftId = crafts[2].Id });

    var demoDate = DateTime.UtcNow.Date.AddDays(1);
    db.Requests.AddRange(
        new Request
        {
            CustomerId = customers[0].Id,
            CraftsmanId = craftsmen[0].Id,
            CraftId = crafts[0].Id,
            NeededOn = demoDate,
            Price = 250m,
            Description = "Seed pending request one",
            Status = RequestStatus.Pending
        },
        new Request
        {
            CustomerId = customers[1].Id,
            CraftsmanId = craftsmen[0].Id,
            CraftId = crafts[0].Id,
            NeededOn = demoDate,
            Price = 275m,
            Description = "Seed pending request two",
            Status = RequestStatus.Pending
        });
    await db.SaveChangesAsync();
}

static IResult ErrorResult(int statusCode, string message, string? fieldName = null)
{
    var errors = string.IsNullOrWhiteSpace(fieldName)
        ? new Dictionary<string, string>()
        : new Dictionary<string, string>
        {
            [fieldName] = message
        };

    return Results.Json(new
    {
        status = statusCode,
        message,
        errors
    }, statusCode: statusCode);
}

public partial class Program
{
}
