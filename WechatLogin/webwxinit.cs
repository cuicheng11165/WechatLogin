namespace WechatLogin
{
    public class webwxinit
    {
        public BaseResponse BaseResponse { get; set; }
        //�Ự����
        public int Count { get; set; }
        //�Ự�б�
        public User[] ContactList { get; set; }
        //ͬ����Կ
        public SyncKey SyncKey { get; set; }
        //������Ϣ
        public User User { get; set; }
        //�Ự˳��
        public string ChatSet { get; set; }
        public string SKey { get; set; }
        public long ClientVersion { get; set; }
        public long SystemTime { get; set; }
    }
}