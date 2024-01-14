using LiftSearch.Data;
using LiftSearch.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LiftSearch.Auth.Model;

public class AuthDbSeeder
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AuthDbSeeder(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        await AddDefaultRoles();
        await AddAdminUser();
    }

    private async Task AddDefaultRoles()
    {
        foreach (var role in UserRoles.All)
        {
            var roleExists = await _roleManager.RoleExistsAsync(role);
            if (!roleExists) await _roleManager.CreateAsync(new IdentityRole(role));
        }
    }
    
    private async Task AddAdminUser()
    {
        var newAdminUser = new User
        {
            UserName = "admin2",
            Email = "admin2@admin.com"
        };

        var existingAdminUser = await _userManager.FindByNameAsync(newAdminUser.UserName);
        if (existingAdminUser == null)
        {
            var createAdminUserResult = await _userManager.CreateAsync(newAdminUser, "Admin22+");
            if (createAdminUserResult.Succeeded) await _userManager.AddToRoleAsync(newAdminUser, UserRoles.Admin);
            /*
            var traveler = new Traveler()
            {
                cancelledCountTraveler = 0,
                registrationDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc),
                lastTripDate = null,
                travelerBio = null,
                UserId = newAdminUser.Id
            };
            _dbContext.Travelers.Add(traveler);
            
            var driver = new Driver()
            {
                cancelledCountDriver = 0,
                registeredDriverDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc),
                lastTripDate = null,
                driverBio = null,
                UserId = newAdminUser.Id
            };

            _dbContext.Drivers.Add(driver);
            await _dbContext.SaveChangesAsync();*/
        }
    }
}