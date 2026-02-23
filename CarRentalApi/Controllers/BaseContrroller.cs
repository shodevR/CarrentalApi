using CarRentalApi.Data;
using Jose;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace CarRentalApi.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;
        
        protected readonly IWebHostEnvironment _hostingEnvironment;
        protected readonly IHttpContextAccessor _contextAccessor;
        protected readonly IConfiguration _configuration;

        protected Int64 ClientUserID = 0;
        protected Int64 UserID = 0;
        protected string UserEmail = "";
        protected string RouteLoc;
        protected Int64 ClientRegID = 0;
        protected string ClientUserName = "";
        protected string ClientPassword = "";
        protected string ConnectionString = "";
        protected string Version = "";
        //protected ApiResponse response;
        protected string ControllerName;
        protected string token = null;


        public BaseController(IWebHostEnvironment hostingEnvironment, IHttpContextAccessor contextAccessor,
            IConfiguration configuration,ApplicationDbContext context)
        {

            _hostingEnvironment = hostingEnvironment;
            _contextAccessor = contextAccessor;
            _configuration = configuration;
            _context = context;
           
            if (_contextAccessor != null && _contextAccessor.HttpContext != null)
            {
                GetDataFromToken();
            }

        }


        protected void GetDataFromToken()
        {
            if (_contextAccessor != null && _contextAccessor.HttpContext != null)
            {
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    JwtSettings settings = _configuration.GetSection("JWT").Get<JwtSettings>();
                    var SecretKey = settings;
                    var key = Encoding.ASCII.GetBytes("Harshit6h777b76r65dbw4@@@@567567Harshit6h777b76r65dbw4@@@@567567");
                    token = _contextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
                    token = token.Replace("Bearer ", "");

                    tokenHandler.ValidateToken(token, new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "harshitissue",
                        ValidAudience = "HarshitAudience",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Harshit6h777b76r65dbw4@@@@567567Harshit6h777b76r65dbw4@@@@567567"))
                    }, out SecurityToken validatedToken);


                    if (validatedToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    {

                        throw new SecurityTokenException("Invalid token");
                    }

                    var jwtToken = (JwtSecurityToken)validatedToken;
                    //var expirationTimeUni = long.Parse(jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Exp).Value);
                    //var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expirationTimeUni).UtcDateTime;
                    //if(expirationTime < DateTime.UtcNow)
                    //{
                    //    throw new SecurityTokenException("Expire Token");
                    //}
                    RouteLoc = jwtToken.Claims.First(x => x.Type == "RouteLoc").Value;
                    _contextAccessor.HttpContext.Session.SetString("RouteLoc", RouteLoc.ToString());
                    //if (RouteLoc != "CBTAdmin")
                    //{
                    //    throw new SecurityTokenException("Invalid token");
                    //}
                    UserID = Int64.Parse(jwtToken.Claims.First(x => x.Type == "nameid").Value);
                    
                    UserEmail = jwtToken.Claims.First(x => x.Type == "email").Value;
                    _contextAccessor.HttpContext.Session.SetString("UserEmail", UserEmail.ToString());
                    _contextAccessor.HttpContext.Session.SetString("UserID", UserID.ToString());
                }
                catch (SecurityTokenException ex)
                {
                    // Handle invalid token scenarios
                    throw new SecurityTokenException("Invalid or expired token", ex);
                }
            }
        }
    }
}
