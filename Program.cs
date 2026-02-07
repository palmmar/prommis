using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Prommis.Data;
using Prommis.Models;
using Prommis.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddScoped<StatisticsService>();
builder.Services.AddRazorPages();

var app = builder.Build();

// Seed test data in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    db.Database.Migrate();

    // Seed roles (idempotent)
    foreach (var roleName in new[] { "User", "Administrator" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    if (!db.Users.Any())
    {
        var users = new (string Name, string Email)[]
        {
            ("Anna Johansson", "anna@test.com"),
            ("Erik Lindberg", "erik@test.com"),
            ("Sofia Nilsson", "sofia@test.com"),
            ("Oscar Bergström", "oscar@test.com"),
        };

        var createdUsers = new List<ApplicationUser>();
        foreach (var (name, email) in users)
        {
            var user = new ApplicationUser { UserName = email, Email = email };
            await userManager.CreateAsync(user, "Test123!");
            createdUsers.Add(user);
        }

        // Assign roles: Anna = Administrator, others = User
        await userManager.AddToRoleAsync(createdUsers[0], "Administrator");
        for (var i = 1; i < createdUsers.Count; i++)
        {
            await userManager.AddToRoleAsync(createdUsers[i], "User");
        }

        var group = new Group
        {
            Name = "Prommis-gänget",
            OwnerId = createdUsers[0].Id,
            CreatedAt = DateTime.UtcNow.AddYears(-1),
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        for (var i = 0; i < createdUsers.Count; i++)
        {
            db.GroupMemberships.Add(new GroupMembership
            {
                GroupId = group.Id,
                UserId = createdUsers[i].Id,
                Role = i == 0 ? GroupRole.Owner : GroupRole.Member,
                JoinedAt = DateTime.UtcNow.AddYears(-1),
            });
        }
        await db.SaveChangesAsync();

        // Generate step data for the past year with realistic patterns
        var rng = new Random(42);
        var today = DateTime.UtcNow.Date;
        var entries = new List<StepEntry>();

        // Base daily steps and variance per user (some walk more than others)
        var profiles = new (int BaseSteps, int Variance)[]
        {
            (10000, 4000),  // Anna  – active
            (7000,  3000),  // Erik  – moderate
            (12000, 5000),  // Sofia – very active
            (5500,  2500),  // Oscar – casual
        };

        for (var i = 0; i < createdUsers.Count; i++)
        {
            var (baseSteps, variance) = profiles[i];
            for (var d = 0; d < 365; d++)
            {
                var date = today.AddDays(-364 + d);
                var dayOfWeek = date.DayOfWeek;

                // Weekend boost / weekday dip
                var modifier = dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 1.2 : 1.0;

                var steps = (int)(baseSteps * modifier + rng.Next(-variance, variance));
                steps = Math.Clamp(steps, 500, 35000);

                entries.Add(new StepEntry
                {
                    UserId = createdUsers[i].Id,
                    Date = date,
                    Steps = steps,
                    CreatedAt = date.AddHours(20),
                });
            }
        }

        db.StepEntries.AddRange(entries);
        await db.SaveChangesAsync();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
