using System;
using System.IO;
using System.Linq;

namespace SecureWebServer.Core.Helpers
{
    /// <summary>
    /// Contains helper methods for working with file paths.
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// Combines parts of a path and converts forward slashes to backslashes.
        /// </summary>
        public static string Combine(params string[] parts)
        {
            string[] cleanParts = parts.Select(p => p.Trim('\\', '/').Replace("/", "\\")).ToArray();
            return Path.Combine(cleanParts);
        }

        /// <summary>
        /// Tests if a path appears to be valid (does not contain invalid characters).
        /// </summary>
        public static bool IsValid(string path)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            try
            {
                path = path.Replace("/", "\\");
                path = Path.GetFullPath(path);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}