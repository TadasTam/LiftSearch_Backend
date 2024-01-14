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

public static class AdditionalEndpoints
{
    public static void AddAdditionalApi(RouteGroupBuilder aditionalGroup)
    {
        // GET ALL TRIPS
        aditionalGroup.MapGet("trips",
            async (LsDbContext dbContext, CancellationToken cancellationToken, JwtTokenService jwtTokenService, HttpContext httpContext) =>
            {
                var claim = httpContext.User;
                string accessToken = httpContext.GetTokenAsync("access_token").Result;
                if (jwtTokenService.TryParseAccessToken(accessToken) == false) 
                    return Results.Unauthorized();
                
                return Results.Ok(
                    (await dbContext.Trips.ToListAsync(cancellationToken))
                    .Select(trip => MakeTripDto(trip)));
            });
        
        
    }
    
    public static TripDto MakeTripDto (Trip trip)
    {
        return new TripDto(trip.Id, trip.tripDate, trip.lastEditTime, trip.seatsCount, trip.startTime, trip.endTime, trip.price, trip.description, trip.startCity, trip.endCity, trip.tripStatus, trip.DriverId);
    }
}