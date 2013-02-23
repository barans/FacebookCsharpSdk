using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Security.Cryptography;

namespace FacebookClient
{
    public class FacebookClient : IFacebookClient
    {
        private string appId;
        private string appSecret;
        private ulong user;
        private string signedRequest;
        private string accessToken;
        private bool fileUploadSupport;

        public void setAppId(string appId)
        {
            this.appId = appId;
        }

        public string getAppId()
        {
            return appId;
        }

        public void setApiSecret(string apiSecret)
        {
            appSecret = apiSecret;
        }

        public string getApiSecret()
        {
            return appSecret; 
        }

        public void setAccessToken(string accessToken)
        {
            this.accessToken = accessToken;
        }

        public void setFileUploadSupport(bool fileUploadSupport)
        {
            this.fileUploadSupport = fileUploadSupport;
        }

        public bool useFileUploadSupport()
        {
            return fileUploadSupport;
        }

        public string getSignedRequest()
        {
            if (string.IsNullOrEmpty(signedRequest))
            {
                string signedRequestString = HttpContext.Current.Request["signed_request"];
                if (!string.IsNullOrEmpty(signedRequestString))
                {
                    signedRequest = parseSignedRequest(signedRequestString);
                }
            }
            return signedRequest;
        }

        public ulong getUser()
        {
            
        }

        public Uri getLogoutUrl()
        {
            
        }

        private string parseSignedRequest(string signedRequestString)
        {
            string[] signedRequest = signedRequestString.Split('.');
            string hmacString = signedRequest[0];
            string jsonObject = signedRequest[1];

            string signature = base64UrlDecode(hmacString);
            var data = jsonDecode(jsonObject);

            if (data.ContainsKey("algorithm"))
            {
                if (data["algorithm"].ToString().ToUpper() == "HMAC-SHA256")
                {
                    string expectedSignature = hashHmac("sha256", jsonObject, this.appSecret);

                    if (expectedSignature != signature)
                        return null;
                    else
                        return expectedSignature;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }



        }

        private string hashHmac(string algo, string jsonObject, string password)
        {
            var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(password));
            var array = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(jsonObject));

            return UTF8Encoding.ASCII.GetString(array);
        }

        private string base64UrlDecode(string hmacString)
        {
            UTF8Encoding.ASCII.GetString(Convert.FromBase64String(hmacString.Replace('-', '+').Replace('_', '/')));
            return string.Empty;
        }

        private Dictionary<string, object> jsonDecode(string jsonObject)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var jObject = serializer.DeserializeObject(jsonObject) as Dictionary<string, object>;

            return jObject;
        }

    }   
}
