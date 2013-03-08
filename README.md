FacebookCsharpSdk
=================
Facebook C# SDK is implementing the methods in Facebook's Official PHP SDK. 
It supports single graph api calls batch graph api calls, single and multiple fql queries 

You can download dll from
http://nuget.org/packages/Facebook.CSharp.SDK/

All methods and examples are below.


Default construction
=================
            var config = new Dictionary<string, object>();

            //your application id and secret from https://developers.facebook.com/apps
            config.Add("appId", "3955.......");
            config.Add("secret", "4c1d...............");
            config.Add("fileUpload", true); //optional

            FacebookClient client = new FacebookClient(config);
=================


Retrieve user id
=================
            ulong facebookId = client.getUser(); //retrieve user id. if user is not added the app this value is 0
=================


Redirect to permission page
=================
            var loginPrms = new Dictionary<string, object>();

            string[] permissions = { "email", "friends_likes", "publish_stream" };

            //or you can use the helper classes FacebookUserDataPermissions, FacebookFriendDataPermissions, FacebookExtendedPermissions for autocomplete in visual studio. 
            //string[] permissions = {FacebookUserDataPermissions.email, FacebookFriendDataPermissions.friends_likes, FacebookExtendedPermissions.publish_stream};
            loginPrms.Add("scope", permissions); //optional

            //your application url
            loginPrms.Add("redirect_uri", new Uri("http://apps.facebook.com/yourappnamespace"));

            //you can save this random string to a session variable and check at the beginning for csrf protection after redirection.
            loginPrms.Add("state", "randomstring"); //optional

            //the default value is "page" you can change this to "popup".
            loginPrms.Add("display", "page"); //optional
            client.getLoginUrl(loginPrms);
=================


Retrieve logout url
=================
            var logoutPrms = new Dictionary<string, object>();

            //optional logout redirect uri.
            logoutPrms.Add("next", "http://apps.facebook.com/yourappnamespace");
            client.getLogoutUrl(loginPrms);
=================



