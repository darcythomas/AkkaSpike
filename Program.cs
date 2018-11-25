using Akka.Actor;
using Akka.Configuration;
using Akka.Persistence;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Console = Colorful.Console;

namespace AkkaSpike
{
    public class Program
    {
        public static ActorSystem system;
        public static bool keepLooping = true;

        private static void Main(string[] args)
        {
            Console.WriteLine("Begin ");

            var config = ConfigurationFactory.ParseString(@"
 
      akka.persistence
      {
        journal
        {
          plugin = ""akka.persistence.journal.sql-server""
          sql-server
          {
              class = ""Akka.Persistence.SqlServer.Journal.SqlServerJournal, Akka.Persistence.SqlServer""
              schema-name = dbo
              auto-initialize = on
              connection-string = ""Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=AkkaSpike;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False""
          }
        }
      }");

            using (system = ActorSystem.Create("my-actor-server", config))
            {
                bool keepLooping = true;
                var orderCoordinator = system.ActorOf(Props.Create(() => new OrderCoordinator()));
                while (keepLooping)
                {
                    string command = Console.ReadLine().ToLower();

                    orderCoordinator.Tell(command);
                }
            }
        }
    }


    public class OrderCoordinator : UntypedActor
    {
        protected override void OnReceive(object message)
        {
            if (message is string msg)
            {
                string[] command = msg.ToLower().Split(' ');

                var orderId = command.Skip(1).FirstOrDefault() ?? "0";

                // var orderActor = Program.system.ActorOf(Props.Create(() => new OrderActor(name)));

                switch (command[0])
                {
                    case "exit":
                        Program.keepLooping = false;
                        break;
                    case "list":

                        var orderActor = Program.system.ActorOf(Props.Create(() => new OrderActor(orderId)));
                        orderActor.Tell(new GetStatus());
                        break;
                    case "place":

                        var orderIdActor = Program.system.ActorOf(Props.Create(() => new OrderIdActor()));
                        orderIdActor.Tell(new OrderPlaced());
                        break;

                    case "shipped":
                        var orderActorS = Program.system.ActorOf(Props.Create(() => new OrderActor(orderId)));
                        orderActorS.Tell(new OrderShipped());
                        break;

                    default:
                        Console.WriteLine("try again", Color.Red);
                        break;
                }
            }
        }
    }

    public class OrderIdActor : ReceivePersistentActor
    {
        public int Id { get; set; }

        public OrderIdActor()
        {
            Command<OrderPlaced>(message =>
            {
                Persist(message, m =>
                {
                    Id++;
                    var order = Program.system.ActorOf(Props.Create(() => new OrderActor($"{Id}")));
                    order.Tell(new OrderPlaced());
                });
            });

            Recover<OrderPlaced>(message => Id++);
        }

        public override string PersistenceId => nameof(OrderIdActor);
    }
    public class EmailActor : ReceivePersistentActor
    {
        private readonly string _orderId;

        public EmailActor(string orderId)
        {
            _orderId = orderId;

            Command<SendEmailConfirmation>(message => { Persist(message, m => { SendEmailConfirmation(m); }); });
            Recover<SendEmailConfirmation>(message => SendEmailConfirmation(message));

            Command<SendEmailItemShipped>(message => { Persist(message, m => { SendEmailItemShipped(m); }); });
            Recover<SendEmailItemShipped>(message => SendEmailItemShippedRestored(message));
        }

        private void SendEmailItemShipped(SendEmailItemShipped message)
        {
            Console.WriteLine("Item Shipped", Color.Aqua);
        }
        private void SendEmailItemShippedRestored(SendEmailItemShipped message)
        {
            Console.WriteLine("Item Shipped (Restore)", Color.Beige);
        }

        private void SendEmailConfirmation(SendEmailConfirmation m)
        {

            if (!IsRecovering)
            {
                Console.WriteLine("Item Shipped", Color.Aqua);
            }
            else
            {
                Console.WriteLine("Item Shipped (Restore)", Color.Beige);
            }
        }

