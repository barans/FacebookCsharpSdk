using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FacebookClient;

namespace FacebookCSharpSDK
{
    class Program
    {
        static void Main(string[] args)
        {
            FacebookClient.FacebookClient client = new FacebookClient.FacebookClient();
            client.getSignedRequest();
        }
    }
}
