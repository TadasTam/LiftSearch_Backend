using FluentValidation;
using LiftSearch.Data.Entities;

namespace LiftSearch.Dtos;


public record TravelerDto(int Id, int tripsCountTraveler, int cancelledCountTraveler, DateTime registeredDate, DateTime? lastTripDate, string? driverBio, string name, string email);
public record CreateTravelerDto(string userId, string? travelerBio);
public record UpdateTravelerDto(string? travelerBio);

public class CreateTravelerDtoValidator : AbstractValidator<CreateTravelerDto>
{
    public CreateTravelerDtoValidator()
    {
        RuleFor(dto => dto.travelerBio).MaximumLength(200);
    }
}

public class UpdateTravelerDtoValidator : AbstractValidator<UpdateTravelerDto>
{
    public UpdateTravelerDtoValidator()
    {
        RuleFor(dto => dto.travelerBio).MaximumLength(200);
    }
}