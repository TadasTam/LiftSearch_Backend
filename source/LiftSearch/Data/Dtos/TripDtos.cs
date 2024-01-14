using FluentValidation;
using LiftSearch.Data.Entities;
using LiftSearch.Data.Entities.Enums;

namespace LiftSearch.Dtos;


public record TripDto(int Id, DateTime tripDate, DateTime lastEditTime, int seatsCount, int? startTime, int? endTime, double price, string description, string startCity, string endCity, TripStatus tripStatus, int driverId);
public record CreateTripDto(DateTime tripDate, int seatsCount, int? startTime, int? endTime, double price, string description, string startCity, string endCity);
public record UpdateTripDto(int? seatsCount, int? startTime, int? endTime, double? price, string description, string startCity, string endCity, TripStatus? tripStatus);

public class CreateTripDtoValidator : AbstractValidator<CreateTripDto>
{
    public CreateTripDtoValidator()
    {
        RuleFor(dto => dto.tripDate).NotEmpty().NotNull().GreaterThan(DateTime.Now);
        RuleFor(dto => dto.seatsCount).NotEmpty().NotNull().GreaterThan(0);
        RuleFor(dto => dto.startTime).InclusiveBetween(0,1440);
        RuleFor(dto => dto.endTime).InclusiveBetween(0,1440);
        RuleFor(dto => dto.price).NotEmpty().NotNull().GreaterThanOrEqualTo(0);
        RuleFor(dto => dto.description).NotNull().MaximumLength(200);
        RuleFor(dto => dto.startCity).NotEmpty().NotNull().Length(min: 4, max: 20);
        RuleFor(dto => dto.endCity).NotEmpty().NotNull().Length(min: 4, max: 20);
    }
}

public class UpdateTripDtoValidator : AbstractValidator<UpdateTripDto>
{
    public UpdateTripDtoValidator()
    {
        RuleFor(dto => dto.seatsCount).NotEmpty().GreaterThan(0);
        RuleFor(dto => dto.startTime).InclusiveBetween(0,1440);
        RuleFor(dto => dto.endTime).InclusiveBetween(0,1440);
        RuleFor(dto => dto.price).NotEmpty().GreaterThanOrEqualTo(0);
        RuleFor(dto => dto.description).MaximumLength(200);
        RuleFor(dto => dto.startCity).NotEmpty().Length(min: 4, max: 20);
        RuleFor(dto => dto.endCity).NotEmpty().Length(min: 4, max: 20);
        RuleFor(dto => dto.tripStatus).IsInEnum();
    }
}