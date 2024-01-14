using System.Data.Common;
using LiftSearch.Data.Entities;
using LiftSearch.Data.Entities.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LiftSearch.Data;

public class LsDbContext : IdentityDbContext<User>
{
    private readonly IConfiguration _configuration;
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<Traveler> Travelers { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<Passenger> Passengers { get; set; }
    
 //   public DbSet<User> Users { get; set; }

    public LsDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_configuration.GetConnectionString("PostgreSQL"));
    }
    
}