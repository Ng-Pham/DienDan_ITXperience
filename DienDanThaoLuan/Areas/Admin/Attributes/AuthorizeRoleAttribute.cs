using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DienDanThaoLuan.Attributes
{
    public class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        private readonly string[] allowedRoles;
        public AuthorizeRoleAttribute(params string[] roles)
        {
            this.allowedRoles = roles;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var userRole = httpContext.Session["Role"] as string;

            if (string.IsNullOrEmpty(userRole))
                return false;

            return allowedRoles.Contains(userRole);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectToRouteResult(
                new System.Web.Routing.RouteValueDictionary
                {
                    { "area", "Admin" },
                    { "controller", "AccessDenied" },
                    { "action", "Index" }
                }
            );
        }
    }
}