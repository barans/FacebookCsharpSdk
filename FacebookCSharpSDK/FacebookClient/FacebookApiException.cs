using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Facebook
{
    class FacebookApiException : Exception
    {
        public string getResult()
        {
            return string.Empty;
        }
        public string getType() 
        {
            return string.Empty;
        }
    }
}
