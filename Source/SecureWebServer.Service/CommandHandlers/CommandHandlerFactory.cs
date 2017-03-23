using SecureWebServer.DataAccess.Repositories;
using SecureWebServer.Service.Security;

namespace SecureWebServer.Service.CommandHandlers
{
    /// <summary>
    /// Creates the correct command handler for the request path, if one exists.
    /// </summary>
    public class CommandHandlerFactory
    {
        private readonly UserRepository _userRepository;
        private readonly SecurityProvider _securityProvider;

        public CommandHandlerFactory(UserRepository userRepository, SecurityProvider securityProvider)
        {
            _userRepository = userRepository;
            _securityProvider = securityProvider;
        }

        /// <summary>
        /// Creates the correct command handler for the request path, if one exists.
        /// </summary>
        public ICommandHandler Create(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            switch (path.ToLowerInvariant())
            {
                case "index.html":
                    return new IndexCommandHandler(_securityProvider);

                case "config.html":
                    return new ConfigCommandHandler(_securityProvider);

                case "log.html":
                    return new LogCommandHandler(_securityProvider);

                case "login.html":
                    return new LoginCommandHandler(_securityProvider);

                case "logout.html":
                    return new LogoutCommandHandler(_securityProvider);

                case "user/overview.html":
                    return new UserOverviewCommandHandler(_userRepository);

                case "user/edit.html":
                    return new UserEditCommandHandler(_userRepository, _securityProvider);
            }

            return null;
        }
    }
}