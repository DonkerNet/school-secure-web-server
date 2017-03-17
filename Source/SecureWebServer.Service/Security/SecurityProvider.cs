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
    public class SecurityProvider
    {
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

        public SecurityProvider(UserRepository userRepository)
        {
            _userRoles = (NameValueCollection)ConfigurationManager.GetSection("userRoles");
            _sessionCache = MemoryCache.Default;
            _hashEncoding = Encoding.UTF8;
            _hashAlgorithm = HashAlgorithm.Create("SHA-512");
            _userRepository = userRepository;
        }

        public bool RoleExists(string role)
        {
            if (string.IsNullOrEmpty(role))
                return false;

            return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        public bool UserIsInRole(string path, string method, User user)
        {
            string roleString = _userRoles[path.ToLowerInvariant()];

            if (string.IsNullOrEmpty(roleString))
                return true;

            if (user == null)
                return false;

            foreach (string[] roleParts in roleString.Split(';').Select(r => r.Split('=')))
            {
                string roleName = roleParts[0];
                string[] rolePermissions = roleParts[1].Split('|');

                if (!rolePermissions.Contains(method))
                    continue;

                if (user.Roles.Contains(roleName))
                    return true;
            }

            return false;
        }

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

        public User GetUserForRequest(RequestMessage request)
        {
            string sessionToken = GetSessionTokenFromRequest(request);

            if (sessionToken == null)
                return null;

            return _sessionCache[$"SessionUser_{sessionToken}"] as User;
        }

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

        private string CreatePasswordHash(string username, string password, string salt)
        {
            string input = $"{username}|{password}|{salt}";
            byte[] inputBytes = _hashEncoding.GetBytes(input);
            byte[] hashBytes = _hashAlgorithm.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashBytes);
        }

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