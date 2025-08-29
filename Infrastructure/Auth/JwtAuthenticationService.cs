using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Entities;
using Core.Interfaces;
using Core.Models.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Auth;

public class JwtAuthenticationService(IOptions<JwtSettings> jwtOptions) : IJwtAuthenticationService {
    private readonly JwtSettings _jwtSettings = jwtOptions.Value ?? throw new InvalidOperationException("JWT settings are not configured");
    
    private readonly string _key = jwtOptions.Value?.Key ?? throw new InvalidOperationException("JWT Key is not configured");
    private readonly string _issuer = jwtOptions.Value?.Issuer ?? "LWService";
    private readonly string _audience = jwtOptions.Value?.Audience ?? "LWService";

    public string GenerateToken(User user, DateTime expiresAt) {
        var    tokenHandler = new JwtSecurityTokenHandler();
        byte[] key          = Encoding.ASCII.GetBytes(_key);

        var claims = new List<Claim> {
            new("UserId", user.Id.ToString()),
            new("FullName", user.FullName),
            new("SuperUser", user.SuperUser.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        // Add authorization group ID if present
        if (user.AuthorizationGroupId.HasValue) {
            claims.Add(new Claim("AuthorizationGroupId", user.AuthorizationGroupId.Value.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor {
            Subject            = new ClaimsIdentity(claims),
            Expires            = expiresAt,
            Issuer             = _issuer,
            Audience           = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token) {
        var    tokenHandler = new JwtSecurityTokenHandler();
        byte[] key          = Encoding.ASCII.GetBytes(_key);

        var validationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(key),
            ValidateIssuer           = true,
            ValidIssuer              = _issuer,
            ValidateAudience         = true,
            ValidAudience            = _audience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };

        try {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch {
            return null;
        }
    }
}