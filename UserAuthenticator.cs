using SmtpServer;
using SmtpServer.Authentication;

namespace SMTPRelay
{
    public class UserAuthenticator : IUserAuthenticator, IUserAuthenticatorFactory
    {
        public Task<bool> AuthenticateAsync(ISessionContext context, string user, string password, CancellationToken token)
        {
            if (Program.AuthStatus == true)
                return Task.FromResult(user == Program.LocalUser && password == Program.LocalPass);
            return Task.FromResult(true);
        }

        public IUserAuthenticator CreateInstance(ISessionContext context)
        {
            return new UserAuthenticator();
        }
    }
}
