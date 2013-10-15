using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Reflection;

namespace SmtpServer
{
    public class SMTPServer
    {
        [STAThread]
        static void Main(string[] args)
        {
            SMTPServer server = new SMTPServer();
            server.RunServer();
        }

        public void RunServer()
        {
            MailListener listener = null;

            do
            {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                Version version = assembly.GetName().Version;
                Console.Title = assembly.GetName() + " v" + version.ToString(3);

                Console.WriteLine("New MailListener started");
                listener = new MailListener(this, IPAddress.Loopback, 25);
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
