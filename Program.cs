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

                var name = $"{nameof(OrderActor)}-{command.Skip(1).FirstOrDefault() ?? "0"}";

                // var orderActor = Program.system.ActorOf(Props.Create(() => new OrderActor(name)));

                switch (command[0])
                {
                    case "exit":
                        Program.keepLooping = false;
                        break;
                    case "list":

                        var orderActor = Program.system.ActorOf(Props.Create(() => new OrderActor(name)));
                        orderActor.Tell(new GetStatus());
                        break;
                    case "place":

                        var orderIdActor = Program.system.ActorOf(Props.Create(() => new OrderIdActor()));
                        orderIdActor.Tell(new OrderPlaced());
                        break;
                    case "ship":

                        var orderActor2 = Program.system.ActorOf(Props.Create(() => new OrderActor(name)));
                        orderActor2.Tell(new OrderShipped());
                        break;
                    default:
                        Console.WriteLine("try again");
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
    public class OrderActor : ReceivePersistentActor
    {

        private List<string> EventLog = new List<string>();
        public OrderActor(string orderId)
        {
            OrderId = orderId;

            Command<OrderPlaced>(message =>
            {
                Persist(message, m => HandleOrderPlaced(m));

            });

            Recover<OrderPlaced>(message => HandleOrderPlaced(message));

            Command<OrderShipped>(message =>
            {
                Persist(message, m =>
                {
                    OrderShipped(m);
                });

            });


            Recover<OrderShipped>(message => OrderShipped(message));


            Command<GetStatus>(message =>
            {
                Console.WriteAscii("Listing events", Color.Blue);
                foreach (var item in EventLog)
                {
                    Console.WriteLine(item);
                }

            });




        }

        private void OrderShipped(OrderShipped message)
        {
            Console.WriteLine("OrderShipped");
            EventLog.Add("OrderShipped");
        }

        private void HandleOrderPlaced(OrderPlaced message)
        {
            if (!this.IsRecovering)
            {
                Console.WriteLine($"New OrderPlaced {OrderId}", Color.BlueViolet);

            }
            Console.WriteLine("OrderPlaced");
            EventLog.Add("OrderPlaced");
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
