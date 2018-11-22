using Akka.Actor;
using Akka.Persistence;
using System;
using System.Collections.Generic;

namespace AkkaSpike
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

          

            using (var system = ActorSystem.Create("my-actor-server"))
            {
                //start two services
                var service1 = system.ActorOf<PlaceOrder>($"{nameof(PlaceOrder)}");
              //  var service2 = system.ActorOf<Service2>("service2");
                Console.ReadKey();
            }
        }
    }


    public class PlaceOrder : ReceivePersistentActor
    {
        public PlaceOrder() {}
        public PlaceOrder(int orderId) { OrderId = orderId;

            Receive(message => {

                Console.WriteLine("here");

            });
        }


        public List<String> Events { get; set; }
        public int OrderId  { get; set; }
     
        public override string PersistenceId => $"{nameof(PlaceOrder)}-{OrderId}";


        protected override  
        
    }
    public class PlaceOrderItem
    {

    }
    
}
