using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Security.Cryptography;

namespace Facebook
{
    public class FacebookClient : IFacebookClient
    {
        private string appId;
        private string appSecret;
        private ulong? user;
        private Dictionary<string, object> signedRequest;
        private string accessToken;
        private bool fileUploadSupport;

        
        public FacebookClient(Dictionary<string, object> config)
        {
            setAppId(Convert.ToString(config["appId"]));
            setApiSecret(Convert.ToString(config["secret"]));

            if (config.ContainsKey("fileUpload"))
                fileUploadSupport = true;
        }
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

        public Dictionary<string, object> getSignedRequest()
        {
            if (signedRequest == null)
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
            if (user != null)
                return user.Value;

            var sigReq = getSignedRequest();
            if(sigReq.ContainsKey("user_id"))
            {
                user = Convert.ToUInt64(sigReq["user_id"]);
                return user.Value;
            }
            return 0;
        }

        public Uri getLogoutUrl()
        {
            return new UriBuilder().Uri;
        }

        public Uri getLoginUrl(Dictionary<string, object> prms)
        {
            Uri currentUri = getCurrentUrl();
            //http://developers.facebook.com/docs/howtos/login/server-side-login/
            if(prms.ContainsKey("scope"));
                
            return new UriBuilder().Uri;
        }

        

        public Uri getLoginStatusUrl()
        {
            return new UriBuilder().Uri;
        }

        public string getAccessToken()
        {
            return string.Empty;
        }

        public string api()
        {
            return string.Empty;
        }

        private Dictionary<string, object> parseSignedRequest(string signedRequestString)
        {
            string[] signedRequest = signedRequestString.Split('.');
            string hmacString = signedRequest[0];
            string jsonObject = signedRequest[1];

            string signature = base64UrlDecode(hmacString);
            var data = jsonDecode(base64UrlDecode(jsonObject));

            if (data.ContainsKey("algorithm"))
            {
                if (data["algorithm"].ToString().ToUpper() == "HMAC-SHA256")
                {
                    string expectedSignature = hashHmac("sha256", jsonObject, getApiSecret());

                    if (expectedSignature != signature)
                        return null;
                    else
                        return data;
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

            return Encoding.Default.GetString(array);
        }

        private string base64UrlDecode(string hmacString)
        {
            string replaced = hmacString.Replace('-', '+').Replace('_', '/');
            replaced = replaced.PadRight(replaced.Length + (4 - replaced.Length % 4) % 4, '=');
            var byteArray = Convert.FromBase64String(replaced);
            return Encoding.Default.GetString(byteArray);
        }

        private Dictionary<string, object> jsonDecode(string jsonObject)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var jObject = serializer.DeserializeObject(jsonObject) as Dictionary<string, object>;

            return jObject;
        }

        private Uri getCurrentUrl()
        {
            var protocol = HttpContext.Current.Request.Url.Scheme;
            var host = HttpContext.Current.Request.Url.Host;
            var port = HttpContext.Current.Request.Url.Port;
            var page = HttpContext.Current.Request.Url.PathAndQuery;

            var currentUri = new UriBuilder(protocol, host, port, page);
            return currentUri.Uri;
        }
    }   
}
