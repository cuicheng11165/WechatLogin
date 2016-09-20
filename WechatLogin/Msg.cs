namespace WechatLogin
{
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
        public RecommendInfo RecommendInfo { get; set; }
        public int ForwardFlag { get; set; }
        public AppInfo AppInfo { get; set; }
        public int HasProductId { get; set; }
        public string Ticket { get; set; }
    }
}