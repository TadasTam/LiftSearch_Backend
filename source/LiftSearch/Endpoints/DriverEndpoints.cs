using System.Security.Claims;
using LiftSearch.Auth;
using LiftSearch.Data;
using LiftSearch.Data.Entities;
using LiftSearch.Data.Entities.Enums;
using LiftSearch.Dtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.IdentityModel.JsonWebTokens;
using O9d.AspNet.FluentValidation;

namespace LiftSearch.Endpoints;

public static class DriverEndpoints
{
    
    public static void AddDriverApi(RouteGroupBuilder driversGroup)
    {
        // GET ALL
        driversGroup.MapGet("drivers",
            async (LsDbContext dbContext, CancellationToken cancellationToken, JwtTokenService jwtTokenService, HttpContext httpContext) =>
            {
                var claim = httpContext.User;
                string accessToken = httpContext.GetTokenAsync("access_token").Result;
                if (jwtTokenService.TryParseAccessToken(accessToken) == false)
                    return Results.Unauthorized();
                
                return Results.Ok(
                    (await dbContext.Drivers.Include(driver => driver.User).ToListAsync(cancellationToken)).Select(
                        driver =>
                            MakeDriverDto(driver, dbContext)));
            });

        // GET ONE
        driversGroup.MapGet("drivers/{driverId}",
            async (int driverId, LsDbContext dbContext, CancellationToken cancellationToken, JwtTokenService jwtTokenService, HttpContext httpContext) =>
            {
                var claim = httpContext.User;
                string accessToken = httpContext.GetTokenAsync("access_token").Result;
                if (jwtTokenService.TryParseAccessToken(accessToken) == false) 
                    return Results.Unauthorized();
                
                var driver = await dbContext.Drivers.Include(driver => driver.User).FirstOrDefaultAsync(driver => driver.Id == driverId, cancellationToken: cancellationToken);
                if (driver == null)
                    return Results.NotFound(new { error = "Such driver not found" });

                return Results.Ok(MakeDriverDto(driver, dbContext));
            });
        
        // GET PASSENGERS
        driversGroup.MapGet("drivers/{driverId}/passengers",
            async (int driverId, LsDbContext dbContext, CancellationToken cancellationToken, HttpContext httpContext, UserManager<User> userManager, JwtTokenService jwtTokenService) =>
            {
                var claim = httpContext.User;
                string accessToken = httpContext.GetTokenAsync("access_token").Result;
                if (jwtTokenService.TryParseAccessToken(accessToken) == false) 
                    return Results.Unauthorized();
                
                var driver = await dbContext.Drivers.Include(driver => driver.User).FirstOrDefaultAsync(driver => driver.Id == driverId, cancellationToken: cancellationToken);
                if (driver == null)
                    return Results.NotFound(new { error = "Such driver not found"});
                
                if (!claim.IsInRole(UserRoles.Admin) && (!claim.IsInRole(UserRoles.Driver) || claim.FindFirstValue(JwtRegisteredClaimNames.Sub) != driver.UserId))
                {
                    return Results.Forbid();
                }
                
                var user = await userManager.FindByIdAsync(claim.FindFirstValue(JwtRegisteredClaimNames.Sub));
                if (user == null) return Results.UnprocessableEntity(new { error = "Invalid token" });
                if (user.forceRelogin) return Results.Forbid();

                return Results.Ok((await dbContext.Passengers.Include(p => p.trip.Driver).Include(p => p.Traveler).Where(p => p.trip.DriverId == driverId).ToListAsync(cancellationToken)).Select(passenger => PassengerEndpoints.MakePassengerDto(passenger)));
            });
        
        // CREATE
        driversGroup.MapPost("drivers", async ([Validate] CreateDriverDto createDriverDto, LsDbContext dbContext, CancellationToken cancellationToken, UserManager<User> userManager, HttpContext httpContext, JwtTokenService jwtTokenService) =>
        {
            var claim = httpContext.User;
            string accessToken = httpContext.GetTokenAsync("access_token").Result;
            if (jwtTokenService.TryParseAccessToken(accessToken) == false) 
                return Results.Unauthorized();
            string? userId;

            if (claim.IsInRole(UserRoles.Admin) && createDriverDto.travelerId != null)
            {
                var traveler = await dbContext.Travelers.FirstOrDefaultAsync(t => t.Id == createDriverDto.travelerId, cancellationToken: cancellationToken);
                if (traveler == null) return Results.NotFound(new { error = "Such traveler does not exist"});
                userId = traveler.UserId;
            }
            else if (claim.IsInRole(UserRoles.Traveler) && !claim.IsInRole(UserRoles.Driver))
                userId = claim.FindFirstValue(JwtRegisteredClaimNames.Sub);
            else
                return Results.Forbid();
            
            var user = await dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken: cancellationToken);
            if (user == null) return Results.NotFound(new { error = "Such user not found"});
            
            var driverCheck = await dbContext.Drivers.FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken: cancellationToken);
            if (driverCheck != null) return Results.UnprocessableEntity(new { error = "This user is already a driver"});
            
            var driver = new Driver()
            {
                cancelledCountDriver = 0,
                registeredDriverDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc),
                lastTripDate = null,
                driverBio = null,
                UserId = user.Id
            };

