using System.IO;
using System.Text;

namespace SecureWebServer.Core.Extensions
{
    /// <summary>
    /// Contains extension methods for <see cref="FileInfo"/> objects.
    /// </summary>
    public static class FileInfoExtensions
    {
        /// <summary>
        /// Checks if the file info represents an existing directory.
        /// </summary>
        public static bool DirectoryExists(this FileInfo fileInfo)
        {
            return fileInfo.Attributes > 0
                && (fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
        }

        /// <summary>
        /// Immediately reads all the content in the file and converts it to a string, using the specified encoding.
        /// </summary>
        public static string ReadFullString(this FileInfo fileInfo, Encoding encoding)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            string result;

            using (StreamReader reader = new StreamReader(fileInfo.OpenRead(), encoding))
                result = reader.ReadToEnd();

            return result;
        }

        /// <summary>
        /// Immediately reads all the content in the file and converts it to a string, using UTF8 encoding.
        /// </summary>
        public static string ReadFullString(this FileInfo fileInfo)
        {
            return ReadFullString(fileInfo, null);
        }
    }
}
