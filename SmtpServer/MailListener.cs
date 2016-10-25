using OpenPop.Mime;
using SmtpServer.Helpers;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SmtpServer
{
    public class MailListener : TcpListener
    {
        private TcpClient client;
        private NetworkStream stream;
        private System.IO.StreamReader reader;
        private System.IO.StreamWriter writer;
        private Thread thread = null;
        private SMTPServer owner;
        const string SUBJECT = "Subject: ";
        const string FROM = "From: ";
        const string TO = "To: ";
        const string MIME_VERSION = "MIME-Version: ";
        const string DATE = "Date: ";
        const string CONTENT_TYPE = "Content-Type: ";
        const string CONTENT_TRANSFER_ENCODING = "Content-Transfer-Encoding: ";


        /// <summary>
        /// Initializes a new instance of the <see cref="MailListener"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="localaddr">The localaddr.</param>
        /// <param name="port">The port.</param>
        public MailListener(SMTPServer server, IPAddress localaddr, int port)
            : base(localaddr, port)
        {
            owner = server;
        }

        /// <summary>
        /// Starts listening for incoming connection requests.
        /// </summary>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Net.SocketPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   </PermissionSet>
        new public void Start()
        {
            base.Start();

            client = AcceptTcpClient();
            client.ReceiveTimeout = SettingsHelper.GetIntOrDefault("ReceiveTimeout", 5000);
            stream = client.GetStream();
            reader = new System.IO.StreamReader(stream);
            writer = new System.IO.StreamWriter(stream);
            writer.NewLine = "\r\n";
            writer.AutoFlush = true;

            thread = new Thread(new ThreadStart(RunThread));
            thread.Start();
        }

        public static byte[] ReadToEnd(System.IO.Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }

        /// <summary>
        /// Runs the thread.
        /// </summary>
        protected void RunThread()
        {
            string line = null;

            writer.WriteLine("220 localhost -- Fake proxy server");

            try
            {
                while (reader != null)
                {
                    line = reader.ReadLine();
                    Console.Error.WriteLine($"Read line {line}");

                    switch (line)
                    {
                        case "DATA":
                            writer.WriteLine("354 Start input, end data with <CRLF>.<CRLF>");
                            StringBuilder allLines = new StringBuilder();

                            line = reader.ReadLine();

                            while (line != null && line != ".")
                            {
                                allLines.AppendLine(line);
                                line = reader.ReadLine();
                            }

                            if (OutputToFile)
                            {
                                string fullBody = allLines.ToString();
                                ASCIIEncoding encoding = new ASCIIEncoding();
                                Byte[] fullBodyBytes = encoding.GetBytes(fullBody);
                                Message mm = new Message(fullBodyBytes);

                                var path = SettingsHelper.GetStringOrDefault("MailOutputPath");
                                var fileName = $"{DateTime.Now.ToString("yyyyMMdd-HHmmss")}-{mm.Headers.Subject}.eml";
                                foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                                {
                                    fileName = fileName.Replace(c, '_');
                                }

                                var fi = new FileInfo(Path.Combine(path, fileName));

                                mm.Save(fi);
                            }
                            writer.WriteLine("250 OK");
                            break;

                        case "QUIT":
                            writer.WriteLine("250 OK");
                            reader = null;
                            break;

                        default:
                            writer.WriteLine("250 OK");
                            break;
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine("Connection lost.");
                Console.WriteLine(ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                client.Close();
                Stop();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [output to file].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [output to file]; otherwise, <c>false</c>.
        /// </value>
        public bool OutputToFile { get; set; }

        /// <summary>
        /// Gets a value indicating whether [is thread alive].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [is thread alive]; otherwise, <c>false</c>.
        /// </value>
        public bool IsThreadAlive
        {
            get { return thread.IsAlive; }
        }
    }
}