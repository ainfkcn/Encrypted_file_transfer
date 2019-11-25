using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace Server
{
    public class UserRegistration : DbContext
    {
        //您的上下文已配置为从您的应用程序的配置文件(App.config 或 Web.config)
        //使用“UserRegistration”连接字符串。默认情况下，此连接字符串针对您的 LocalDb 实例上的
        //“Server.UserRegistration”数据库。
        // 
        //如果您想要针对其他数据库和/或数据库提供程序，请在应用程序配置文件中修改“UserRegistration”
        //连接字符串。
        public UserRegistration() : base("name=UserRegistration") { }
        //为您要在模型中包含的每种实体类型都添加 DbSet。有关配置和使用 Code First  模型
        //的详细信息，请参阅 http://go.microsoft.com/fwlink/?LinkId=390109。
        public DbSet<User> UserContext { get; set; }
    }
    public class User
    {
        /// <summary>
        /// 用户名，主键
        /// </summary>
        [Key] public string Name { get; set; }
        /// <summary>
        /// 用户密码的hash（使用C#的object.gethashcode()方法）
        /// </summary>
        public int Password { get; set; }
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public User() { }
        /// <summary>
        /// 带参构造函数
        /// </summary>
        /// <param name="name">用户名</param>
        /// <param name="pw">用户的密码（此刻还没有取哈希）</param>
        public User(string name, string pw)
        {
            Name = name.Clone() as string;
            Password = pw.GetHashCode();
        }
    }
}