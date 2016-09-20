using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Collections.Specialized;
using System.IO;

namespace WechatLogin
{

    class Program
    {
        

        static void Main(string[] args)
        {

            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            var login = new WechatCredential();
            login.Login();


          


        }
    }
}
