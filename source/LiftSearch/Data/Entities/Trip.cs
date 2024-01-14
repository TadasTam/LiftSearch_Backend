using System.ComponentModel.DataAnnotations;
using LiftSearch.Data.Entities.Enums;

namespace LiftSearch.Data.Entities;

public class Trip
{
    public int Id { get; set; }
    public required DateTime tripDate { get; set; }
    public required DateTime lastEditTime { get; set; }
    public required int seatsCount { get; set; }
    public int? startTime { get; set; }
    public int? endTime { get; set; }
    public required double price { get; set; }
    public string description { get; set; }
    public required string startCity { get; set; }
    public required string endCity { get; set; }
    public required TripStatus tripStatus { get; set; }
    
    [Required]
    public required int DriverId { get; set; }
    public Driver Driver { get; set; }
}




