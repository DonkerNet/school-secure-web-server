using System.IO;
using System.Linq;

namespace SecureWebServer.Core.Helpers
{
    public static class PathHelper
    {
        public static string Combine(params string[] parts)
        {
            string[] cleanParts = parts.Select(p => p.Trim('\\', '/').Replace("/", "\\")).ToArray();
            return Path.Combine(cleanParts);
        }
    }
}