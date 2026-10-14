using BookLibrary.Domain.Constants;
using BookLibrary.Domain.Entities;
using BookLibrary.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookLibrary.Infrastructure.Persistence;

public class DatabaseInitializer
{
    private readonly AppDbContext _db;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        AppDbContext db,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _roleManager = roleManager;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_db.Database.IsRelational())
        {
            await _db.Database.MigrateAsync(ct);
        }
        else
        {
            await _db.Database.EnsureCreatedAsync(ct);
        }

        await SeedRolesAsync();
        await SeedInitialAdminIfConfiguredAsync();
        await SeedReferenceLanguagesAsync(ct);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in new[] { RoleNames.Admin, RoleNames.User })
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    private async Task SeedInitialAdminIfConfiguredAsync()
    {
        var adminEmail = _configuration["AdminBootstrap:Email"];
        var adminPassword = _configuration["AdminBootstrap:Password"];
        var adminDisplayName = _configuration["AdminBootstrap:DisplayName"] ?? "System Administrator";

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            _logger.LogInformation("No AdminBootstrap credentials configured in environment/configuration. Skipping initial admin bootstrap.");
            return;
        }

        var existingAdmin = await _userManager.FindByEmailAsync(adminEmail.Trim());
        if (existingAdmin != null)
        {
            if (!await _userManager.IsInRoleAsync(existingAdmin, RoleNames.Admin))
            {
                await _userManager.AddToRoleAsync(existingAdmin, RoleNames.Admin);
            }
            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = adminEmail.Trim(),
            Email = adminEmail.Trim(),
            DisplayName = adminDisplayName.Trim(),
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(adminUser, RoleNames.Admin);
            _logger.LogInformation("Bootstrapped initial Administrator account for {Email}.", adminEmail);
        }
        else
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Failed to bootstrap initial Administrator account: {Errors}", errors);
        }
    }

    private async Task SeedReferenceLanguagesAsync(CancellationToken ct)
    {
        if (await _db.Languages.AnyAsync(ct))
            return;

        _db.Languages.AddRange(
            new Language { Name = "English", Code = "en", NativeName = "English", IsActive = true },
            new Language { Name = "Georgian", Code = "ka", NativeName = "ქართული", IsActive = true },
            new Language { Name = "Spanish", Code = "es", NativeName = "Español", IsActive = true },
            new Language { Name = "French", Code = "fr", NativeName = "Français", IsActive = true },
            new Language { Name = "German", Code = "de", NativeName = "Deutsch", IsActive = true }
        );
        await _db.SaveChangesAsync(ct);
    }
}
