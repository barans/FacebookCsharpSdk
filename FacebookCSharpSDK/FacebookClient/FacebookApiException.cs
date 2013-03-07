using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace Facebook
{
    public class FacebookApiException : ApplicationException
    {
        public string Error { get; set; }

        public string Type { get; set; }

        public FacebookApiException()
        {
           
        }
        public FacebookApiException(string message) : base(message)
        {
            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            var jsonObject = javaScriptSerializer.Deserialize<Dictionary<string, Dictionary<string,object>>>(message);

            Error = jsonObject["error"]["message"].ToString();
            Type = jsonObject["error"]["type"].ToString();
        }

        public string getResult()
        {
            return Error;
        }
        public string getType()
        {
            return Type;
        }
    }
}