A default server side flow example and all methods are explained in detail
=================

            //the user is not added the application we redirect it to our permission screen.
            if (facebookId == 0)
            {
                Response.Write("<script>top.location.href='" + client.getLoginUrl(loginPrms) + "';</script>");
            }
            else
            {
                //we get the short lived access token and exchange it for 60 days extension.
                client.setExtendedAccessToken();

                //all methods and their return values for testing

                // getAccessToken first retrieves the application access token, if exists this value is changed to user access token for our application.
                Response.Write("php sdk equivalent $facebook->getAccessToken();" + client.getAccessToken() + "<br/>");

                //gets our app secret from the config value.
                Response.Write("php sdk equivalent $facebook->getApiSecret();" + client.getApiSecret() + "<br/>");

                //gets our app id from the config value.
                Response.Write("php sdk equivalent $facebook->getAppId();" + client.getAppId() + "<br/>");

                //gets our login url with permissions we defined in loginPrms variable. getLoginStatusUrl is basically a wrapper for getLoginUrl.
                Response.Write("php sdk equivalent $facebook->getLoginStatusUrl();" + client.getLoginStatusUrl(loginPrms) + "<br/>");
                Response.Write("php sdk equivalent $facebook->getLoginUrl();" + client.getLoginUrl(loginPrms) + "<br/>");

                //retrieves the signed_request post value from facebook in our application and retrieves a Dictionary<string, object>.
                //for more info please refer to  http://developers.facebook.com/docs/reference/login/signed-request/

                foreach (KeyValuePair<string, object> kvp in client.getSignedRequest())
                {
                    //user is json object
                    if (kvp.Key == "user")
                    {
                        foreach (KeyValuePair<string, object> fields in client.getSignedRequest()["user"] as Dictionary<string, object>)
                        {
                            //age in user is also a json object
                            if(fields.Key == "age")
                            {
                                foreach (KeyValuePair<string, object> field in fields.Value as Dictionary<string, object>)
                                {
                                    Response.Write("php sdk equivalent $facebook->getSignedRequest();" + field.Key + " - " + field.Value + "<br/>");
                                }
                            }
                            else
                            {
                                Response.Write("php sdk equivalent $facebook->getSignedRequest();" + fields.Key + " - " + fields.Value + "<br/>");
                            }
                        }
                    }
                    //page is json object
                    if (kvp.Key == "page")
                    {
                        foreach (KeyValuePair<string, object> fields in client.getSignedRequest()["page"] as Dictionary<string, object>)
                        {
                            Response.Write("php sdk equivalent $facebook->getSignedRequest();" + fields.Key + " - " + fields.Value + "<br/>");
                        }
                    }
                    else
                    {
                        Response.Write("php sdk equivalent $facebook->getSignedRequest();" + kvp.Key + " - " + kvp.Value + "<br/>");
                    }

                }


                //retrieves the signed user facebook id as ulong
                Response.Write("php sdk equivalent $facebook->getUser();" + client.getUser() + "';</script>" + "<br/>");

                //retrieves the fileUpload value from config
                Response.Write("php sdk equivalent $facebook->useFileUploadSupport();" + client.useFileUploadSupport() + "<br/>");

                //you can retrieve fql queries using fql method. 
                //this query retrieves current user and his /her friends uid, name, pic_square fields
                Response.Write(client.fql("SELECT uid, name, pic_square FROM user WHERE uid = me() OR uid IN (SELECT uid2 FROM friend WHERE uid1 = me())") + "<br/>");

                //you can also make batch fql queries and reference them. in this example we first retrieve the user list attends to a particual event in "query1"
                //and then retrieve their name, url, pic fields in "query2" referencing the results in "query1"
                var batchFqlQueries = new Dictionary<string, string>();
                batchFqlQueries.Add("query1", "SELECT uid, rsvp_status FROM event_member WHERE eid=221426641328833");
                batchFqlQueries.Add("query2", "SELECT name, url, pic FROM profile WHERE id IN (SELECT uid FROM #query1)"); //referencing #query1
                Response.Write(client.fql(batchFqlQueries) + "<br/>");

                //you can make simple graph api calls like in php sdk
                Response.Write(client.api("/me/friends", "GET", null)); // or equivalently Response.Write(client.api("/me/friends", FacebookApiMethodType.GET, null));

                //same call in more object oriented way
                var graphApiCall = new FacebookGraphApiRequest();
                graphApiCall.Method = FacebookApiMethodType.GET;
                graphApiCall.Path = "/me";
                graphApiCall.Params = null;
                Response.Write(client.api(graphApiCall));

                //this is an example of batch graph api request. retrieve the current user public info from /me and posts a wall post to this user's wall in one call
                List<FacebookGraphApiRequest> list = new List<FacebookGraphApiRequest>();

                //first request
                var request = new FacebookGraphApiRequest();
                request.Method = FacebookApiMethodType.GET; //or just type "GET" string like request.Method = "GET"
                request.Path = "/me";
                request.Params = new NameValueCollection(); // Graph api GET calls do not need parameters. 

                list.Add(request);

                //second request is wall post.
                request.Method = FacebookApiMethodType.POST; //or just type "POST" sting like request.Method = "POST"
                request.Path = "/me/feed";
                //we enter the wall post parameter fields link and message
                // you can refer to http://developers.facebook.com/docs/reference/api/publishing/ Other Objects section
                request.Params = new NameValueCollection();
                request.Params.Add("link", "www.arcademonk.com");
                request.Params.Add("message", "C# SDK Batch Request Messsage");

                list.Add(request);

                //retrieves the result from these two requests
                Response.Write(client.api(list));

                //you can catch all api errors using FacebookApiException class 
                try
                {
                    //this user is invalid
                    client.api("/arcademonk.notvalid", "GET", null);
                }
                catch (FacebookApiException exception)
                {
                    Response.Write(exception.Type + exception.Error);
                }
