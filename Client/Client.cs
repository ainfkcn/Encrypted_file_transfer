using ClassLibrary;
using System;
using System.IO;
using static ClassLibrary.EnDe;
using static System.Console;

namespace Client
{
    public class Client
    {
        /// <summary>
        /// 构想中的交互函数，无参型；有参型用来做shell
        /// </summary>
        public void Shell()
        {
            #region 变量声明，解决作用域
            string op;                  //用户操作
            string FileName = null;     //文件名
            string LocalDirectory = Directory.GetCurrentDirectory();//本地路径
            bool Login = false;         //是否登录
            bool DorU = false;          //是否在上传或下载中
            FileStream fs = null;       //文件流
            #endregion
            try
            {
                while (true)
                {
                    //登陆后的提示符和操作逻辑
                    else if (Login && !DorU)
                    {
                        //下载
                        if (op.Equals("d") || op.Contains("down") || op.Equals("1"))
                        {
                            Write("FileName:");
                            FileName = ReadLine();//读取要下载的文件名
                            if (FileName.Contains("rsa_private"))
                            {
                                WriteLine("不允许下载私钥");
                                continue;
                            }
                            DorU = true;
                            fs = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write);
                            fs.Seek(0, SeekOrigin.End);//打开文件并将光标设置到末尾（断点续传需要）
                            pSend = new Package { ServiceType = Service.DownLoadSYN };
                            pSend.PayLoad.Add(Encode(FileName));//将文件名发送给服务端，让服务器寻找文件
                            DriveInfo driveInfo = new DriveInfo(Directory.GetDirectoryRoot(Directory.GetCurrentDirectory()));
                            pSend.PayLoad.Add(Encode(driveInfo.AvailableFreeSpace.ToString()));
                        }
                        //上传
                        else if (op.Equals("u") || op.Contains("up") || op.Equals("2"))
                        {
                            DorU = true;//设置上传下载中标志位
                            Write("FileName:");
                            FileName = ReadLine();//读取要上传的文件名
                            //打开文件
                            try { fs = new FileStream(LocalDirectory + @"\" + FileName, FileMode.Open, FileAccess.Read); }
                            //文件不存在，解除上传下载模式
                            catch (FileNotFoundException)
                            {
                                DorU = false;//恢复上传下载标志位
                                FileName = null;
                                WriteLine("本地不存在此文件，请检查拼写");
                                continue;
                            }
                            catch (UnauthorizedAccessException)
                            {
                                DorU = false;//恢复上传下载标志位
                                FileName = null;
                                WriteLine("不能上传文件夹");
                                continue;
                            }
                            pSend = new Package { ServiceType = Service.UpLoadSYN };
                            pSend.PayLoad.Add(Encode(FileName));//将文件名发送给服务器
                            FileInfo fi = new FileInfo(fs.Name);
                            pSend.PayLoad.Add(Encode(fi.Length.ToString()));
                        }
                        //如果输入了其他乱七八糟的指令
                        else
                        {
                            continue;
                        }
                        //发送数据包
                        Package.Send(socket, pSend, key, rsa);
                    }
                    //接收数据包
                    pRecive = Package.Recive(socket, key, rsa);
                    //处理逻辑
                    switch (pRecive.ServiceType)
                    {
                        #region 下载
                        //下载中（将接收到的数据写入文件）
                        case Service.DownLoad:
                            fs.Write(pRecive.PayLoad[0], 0, Size.FileSize);
                            pSend = new Package { ServiceType = Service.ACK };
                            Package.Send(socket, pSend, key, rsa);
                            break;
                        //下载结束
                        case Service.EOF:
                            DorU = false;
                            WriteLine("下载成功");
                            fs.Close();
                            break;
                        //服务器未找到要下载的文件
                        case Service.FileNotFound:
                            fs.Close();
                            File.Delete(FileName);//删除创建的空文件
                            FileName = null;
                            DorU = false;
                            WriteLine("远程不存在此文件或是目录，请确定文件存在且不是目录");
                            break;
                        //客户磁盘空间不足
                        case Service.ClientNoEnoughSpace:
                            fs.Close();
                            File.Delete(FileName);//删除创建的空文件
                            FileName = null;
                            DorU = false;
                            WriteLine("磁盘空间不足");
                            break;
                        //试图下载私钥
                        case Service.TryDownloadPrivate:
                            fs.Close();
                            File.Delete(FileName);//删除创建的空文件
                            FileName = null;
                            DorU = false;
                            WriteLine("不允许下载私钥");
                            break;
                        #endregion
                        #region 上传
                        //上传中（读取文件发送给服务器端）
                        case Service.ACK:
                            if (Login)
                            {
                                byte[] buffer = new byte[Size.FileSize];
                                //未到文件末尾
                                if (fs.Read(buffer, 0, Size.FileSize) != 0)
                                {
                                    pSend = new Package { ServiceType = Service.UpLoad };
                                    pSend.PayLoad.Add(buffer);
                                }
                                //到了文件末尾
                                else
                                {
                                    DorU = false;
                                    WriteLine("上传完成");
                                    fs.Close();
                                    pSend = new Package { ServiceType = Service.EOF };
                                }
                                Package.Send(socket, pSend, key, rsa);
                            }
                            break;
                        //服务器空间不足
                        case Service.ServerNoEnoughSpace:
                            DorU = false;
                            WriteLine("上传失败，服务器空间不足");
                            fs.Close();
                            break;
                            #endregion
                    }
                }
            }
            //特殊错误情况：如果远程主机关闭了套接字，则Receive函数立刻返回。但由于未收到信息，所以反序列化时会报错
            catch (System.Runtime.Serialization.SerializationException) { WriteLine("远程主机已断开连接"); }
            catch (Exception e) { WriteLine(e.ToString()); }
        }
    }
}
