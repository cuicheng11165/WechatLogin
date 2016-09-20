using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace WechatLogin
{
    public class WechatCredential
    {
        private string uuid;
        private int tip;
        const string UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_2) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/44.0.2403.125 Safari/537.36";
        private string push_uri;
        private string redirectUrl;


        private string Uin;
        private string Sid;
        private string Skey;
        private string DeviceID = "e000000000000000";

        private CookieContainer cookieContainer = new CookieContainer();

        private string base_uri;
        private string pass_ticket;

        private string me = "";
        private string to = "";

        public WechatCredential() { }

        public void Login()
        {
            GetUUID();
            ShowQRCode();
            while (WaitForLogin()) { }

            if (CellphoneLogin())
            {
            }

            WebWechatInit();

            //WebWeChatGetContact();
            while (true)
            {
                Send();
                for (int i = 0; i < 5; i++)
                {
                    SyncMsg();
                    Thread.Sleep(1000);
                }
            }
        }

        public void Send()
        {
            long time = Time.Now();
            string url = "http://wx.qq.com/cgi-bin/mmwebwx-bin/webwxsendmsg" +
                "?sid=" + Sid +
                "&skey=" + Skey +
                "&pass_ticket=" + pass_ticket +
                "&r=" + time;
            var request = HttpWebRequest.CreateHttp(url);
            request.Method = "POST";
            request.CookieContainer = cookieContainer;
            JObject jsonObj = new JObject();
            jsonObj.Add("BaseRequest", JObject.FromObject(new BaseRequest { Sid = Sid, Skey = Skey, Uin = Uin, DeviceID = DeviceID }));
            SendMsg msg = new SendMsg();
            msg.FromUserName = "cuicheng11165";
            msg.ToUserName = to;
            msg.Type = 1;
            msg.Content = DateTime.Now.ToString();
            msg.ClientMsgId = time;
            msg.LocalID = time;

            jsonObj.Add("Msg", JObject.FromObject(msg));

            byte[] byteArray = Encoding.UTF8.GetBytes(jsonObj.ToString().Replace("\r\n", ""));
            request.ContentType = "application/json; charset=UTF-8";
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            WebResponse response = request.GetResponse();
            dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string ret = reader.ReadToEnd();
            //webwxsendmsg wxsendmsg = JsonConvert.DeserializeObject<webwxsendmsg>(ret);         
        }

        private void WebWeChatGetContact()
        {
            var uri = string.Format("https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxgetcontact?pass_ticket={0}&skey={1}&r={2}", pass_ticket, Skey, Time.Now());

            var request = HttpWebRequest.CreateHttp(uri);
            request.CookieContainer = cookieContainer;
            request.ContentType = "application/json; charset=UTF-8";
            request.Method = "POST";
            var response = request.GetResponse();

            var result = new StreamReader(response.GetResponseStream()).ReadToEnd();



            webwxgetcontact getcontact = JsonConvert.DeserializeObject<webwxgetcontact>(result);
            foreach (User user in getcontact.MemberList)
            {
                user.setDisplayName();
                Data.Contactlist.Add(user.UserName, user);

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

            var uri = string.Format("{0}/webwxinit?pass_ticket={1}&skey={2}&r={3}", base_uri, pass_ticket, Skey, Time.Now());


            var request = WebRequest.CreateHttp(uri);
            request.ContentType = "application/json; charset=UTF-8";
            request.Method = "POST";
            request.CookieContainer = cookieContainer;
            var requestStream = request.GetRequestStream();
            string jsonString = "{  \"BaseRequest\": {    \"Uin\": \"" + Uin + "\",    \"Sid\": \"" + Sid + "\",    \"Skey\": \"" + Skey + "\",    \"DeviceID\": \"e000000000000000\"  }}";
            var jsonContent = Encoding.UTF8.GetBytes(jsonString);
            requestStream.Write(jsonContent, 0, jsonContent.Length);
            requestStream.Close();

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

            Data.synckey = init.SyncKey;

            return true;
        }

        private bool CellphoneLogin()
        {
            var request = HttpWebRequest.CreateHttp(redirectUrl);
            request.CookieContainer = cookieContainer;
            var response = request.GetResponse();

            XmlDocument doc = new XmlDocument();
            doc.Load(response.GetResponseStream());

            //< error >
            //< ret > 0 </ ret >
            //< message ></ message >
            //< skey > @crypt_e2dfb49c_5a3e0b7051e33c721f57f15fbc17708e </ skey >
            //< wxsid > GGPOpVZhv / beOO2m </ wxsid >
            //< wxuin > 782693340 </ wxuin >
            //< pass_ticket > o7UeaE % 2FrUqD1cSfRI6oAB4v84hypalxwIodlJD0FBkSEtYJYkflh2RoOxUPt6HSg </ pass_ticket >
            //< isgrayscale > 1 </ isgrayscale >
            //   </ error >

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
            var url = string.Format("https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip={0}&uuid={1}&_={2}", tip, uuid, Time.Now());

            var waitForLoginRequest = HttpWebRequest.CreateHttp(url);
            waitForLoginRequest.CookieContainer = cookieContainer;
            var response = waitForLoginRequest.GetResponse();

            var text = new StreamReader(response.GetResponseStream()).ReadToEnd();

            var loginDic = text.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
               .ToDictionary(a => a.Substring(0, a.IndexOf("=")).Trim(), a => a.Substring(a.IndexOf("=") + 1).Trim());

            if (loginDic["window.code"] == "201")
            {
                tip = 0;
                return true;
            }
            if (loginDic["window.code"] == "200")
            {
                var services = new Dictionary<string, string> {
                { "wx2.qq.com", "webpush2.weixin.qq.com"},
                { "qq.com", "webpush.weixin.qq.com"},
                { "web1.wechat.com", "webpush1.wechat.com"},
                { "web2.wechat.com", "webpush2.wechat.com"},
                { "wechat.com", "webpush.wechat.com"},
                { "web1.wechatapp.com", "webpush1.wechatapp.com"}
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

        void SyncMsg()
        {
            string url = "http://webpush.weixin.qq.com/cgi-bin/mmwebwx-bin/synccheck" +
                "?pass_ticket=" + Data.pass_ticket +
                "&skey=" + Skey +
                "&sid=" + Sid +
                "&uin=" + Uin +
                "&deviceid=" + DeviceID +
                "&synckey=" + Data.synckey.get_urlstring() +
                "&_=" + Time.Now();

            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string ret_str = reader.ReadToEnd().Split('=')[1];
            synccheck ret = JsonConvert.DeserializeObject<synccheck>(ret_str);

            reader.Close();
            dataStream.Close();
            response.Close();


          

            Console.WriteLine("同步消息");

            Console.WriteLine(ret_str);



        }

        private void ShowQRCode()
        {
            var fetchQRImageUri = string.Format("https://login.weixin.qq.com/qrcode/{0}?t=webwx&_={1}", uuid, Time.Now());

            var fetchQRImageRequest = HttpWebRequest.CreateHttp(fetchQRImageUri);
            fetchQRImageRequest.CookieContainer = cookieContainer;

            var response = fetchQRImageRequest.GetResponse();

            var stream = response.GetResponseStream();

            byte[] buffer = new byte[1 * 1024];
            int bytesProcessed = 0;
            var fs = new FileStream("qrcode.jpg", FileMode.Create, FileAccess.Write);
            int bytesRead;
            do
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                fs.Write(buffer, 0, bytesRead);
                bytesProcessed += bytesRead;
            }
            while (bytesRead > 0);
            fs.Flush();
            fs.Close();


            tip = 1;
            Process ps = new Process();
            ps.StartInfo = new ProcessStartInfo("qrcode.jpg");
            ps.Start();

            Console.WriteLine("请使用微信扫描二维码以登录");
        }

        private void GetUUID()
        {
            var fetchUUIDUri = new Uri(string.Format("https://login.weixin.qq.com/jslogin?appid=wx782c26e4c19acffb&fun=new&lang=zh_CN&_={0}", Time.Now().ToString()));

            var fetchUIIDRequest = HttpWebRequest.CreateHttp(fetchUUIDUri);
            fetchUIIDRequest.CookieContainer = new CookieContainer();
            fetchUIIDRequest.Method = "GET";
            fetchUIIDRequest.UserAgent = UserAgent;

            var response = fetchUIIDRequest.GetResponse();
            this.cookieContainer = fetchUIIDRequest.CookieContainer;

            var toEnd = new StreamReader(response.GetResponseStream()).ReadToEnd();

            var uiidDic = toEnd.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .ToDictionary(a => a.Substring(0, a.IndexOf("=")).Trim(), a => a.Substring(a.IndexOf("=") + 1).Trim());

            var resultCode = uiidDic["window.QRLogin.code"];

            if (!string.Equals(resultCode, "200", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Get uiid failed");
            }

            uuid = uiidDic["window.QRLogin.uuid"].Trim('"');

        }



    }


    public class BaseRequest
    {
        public string Uin { get; set; }
        public string Sid { get; set; }
        public string Skey { get; set; }
        public string DeviceID { get; set; }
    }

    public class SendMsg
    {
        public string FromUserName { get; set; }
        public string ToUserName { get; set; }
        public int Type { get; set; }
        public string Content { get; set; }
        public long ClientMsgId { get; set; }
        public long LocalID { get; set; }
    }

    public class webwxsendmsg
    {
        public BaseResponse BaseResponse { get; set; }
        public long MsgID { get; set; }
        public string LocalID { get; set; }
    }

    public class BaseResponse
    {
        public int Ret { get; set; }
        public string ErrMsg { get; set; }
    }

    public class Msg
    {
        public long MsgId { get; set; }
        public string FromUserName { get; set; }
        public string ToUserName { get; set; }
        public int MsgType { get; set; }
        public string Content { get; set; }
        public int Status { get; set; }
        public int ImgStatus { get; set; }
        public long CreateTime { get; set; }
        public int VoiceLength { get; set; }
        public int PlayLength { get; set; }
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public string MediaId { get; set; }
        public string Url { get; set; }
        public int AppMsgType { get; set; }
        public int StatusNotifyCode { get; set; }
        public string StatusNotifyUserName { get; set; }
        //public RecommendInfo RecommendInfo { get; set; }
        public int ForwardFlag { get; set; }
        //public AppInfo AppInfo { get; set; }
        public int HasProductId { get; set; }
        public string Ticket { get; set; }
    }

    public static class Time
    {
        /// <summary>
        /// 将时间转换成UNIX时间戳
        /// </summary>
        /// <param name="dt">时间</param>
        /// <returns>UNIX时间戳</returns>
        public static UInt32 Now()
        {
            TimeSpan ts = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            UInt32 uiStamp = Convert.ToUInt32(ts.TotalSeconds);
            return uiStamp;
        }
    }

    public class webwxgetcontact
    {
        public BaseResponse BaseResponse { get; set; }
        public int MemberCount { get; set; }
        public User[] MemberList { get; set; }
    }

    public class User
    {
        public long Uin { get; set; }
        public string UserName { get; set; }
        public string NickName { get; set; }
        public string HeadImgUrl { get; set; }
        public string RemarkName { get; set; }
        public string PYInitial { get; set; }
        public string PYQuanPin { get; set; }
        public string RemarkPYInitial { get; set; }
        public string RemarkPYQuanPin { get; set; }
        public int HideInputBarFlag { get; set; }
        public int StarFriend { get; set; }
        public int Sex { get; set; }
        public string Signature { get; set; }
        public int AppAccountFlag { get; set; }
        public int VerifyFlag { get; set; }
        public int ContactFlag { get; set; }
        public int SnsFlag { get; set; }

        //me
        public int WebWxPluginSwitch { get; set; }
        public int HeadImgFlag { get; set; }

        //friend
        public int MemberCount { get; set; }
        public User[] MemberList { get; set; }
        public int OwnerUin { get; set; }
        public int Statues { get; set; }
        public int AttrStatus { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string Alias { get; set; }
        public int UniFriend { get; set; }
        public string DisplayName { get; set; }
        public int ChatRoomId { get; set; }

        //member
        //public int AttrStatus { get; set; }
        //public string DisplayName { get; set; }
        public int MemberStatus { get; set; }

        public void setDisplayName()
        {
            DisplayName = RemarkName.Equals("") ? NickName : RemarkName;
            DisplayName = DisplayName.Equals("") ? UserName : DisplayName;
        }
    }

    public static class Data
    {
        public static string skey;
        public static string wxsid;
        public static string wxuin;
        public static string webwx_data_ticket;
        public static string pass_ticket;
        public static string device_id = "e000000000000000";
        public static string cookie;
        public static SyncKey synckey;
        public static BaseRequest baseRequest;


        //个人信息
        public static User me;
        //主窗口

        //会话列表
        public static Dictionary<string, User> Chatlist = new Dictionary<string, User>();
        //通讯录列表
        public static Dictionary<string, User> Contactlist = new Dictionary<string, User>();



    }

    public class SyncKey
    {
        public int Count { get; set; }
        public Key_Val[] List { get; set; }

        public string get_urlstring()
        {
            string urlstring = "";
            for (int i = 0; i < Count; i++)
            {
                if (i != 0) urlstring += "|";
                urlstring += List[i].Key + "_" + List[i].Val;
            }
            return urlstring;
        }
    }

    public class Key_Val
    {
        public int Key { get; set; }
        public long Val { get; set; }
    }

    public class webwxinit
    {
        public BaseResponse BaseResponse { get; set; }
        //会话数量
        public int Count { get; set; }
        //会话列表
        public User[] ContactList { get; set; }
        //同步密钥
        public SyncKey SyncKey { get; set; }
        //个人信息
        public User User { get; set; }
        //会话顺序
        public string ChatSet { get; set; }
        public string SKey { get; set; }
        public long ClientVersion { get; set; }
        public long SystemTime { get; set; }
    }

    public class synccheck
    {
        public string retcode { get; set; }
        public string selector { get; set; }
    }

}


