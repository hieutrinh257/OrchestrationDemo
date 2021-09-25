using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using System;
using System.Text;

using WebApp.Infrastructure.Messaging;

namespace HotelService
{
  class Program
  {
    public static readonly IServiceProvider _container = ContainerBuilder.Build();
    static void Main(string[] args)
    {
      var messageManager = _container.GetService<IMessagePublisher>();
      messageManager.ConsumerTopicExchange("Hotel.#", "", "");

      Console.WriteLine("Hotel service start");
      Console.ReadLine();
    }
  }
}
