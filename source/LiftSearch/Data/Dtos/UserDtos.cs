using FluentValidation;
using LiftSearch.Data.Entities;
using LiftSearch.Data.Entities.Enums;

namespace LiftSearch.Dtos;


// public record CreateUserDto(string name, string lastname, string email, string phone);
// public record UpdateUserDto(string name, string lastname, string email, string phone);





public record UserDto(string UserId, string Username, string Email);
public record RegisterUserDto(string Username, string Password, string Email);
public record LoginUserDto(string Username, string Password);
public record LogoutUserDto(string RefreshToken);
public record RefreshAccessTokenDto(string RefreshToken);

public record SuccesfullLoginDto(string AccessToken, string RefreshToken);