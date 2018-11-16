using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace smtpsender
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 30000; i++)
            {
                if(i%1000==0)
                    Console.WriteLine(i);

                try
                {
                    SendMail();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1));
            }

            sw.Stop();

            Console.WriteLine("Time millis " +sw.ElapsedMilliseconds);

            Console.ReadKey();
        }

        private static void SendMail()
        {
            var EmailFrom = "yourgmailadress@gmail.com";
            var EmailTo = "destination@somedomain.com";
            var Subject = "The subject of your email";
            var Body = "What do you want your email to say";

            using(var client = new SmtpClient("localhost", 25))
            {
                client.EnableSsl = false;
                client.Credentials = new System.Net.NetworkCredential("usr", "pass");
                client.Send(EmailFrom, EmailTo, Subject, Body);
            }
        }
    }
}
