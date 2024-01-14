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

public static class PassengerEndpoints
{
    public static void AddPassengerApi(RouteGroupBuilder passengerGroup)
    {
        // GET ALL
        passengerGroup.MapGet("passengers",
            async (int driverId, int tripId, LsDbContext dbContext, CancellationToken cancellationToken, HttpContext httpContext, UserManager<User> userManager, JwtTokenService jwtTokenService) =>
            {
                var claim = httpContext.User;
                string accessToken = httpContext.GetTokenAsync("access_token").Result;
                if (jwtTokenService.TryParseAccessToken(accessToken) == false) 
                    return Results.Unauthorized();
                
                var driver = await dbContext.Drivers.FirstOrDefaultAsync(driver => driver.Id == driverId, cancellationToken: cancellationToken);
                if (driver == null)
                    return Results.NotFound(new { error = "Such driver not found"});
                
                if (!claim.IsInRole(UserRoles.Admin) && (!claim.IsInRole(UserRoles.Driver) || claim.FindFirstValue(JwtRegisteredClaimNames.Sub) != driver.UserId))
                {
                    return Results.Forbid();
                }
                
                var user = await userManager.FindByIdAsync(claim.FindFirstValue(JwtRegisteredClaimNames.Sub));
                if (user == null) return Results.UnprocessableEntity(new { error = "Invalid token" });
                if (user.forceRelogin) return Results.Unauthorized();
                
                var trip = await dbContext.Trips.FirstOrDefaultAsync(trip =>
                    trip.Id == tripId && trip.Driver.Id == driverId, cancellationToken: cancellationToken);
                if (trip == null) return Results.NotFound(new { error = "Such trip not found"});
                
                return Results.Ok(
                    (await dbContext.Passengers
                        .Where(passenger => passenger.trip.Id == tripId && passenger.trip.Driver.Id == driverId)
                        .Include(passenger => passenger.trip).Include(passenger => passenger.Traveler).ToListAsync(cancellationToken))
                        .Select(passenger => MakePassengerDto(passenger)));
            });

        // GET ONE
        passengerGroup.MapGet("passengers/{passengerId}",
            async (int driverId, int tripId, int passengerId, LsDbContext dbContext, CancellationToken cancellationToken, HttpContext httpContext, UserManager<User> userManager, JwtTokenService jwtTokenService) =>
            {
                var claim = httpContext.User;
                string accessToken = httpContext.GetTokenAsync("access_token").Result;
                if (jwtTokenService.TryParseAccessToken(accessToken) == false) 
                    return Results.Unauthorized();
                
                var driver = await dbContext.Drivers.FirstOrDefaultAsync(driver => driver.Id == driverId, cancellationToken: cancellationToken);
                if (driver == null)
                    return Results.NotFound(new { error = "Such driver not found"});

                if (!claim.IsInRole(UserRoles.Admin) && (!claim.IsInRole(UserRoles.Driver) || claim.FindFirstValue(JwtRegisteredClaimNames.Sub) != driver.UserId))
                {
                    return Results.Forbid();
                }
                
                var user = await userManager.FindByIdAsync(claim.FindFirstValue(JwtRegisteredClaimNames.Sub));
                if (user == null) return Results.UnprocessableEntity(new { error = "Invalid token" });
                if (user.forceRelogin) return Results.Unauthorized();
                
                var trip = await dbContext.Trips.FirstOrDefaultAsync(trip =>
                    trip.Id == tripId && trip.Driver.Id == driverId, cancellationToken: cancellationToken);
                if (trip == null) return Results.NotFound(new { error = "Such trip not found"});
                
                var passenger = await dbContext.Passengers.Include(passenger => passenger.trip)
                    .Include(passenger => passenger.Traveler).FirstOrDefaultAsync(passenger =>
                    passenger.Id == passengerId && passenger.trip.Id == tripId && passenger.trip.Driver.Id == driverId, cancellationToken: cancellationToken);
                if (passenger == null) return Results.NotFound(new { error = "Such passenger not found"});

                return Results.Ok(MakePassengerDto(passenger));
            });
        
        // CREATE
        passengerGroup.MapPost("passengers",
            async (int driverId, int tripId, [Validate] CreatePassengerDto createPassengerDto, LsDbContext dbContext, CancellationToken cancellationToken, HttpContext httpContext, UserManager<User> userManager, JwtTokenService jwtTokenService) =>
            {
                var claim = httpContext.User;
                string accessToken = httpContext.GetTokenAsync("access_token").Result;
                if (jwtTokenService.TryParseAccessToken(accessToken) == false) 
                    return Results.Unauthorized();
                if (!claim.IsInRole(UserRoles.Traveler))
                {
                    return Results.Forbid();
                }
                
                var user = await userManager.FindByIdAsync(claim.FindFirstValue(JwtRegisteredClaimNames.Sub));
                if (user == null) return Results.UnprocessableEntity(new { error = "Invalid token" });
                if (user.forceRelogin) return Results.Unauthorized();
                
                var userId = claim.FindFirstValue(JwtRegisteredClaimNames.Sub);
                
                var driver = await dbContext.Drivers.Include(driver => driver.User).FirstOrDefaultAsync(driver => driver.Id == driverId, cancellationToken: cancellationToken);
                if (driver == null) return Results.NotFound(new { error = "Such driver not found" });
            
                var trip = await dbContext.Trips.FirstOrDefaultAsync(trip => trip.Id == tripId && trip.Driver.Id == driverId, cancellationToken: cancellationToken);
                if (trip == null) return Results.NotFound(new { error = "Such trip not found" });
                //TODO trip validation
                
                if(driver.User.Id == userId) return Results.UnprocessableEntity(new { error = "Driver cannot register to it's own trip" });
                
                var passengerCheck = await dbContext.Passengers.FirstOrDefaultAsync(p => p.trip.Id == tripId && p.Traveler.UserId == userId, cancellationToken: cancellationToken);
                if (passengerCheck != null) return Results.UnprocessableEntity(new { error = "This user has already registered to this trip" });
                
                var traveler = await dbContext.Travelers.FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken: cancellationToken);

                var passenger = new Passenger
                {
                    registrationStatus = false,
                    startCity = createPassengerDto.startCity,
                    endCity = createPassengerDto.endCity,
                    startAdress = createPassengerDto.startAdress,
                    endAdress = createPassengerDto.endAdress,
                    comment = createPassengerDto.comment,
                    trip = trip,
                    TravelerId = traveler.Id
                };

                dbContext.Passengers.Add(passenger);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Created($"/api/drivers/{driver.Id}/trips/{trip.Id}/passenger/{passenger.Id}", MakePassengerDto(passenger));
            });
        
