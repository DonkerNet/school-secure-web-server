using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using SecureWebServer.Core.Extensions;

namespace SecureWebServer.Core.Response
{
    public class ResponseMessage
    {
        public string HttpVersion { get; }
        public HttpStatusCode StatusCode { get; }
        public NameValueCollection Headers { get; }
        public Stream Content { get; private set; }

        public ResponseMessage(HttpStatusCode statusCode)
        {
            HttpVersion = "HTTP/1.0";
            StatusCode = statusCode;
            Headers = new NameValueCollection();
            Content = new MemoryStream();
        }

        #region Content methods

        public void SetByteContent(byte[] content, string contentType)
        {
            MemoryStream newContentStream = new MemoryStream();

            if (content != null && content.Length > 0)
                newContentStream.Write(content, 0, content.Length);

            Content.Dispose();
            Content = newContentStream;

            if (!string.IsNullOrEmpty(contentType))
                Headers["Content-Type"] = contentType;
        }

        public void SetByteContent(byte[] content)
        {
            SetByteContent(content, "application/octet-stream");
        }

        public void SetStringContent(string content, Encoding encoding, string contentType)
        {
            byte[] buffer = null;

            if (!string.IsNullOrEmpty(content))
            {
                if (encoding == null)
                    encoding = Encoding.ASCII;

                buffer = encoding.GetBytes(content);
            }

            SetByteContent(buffer, contentType);
        }

        public void SetStringContent(string content, Encoding encoding)
        {
            SetStringContent(content, encoding, "text/plain");
        }

        public void SetStringContent(string content, string contentType)
        {
            SetStringContent(content, null, contentType);
        }

        public void SetStringContent(string content)
        {
            SetStringContent(content, null, "text/plain");
        }

        #endregion

        public void WriteToStream(Stream outputStream)
        {
            // Write the headers
            using (StreamWriter writer = new StreamWriter(outputStream, Encoding.ASCII, 1024, true))
            {
                // Write the Status-Line to the stream
                writer.WriteLine("{0} {1} {2}", HttpVersion, (int)StatusCode, GetStatusDescription());

                // Write all the headers to the stream
                foreach (string headerName in Headers.Keys)
                {
                    // Ignore the Content-Length header since we'll add that manually if needed
                    if (string.Equals(headerName, "Content-Length", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    // Ignore the Connection header since we'll add that manually
                    if (string.Equals(headerName, "Connection", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    // Write the header for each value that was found
                    string[] headerValues = Headers.GetValues(headerName);
                    if (headerValues != null && headerValues.Length > 0)
                    {
                        foreach (string headerValue in headerValues)
                            writer.WriteLine("{0}: {1}", headerName, headerValue);
                    }
                }

                // Write the Content-Length and Connection headers
                if (Content.Length > 0)
                    writer.WriteLine("Content-Length: {0}", Content.Length);

                writer.WriteLine("Connection: close");

                // Write an empty line
                writer.WriteLine();

                writer.Flush();
            }

            // Finally, write the body
            if (Content.Length > 0)
            {
                Content.Position = 0;
                Content.CopyTo(outputStream, 0, Content.Length);
            }
        }

        private string GetStatusDescription()
        {
            switch (StatusCode)
            {
                case HttpStatusCode.OK:
                    return "OK";
                case HttpStatusCode.RequestUriTooLong:
                    return "Request URI Too Long";
                case HttpStatusCode.HttpVersionNotSupported:
                    return "HTTP Version Not Supported";
            }

            // Build a description from the enum string representation where a space is added before each uppercase character
            // i.e.: "BadRequest" becomes "Bad Request"

            string statusCodeString = StatusCode.ToString();

            StringBuilder descriptionBuilder = new StringBuilder();
            descriptionBuilder.Append(statusCodeString[0]);

            for (int i = 1; i < statusCodeString.Length; i++)
            {
                char c = statusCodeString[i];

                if (char.IsUpper(c))
                    descriptionBuilder.Append(' ');

                descriptionBuilder.Append(c);
            }

            return descriptionBuilder.ToString();
        }

        public string ToString(bool verbose)
        {
            StringBuilder builder = new StringBuilder();

            // Append Status-Line
            builder.AppendFormat("{0} {1} {2}", HttpVersion, (int)StatusCode, GetStatusDescription());

            if (!verbose)
                return builder.ToString();

            // Append headers
            foreach (string headerName in Headers.Keys)
            {
                string[] headerValues = Headers.GetValues(headerName);
                string headerValuesString = headerValues != null
                    ? string.Join(";", headerValues)
                    : string.Empty;
                builder.AppendFormat("\r\n{0}: {1}", headerName, headerValuesString);
            }

            // Append body (assume ASCII)
            if (Content?.Length > 0)
            {
                builder.Append("\r\n\r\n");

                Content.Position = 0;
                using (StreamReader reader = new StreamReader(Content, Encoding.ASCII, false, 1024, true))
                    builder.Append(reader.ReadToEnd());
                Content.Position = 0;
            }

            return builder.ToString();
        }

        public override string ToString()
        {
            return ToString(true);
        }
    }
}