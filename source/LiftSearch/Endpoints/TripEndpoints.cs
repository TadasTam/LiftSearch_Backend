using System.Security.Claims;
using LiftSearch.Auth;
using LiftSearch.Data;
using LiftSearch.Data.Entities;
using LiftSearch.Data.Entities.Enums;
using LiftSearch.Dtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using O9d.AspNet.FluentValidation;

namespace LiftSearch.Endpoints;

public static class TripEndpoints
{
    public static void AddTripApi(RouteGroupBuilder tripsGroup)
    {
        // GET ALL
        tripsGroup.MapGet("trips",
            async (int driverId, LsDbContext dbContext, CancellationToken cancellationToken, JwtTokenService jwtTokenService, HttpContext httpContext) =>
            {
                var claim = httpContext.User;
                string accessToken = httpContext.GetTokenAsync("access_token").Result;
                if (jwtTokenService.TryParseAccessToken(accessToken) == false) 
                    return Results.Unauthorized();
                
                var driver = await dbContext.Drivers.FirstOrDefaultAsync(driver => driver.Id == driverId, cancellationToken: cancellationToken);
                if (driver == null)
                    return Results.NotFound(new { error = "Such driver not found" });
                
                return Results.Ok(
                    (await dbContext.Trips.Where(trip => trip.Driver.Id == driverId).ToListAsync(cancellationToken))
                    .Select(trip => MakeTripDto(trip)));
            });

        // GET ONE
        tripsGroup.MapGet("trips/{tripId}",
            async (int driverId, int tripId, LsDbContext dbContext, CancellationToken cancellationToken, JwtTokenService jwtTokenService, HttpContext httpContext) =>
            {
                var claim = httpContext.User;
                string accessToken = httpContext.GetTokenAsync("access_token").Result;
                if (jwtTokenService.TryParseAccessToken(accessToken) == false) 
                    return Results.Unauthorized();
                
                var driver = await dbContext.Drivers.FirstOrDefaultAsync(driver => driver.Id == driverId, cancellationToken: cancellationToken);
                if (driver == null)
                    return Results.NotFound(new { error = "Such driver not found" });
                
                var trip = await dbContext.Trips.FirstOrDefaultAsync(trip =>
                    trip.Id == tripId && trip.Driver.Id == driverId, cancellationToken: cancellationToken);
                if (trip == null) return Results.NotFound(new { error = "Such trip not found" });

                return Results.Ok(MakeTripDto(trip));
            });

        // CREATE
        tripsGroup.MapPost("trips",
            async (int driverId, [Validate] CreateTripDto createTripDto, LsDbContext dbContext, CancellationToken cancellationToken, HttpContext httpContext, UserManager<User> userManager, JwtTokenService jwtTokenService) =>
            {
                if (createTripDto.startTime >= createTripDto.endTime) return Results.UnprocessableEntity(new { error = "Start time cannot be later then end time" });
                
                var driver = await dbContext.Drivers.FirstOrDefaultAsync(driver => driver.Id == driverId, cancellationToken: cancellationToken);
                if (driver == null)
                    return Results.NotFound(new { error = "Such driver not found" });
                
                var claim = httpContext.User;
                string accessToken = httpContext.GetTokenAsync("access_token").Result;
                if (jwtTokenService.TryParseAccessToken(accessToken) == false) 
                    return Results.Unauthorized();
                if (!claim.IsInRole(UserRoles.Driver) || claim.FindFirstValue(JwtRegisteredClaimNames.Sub) != driver.UserId)
                {
                    return Results.Forbid();
                }
                
                var user = await userManager.FindByIdAsync(claim.FindFirstValue(JwtRegisteredClaimNames.Sub));
                if (user == null) return Results.UnprocessableEntity(new { error = "Invalid token" });
                if (user.forceRelogin) return Results.Forbid();
                
                var trip = new Trip
                {
                    tripDate = DateTime.SpecifyKind(createTripDto.tripDate, DateTimeKind.Utc),
                    lastEditTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc),
                    seatsCount = createTripDto.seatsCount,
                    startTime = createTripDto.startTime,
                    endTime = createTripDto.endTime,
                    price = createTripDto.price,
                    description = createTripDto.description,
                    startCity = createTripDto.startCity,
                    endCity = createTripDto.endCity,
                    tripStatus = TripStatus.Active,
                    DriverId = driverId
                };

                dbContext.Trips.Add(trip);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Created($"/api/drivers/{driver.Id}/trips/{trip.Id}", MakeTripDto(trip));
            });