            dbContext.Drivers.Add(driver);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            await userManager.AddToRoleAsync(user, UserRoles.Driver);

            return Results.Created($"/api/drivers/{driver.Id}",MakeDriverDto(driver, dbContext));
        });
        
        // UPDATE
        driversGroup.MapPut("drivers/{driverId}",
            async (int driverId, [Validate] UpdateDriverDto updateDriverDto, LsDbContext dbContext, CancellationToken cancellationToken, HttpContext httpContext, UserManager<User> userManager, JwtTokenService jwtTokenService) =>
            {
                var claim = httpContext.User;
                string accessToken = httpContext.GetTokenAsync("access_token").Result;
                if (jwtTokenService.TryParseAccessToken(accessToken) == false) 
                    return Results.Unauthorized();
                
                var driver = await dbContext.Drivers.Include(driver => driver.User).FirstOrDefaultAsync(driver => driver.Id == driverId, cancellationToken: cancellationToken);
                if (driver == null)
                    return Results.NotFound(new { error = "Such driver not found"});
                
                if (!claim.IsInRole(UserRoles.Driver) || claim.FindFirstValue(JwtRegisteredClaimNames.Sub) != driver.UserId)
                {
                    return Results.Forbid();
                }
                
                var user = await userManager.FindByIdAsync(claim.FindFirstValue(JwtRegisteredClaimNames.Sub));
                if (user == null) return Results.UnprocessableEntity(new { error = "Invalid token" });
                if (user.forceRelogin) return Results.Unauthorized();

                driver.driverBio = updateDriverDto.driverBio ?? driver.driverBio;

                dbContext.Update(driver);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Ok(MakeDriverDto(driver, dbContext));
            });

        // DELETE
        driversGroup.MapDelete("drivers/{driverId}", async (int driverId, LsDbContext dbContext, CancellationToken cancellationToken, UserManager<User> userManager, HttpContext httpContext, JwtTokenService jwtTokenService) =>
        {
            var claim = httpContext.User;
            string accessToken = httpContext.GetTokenAsync("access_token").Result;
            if (jwtTokenService.TryParseAccessToken(accessToken) == false) 
                return Results.Unauthorized();
            
            var driver = await dbContext.Drivers.Include(driver => driver.User).FirstOrDefaultAsync(driver => driver.Id == driverId, cancellationToken: cancellationToken);
            if (driver == null)
                return Results.NotFound(new { error = "Such driver not found"});
            
            if (!claim.IsInRole(UserRoles.Admin) && (!claim.IsInRole(UserRoles.Driver) || claim.FindFirstValue(JwtRegisteredClaimNames.Sub) != driver.UserId))
            {
                return Results.Forbid();
            }

            var user = await userManager.FindByIdAsync(claim.FindFirstValue(JwtRegisteredClaimNames.Sub));
            if (user == null) return Results.UnprocessableEntity(new { error = "Invalid token" });
            if (user.forceRelogin) return Results.Unauthorized();
            
            var countActiveTrips = dbContext.Trips.Include(t => t.Driver).Count(t => t.DriverId == driverId && t.tripStatus == TripStatus.Active);
            if (countActiveTrips != 0)
                return Results.UnprocessableEntity(new { error = "Driver can't be removed because he has active trips"});
            
            await userManager.RemoveFromRoleAsync(driver.User, UserRoles.Driver);

            dbContext.Remove(driver);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });
    }
    
    public static DriverDto MakeDriverDto (Driver driver, LsDbContext dbContext)
    {
        return new DriverDto(driver.Id, GetCompletedTripsCount(driver, dbContext), driver.cancelledCountDriver, driver.registeredDriverDate, driver.lastTripDate, driver.driverBio, driver.User.UserName, driver.User.Email);
    }
    
    public static int GetCompletedTripsCount(Driver driver, LsDbContext dbContext)
    {
        return dbContext.Trips.Include(t => t.Driver).Count(t => t.DriverId == driver.Id && t.tripStatus == TripStatus.Finished);
    }
}