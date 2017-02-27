using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using SecureWebServer.Core.Extensions;

namespace SecureWebServer.Core.Request
{
    public class RequestMessage
    {
        public string HttpMethod { get; private set; }
        public string Path { get; private set; }
        public string QueryString { get; private set; }
        public string HttpVersion { get; private set; }
        public NameValueCollection Headers { get; }
        public NameValueCollection FormData { get; }
        public Stream Content { get; private set; }

        private RequestMessage(string httpMethod, string path, string queryString, string httpVersion, NameValueCollection headers, NameValueCollection formData, Stream content)
        {
            HttpMethod = httpMethod;
            Path = path;
            QueryString = queryString;
            HttpVersion = httpVersion;
            Headers = headers;
            FormData = formData;
            Content = content;
        }

        public static RequestMessage FromStream(Stream inputStream)
        {
            NameValueCollection headers;
            NameValueCollection formData;
            string httpMethod;
            string path;
            string queryString;
            string httpVersion;
            Stream content = null;

            const int bufferSize = 1024;

            using (StreamReader reader = new StreamReader(inputStream, Encoding.ASCII, false, bufferSize, true))
            {
                // Read Request-Line

                string line = reader.ReadLine();

                // Chrome keeps a second connection opened for any future requests
                // When Chrome is closed, this results in an "empty" request which we should ignore
                if (string.IsNullOrEmpty(line))
                    return null;

                string[] requestLineParts = line.Split(new[] { ' ' }, 3);

                if (requestLineParts.Length != 3)
                    throw new RequestException(HttpStatusCode.BadRequest, "Request-Line malformed.");

                headers = new NameValueCollection();
                formData = new NameValueCollection();
                httpMethod = requestLineParts[0].ToUpperInvariant();
                httpVersion = requestLineParts[2];

                string[] requestUriParts = requestLineParts[1].Trim('/').Split('?');
                path = requestUriParts[0];
                queryString = requestUriParts.Length > 1 ? requestUriParts[1] : string.Empty;

                // Read headers

                while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    string[] headerParts = line.Split(new[] { ':' }, 2);

                    string headerName = headerParts[0].Trim();
                    string headerValue = headerParts.Length > 1 ? headerParts[1].Trim() : string.Empty;

                    headers.Add(headerName, headerValue);
                }
            }

            // Read body if the Content-Length header is present
            // TODO: what if there is a body but no Content-Length header?

            string contentLengthString = headers["Content-Length"];

            if (!string.IsNullOrEmpty(contentLengthString))
            {
                int contentLength;
                if (!int.TryParse(contentLengthString, out contentLength))
                    throw new RequestException(HttpStatusCode.BadRequest, "Invalid Content-Length header value.");

                // Copy the content to a new stream and reset the streams position
                content = new MemoryStream();
                inputStream.CopyTo(content, 0, contentLength);
                content.Position = 0L;

                // Read and parse formdata if the body contains formdata

                ParseFormData(headers, formData, content);
            }

            return new RequestMessage(httpMethod, path, queryString, httpVersion, headers, formData, content);
        }

        /// <summary>
        /// Checks if the request has a body containg form data, parses and places it into the form data collection.
        /// </summary>
        private static void ParseFormData(NameValueCollection headers, NameValueCollection formData, Stream content)
        {
            string contentType = headers["Content-Type"];

            if (!string.IsNullOrEmpty(contentType))
            {
                string[] contentTypeParts = contentType.Split(';');

                if (string.Equals(contentTypeParts[0], "application/x-www-form-urlencoded", StringComparison.InvariantCultureIgnoreCase)) // TODO: multipart/form-data support?
                {
                    // Parse charset if present or use default

                    string charsetPart = contentTypeParts.FirstOrDefault(ctp => ctp.StartsWith("charset", StringComparison.InvariantCultureIgnoreCase));
                    string charset = charsetPart != null && charsetPart.Contains('=')
                        ? charsetPart.Split(new[] { '=' }, 2).Last()
                        : null;

                    if (string.IsNullOrEmpty(charset))
                        charset = "ISO-8859-1";

                    Encoding encoding = Encoding.GetEncoding(charset);

                    // Convert the content stream to a string
                    byte[] contentBytes = new byte[content.Length];
                    content.Read(contentBytes, 0, contentBytes.Length);
                    content.Position = 0L;
                    string formDataString = encoding.GetString(contentBytes);

                    // Extract the form data fields and put them in the header collection
                    foreach (string formDataField in formDataString.Split('&').Where(i => !string.IsNullOrEmpty(i)))
                    {
                        string[] itemParts = formDataField.Split(new[] { '=' }, 2);

                        string itemName = HttpUtility.UrlDecode(itemParts[0].Trim());
                        string itemValue = itemParts.Length > 1 ? HttpUtility.UrlDecode(itemParts[1].Trim()) : string.Empty;

                        formData.Add(itemName, itemValue);
                    }
                }
            }
        }
    }
}