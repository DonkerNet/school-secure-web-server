using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using SecureWebServer.Core.Entities;

namespace SecureWebServer.Service.Security
{
    public static class SecurityProvider
    {
        private static readonly NameValueCollection UserRoles;

        static SecurityProvider()
        {
            UserRoles = (NameValueCollection)ConfigurationManager.GetSection("userRoles");
        }

        public static bool UserIsInRole(string path, string permission, User user)
        {
            string roleString = UserRoles[path.ToLowerInvariant()];

            if (string.IsNullOrEmpty(roleString))
                return true;

            foreach (string[] roleParts in roleString.Split(';').Select(r => r.Split('=')))
            {
                string roleName = roleParts[0];
                string[] rolePermissions = roleParts[1].Split('|');

                if (!rolePermissions.Contains(permission))
                    continue;

                if (user.Roles.Contains(roleName))
                    return true;
            }

            return false;
        }

        // TODO: authenticate user

        // TODO: get authenticated user from (encrypted?) cookie
    }
}