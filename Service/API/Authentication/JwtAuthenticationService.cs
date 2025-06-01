// using System;
// using System.IdentityModel.Tokens.Jwt;
// using System.Security.Claims;
// using System.Text;
// using Microsoft.Extensions.Configuration;
// using Microsoft.IdentityModel.Tokens;
//
// namespace Service.API.Authentication
// {
//     public interface IJwtAuthenticationService
//     {
//         string GenerateToken(string username, int employeeId);
//         ClaimsPrincipal ValidateToken(string token);
//     }
//
//     public class JwtAuthenticationService : IJwtAuthenticationService
//     {
//         private readonly IConfiguration _configuration;
//         private readonly string _key;
//         private readonly string _issuer;
//         private readonly string _audience;
//         private readonly int _expiryMinutes;
//
//         public JwtAuthenticationService(IConfiguration configuration)
//         {
//             _configuration = configuration;
//             _key = _configuration["Jwt:Key"];
//             _issuer = _configuration["Jwt:Issuer"];
//             _audience = _configuration["Jwt:Audience"];
//             _expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryMinutes", 60);
//         }
//
//         public string GenerateToken(string username, int employeeId)
//         {
//             var tokenHandler = new JwtSecurityTokenHandler();
//             var key = Encoding.ASCII.GetBytes(_key);
//             
//             // Set expiration to midnight
//             var now = DateTime.UtcNow;
//             var midnight = now.Date.AddDays(1);
//             var expires = midnight;
//
//             var tokenDescriptor = new SecurityTokenDescriptor
//             {
//                 Subject = new ClaimsIdentity(new[]
//                 {
//                     new Claim("Username", username),
//                     new Claim("EmployeeID", employeeId.ToString()),
//                     new Claim(ClaimTypes.Name, username)
//                 }),
//                 Expires = expires,
//                 Issuer = _issuer,
//                 Audience = _audience,
//                 SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
//             };
//
//             var token = tokenHandler.CreateToken(tokenDescriptor);
//             return tokenHandler.WriteToken(token);
//         }
//
//         public ClaimsPrincipal ValidateToken(string token)
//         {
//             var tokenHandler = new JwtSecurityTokenHandler();
//             var key = Encoding.ASCII.GetBytes(_key);
//
//             var validationParameters = new TokenValidationParameters
//             {
//                 ValidateIssuerSigningKey = true,
//                 IssuerSigningKey = new SymmetricSecurityKey(key),
//                 ValidateIssuer = true,
//                 ValidIssuer = _issuer,
//                 ValidateAudience = true,
//                 ValidAudience = _audience,
//                 ValidateLifetime = true,
//                 ClockSkew = TimeSpan.Zero
//             };
//
//             try
//             {
//                 var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
//                 return principal;
//             }
//             catch
//             {
//                 return null;
//             }
//         }
//     }
// }