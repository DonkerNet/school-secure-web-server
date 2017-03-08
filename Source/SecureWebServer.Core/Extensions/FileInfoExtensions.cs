using System.IO;
using System.Text;

namespace SecureWebServer.Core.Extensions
{
    public static class FileInfoExtensions
    {
        public static bool IsDirectory(this FileInfo fileInfo)
        {
            return fileInfo.Attributes > 0
                && (fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
        }

        public static string ReadFullString(this FileInfo fileInfo, Encoding encoding)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            string result;

            using (StreamReader reader = new StreamReader(fileInfo.OpenRead(), encoding))
                result = reader.ReadToEnd();

            return result;
        }

        public static string ReadFullString(this FileInfo fileInfo)
        {
            return ReadFullString(fileInfo, null);
        }
    }
}
