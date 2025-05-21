using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace DienDanThaoLuan.Filters
{
    public class SessionTimeoutAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (HttpContext.Current.Session["LastActivity"] != null)
            {
                DateTime lastActivity = (DateTime)HttpContext.Current.Session["LastActivity"];
                if ((DateTime.Now - lastActivity).TotalMinutes >= 10)
                {
                    HttpContext.Current.Session.Clear();
                    FormsAuthentication.SignOut();
                    filterContext.Result = new RedirectResult("/Account/Login");
                    return ;
                }
            }

            HttpContext.Current.Session["LastActivity"] = DateTime.Now;
            base.OnActionExecuting(filterContext);
        }
    }
}