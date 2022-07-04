using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace SMTPRelay
{
    public class Relay
    {
        private readonly CancellationTokenSource CancelTokenSource;
        private readonly CancellationToken CancelToken;
        private readonly PosixSignalRegistration Signal;
        private readonly ISmtpServerOptions options;
        private readonly ServiceProvider ServiceProvider;
        private readonly IUserAuthenticator Auth;
        private readonly SmtpServer.SmtpServer Server;

        public Relay()
        {
            CancelTokenSource = new CancellationTokenSource();
            CancelToken = CancelTokenSource.Token;

            Signal = PosixSignalRegistration.Create(PosixSignal.SIGTERM, WaitStop);

            //var option = new SmtpServerOptionsBuilder()
            //    .ServerName(Program.ServerAddress)
            //    .MaxMessageSize(Program.MaxMessageSize)
            //    .MaxRetryCount(Program.MaxRetryCount)
            //    .CommandWaitTimeout(Program.CommandWaitTimeout);
                //.Endpoint(builder =>
                //    builder
                //        .Port(Program.Port, Program.IsSecure)
                //        .AllowUnsecureAuthentication(Program.AllowUnSecure)
                        
                //);
            if (Program.IsSecure)
            {
                options = new SmtpServerOptionsBuilder()
                    .ServerName(Program.ServerAddress)
                    .MaxMessageSize(Program.MaxMessageSize)
                    .MaxRetryCount(Program.MaxRetryCount)
                    .CommandWaitTimeout(Program.CommandWaitTimeout)
                    .Endpoint(builder =>
                        builder
                            .Port(Program.Port, Program.IsSecure)
                            .AllowUnsecureAuthentication(Program.AllowUnSecure)
                            .Certificate(LoadCertificate())).Build();
            }
            else
            {
                options = new SmtpServerOptionsBuilder()
                    .ServerName(Program.ServerAddress)
                    .MaxMessageSize(Program.MaxMessageSize)
                    .MaxRetryCount(Program.MaxRetryCount)
                    .CommandWaitTimeout(Program.CommandWaitTimeout)
                    .Port(Program.Port).Build();
                    //.Endpoint(builder =>
                    //    builder
                    //        .Port(Program.Port, Program.IsSecure)
                    //        .AllowUnsecureAuthentication(Program.AllowUnSecure)).Build();
                //option.Endpoint(builder =>
                //    builder
                //        .Port(Program.Port, Program.IsSecure)
                //        .AllowUnsecureAuthentication(Program.AllowUnSecure));
                //options = option.Build();
            }

            ServiceProvider = new ServiceProvider();

            if (Program.AuthStatus == true)
            {
                Auth = new UserAuthenticator();
                ServiceProvider.Add(Auth);
            }
            ServiceProvider.Add(new MessageRelay());

            Server = new SmtpServer.SmtpServer(options, ServiceProvider);
            Server.SessionCreated +=Server_SessionCreated;
            Server.StartAsync(CancelToken).Wait();
        }

        private void Server_SessionCreated(object? sender, SessionEventArgs e)
        {
            Console.WriteLine("Session Created!");
        }

        private X509Certificate2 LoadCertificate()
        {
            var cert = File.ReadAllBytes(Program.CrtPath);
            var pass = File.ReadAllText(Program.KeyPath);
            return new X509Certificate2(cert, pass);
        }

        private void WaitStop(PosixSignalContext context)
        {
            context.Cancel = true;
            CancelTokenSource.Cancel();
            Server.Shutdown();
        }
    }
}
