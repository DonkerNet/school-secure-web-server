using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Cryptography;
using System.Text;
using SecureWebServer.Core.Entities;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;
using SecureWebServer.DataAccess.Repositories;

namespace SecureWebServer.Service.Security
{
    /// <summary>
    /// Class for managing authentication and authorization of users for HTTP requests.
    /// </summary>
    public class SecurityProvider
    {
        /// <summary>
        /// Gets all the roles that are supported by the webserver.
        /// </summary>
        public static string[] Roles { get; }

        private readonly NameValueCollection _userRoles;
        private readonly ObjectCache _sessionCache;
        private readonly Encoding _hashEncoding;
        private readonly HashAlgorithm _hashAlgorithm;
        private readonly UserRepository _userRepository;

        static SecurityProvider()
        {
            Roles = new[]
            {
                "ondersteuners",
                "beheerders"
            };
        }

        /// <summary>
        /// Creates a new <see cref="SecurityProvider"/> instance where the specified user repository is used for retrieving users for authentication.
        /// </summary>
        public SecurityProvider(UserRepository userRepository)
        {
            // User roles and their permissions are configured in a section in the app.config
            _userRoles = (NameValueCollection)ConfigurationManager.GetSection("userRoles");

            _sessionCache = MemoryCache.Default;
            _hashEncoding = Encoding.UTF8;
            _hashAlgorithm = HashAlgorithm.Create("SHA-512");
            _userRepository = userRepository;
        }

        /// <summary>
        /// Checks if the specified role is supported.
        /// </summary>
        public bool RoleExists(string role)
        {
            if (string.IsNullOrEmpty(role))
                return false;

            return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if the specified user is allowed to call the specified path using the specified HTTP method.
        /// </summary>
        public bool UserIsInRole(string path, string httpMethod, User user)
        {
            string roleString = _userRoles[path.ToLowerInvariant()];

            // If no roles are configured for the path, assume everyone is allowed to see it
            if (string.IsNullOrEmpty(roleString))
                return true;

            // If roles are configured for a path, then the path may only be accessed by an existing user with the proper permissions

            if (user == null)
                return false;

            // Search for any role that the user has and that matches the specified path and HTTP method
            foreach (string[] roleParts in roleString.Split(';').Select(r => r.Split('=')))
            {
                string roleName = roleParts[0];
                string[] rolePermissions = roleParts[1].Split('|');

                if (!rolePermissions.Contains(httpMethod))
                    continue;

                if (user.Roles.Contains(roleName))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Authenticates a user and writes a response cookie containing the token of the login session.
        /// </summary>
        public bool AuthenticateUser(string username, string password, ResponseMessage response)
        {
            User user = _userRepository.GetByName(username);

            if (user != null)
            {
                string hash = CreatePasswordHash(username, password, user.PasswordSalt);

                if (hash == user.PasswordHash)
                {
                    string sessionToken = Guid.NewGuid().ToString("N");
                    _sessionCache.Set($"SessionUser_{sessionToken}", user, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(20D) });
                    response.Headers["Set-Cookie"] = $"sessionToken={sessionToken}";

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a user based on the token of the login session, if that is present in the request and the user is actually authenticated.
        /// </summary>
        public User GetUserForRequest(RequestMessage request)
        {
            string sessionToken = GetSessionTokenFromRequest(request);

            if (sessionToken == null)
                return null;

            return _sessionCache[$"SessionUser_{sessionToken}"] as User;
        }

        /// <summary>
        /// Logs out a user and clears the login session cookie.
        /// </summary>
        public void LogoutUser(RequestMessage request, ResponseMessage response)
        {
            string sessionToken = GetSessionTokenFromRequest(request);

            if (sessionToken != null)
            {
                _sessionCache.Remove(sessionToken);
                response.Headers["Set-Cookie"] = "sessionToken=expired; expires=Thu, 01 Jan 1970 00:00:00 GMT";
            }

            request.User = null;
        }

        // Creates a base64 password hash
        private string CreatePasswordHash(string username, string password, string salt)
        {
            string input = $"{username}|{password}|{salt}";
            byte[] inputBytes = _hashEncoding.GetBytes(input);
            byte[] hashBytes = _hashAlgorithm.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashBytes);
        }

        // Extracts the session token from the request cookie if it is present
        private string GetSessionTokenFromRequest(RequestMessage request)
        {
            string cookieHeader = request.Headers["Cookie"];

            if (string.IsNullOrEmpty(cookieHeader))
                return null;

            string sessionToken = null;

            foreach (string[] cookie in cookieHeader.Split(';').Select(c => c.Split('=')))
            {
                if (string.Equals(cookie[0].Trim(), "sessionToken"))
                {
                    sessionToken = cookie[1].Trim();
                    break;
                }
            }

            if (string.IsNullOrEmpty(sessionToken))
                return null;

            return sessionToken;
        }
    }
}