using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElnApplication.Controllers.Apis
{
    public static class CommonApi
    {
        public static void SetSession(this HttpContext httpContext, string key, string value)
        {
            if(!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                httpContext.Session.SetString(key, value);
        }

        public static string GetSession(this HttpContext httpContext, string key)
        {
            return httpContext.Session.GetString(key);
        }

        public static void RemoveSession(this HttpContext httpContext, string key)
        {
            httpContext.Session.Remove(key);
        }
    }
}
