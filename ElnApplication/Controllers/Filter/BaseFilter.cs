using ElnApplication;
using ElnApplication.Controllers.Apis;
using ElnApplication.Controllers.LoginApis;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Net;

namespace SessionLogin.Controllers
{
    public class BaseFilter : ActionFilterAttribute
    {
        public enum AuthorizationState
        {
            /// <summary>
            /// Indicates "undefined"
            /// </summary>
            None = 0,

            /// <summary>
            /// Authenicated and authorized to use the application
            /// </summary>
            OK = 1,

            /// <summary>
            /// Authenicated but not authorized to use the application
            /// </summary>
            Unauthorized = 2,

            /// <summary>
            /// Not authenicated
            /// </summary>
            Invalid = 3,

            /// <summary>
            /// No credentials provided
            /// </summary>
            Anonymous = 4,

            /// <summary>
            /// An exception occurred while determining the authorization state
            /// </summary>
            Exception = 5,

            /// <summary>
            /// The session is about to be terminated
            /// </summary>
            Signout = 6
        }

        private const string ELNAPI_ISAUTHORIZED = "/api/v1/me/isauthorized";

        private const string AUTHORIZATIONCOOKIENAME = "AuthorizationEln";
        protected const string SCISIDCOOKIENAME = "SCISID9944";

        private FilterContext _context;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            this._context = context;

            if (context.HttpContext.Request.Host.Host == "localhost")
                return;

            // Authorize the request
            AuthorizationState state = this.CheckAuthorization(context.HttpContext.Request.Scheme + "://" + context.HttpContext.Request.Host.Value + BaseFilter.ELNAPI_ISAUTHORIZED);
            if (state.Equals(AuthorizationState.OK))
            {
                if (string.IsNullOrEmpty(GetSciSidToken())) // SSO 로그인과 PP 로그인은 다르므로 SCISID 쿠키가 없으면 PP로그인 처리
                {
                    string loginUrl = GetRedirectLoginUrl(state);
                    context.Result = new RedirectResult(loginUrl);
                }
                else
                {
                    return;
                }
            }
            else // SSO 로그인 처리
            {
                // We are not authorized, redirect to login page
                string loginUrl = GetRedirectLoginUrl(state);
                context.Result = new RedirectResult(loginUrl);
            }
        }

        
        private AuthorizationState CheckAuthorization(string pathAndQuery, string overrideToken = null)
        {
            try
            {
                string token = overrideToken ?? this.GetAuthorizationToken();
                HttpWebResponse apiResponse = RequestUtil.ExecuteWebRequest(pathAndQuery, token, "GET", "application/json");
                int retval = int.Parse(apiResponse.GetMessage());
                return (AuthorizationState)retval;
            }
            catch (Exception ex)
            {
                return AuthorizationState.Exception;
            }
        }

        private string GetAuthorizationToken()
        {
            return _context.HttpContext.Request.Cookies[AUTHORIZATIONCOOKIENAME];
        }

        private string GetSciSidToken()
        {
            return _context.HttpContext.Request.Cookies[SCISIDCOOKIENAME];
        }

        private string GetRedirectLoginUrl(AuthorizationState state, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(returnUrl)) returnUrl = this.CreateDefaultReturnUrl();

            QueryStringBuilder query = new QueryStringBuilder();
            if (!string.IsNullOrEmpty(returnUrl)) query.Add("redir", returnUrl);

            string loginURL = _context.HttpContext.Request.Scheme + "://" + _context.HttpContext.Request.Host.Value + ":" + Constants.PPPort +"/security/login" + query.GetQueryString(true);

            return loginURL;
        }

        private string CreateDefaultReturnUrl()
        {
            string retval = this._context.HttpContext.Request.GetDisplayUrl();
            return retval;
        }
    }
}
