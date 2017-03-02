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
        private Stream _content;

        public string HttpVersion { get; }
        public HttpStatusCode StatusCode { get; }
        public NameValueCollection Headers { get; }
        
        public ResponseMessage(HttpStatusCode statusCode)
        {
            HttpVersion = "HTTP/1.0";
            StatusCode = statusCode;
            Headers = new NameValueCollection();
        }

        #region Content methods

        public void SetContentStream(Stream contentStream, string contentType)
        {
            _content?.Dispose();
            _content = contentStream;

            if (!string.IsNullOrEmpty(contentType))
                Headers["Content-Type"] = contentType;
        }

        public void SetByteContent(byte[] content, string contentType)
        {
            MemoryStream contentStream = new MemoryStream();

            if (content != null && content.Length > 0)
                contentStream.Write(content, 0, content.Length);

            SetContentStream(contentStream, contentType);
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
                if (_content?.Length > 0)
                    writer.WriteLine("Content-Length: {0}", _content.Length);

                writer.WriteLine("Connection: close");

                // Write an empty line
                writer.WriteLine();

                writer.Flush();
            }

            // Finally, write the body
            if (_content?.Length > 0)
            {
                _content.Position = 0;
                _content.CopyTo(outputStream, 0, _content.Length);
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

            return builder.ToString();
        }

        public override string ToString()
        {
            return ToString(true);
        }
    }
}