        public override string PersistenceId => $"{nameof(EmailActor)}-{_orderId}";
    }
    public class PaymentActor : ReceivePersistentActor
    {
        private string _orderId;
        public PaymentActor(string orderId)
        {
            _orderId = orderId;

            Command<ProcessPayment>(message =>
            {
                Persist(message, m =>
                {
                    var order = Program.system.ActorOf(Props.Create(() => new OrderActor($"{_orderId}")));
                    order.Tell(new PaymentCompleted());

                    Console.WriteLine("Payment processed");
                });

            });
        }

        public override string PersistenceId => $"{nameof(PaymentActor)}-{_orderId}";
    }
    public class OrderActor : ReceivePersistentActor
    {

        private List<string> EventLog = new List<string>();

        public OrderActor(string orderId)
        {
            OrderId = orderId;

            Command<OrderPlaced>(message => { Persist(message, m => HandleOrderPlaced(m)); });
            Recover<OrderPlaced>(message => HandleOrderPlaced(message));

            Command<OrderShipped>(message => { Persist(message, m => { OrderShipped(m); }); });
            Recover<OrderShipped>(message => OrderShipped(message));

            Command<NotifyWarehouse>(message => { Persist(message, m => { NotifyWarehouse(m); }); });
            Recover<NotifyWarehouse>(message => NotifyWarehouse(message));

            Command<ProcessPayment>(message => { Persist(message, m => { ProcessPayment(m); }); });
            Recover<ProcessPayment>(message => ProcessPayment(message));

            Command<SendEmailConfirmation>(message => { Persist(message, m => { SendEmailConfirmation(m); }); });
            Recover<SendEmailConfirmation>(message => SendEmailConfirmation(message));

            Command<SendEmailItemShipped>(message => { Persist(message, m => { SendEmailItemShipped(m); }); });
            Recover<SendEmailItemShipped>(message => SendEmailItemShipped(message));

            Command<PaymentCompleted>(message => { Persist(message, m => { PaymentCompleted(m); }); });
            Recover<PaymentCompleted>(message => PaymentCompleted(message));


            Command<GetStatus>(message =>
            {
                Console.WriteAscii("Listing events", Color.Blue);
                foreach (var item in EventLog)
                {
                    Console.WriteLine(item, Color.BlueViolet);
                }

            });
        }

        private void PaymentCompleted(PaymentCompleted message)
        {
            var order = Program.system.ActorOf(Props.Create(() => new EmailActor($"{OrderId}")));
            order.Tell(new SendEmailConfirmation());
        }

        private void SendEmailItemShipped(SendEmailItemShipped message)
        {
            EventLog.Add("Order shipped, Email Sent");
        }

        private void SendEmailConfirmation(SendEmailConfirmation message)
        {
            EventLog.Add("Payment processed, Email Sent");
        }

        private void ProcessPayment(ProcessPayment m)
        {
            EventLog.Add("Payment processed");
        }

        private void NotifyWarehouse(NotifyWarehouse message)
        {
            EventLog.Add("Notify Warehouse");
        }

        private void OrderShipped(OrderShipped message)
        {
            EventLog.Add("Order Shipped");
            var order = Program.system.ActorOf(Props.Create(() => new EmailActor($"{OrderId}")));
            order.Tell(new SendEmailItemShipped());
        }

        private void HandleOrderPlaced(OrderPlaced message)
        {
            if (!IsRecovering)
            {
                var order = Program.system.ActorOf(Props.Create(() => new PaymentActor($"{OrderId}")));
                order.Tell(new ProcessPayment());
            }
            Console.WriteLine($"OrderPlaced {OrderId}");
            EventLog.Add("Order Placed");
        }


        public List<string> Events { get; set; }
        public string OrderId { get; set; }

        public override string PersistenceId => $"{nameof(OrderActor)}-{OrderId}";

    }


    public class GetStatus
    {

    }
    public class OrderPlaced
    {

    }
    public class OrderShipped
    {

    }
    public class NotifyWarehouse
    {

    }
    public class PaymentCompleted
    {

    }
    public class ProcessPayment
    {

    }
    public class SendEmailConfirmation
    {

    }
    public class SendEmailItemShipped
    {

    }

}
