using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EdTechApi.Services;

public interface IJwtService
{
    string GenerateToken(int userId, string role, int tokenVersion);
    string GenerateRefreshToken(int userId, string role, int tokenVersion);
    ClaimsPrincipal? VerifyToken(string token, bool ignoreExpiration = false);
    (int userId, string role, int tokenVersion)? DecodeToken(string token);
}

public class JwtService : IJwtService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryHours;

    public JwtService(IConfiguration config)
    {
        _secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        _issuer = config["Jwt:Issuer"] ?? "EdTechApi";
        _audience = config["Jwt:Audience"] ?? "EdTechApp";
        _expiryHours = int.TryParse(config["Jwt:ExpiryHours"], out var h) ? h : 24;
    }

    public string GenerateToken(int userId, string role, int tokenVersion)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim("tokenVersion", tokenVersion.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_expiryHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken(int userId, string role, int tokenVersion)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim("tokenVersion", tokenVersion.ToString()),
            new Claim("refresh", "true")
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? VerifyToken(string token, bool ignoreExpiration = false)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var handler = new JwtSecurityTokenHandler();
            var result = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = !ignoreExpiration,
                ClockSkew = TimeSpan.Zero
            }, out _);
            return result;
        }
        catch
        {
            return null;
        }
    }

    public (int userId, string role, int tokenVersion)? DecodeToken(string token)
    {
        var principal = VerifyToken(token);
        if (principal == null) return null;

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;
        var versionClaim = principal.FindFirst("tokenVersion")?.Value;

        if (userIdClaim == null || roleClaim == null) return null;

        return (int.Parse(userIdClaim), roleClaim, int.Parse(versionClaim ?? "0"));
    }
}
