using System;
using System.Net;

namespace ElnApplication.Controllers.Apis
{
    /// <summary>
    /// Summary description for RequestUtil
    /// </summary>
    public static class RequestUtil
    {
        #region HttpWebResponse extension methods

        public static string GetMessage(this HttpWebResponse response)
        {
            var responseValue = string.Empty;

            // grab the response  
            using (var responseStream = response.GetResponseStream())
            {
                using (var reader = new System.IO.StreamReader(responseStream))
                {
                    responseValue = reader.ReadToEnd();
                }
            }

            return responseValue;
        }

        #endregion

        public static HttpWebRequest CreateWebRequest(string pathAndQuery, string token, string method, string accept = "application/json")
        {
            var request = (HttpWebRequest)WebRequest.Create(pathAndQuery);
            request.Method = method;
            request.Accept = accept ?? "application/json";
            if (method == "POST" || method == "PUT") request.ContentLength = 0;
            if (!string.IsNullOrEmpty(token)) request.Headers["Authorization"] = token;
            return request;
        }

        public static HttpWebResponse ExecuteWebRequest(string pathAndQuery, string token, string method, string accept = "application/json")
        {
            var request = RequestUtil.CreateWebRequest(pathAndQuery, token, method, accept);
            try
            {
                return (HttpWebResponse)request.GetResponse();
            }
            catch (WebException wex)
            {
                HttpWebResponse exResponse = wex.Response as HttpWebResponse;
                if (exResponse != null && exResponse.StatusCode.Equals(HttpStatusCode.Unauthorized))
                {
                    throw new UnauthorizedAccessException();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
