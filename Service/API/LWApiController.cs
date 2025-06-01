// using Microsoft.AspNetCore.Mvc;
// using System.Security.Claims;
//
// namespace Service.API
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     public class LWApiController : ControllerBase
//     {
//         protected readonly Data Data = new();
//         
//         protected int EmployeeID
//         {
//             get
//             {
//                 int empID = -1;
//                 var empIDClaim = User.FindFirst("EmployeeID");
//                 if (empIDClaim == null) 
//                     return empID;
//                 int.TryParse(empIDClaim.Value, out empID);
//                 return empID;
//             }
//         }
//     }
// }