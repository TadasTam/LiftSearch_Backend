using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LiftSearch.Auth;

public class JwtTokenService
{
    private readonly SymmetricSecurityKey _authSigningKey;
    private readonly string _issuer;
    private readonly string _audience;
    
    public JwtTokenService(IConfiguration configuration)
    {
        _audience = configuration["Jwt:ValidAudience"];
        _issuer = configuration["Jwt:ValidIssuer"];
        _authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]));
    }
    
    public string CreateAccessToken(string userName, string userId, IEnumerable<string> roles, int driverid, int travelerid)
    {
        
        var authClaims = new List<Claim>()
        {
            new(ClaimTypes.Name, userName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, userId),
            new("driverid", driverid.ToString()),
            new("travelerid", travelerid.ToString()),
        };
        
        authClaims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(

            issuer: _issuer,
            audience: _audience,
            expires: DateTime.UtcNow.AddMinutes(5),
            claims: authClaims,
            signingCredentials: new SigningCredentials(_authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public string CreateRefreshToken(string userId)
    {
        var authClaims = new List<Claim>()
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, userId)
        };
        
        var token = new JwtSecurityToken(

            issuer: _issuer,
            audience: _audience,
            expires: DateTime.UtcNow.AddHours(24),
            claims: authClaims,
            signingCredentials: new SigningCredentials(_authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


    public bool TryParseRefreshToken(string refreshToken, out ClaimsPrincipal? claims, out bool expired)
    {
        claims = null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = _authSigningKey,
                ValidateLifetime = true
            };

            claims = tokenHandler.ValidateToken(refreshToken, validationParameters, out _);
            expired = false;
            return true;
        }
        catch (SecurityTokenExpiredException e)
        {
            expired = true;
            return false;
        }
        catch
        {
            expired = false;
            return false;
        }
    }
    
    public bool TryParseAccessToken(string AccessToken)
    {
        //claims = null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = _authSigningKey,
                ValidateLifetime = true
            };

            tokenHandler.ValidateToken(AccessToken, validationParameters, out _);
            return true;
        }
        catch (SecurityTokenExpiredException e)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }
}