using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Security;
using ThesisPortal.Models;

namespace ThesisPortal.Controllers
{
    [Authorize]
    public class AuthController : ApiController
    {
        [HttpGet]
        [AllowAnonymous]
        [Route("api/auth/identify")]
        [ResponseType(typeof(string))]
        public IHttpActionResult Identify()
        {
            if (User.Identity.IsAuthenticated)
                return Ok(User.Identity.Name);
            else
                return Unauthorized();
        }

        [AllowAnonymous]
        [Route("api/auth/in")]
        public bool SignIn(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                if (Membership.ValidateUser(model.Username, model.Password))
                {
                    FormsAuthentication.SetAuthCookie(model.Username, false);
                    return true;
                }
            }
            return false;
        }

        [Authorize]
        [HttpGet]
        [Route("api/auth/out")]
        public void SignOut()
        {
            FormsAuthentication.SignOut();
        }
    }
}