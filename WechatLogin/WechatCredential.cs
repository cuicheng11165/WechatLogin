using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WechatLogin
{
    public class WechatCredential
    {
        const string UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_2) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/44.0.2403.125 Safari/537.36";

        private string base_uri;

        public Dictionary<string, User> Contactlist = new Dictionary<string, User>();

        private CookieContainer cookieContainer = new CookieContainer();
        private readonly string DeviceID = "e000000000000000";

        private string me = "";
        private string pass_ticket;
        private string push_uri;
        private string redirectUrl;
        private string Sid;
        private string Skey;
        private SyncKey synckey;
        private int tip;
        private string to = "";


        private string Uin;
        private string uuid;

        private BaseRequest baseRequest => new BaseRequest { Sid = Sid, Skey = Skey, Uin = Uin, DeviceID = DeviceID };


        public void Login()
        {
            GetUUID();
            ShowQRCode();
            while (WaitForLogin())
            {
            }

            if (CellphoneLogin())
            {
            }

            WebWechatInit();

            //WebWeChatGetContact();
            while (true)
            {
                Send();
                for (var i = 0; i < 5; i++)
                {
                    SyncMsg();
                    Thread.Sleep(1000);
                }
            }
        }

        public void Send()
        {
            long time = Time.Now();
            var url = "http://wx.qq.com/cgi-bin/mmwebwx-bin/webwxsendmsg" +
                      "?sid=" + Sid +
                      "&skey=" + Skey +
                      "&pass_ticket=" + pass_ticket +
                      "&r=" + time;
            var request = WebRequest.CreateHttp(url);
            request.Method = "POST";
            request.CookieContainer = cookieContainer;
            var msg = new SendMsg
            {
                FromUserName = "cuicheng11165",
                ToUserName = to,
                Type = 1,
                Content = DateTime.Now.ToString(),
                ClientMsgId = time,
                LocalID = time
            };

            var jsonObj = new JObject
            {
                {
                    "BaseRequest",
                    JObject.FromObject(new BaseRequest {Sid = Sid, Skey = Skey, Uin = Uin, DeviceID = DeviceID})
                },
                {"Msg", JObject.FromObject(msg)}
            };

            var byteArray = Encoding.UTF8.GetBytes(jsonObj.ToString().Replace("\r\n", ""));
            request.ContentType = "application/json; charset=UTF-8";
            request.ContentLength = byteArray.Length;
            var dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            var response = request.GetResponse();
            dataStream = response.GetResponseStream();
            var reader = new StreamReader(dataStream);
            var ret = reader.ReadToEnd();
            //webwxsendmsg wxsendmsg = JsonConvert.DeserializeObject<webwxsendmsg>(ret);         
        }

        private void WebWeChatGetContact()
        {
            var uri =
                string.Format("https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxgetcontact?pass_ticket={0}&skey={1}&r={2}",
                    pass_ticket, Skey, Time.Now());

            var request = WebRequest.CreateHttp(uri);
            request.CookieContainer = cookieContainer;
            request.ContentType = "application/json; charset=UTF-8";
            request.Method = "POST";
            var response = request.GetResponse();

            var result = new StreamReader(response.GetResponseStream()).ReadToEnd();


            var getcontact = JsonConvert.DeserializeObject<webwxgetcontact>(result);
            foreach (var user in getcontact.MemberList)
            {
                user.setDisplayName();
                Contactlist.Add(user.UserName, user);

                if (user.DisplayName.IndexOf("Cui") > 0)
                {
                    me = user.UserName;
                }
                else if (user.DisplayName.IndexOf("Robert") > 0)
                {
                    to = user.UserName;
                }
                Console.WriteLine("NickName {0}, DisplayName {1}", user.NickName, user.DisplayName);
            }
        }

        private bool WebWechatInit()
        {
            var uri = string.Format("{0}/webwxinit?pass_ticket={1}&skey={2}&r={3}", base_uri, pass_ticket, Skey,
                Time.Now());


            var request = WebRequest.CreateHttp(uri);
            request.ContentType = "application/json; charset=UTF-8";
            request.Method = "POST";
            request.CookieContainer = cookieContainer;
            using (var requestStream = request.GetRequestStream())
            {
                var jsonContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(baseRequest));
                requestStream.Write(jsonContent, 0, jsonContent.Length);
            }

            var response = request.GetResponse();
            var text = new StreamReader(response.GetResponseStream()).ReadToEnd();

            var init = JsonConvert.DeserializeObject<webwxinit>(text);

            foreach (var user in init.ContactList)
            {
                Console.WriteLine("NickName {0}, DisplayName {1}", user.NickName, user.DisplayName);
                if (user.NickName.Equals("Robert"))
                {
                    Console.WriteLine("NickName {0}, DisplayName {1}", user.NickName, user.DisplayName);
                    to = user.UserName;
                }
            }

            synckey = init.SyncKey;

            return true;
        }

        private bool CellphoneLogin()
        {
            var request = WebRequest.CreateHttp(redirectUrl);
            request.CookieContainer = cookieContainer;
            var response = request.GetResponse();

            var doc = new XmlDocument();
            doc.Load(response.GetResponseStream());

            var ret = new Dictionary<string, string>();
            foreach (var node in doc.DocumentElement.ChildNodes.Cast<XmlElement>())
            {
                ret[node.Name] = node.InnerText;
            }

            Uin = ret["wxuin"];
            Sid = ret["wxsid"];
            Skey = ret["skey"];
            pass_ticket = ret["pass_ticket"];

            return false;
        }

        private bool WaitForLogin()
        {
            var url = string.Format("https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip={0}&uuid={1}&_={2}", tip,
                uuid, Time.Now());

            var waitForLoginRequest = WebRequest.CreateHttp(url);
            waitForLoginRequest.CookieContainer = cookieContainer;
            var response = waitForLoginRequest.GetResponse();

            var text = new StreamReader(response.GetResponseStream()).ReadToEnd();

            var loginDic = text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .ToDictionary(a => a.Substring(0, a.IndexOf("=")).Trim(), a => a.Substring(a.IndexOf("=") + 1).Trim());

            if (loginDic["window.code"] == "201")
            {
                tip = 0;
                return true;
            }
            if (loginDic["window.code"] == "200")
            {
                var services = new Dictionary<string, string>
                {
                    {"wx2.qq.com", "webpush2.weixin.qq.com"},
                    {"qq.com", "webpush.weixin.qq.com"},
                    {"web1.wechat.com", "webpush1.wechat.com"},
                    {"web2.wechat.com", "webpush2.wechat.com"},
                    {"wechat.com", "webpush.wechat.com"},
                    {"web1.wechatapp.com", "webpush1.wechatapp.com"}
                };

                redirectUrl = loginDic["window.redirect_uri"].Trim('"');

                redirectUrl += "&fun=new";

                base_uri = redirectUrl.Substring(0, redirectUrl.LastIndexOf("/"));

                push_uri = base_uri;

                foreach (var entry in services)
                {
                    if (base_uri.IndexOf(entry.Key) > 0)
                    {
                        push_uri = string.Format("https://{0}/cgi-bin/mmwebwx-bin", entry.Value);
                    }
                }
            }
            return false;
        }

        private void SyncMsg()
        {
            var url = "http://webpush.weixin.qq.com/cgi-bin/mmwebwx-bin/synccheck" +
                      "?pass_ticket=" + pass_ticket +
                      "&skey=" + Skey +
                      "&sid=" + Sid +
                      "&uin=" + Uin +
                      "&deviceid=" + DeviceID +
                      "&synckey=" + synckey.get_urlstring() +
                      "&_=" + Time.Now();

            var request = WebRequest.Create(url);
            using (var response = request.GetResponse())
            using (var dataStream = response.GetResponseStream())
            using (var reader = new StreamReader(dataStream))
            {
                var ret_str = reader.ReadToEnd().Split('=')[1];
                var ret = JsonConvert.DeserializeObject<synccheck>(ret_str);
            }
        }

        private void ShowQRCode()
        {
            var fetchQRImageUri = $"https://login.weixin.qq.com/qrcode/{uuid}?t=webwx&_={Time.Now()}";

            var fetchQRImageRequest = WebRequest.CreateHttp(fetchQRImageUri);
            fetchQRImageRequest.CookieContainer = cookieContainer;

            var response = fetchQRImageRequest.GetResponse();

            var stream = response.GetResponseStream();

            var buffer = new byte[1024];
            var bytesProcessed = 0;
            using (var fs = new FileStream("qrcode.jpg", FileMode.Create, FileAccess.Write))
            {
                int bytesRead;
                do
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    fs.Write(buffer, 0, bytesRead);
                    bytesProcessed += bytesRead;
                } while (bytesRead > 0);
            }

            tip = 1;
            var ps = new Process { StartInfo = new ProcessStartInfo("qrcode.jpg") };
            ps.Start();

            Console.WriteLine("请使用微信扫描二维码以登录");
        }

        private void GetUUID()
        {
            var fetchUUIDUri = new Uri($"https://login.weixin.qq.com/jslogin?appid=wx782c26e4c19acffb&fun=new&lang=zh_CN&_={Time.Now()}");

            var fetchUIIDRequest = WebRequest.CreateHttp(fetchUUIDUri);
            fetchUIIDRequest.CookieContainer = new CookieContainer();
            fetchUIIDRequest.Method = "GET";
            fetchUIIDRequest.UserAgent = UserAgent;

            var response = fetchUIIDRequest.GetResponse();
            cookieContainer = fetchUIIDRequest.CookieContainer;

            var toEnd = new StreamReader(response.GetResponseStream()).ReadToEnd();

            var uiidDic = toEnd.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .ToDictionary(a => a.Substring(0, a.IndexOf("=")).Trim(), a => a.Substring(a.IndexOf("=") + 1).Trim());

            var resultCode = uiidDic["window.QRLogin.code"];

            if (!string.Equals(resultCode, "200", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Get uiid failed");
            }

            uuid = uiidDic["window.QRLogin.uuid"].Trim('"');
        }
    }
}