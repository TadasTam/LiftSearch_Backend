using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentValidation;
using LiftSearch.Auth;
using LiftSearch.Auth.Model;
using LiftSearch.Data;
using LiftSearch.Data.Entities;
using LiftSearch.Data.Entities.Enums;
using LiftSearch.Dtos;
using LiftSearch.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using O9d.AspNet.FluentValidation;
using static FluentValidation.DependencyInjectionExtensions;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
builder.Services.AddDbContext<LsDbContext>();
builder.Services.AddTransient<JwtTokenService>();
builder.Services.AddScoped<AuthDbSeeder>();


var services = new ServiceCollection();

builder.Services.AddValidatorsFromAssemblyContaining<UserDto>();
builder.Services.AddValidatorsFromAssemblyContaining<DriverDto>();
builder.Services.AddValidatorsFromAssemblyContaining<TripDto>();
builder.Services.AddValidatorsFromAssemblyContaining<PassengerDto>();



builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<LsDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters.ValidAudience = builder.Configuration["Jwt:ValidAudience"];
    options.TokenValidationParameters.ValidIssuer = builder.Configuration["Jwt:ValidIssuer"];
    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]));
});


 
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
);
    
app.UseAuthentication();
app.UseAuthorization();

/*
app.UseExceptionHandler(c => c.Run(async context =>
{
    var exception = context.Features
        .Get<IExceptionHandlerFeature>()
        ?.Error;
    if (exception is not null)
    {
        var response = new { error = exception.Message };
        context.Response.StatusCode = 400;

        await context.Response.WriteAsJsonAsync(response);
    }
}));
*/

app.AddAutApi();

var driversGroup = app.MapGroup("/api").WithValidationFilter();
DriverEndpoints.AddDriverApi(driversGroup);

var travelerGroup = app.MapGroup("/api").WithValidationFilter();
TravelerEndpoint.AddTravelerApi(travelerGroup);

var tripsGroup = app.MapGroup("/api/drivers/{driverId}").WithValidationFilter();
TripEndpoints.AddTripApi(tripsGroup);

var passengersGroup = app.MapGroup("/api/drivers/{driverId}/trips/{tripId}").WithValidationFilter();
PassengerEndpoints.AddPassengerApi(passengersGroup);

var additionalGroup = app.MapGroup("/api").WithValidationFilter();
AdditionalEndpoints.AddAdditionalApi(additionalGroup);

using var scope = app.Services.CreateScope();

var dbContext = scope.ServiceProvider.GetRequiredService<LsDbContext>();
dbContext.Database.Migrate();

var dbSeeder = scope.ServiceProvider.GetRequiredService<AuthDbSeeder>();
await dbSeeder.SeedAsync();

app.Run();