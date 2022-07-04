using SharpConfig;

namespace SMTPRelay
{
    class Program
    {
        public static readonly string CfgFile = AppDomain.CurrentDomain.BaseDirectory + "config.ini";

        #region Settings
        public static string ServerAddress { get; private set; }
        public static List<int> ServerPorts { get; private set; }
        public static int MaxMessageSize { get; private set; }
        public static int MaxRetryCount { get; private set; }
        public static TimeSpan CommandWaitTimeout { get; private set; }
        public static bool AuthStatus { get; private set; }
        public static bool IsSecure { get; private set; }
        public static bool AllowUnSecure { get; private set; }
        public static string CrtPath { get; private set; }
        public static string KeyPath { get; private set; }
        public static string LocalUser { get; private set; }
        public static string LocalPass { get; private set; }

        public static string RemoteAddress { get; private set; }
        public static int Port { get; private set; }
        public static MailKit.Security.SecureSocketOptions RemoteAuth { get; private set; }
        public static string RemoteUser { get; private set; }
        public static string RemotePass { get; private set; }
        public static string ProxyMethod { get; private set; }
        public static string ProxyAddress { get; private set; }
        public static int ProxyPort { get; private set; }
        public static string ProxyUser { get; private set; }
        public static string ProxyPass { get; private set; }
        #endregion

        public static void Main(string[] args)
        {
            CheckHasConfig();
            LoadConfig();

            ThreadPool.QueueUserWorkItem(_ => { var R = new Relay(); } );

            Thread.Sleep(-1);
        }

        private static void CheckHasConfig()
        {
            if (!File.Exists(Program.CfgFile))
            {
                using (var file = File.Create(Program.CfgFile))
                {
                    using (StreamWriter sw = new StreamWriter(file))
                    {
                        sw.AutoFlush = true;
                        sw.WriteLine("[local] # Local SMTP Server Settings");
                        sw.WriteLine("address = 127.0.0.1 # Listen IP");
                        sw.WriteLine("port = 25 # Listen Port, Use , Split");
                        sw.WriteLine("msgsize = 1024000 # Receive Max Email Size");
                        sw.WriteLine("retry = 3 # Max Retry Count");
                        sw.WriteLine("timeout = 30 # Command Wait Timeout (second)");
                        sw.WriteLine("auth = false # Enable Authenticator");
                        sw.WriteLine("issafe = false #  Indicates whether the port is secure by default.");
                        sw.WriteLine("allownosafe = false # Sets a value indicating whether authentication should be allowed on an unsecure session.");
                        sw.WriteLine("crt = # cert/pem path");
                        sw.WriteLine("key = # key path");
                        sw.WriteLine("user = # Default UserName");
                        sw.WriteLine("pass = # Default Password");
                        sw.WriteLine("");
                        sw.WriteLine("[remote] # Forward To Remote Server Settings");
                        sw.WriteLine("address = smtp.gmail.com # Server Address");
                        sw.WriteLine("port = 587 # Server Port");
                        sw.WriteLine("auth = tls # Auth Method(none,ssl,tls,starttls);");
                        sw.WriteLine("user = test@gmail.com # UserName");
                        sw.WriteLine("pass = testapppassword # App Password");
                        sw.WriteLine("proxy = none # Proxy Method(none, socks5, http, https)");
                        sw.WriteLine("paddress = localhost # Proxy Address");
                        sw.WriteLine("pport = 8123 # Proxy Port");
                        sw.WriteLine("puser = proxyusername # Proxy Username");
                        sw.WriteLine("ppass = hackme # Proxy Password");
                    }
                }
            }
        }

        private static void LoadConfig()
        {
            var cfg = Configuration.LoadFromFile(Program.CfgFile);
            var local = cfg["local"];

            ServerAddress = local["address"].StringValue;
            ServerPorts = new List<int>();

            var ports = local["port"].StringValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < ports.Length; i++)
            {
                if (int.TryParse(ports[i], out var port))
                {
                    ServerPorts.Add(port);
                }
                else
                {
                    Console.WriteLine("Can't Parse Local Server Port!");
                }
            }

            MaxMessageSize = local["msgsize"].IntValue;
            MaxRetryCount = local["retry"].IntValue;
            CommandWaitTimeout = TimeSpan.FromSeconds(local["timeout"].DoubleValue);

            switch (local["auth"].StringValue.ToLower())
            {
                case "true":
                    AuthStatus = true;
                    break;
                case "false":
                    AuthStatus = false;
                    break;
                default:
                    Console.WriteLine("Can't Parse Local Server Auth Status, Default Don't Use Auth!");
                    AuthStatus = false;
                    break;
            }

            IsSecure = local["issafe"].BoolValue;
            AllowUnSecure = local["allownosafe"].BoolValue;

            CrtPath = local["crt"].StringValue;
            KeyPath = local["key"].StringValue;

            LocalUser = local["user"].StringValue;
            LocalPass = local["pass"].StringValue;

            var remote = cfg["remote"];

            RemoteAddress = remote["address"].StringValue;
            Port = remote["port"].IntValue;

            switch (remote["auth"].StringValue.ToLower())
            {
                case "none":
                    RemoteAuth = MailKit.Security.SecureSocketOptions.None;
                    break;
                case "ssl":
                    RemoteAuth = MailKit.Security.SecureSocketOptions.SslOnConnect;
                    break;
                case "tls":
                    RemoteAuth = MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable;
                    break;
                case "starttls":
                    RemoteAuth = MailKit.Security.SecureSocketOptions.StartTls;
                    break;
                default:
                    Console.WriteLine("Can't Parse Remote Auth Method! Use Default None!");
                    RemoteAuth = MailKit.Security.SecureSocketOptions.None;
                    break;
            }

            RemoteUser = remote["user"].StringValue;
            RemotePass = remote["pass"].StringValue;

            switch (remote["proxy"].StringValue.ToLower())
            {
                case "none":
                    ProxyMethod = "none";
                    break;
                case "socks5":
                    ProxyMethod = "socks5";
                    break;
                case "http":
                    ProxyMethod = "http";
                    break;
                case "https":
                    ProxyMethod = "https";
                    break;
                default:
                    Console.WriteLine("Can't Parse Remote Proxy Auth! Use Default None!");
                    ProxyMethod = "none";
                    break;
            }

            ProxyAddress = remote["paddress"].StringValue;
            ProxyPort = remote["pport"].IntValue;

            ProxyUser = remote["puser"].StringValue;
            ProxyPass = remote["ppass"].StringValue;
        }

        
    }
}