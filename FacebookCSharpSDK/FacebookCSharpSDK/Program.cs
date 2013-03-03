using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FacebookCSharpSDK;
using Facebook;

namespace FacebookCSharpSDK
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new Dictionary<string, object>() { 
                {"appId", "395580793811588"},
                {"secret", "4c1d099f9377b52eb7905f3bf10fe77b"},
                {"fileUpload", true}
            };

            FacebookClient client = new FacebookClient(config);


            client.getSignedRequest();
        }
    }
}
