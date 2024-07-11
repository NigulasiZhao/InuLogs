using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace InuLogs.src.Filters
{
    internal class CustomAuthenticationFilter : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {

            if (!context.HttpContext.Session.TryGetValue("isAuth", out var isAuth))
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
