using FluentValidation;
using LiftSearch.Data.Entities;

namespace LiftSearch.Dtos;


public record DriverDto(int Id, int tripsCountDriver, int cancelledCountDriver, DateTime registeredDriverDate, DateTime? lastTripDate, string? driverBio, string name, string email);
public record CreateDriverDto(int? travelerId, string? driverBio);
public record UpdateDriverDto(string? driverBio);

public class CreateDriverDtoValidator : AbstractValidator<CreateDriverDto>
{
    public CreateDriverDtoValidator()
    {
        RuleFor(dto => dto.driverBio).MaximumLength(200);
    }
}

public class UpdateDriverDtoValidator : AbstractValidator<UpdateDriverDto>
{
    public UpdateDriverDtoValidator()
    {
        RuleFor(dto => dto.driverBio).MaximumLength(200);
    }
}