using Akka.Actor;
using Akka.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AkkaSpike
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Begin ");

          

            using (var system = ActorSystem.Create("my-actor-server"))
            {
                bool keepLooping = true;
                while (keepLooping)
                {
                    string[] command =   Console.ReadLine().ToLower().Split(' ');



                    var orderActor = system.ActorOf(Props.Create(() => new OrderActor(command.Skip(1).FirstOrDefault()?? "0") ));

                    switch (command[0])
                    {
                        case "exit":
                            keepLooping = false;
                            break;
                        case "list":
                            orderActor.Tell(new GetStatus());
                            break;
                        case "place":
                            orderActor.Tell(new OrderPlaced());
                            break;
                        case "ship":
                            orderActor.Tell(new OrderShipped());
                            break;
                        default :
                            Console.WriteLine("try again");
                            break;
                    }

                }


            }
        }
    }


    public class OrderCoordinator : UntypedActor
 {
        protected override void OnReceive(object message)
        {
            if (message is int msg)
            {
                Console.WriteLine(msg);
            }
        }
    }

    public class OrderActor : ReceivePersistentActor
    {

        private List<String> EventLog = new List<string>();
        public OrderActor(string orderId) {
            OrderId = orderId;

            Command<OrderPlaced>(message =>
            {
                Persist(message, m => HandleOrderPlaced(m) );

            });

            Recover<OrderPlaced>(message =>  HandleOrderPlaced(message));

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
                Console.WriteLine("Listing events");
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
            Console.WriteLine("OrderPlaced");
            EventLog.Add("OrderPlaced");
        }

        //public override 



        public List<String> Events { get; set; }
        public string OrderId  { get; set; }
     
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

}
