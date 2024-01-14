using System.ComponentModel.DataAnnotations;
using LiftSearch.Data.Entities.Enums;

namespace LiftSearch.Data.Entities;

public class Passenger
{
    public int Id { get; set; }
    
    public bool registrationStatus { get; set; }
    public required string startCity { get; set; }
    public required string endCity { get; set; }
    public string? startAdress { get; set; }
    public string? endAdress { get; set; }
    public string? comment { get; set; }
    
    public required Trip trip { get; set; }
    
    [Required]
    public required int TravelerId { get; set; }
    
    public Traveler Traveler { get; set; }
    
    
   // public required User traveler { get; set; }
}
