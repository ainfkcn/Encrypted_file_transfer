namespace ClassLibrary
{
    /// <summary>
    /// 全局变量，缓冲区大小
    /// </summary>
    public static class Size
    {
        public static int BufferSize = 2048;
        public static int FileSize = 1024;
    }

    public enum Direction : int
    {
        DownLoad,
        UpLoad,
        Normal,
    }

    /// <summary>
    /// 通信协议的功能号
    /// </summary>
    public enum Service : int
    {
        //C<->S
        ACK,
        DownLoad,
        UpLoad,
        EOF,

        //C->S
        Login,
        Registration,
        DownLoadSYN,
        UpLoadSYN,

        //S->C
        //注册
        RegistrationSuccess,
        UserExist,
        EmptyPassword,
        //登陆
        LoginSuccess,
        WrongPassword,
        //文件传输
        FileNotFound,
    }
}
