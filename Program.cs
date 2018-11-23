using Akka.Actor;
using Akka.Persistence;
using System;
using System.Collections.Generic;
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
                //start two services
                var service1 = system.ActorOf<PlaceOrder>($"{nameof(PlaceOrder)}");
                //  var service2 = system.ActorOf<Service2>("service2");
           
                var basicActorX = system.ActorOf<PlaceOrder>();
                basicActorX.Tell(new PlaceOrderItem());

                var basicActor = system.ActorOf<BasicActor>();
                basicActor.Tell("Hello World!");

                Thread.Sleep(1000);
                Console.ReadKey();
            }
        }
    }

    public class BasicActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
            if (message is int  msg)
            {                
                Console.WriteLine(msg);
            }
        }
    }

    public class PlaceOrder : ReceivePersistentActor
    {
        public PlaceOrder() {

            Command<PlaceOrderItem>(message =>
            {

                Console.WriteLine("here");

               
            });
        }

        //public override 

       

        public List<String> Events { get; set; }
        public int OrderId  { get; set; }
     
        public override string PersistenceId => $"{nameof(PlaceOrder)}-{OrderId}";


       
        
    }
    public class PlaceOrderItem
    {

    }
    
}
