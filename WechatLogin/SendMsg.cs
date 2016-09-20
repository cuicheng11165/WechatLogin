namespace WechatLogin
{
    public class SendMsg
    {
        public string FromUserName { get; set; }
        public string ToUserName { get; set; }
        public int Type { get; set; }
        public string Content { get; set; }
        public long ClientMsgId { get; set; }
        public long LocalID { get; set; }
    }
}