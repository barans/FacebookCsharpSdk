using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Facebook
{
    interface IFacebookClient
    {
        string api(string path, string method, NameValueCollection nvc);
        string api(FacebookGraphApiRequest req);
        string api(List<FacebookGraphApiRequest> req);
        string fql(string fqlQuery);
        string fql(Dictionary<string, string> fqlQueries);
        string getAccessToken();
        string getApiSecret();
        string getAppId();
        Uri getLoginStatusUrl(Dictionary<string, object> prms);
        Uri getLoginUrl(Dictionary<string, object> prms);
        Uri getLogoutUrl(Dictionary<string, object> prms);
        Dictionary<string, object> getSignedRequest();
        ulong getUser();
        void setAccessToken(string accessToken);
        void setApiSecret(string apiSecret);
        void setAppId(string appId);
        void setFileUploadSupport(bool uploadSupport);
        void setExtendedAccessToken();
        bool useFileUploadSupport();
    }
}
