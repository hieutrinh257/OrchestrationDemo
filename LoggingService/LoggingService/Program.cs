using System;

using WebApp.Infrastructure.Messaging;

namespace LoggingService
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Logging Service Start");
      IMessagePublisher message = new MessagePublisher();

      message.ConsumerTopicExchange("#", "", "");
      Console.WriteLine("Listenting");
      Console.ReadLine();
    }
  }
}
