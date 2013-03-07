using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

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
            if (sigReq != null && sigReq.ContainsKey("user_id"))
            {
                user = Convert.ToUInt64(sigReq["user_id"]);
                return user.Value;
            }
            return 0;
        }

        public Uri getLogoutUrl(Dictionary<string, object> prms)
        {
            return new UriBuilder("https://www.facebook.com/logout.php?next=" + (prms.ContainsKey("next") ? prms["next"].ToString() : getCurrentUrl().ToString()) + "&access_token=" + getAccessToken()).Uri;
        }

        public Uri getLoginUrl(Dictionary<string, object> prms)
        {
            Uri currentUri = getCurrentUrl();

            string scope = string.Empty;
            string appId = getAppId();
            string state = string.Empty;
            if (prms.ContainsKey("scope"))
            {
                scope = string.Join(",", prms["scope"] as string[]);
            }
            if (prms.ContainsKey(("redirect_uri")))
            {
                currentUri = prms["redirect_uri"] as Uri;
            }
            if (prms.ContainsKey("state"))
            {
                state = prms["state"] as string;
            }

            return new UriBuilder("https://www.facebook.com/dialog/oauth?client_id=" + appId + "&redirect_uri=" + currentUri + "&scope=" + scope + "&state=" + state).Uri;
        }

        public Uri getLoginStatusUrl(Dictionary<string, object> prms)
        {
            return getLoginUrl(prms);
        }

        public string api(FacebookGraphApiRequest req)
        {
            return api(req.Path, req.Method, req.Params);
        }

        private string ToQueryString(NameValueCollection source, bool removeEmptyEntries)
        {
            return source != null ? String.Join("&", source.AllKeys
                .Where(key => !removeEmptyEntries || source.GetValues(key)
                    .Where(value => !String.IsNullOrEmpty(value))
                    .Any())
                .SelectMany(key => source.GetValues(key)
                    .Where(value => !removeEmptyEntries || !String.IsNullOrEmpty(value))
                    .Select(value => String.Format("{0}={1}", HttpUtility.UrlEncode(key), value != null ? HttpUtility.UrlEncode(value) : string.Empty)))
                .ToArray())
                : string.Empty;
        }

        public string api(List<FacebookGraphApiRequest> req)
        {
            List<FacebookBatchRequestObject> batchList = new List<FacebookBatchRequestObject>();

            foreach (var facebookGraphApiRequest in req)
            {
                batchList.Add(new FacebookBatchRequestObject()
                                  {
                                      method = facebookGraphApiRequest.Method,
                                      relative_url = facebookGraphApiRequest.Path,
                                      body = ToQueryString(facebookGraphApiRequest.Params, true)
                                  });
            }
            JavaScriptSerializer jss = new JavaScriptSerializer();

            string json = jss.Serialize(batchList);

            return api("", "POST", new NameValueCollection()
                                {
                                    {"batch", json}
                                });
        }

        public string fql(Dictionary<string, string> fqlQueries)
        {
            string graphApiUrl = "https://graph.facebook.com/";

            try
            {
                using (WebClient client = new WebClient())
                {
                    JavaScriptSerializer jss = new JavaScriptSerializer();

                    string json = jss.Serialize(fqlQueries);
                    json = HttpUtility.UrlEncode(json);
                    Stream data = client.OpenRead(graphApiUrl + "fql?q=" + json + "&access_token=" + getAccessToken());
                    StreamReader reader = new StreamReader(data);
                    return reader.ReadToEnd();
                }
            }
            catch (WebException webException)
            {

                Stream data = webException.Response.GetResponseStream();
                StreamReader reader = new StreamReader(data);

                throw new FacebookApiException(reader.ReadToEnd());
            } 
        }
        public string fql(string fqlQuery)
        {
            string graphApiUrl = "https://graph.facebook.com/";

            try
            {
                using (WebClient client = new WebClient())
                {
                    Stream data = client.OpenRead(graphApiUrl + "fql?q=" + fqlQuery + "&access_token=" + getAccessToken());
                    StreamReader reader = new StreamReader(data);
                    return reader.ReadToEnd();
                }
            }
            catch (WebException webException)
            {

                Stream data = webException.Response.GetResponseStream();
                StreamReader reader = new StreamReader(data);

                throw new FacebookApiException(reader.ReadToEnd());
            }
        }

        public string getAccessToken()
        {
            if (!string.IsNullOrEmpty(accessToken))
                return accessToken;

            setAccessToken(getApplicationAccessToken());

            string userAccessToken = getUserAccessToken();
            if (!string.IsNullOrEmpty(userAccessToken))
                setAccessToken(userAccessToken);

            return accessToken;
        }

        private string getUserAccessToken()
        {
            var signedRequest = this.signedRequest;
            if (signedRequest != null)
            {
                if (signedRequest.ContainsKey("oauth_token"))
                {
                    accessToken = signedRequest["oauth_token"].ToString();
                    return signedRequest["oauth_token"].ToString();
                }
            }

            if (signedRequest.ContainsKey("code"))
            {
                string accToken = getAccessTokenFromCode(signedRequest["code"].ToString());
                return accToken;
            }
            return null;
        }

        private string getAccessTokenFromCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return null;

            using (WebClient client = new WebClient())
            {
                Stream data =
                    client.OpenRead("https://graph.facebook.com/oauth/access_token?client_id=" + getAppId() +
                                    "&client_secret=" + getApiSecret() + "&redirect_uri=" + getCurrentUrl() + "&code=" +
                                    code);
                StreamReader reader = new StreamReader(data);
                return reader.ReadToEnd().Split('&')[0].Split('=')[1];
            }
        }

        private string getApplicationAccessToken()
        {
            using (WebClient client = new WebClient())
            {
                Stream data =
                    client.OpenRead("https://graph.facebook.com/oauth/access_token?client_id=" + getAppId() +
                                    "&client_secret=" + getApiSecret() + "&grant_type=client_credentials");
                StreamReader reader = new StreamReader(data);
                return reader.ReadToEnd().Split('=')[1];
            }
        }

        public void setExtendedAccessToken()
        {
            using (WebClient client = new WebClient())
            {
                Stream data =
                    client.OpenRead("https://graph.facebook.com/oauth/access_token?client_id=" + getAppId() +
                                    "&client_secret=" + getApiSecret() + "&grant_type=fb_exchange_token&fb_exchange_token=" + getAccessToken());
                StreamReader reader = new StreamReader(data);
                accessToken =  reader.ReadToEnd().Split('&')[0].Split('=')[1];
            }
        }

        public string api(string path, string method, NameValueCollection prms)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

                string graphApiUrl = "https://graph.facebook.com/";
                switch (method.ToUpper())
                {
                    case "POST":
                        {
                            prms.Add("access_token", getAccessToken());
                            try
                            {
                                byte[] response = client.UploadValues(graphApiUrl + path, prms);

                                string resp = Encoding.UTF8.GetString(response);
                                if (resp == "true")
                                    return resp;

                                return resp;
                            }
                            catch (WebException webException)
                            {
                                Stream data = webException.Response.GetResponseStream();
                                StreamReader reader = new StreamReader(data);

                                throw new FacebookApiException(reader.ReadToEnd());
                            }
                        }
                    case "GET":
                        {
                            try
                            {
                                
                                Stream data = client.OpenRead(graphApiUrl + path + (path.Contains("?") ? "&access_token=" + getAccessToken() : "?access_token=" + getAccessToken() ) );
                                StreamReader reader = new StreamReader(data);
                                return reader.ReadToEnd();
                            }
                            catch (WebException webException)
                            {

                                Stream data = webException.Response.GetResponseStream();
                                StreamReader reader = new StreamReader(data);

                                throw new FacebookApiException(reader.ReadToEnd());
                            }
                            
                            
                        }
                    case "DELETE":
                        try
                        {
                            return client.UploadString(graphApiUrl + path + "?access_token=" + getAccessToken(), "DELETE", "");
                        }
                        catch (WebException webException)
                        {
                            Stream data = webException.Response.GetResponseStream();
                            StreamReader reader = new StreamReader(data);

                            throw new FacebookApiException(reader.ReadToEnd());
                        }
                        
                        break;
                }
                throw new FacebookApiException("Unknown Api Method");
            }
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
                    string expectedSignature = hashHmac(jsonObject, getApiSecret());

                    if (expectedSignature != signature)
                        return null;
                    return data;
                }
                return null;
            }
            return null;
        }

        private string hashHmac(string jsonObject, string password)
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
