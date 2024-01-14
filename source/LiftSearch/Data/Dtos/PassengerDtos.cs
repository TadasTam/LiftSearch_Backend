using FluentValidation;
using LiftSearch.Data.Entities;
using LiftSearch.Data.Entities.Enums;

namespace LiftSearch.Dtos;

public record PassengerDto(int Id, bool registrationStatus, string startCity, string endCity, string? startAdress, string? endAdress, string? comment, int travelerId, int tripId, int driverId);
public record CreatePassengerDto(string startCity, string endCity, string? startAdress, string? endAdress, string? comment);
public record UpdatePassengerDto(bool? registrationStatus, string startCity, string endCity, string? startAdress, string? endAdress, string? comment);

public class CreatePassengerDtoValidator : AbstractValidator<CreatePassengerDto>
{
    public CreatePassengerDtoValidator()
    {
        RuleFor(dto => dto.startCity).NotEmpty().NotNull().Length(min: 4, max: 20);
        RuleFor(dto => dto.endCity).NotEmpty().NotNull().Length(min: 4, max: 20);
        RuleFor(dto => dto.startAdress).Length(min: 4, max: 30);
        RuleFor(dto => dto.endAdress).Length(min: 4, max: 30);
        RuleFor(dto => dto.comment).MaximumLength(200);
    }
}

public class UpdatePassengerDtoValidator : AbstractValidator<UpdatePassengerDto>
{
    public UpdatePassengerDtoValidator()
    {
        RuleFor(dto => dto.startCity).NotEmpty().Length(min: 4, max: 20);
        RuleFor(dto => dto.endCity).NotEmpty().Length(min: 4, max: 20);
        RuleFor(dto => dto.startAdress).Length(min: 4, max: 30);
        RuleFor(dto => dto.endAdress).Length(min: 4, max: 30);
        RuleFor(dto => dto.comment).MaximumLength(200);
    }
}
