using System;
using System.Diagnostics;
using SecureWebServer.Core.Error;
using SecureWebServer.Core.Request;
using SecureWebServer.Service.Config;

namespace SecureWebServer.Service
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerConfiguration config = ServerConfiguration.Get();

            IRequestHandler requestHandler = new RequestHandler();
            IErrorHandler errorHandler = new ErrorHandler();

            RequestListener requestListener = new RequestListener(requestHandler, errorHandler);
            requestListener.Start(config.WebPort);

            // Restart the listener on config change
            ServerConfiguration.SavedCallback += c =>
            {
                Console.WriteLine($"Config updated. Reinitializing for port {c.WebPort}.");
                requestListener.Restart(c.WebPort);
            };

            Console.WriteLine(
$@"Server started for port {config.WebPort}.
Type 'browse' to open the default page in your browser.
Type 'shutdown' to stop the server.");

            bool canRun = true;

            while (canRun)
            {
                Console.Write("> ");
                string input = Console.ReadLine() ?? string.Empty;

                switch (input.ToLowerInvariant())
                {
                    case "browse":
                        Console.WriteLine("Opening browser.");
                        Process.Start("http://localhost:" + config.WebPort);
                        break;
                    case "shutdown":
                        Console.WriteLine("Shutting down.");
                        canRun = false;
                        break;
                    default:
                        Console.WriteLine("Huh?");
                        break;
                }
            }

            requestListener.Stop();

            Environment.Exit(0);
        }
    }
}