        // UPDATE
        tripsGroup.MapPut("trips/{tripId}", async (int driverId, int tripId, [Validate] UpdateTripDto updateTripDto,
            LsDbContext dbContext, CancellationToken cancellationToken, HttpContext httpContext, UserManager<User> userManager, JwtTokenService jwtTokenService) =>
        {
            var driver = await dbContext.Drivers.FirstOrDefaultAsync(driver => driver.Id == driverId, cancellationToken: cancellationToken);
            if (driver == null)
                return Results.NotFound(new { error = "Such driver not found" });

            var claim = httpContext.User;
            string accessToken = httpContext.GetTokenAsync("access_token").Result;
            if (jwtTokenService.TryParseAccessToken(accessToken) == false) 
                return Results.Unauthorized();
            if (!claim.IsInRole(UserRoles.Driver) || claim.FindFirstValue(JwtRegisteredClaimNames.Sub) != driver.UserId)
            {
                return Results.Forbid();
            }
            
            if (updateTripDto.startTime >= updateTripDto.endTime) return Results.UnprocessableEntity(new { error = "Start time cannot be later then end time" });
            
            var user = await userManager.FindByIdAsync(claim.FindFirstValue(JwtRegisteredClaimNames.Sub));
            if (user == null) return Results.UnprocessableEntity(new { error = "Invalid token" });
            if (user.forceRelogin) return Results.Unauthorized();
            
            var trip = await dbContext.Trips.FirstOrDefaultAsync(trip =>
                trip.Id == tripId && trip.Driver.Id == driverId, cancellationToken: cancellationToken);
            if (trip == null) return Results.NotFound(new { error = "Such trip not found" });

            trip.seatsCount = updateTripDto.seatsCount ?? trip.seatsCount;
            trip.lastEditTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            trip.startTime = updateTripDto.startTime ?? trip.startTime;
            trip.endTime = updateTripDto.endTime ?? trip.endTime;
            trip.price = updateTripDto.price ?? trip.price;
            trip.description = updateTripDto.description ?? trip.description;
            trip.startCity = updateTripDto.startCity ?? trip.startCity;
            trip.endCity = updateTripDto.endCity ?? trip.endCity;
            trip.tripStatus = updateTripDto.tripStatus ?? trip.tripStatus;

            dbContext.Update(trip);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(MakeTripDto(trip));
        });

        // DELETE
        tripsGroup.MapDelete("trips/{tripId}", async (int driverId, int tripId, LsDbContext dbContext, CancellationToken cancellationToken, HttpContext httpContext, UserManager<User> userManager, JwtTokenService jwtTokenService) =>
        {
            //TODO shouldnt be able to delete trip if it has active passengers
            var driver = await dbContext.Drivers.FirstOrDefaultAsync(driver => driver.Id == driverId, cancellationToken: cancellationToken);
            if (driver == null)
                return Results.NotFound(new { error = "Such driver not found" });
            
            var claim = httpContext.User;
            string accessToken = httpContext.GetTokenAsync("access_token").Result;
            if (jwtTokenService.TryParseAccessToken(accessToken) == false) 
                return Results.Unauthorized();
            if (!claim.IsInRole(UserRoles.Driver) || claim.FindFirstValue(JwtRegisteredClaimNames.Sub) != driver.UserId)
            {
                return Results.Forbid();
            }
            
            var user = await userManager.FindByIdAsync(claim.FindFirstValue(JwtRegisteredClaimNames.Sub));
            if (user == null) return Results.UnprocessableEntity(new { error = "Invalid token" });
            if (user.forceRelogin) return Results.Unauthorized();
            
            var trip = await dbContext.Trips.FirstOrDefaultAsync(trip =>
                trip.Id == tripId && trip.Driver.Id == driverId, cancellationToken: cancellationToken);
            if (trip == null) return Results.NotFound(new { error = "Such trip not found" });

            incrementCancelledTrips(driver, dbContext);
            
            dbContext.Remove(trip);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });
    }
    
    public static TripDto MakeTripDto (Trip trip)
    {
        return new TripDto(trip.Id, trip.tripDate, trip.lastEditTime, trip.seatsCount, trip.startTime, trip.endTime, trip.price, trip.description, trip.startCity, trip.endCity, trip.tripStatus, trip.DriverId);
    }
    
    public static void incrementCancelledTrips(Driver driver, LsDbContext dbContext)
    {
        driver.cancelledCountDriver += 1;
        dbContext.Update(driver);
    }
}