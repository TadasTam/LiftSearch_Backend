using System.ComponentModel.DataAnnotations;

namespace LiftSearch.Data.Entities;

public class Traveler
{
    public int Id { get; set; }
    
    public required DateTime registrationDate { get; set; }
    public required int cancelledCountTraveler { get; set; }
    public DateTime? lastTripDate { get; set; }
    public string? travelerBio { get; set; }
    
    [Required]
    public required string UserId { get; set; }
    
    public User User { get; set; }
}