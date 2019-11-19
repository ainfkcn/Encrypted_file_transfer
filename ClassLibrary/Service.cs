namespace ClassLibrary
{
    /// <summary>
    /// 全局变量，缓冲区大小
    /// </summary>
    public static class BufferSize
    {
        public static int Size = 2048;
    }

    /// <summary>
    /// 通信协议的功能号
    /// </summary>
    public enum Service : int
    {
        ACK,
        Login
    }
}