        // UPDATE
        passengerGroup.MapPut("passengers/{passengerId}", async (int driverId, int tripId, int passengerId, [Validate] UpdatePassengerDto updatePassengerDto,
            LsDbContext dbContext, CancellationToken cancellationToken, HttpContext httpContext, UserManager<User> userManager, JwtTokenService jwtTokenService) =>
        {
            var claim = httpContext.User;
            string accessToken = httpContext.GetTokenAsync("access_token").Result;
            if (jwtTokenService.TryParseAccessToken(accessToken) == false) 
                return Results.Unauthorized();
            
            var user = await userManager.FindByIdAsync(claim.FindFirstValue(JwtRegisteredClaimNames.Sub));
            if (user == null) return Results.UnprocessableEntity(new { error = "Invalid token" });
            if (user.forceRelogin) return Results.Unauthorized();
            
            var driver = await dbContext.Drivers.FirstOrDefaultAsync(driver => driver.Id == driverId, cancellationToken: cancellationToken);
            if (driver == null) return Results.NotFound(new { error = "Such driver not found" });
            
            var trip = await dbContext.Trips.FirstOrDefaultAsync(trip =>
                trip.Id == tripId && trip.Driver.Id == driverId, cancellationToken: cancellationToken);
            if (trip == null) return Results.NotFound(new { error = "Such trip not found" });
            //TODO trip validation
            
            var passenger = await dbContext.Passengers.Include(passenger => passenger.trip)
                .Include(passenger => passenger.Traveler).FirstOrDefaultAsync(passenger =>
                passenger.Id == passengerId && passenger.trip.Id == tripId && passenger.trip.Driver.Id == driverId, cancellationToken: cancellationToken);
            if (passenger == null) return Results.NotFound(new { error = "Such passenger not found" });
            
            
            var userId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!httpContext.User.IsInRole(UserRoles.Traveler) || passenger.Traveler.UserId != userId)
            {
                return Results.Forbid();
            }

            passenger.registrationStatus = updatePassengerDto.registrationStatus ?? passenger.registrationStatus;
            passenger.startCity = updatePassengerDto.startCity ?? passenger.startCity;
            passenger.endCity = updatePassengerDto.endCity ?? passenger.endCity;
            passenger.startAdress = updatePassengerDto.startAdress ?? passenger.startAdress;
            passenger.endAdress = updatePassengerDto.endAdress ?? passenger.endAdress;
            passenger.comment = updatePassengerDto.comment ?? passenger.comment;

            dbContext.Update(passenger);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(MakePassengerDto(passenger));
        });

        // DELETE
        passengerGroup.MapDelete("passengers/{passengerId}", async (int driverId, int tripId, int passengerId, LsDbContext dbContext, CancellationToken cancellationToken, HttpContext httpContext, UserManager<User> userManager, JwtTokenService jwtTokenService) =>
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
            
            var passenger = await dbContext.Passengers.Include(passenger => passenger.Traveler).FirstOrDefaultAsync(passenger =>
                passenger.Id == passengerId && passenger.trip.Id == tripId && passenger.trip.DriverId == driverId, cancellationToken: cancellationToken);
            if (passenger == null) return Results.NotFound(new { error = "Such passenger not found" });

            if (!claim.IsInRole(UserRoles.Admin) && (!claim.IsInRole(UserRoles.Driver) || claim.FindFirstValue(JwtRegisteredClaimNames.Sub) != driver.UserId) && (!claim.IsInRole(UserRoles.Traveler) || claim.FindFirstValue(JwtRegisteredClaimNames.Sub) != passenger.Traveler.UserId))
            {
                return Results.Forbid();
            }
            
            var user = await userManager.FindByIdAsync(claim.FindFirstValue(JwtRegisteredClaimNames.Sub));
            if (user == null) return Results.UnprocessableEntity("Invalid token");
            if (user.forceRelogin) return Results.Unauthorized();

            incrementCancelledTrips(passenger.Traveler, dbContext);
            
            dbContext.Remove(passenger);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });
    }
    
    public static PassengerDto MakePassengerDto (Passenger passenger)
    {
        return new PassengerDto(passenger.Id, passenger.registrationStatus, passenger.startCity, passenger.endCity, passenger.startAdress, passenger.endAdress, passenger.comment, passenger.Traveler.Id, passenger.trip.Id, passenger.trip.DriverId);
    }
    
    public static void incrementCancelledTrips(Traveler traveler, LsDbContext dbContext)
    {
        traveler.cancelledCountTraveler += 1;
        dbContext.Update(traveler);
    }
}