using Microsoft.AspNetCore.Mvc;
using WalliDO.Service.Enum;
using WalliDO.Service.Models.Response;
using System.Security.Claims;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using WalliDO.Service.Interfaces;
using WalliDO.Service.Models.Abstract;

namespace WalliDO.Service.Controllers.Abstraction
{
    [ApiController]
    [Route("[controller]")]
    public abstract class StandartController : ControllerBase
    {
        private protected Guid CurrentUserId
        {
            get
            {
                try
                {
                    _ = Guid.TryParse(this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out Guid userId);

                    return userId;
                }
                catch (Exception)
                {
                    return Guid.Empty;
                }
            }
        }

        private protected string? CurrentUserRole
        {
            get
            {
                try
                {
                    var userRole = this.User.FindFirst(ClaimTypes.Role)?.Value;
                    return userRole;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        protected ActionResult ActionResponse(StandartResponse response)
        {
            if (response.Status != ResponseStatuses.Success)
            {
                return BadRequest(response);
            }
            else
            {
                return Ok(response);
            }
        }
    }
}
