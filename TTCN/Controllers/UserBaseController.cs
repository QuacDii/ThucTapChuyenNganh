using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace TTCN.Controllers
{
    public class UserBaseController : Controller
    {
        private const int UserTimeoutMinutes = 60;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var vaiTro = session.GetString("UserVaiTro");
            var startTimeStr = session.GetString("SessionStartTime");

            if (string.IsNullOrEmpty(vaiTro) || string.IsNullOrEmpty(startTimeStr))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                base.OnActionExecuting(context);
                return;
            }

            if (vaiTro == "User")
            {
                if (DateTime.TryParse(startTimeStr, out DateTime startTime))
                {
                    var tg = DateTime.UtcNow - startTime;
                    if (tg.TotalMinutes > UserTimeoutMinutes)
                    {
                        session.Clear();
                        context.Result = new RedirectToActionResult("Login", "Account", null);
                    }
                }
            }
            base.OnActionExecuting(context);
        }
    }
}