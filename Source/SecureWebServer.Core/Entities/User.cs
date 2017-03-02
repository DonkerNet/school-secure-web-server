using System.Collections.Generic;
using System.Linq;

namespace SecureWebServer.Core.Entities
{
    public class User
    {
        public string Name { get; }
        public string[] Roles { get; }

        public User(string name, IEnumerable<string> roles)
        {
            Name = name;
            Roles = roles?.ToArray() ?? new string[0];
        }
    }
}