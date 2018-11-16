using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MimeKit;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using SmtpServer.Authentication;

namespace SmtpServer
{
    public class OutstandingWork
    {
        private List<MimeMessage> MessagesToSend = new List<MimeMessage>();

        object lockObj = new object();

        public void Add(MimeMessage m)
        {
            lock (lockObj)
            {
                MessagesToSend.Add(m);
            }
        }

        public List<MimeMessage> Get()
        {
            lock (lockObj)
            {
                var result = MessagesToSend;
                MessagesToSend= new List<MimeMessage>();
                return result;
            }
        }
    }


    class Sender
    {
        private readonly OutstandingWork _work;

        public Sender(OutstandingWork work)
        {
            _work = work;
        }

        public void SendAndWait()
        {
            while (true)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                var elements = _work.Get();
                Console.WriteLine("\nGetting " + elements.Count);
                var results = elements
                    .AsParallel()
                    .Select(RestSend)
                    .ToArray();
            }
        }

        public bool RestSend(MimeMessage message)
        {
            //Console.Write("*");
            Console.WriteLine("************* you got mail *************");
            Console.WriteLine("from: " + message.From);
            Console.WriteLine("to: " + message.To);
            Console.WriteLine(message.TextBody);

            return true;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello SMTP World !");

            OutstandingWork buffer = new OutstandingWork();

            var options = new SmtpServerOptionsBuilder()
                .CommandWaitTimeout(TimeSpan.FromSeconds(1))
                .ServerName("localhost")
                .Port(25, 587)
                .MessageStore(new SampleMessageStore(buffer))
                .UserAuthenticator(new SampleUserAuthenticator())
                .Build();

            var smtpServer = new SmtpServer(options);

            TaskFactory factory = new TaskFactory();
            factory.StartNew(() => { new Sender(buffer).SendAndWait(); });

            smtpServer.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
    }

    public class SampleMessageStore : MessageStore
    {
        private readonly OutstandingWork _buffer;

        public SampleMessageStore(OutstandingWork buffer)
        {
            _buffer = buffer;
        }

        public override Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken)
        {
            var textMessage = (ITextMessage)transaction.Message;
            MimeMessage message = MimeKit.MimeMessage.Load(textMessage.Content);
            _buffer.Add(message);
            return Task.FromResult(SmtpResponse.Ok);
        }
    }

    public class SampleUserAuthenticator : IUserAuthenticator, IUserAuthenticatorFactory
    {
        public Task<bool> AuthenticateAsync(ISessionContext context, string user, string password, CancellationToken token)
        {
            Console.WriteLine("User={0} Password={1}", user, password);

            return Task.FromResult(user.Length > 4);
        }

        public IUserAuthenticator CreateInstance(ISessionContext context)
        {
            return new SampleUserAuthenticator();
        }
    }
}
