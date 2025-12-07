using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DoAnChuyenNganh.Filters
{
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {


            var role = context.HttpContext.Session.GetString("RoleName");



            if (string.IsNullOrEmpty(role) || !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                // If AJAX request you may want to return 401/403
                if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {


                    context.Result = new UnauthorizedResult();
                    return;
                }

                // Redirect to Login (preserve returnUrl if needed)

                context.Result = new RedirectToActionResult("Login", "Auth", new { area = "" });
            }


        }
    }
}
