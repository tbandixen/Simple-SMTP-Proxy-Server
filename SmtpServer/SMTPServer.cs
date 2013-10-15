using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Reflection;
using SmtpServer.Helpers;

namespace SmtpServer
{
    public class SMTPServer
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        [STAThread]
        static void Main(string[] args)
        {
            SMTPServer server = new SMTPServer();
            server.RunServer();
        }

        /// <summary>
        /// Runs the server.
        /// </summary>
        public void RunServer()
        {
            MailListener listener = null;

            do
            {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                Version version = assembly.GetName().Version;
                Console.Title = assembly.GetName().Name + " v" + version.ToString(3);

                Console.WriteLine("New MailListener started");
                listener = new MailListener(this, IPAddress.Loopback, SettingsHelper.GetIntOrDefault("ServerPort", 25));
                listener.OutputToFile = true;
                listener.Start();
                while (listener.IsThreadAlive)
                {
                    Thread.Sleep(500);
                }
            } while (listener != null);

        }
    }

}
