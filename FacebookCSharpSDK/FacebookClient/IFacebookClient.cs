using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FacebookClient
{
    interface IFacebookClient
    {
        string api();
        string getAccessToken();
        string getApiSecret();
        string getAppId();
        Uri getLoginStatusUrl();
        Uri getLoginUrl();
        Uri getLogoutUrl();
        string getSignedRequest();
        ulong getUser();
        void setAccessToken(string accessToken);
        void setApiSecret(string apiSecret);
        void setAppId(string appId);
        void setFileUploadSupport(bool uploadSupport);
        bool useFileUploadSupport();
    }
}
