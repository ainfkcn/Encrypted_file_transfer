namespace ClassLibrary
{
    /// <summary>
    /// 全局变量，缓冲区大小
    /// </summary>
    public static class Size
    {
        //套接字接收缓冲区
        public static int BufferSize = 2048;
        //文件读入缓冲区
        public static int FileSize = 1024;
    }
    /// <summary>
    /// 通信协议的功能号
    /// </summary>
    public enum Service : int
    {
        //C<->S
        ACK,
        EOF,
        Directory,

        //C->S
        Login,
        Logout,
        Registration,
        DownLoadSYN,
        UpLoadSYN,
        UpLoad,
        ChangeDirectory,

        //S->C
        RegistrationSuccess,
        UserExist,
        EmptyPassword,
        LoginSuccess,
        WrongPassword,
        FileNotFound,
        TryDownloadPrivate,
        ClientNoEnoughSpace,
        ServerNoEnoughSpace,
        DownLoad,
        DirectoryNotFound
    }
}
