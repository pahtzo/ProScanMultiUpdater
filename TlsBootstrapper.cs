/*

MIT License

Copyright (c) 2026 Nick DeBaggis

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

 */

// ============================================================================
// File: TlsBootstrapper.cs
// ============================================================================


using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;

namespace ProScanMultiUpdater
{
    internal class TlsBootstrapper
    {
        /// <summary>
        /// Initializes TLS policy.
        /// Returns a status string suitable for UI or logging.
        /// Call once at application startup.
        /// </summary>
        public static string Initialize(string probeHost, int port = 443)
        {
            string host = NormalizeHost(probeHost);

            try
            {
                var negotiated = ProbeNegotiatedTls(host, port);

                if (IsAcceptable(negotiated))
                {
                    ServicePointManager.SecurityProtocol =
                        SecurityProtocolType.SystemDefault;

                    return
                        $"TLS bootstrap OK.\r\n" +
                        $"Probe host: {host}:{port}\r\n" +
                        $"Negotiated protocol: {FormatProtocol(negotiated)}\r\n" +
                        $"SecurityProtocol: SystemDefault (OS-managed)";
                }

                ForceTls12();

                return
                    $"TLS bootstrap WARNING.\r\n" +
                    $"Probe host: {host}:{port}\r\n" +
                    $"Negotiated protocol: {FormatProtocol(negotiated)}\r\n" +
                    $"Reason: Protocol below TLS 1.2\r\n" +
                    $"Action: Forced SecurityProtocol = TLS 1.2";
            }
            catch (Exception ex)
            {
                ForceTls12();

                return
                    $"TLS bootstrap ERROR.\r\n" +
                    $"Probe host: {host}:{port}\r\n" +
                    $"Reason: {ex.GetType().Name}: {ex.Message}\r\n" +
                    $"Likely cause: TLS 1.2 disabled or missing Windows 7 updates\r\n" +
                    $"Action: Forced SecurityProtocol = TLS 1.2 (may still fail until OS is fixed or upgraded to a supported version)";
            }
        }
        
        private static string NormalizeHost(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Probe host is empty.");

            // If it's already a hostname, return it
            if (!input.Contains("://"))
                return input;

            // Parse URL and extract hostname
            var uri = new Uri(input, UriKind.Absolute);
            return uri.Host;
        }

        private static bool IsAcceptable(SslProtocols protocol)
        {
            // TLS 1.2 or newer (newer may be reported as Tls12)
            return protocol != SslProtocols.Tls &&
                   protocol != SslProtocols.Tls11;
        }

        private static void ForceTls12()
        {
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12;
        }

        private static SslProtocols ProbeNegotiatedTls(string host, int port)
        {
            using (var client = new TcpClient())
            {
                client.Connect(host, port);

                using (var ssl = new SslStream(
                    client.GetStream(),
                    false,
                    (sender, cert, chain, errors) => true))
                {
                    ssl.AuthenticateAsClient(host);
                    return ssl.SslProtocol;
                }
            }
        }

        private static string FormatProtocol(SslProtocols protocol)
        {
            switch (protocol)
            {
                case SslProtocols.Tls:
                    return "TLS 1.0";
                case SslProtocols.Tls11:
                    return "TLS 1.1";
                case SslProtocols.Tls12:
                    return "TLS 1.2 (or newer via OS)";
                default:
                    return protocol.ToString();
            }
        }
    }
}
