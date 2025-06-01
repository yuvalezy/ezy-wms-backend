// using System;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Logging;
//
// namespace Service.API.Authentication
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     public class AuthenticationController : ControllerBase
//     {
//         private readonly IJwtAuthenticationService _jwtService;
//         private readonly ILogger<AuthenticationController> _logger;
//
//         public AuthenticationController(IJwtAuthenticationService jwtService, ILogger<AuthenticationController> logger)
//         {
//             _jwtService = jwtService;
//             _logger = logger;
//         }
//
//         [AllowAnonymous]
//         [HttpPost("login")]
//         public async Task<IActionResult> Login([FromBody] LoginRequest request)
//         {
//             try
//             {
//                 bool valid = false;
//                 bool isValidBranch = false;
//                 int empID = -1;
//
//                 if (!string.IsNullOrWhiteSpace(request.Username))
//                 {
//                     valid = Data.ValidateAccess(request.Username, out empID, out isValidBranch);
//                 }
//
//                 if (valid)
//                 {
//                     var token = _jwtService.GenerateToken(request.Username, empID);
//                     
//                     return Ok(new LoginResponse
//                     {
//                         AccessToken = token,
//                         TokenType = "Bearer",
//                         ExpiresIn = GetSecondsUntilMidnight(),
//                         Username = request.Username,
//                         EmployeeId = empID
//                     });
//                 }
//                 else
//                 {
//                     if (empID > 0 && !isValidBranch)
//                     {
//                         return BadRequest(new { error = "invalid_grant", error_description = "The user does not have a valid branch defined." });
//                     }
//
//                     return BadRequest(new { error = "invalid_grant", error_description = "The user name or password is incorrect." });
//                 }
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error during login");
//                 return StatusCode(500, new { error = "server_error", error_description = "An error occurred during authentication." });
//             }
//         }
//
//         private int GetSecondsUntilMidnight()
//         {
//             var now = DateTime.Now;
//             var midnight = now.Date.AddDays(1);
//             var timeToMidnight = midnight - now;
//             return (int)timeToMidnight.TotalSeconds;
//         }
//     }
//
//     public class LoginRequest
//     {
//         public string Username { get; set; }
//         public string Password { get; set; }
//         public string GrantType { get; set; } = "password";
//     }
//
//     public class LoginResponse
//     {
//         public string AccessToken { get; set; }
//         public string TokenType { get; set; }
//         public int ExpiresIn { get; set; }
//         public string Username { get; set; }
//         public int EmployeeId { get; set; }
//     }
// }