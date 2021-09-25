using HotelService.DataAccess;
using HotelService.Manager;
using HotelService.Models;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Text;

using WebApp.Infrastructure.Messaging;

namespace HotelService
{
  public static class ContainerBuilder
  {
    public static IServiceProvider Build()
    {
      var container = new ServiceCollection();

      container.AddSingleton<IMessagePublisher, MessagePublisher>();
      container.AddSingleton<IDataAccess<Hotel>, HotelInmemoryStorage>();
      container.AddSingleton<IFactory, HotelFactory>();

      return container.BuildServiceProvider();
    }
  }
}
