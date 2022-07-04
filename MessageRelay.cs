using MailKit.Net.Proxy;
using MailKit.Net.Smtp;
using SmtpServer;
using SmtpServer.Storage;
using System.Buffers;

namespace SMTPRelay
{
    public class MessageRelay : MessageStore
    {
        public override async Task<SmtpServer.Protocol.SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            using (var ms = new MemoryStream())
            {
                var position = buffer.GetPosition(0);

                while (buffer.TryGet(ref position, out var memory))
                {
                    await ms.WriteAsync(memory, cancellationToken);
                }

                ms.Position = 0;

                var message = await MimeKit.MimeMessage.LoadAsync(ms, cancellationToken);

                using (var client = new SmtpClient())
                {
                    switch (Program.ProxyMethod)
                    {
                        case "socks5":
                            if (Program.ProxyUser == "")
                            {
                                var socks5 = new Socks5Client(Program.ProxyAddress, Program.ProxyPort);
                                client.ProxyClient = socks5;
                            }
                            else
                            {
                                var socks5 = new Socks5Client(Program.ProxyAddress, Program.ProxyPort, new System.Net.NetworkCredential(Program.ProxyUser, Program.ProxyPass));
                                client.ProxyClient = socks5;
                            }
                            break;
                        case "http":
                            if (Program.ProxyUser == "")
                            {
                                var http = new HttpProxyClient(Program.ProxyAddress, Program.ProxyPort);
                                client.ProxyClient = http;
                            }
                            else
                            {
                                var http = new HttpProxyClient(Program.ProxyAddress, Program.ProxyPort, new System.Net.NetworkCredential(Program.ProxyUser, Program.ProxyPass));
                                client.ProxyClient = http;
                            }
                            break;
                        case "https":
                            if (Program.ProxyUser == "")
                            {
                                var https = new HttpsProxyClient(Program.ProxyAddress, Program.ProxyPort);
                                client.ProxyClient = https;
                            }
                            else
                            {
                                var https = new HttpsProxyClient(Program.ProxyAddress, Program.ProxyPort, new System.Net.NetworkCredential(Program.ProxyUser, Program.ProxyPass));
                                client.ProxyClient = https;
                            }
                            break;
                        case "none":
                        default:
                            break;
                    }
                    try
                    {
                        client.Connect(Program.RemoteAddress, Program.Port, Program.RemoteAuth, cancellationToken);
                        client.Authenticate(Program.RemoteUser, Program.RemotePass, cancellationToken);
                        client.Send(message, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    client.Disconnect(true);
                }
                return SmtpServer.Protocol.SmtpResponse.Ok;
            }
        }
    }
}
