using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace WebApp.Infrastructure.Messaging
{
  public class MessagePublisher : IMessagePublisher
  {
    public void ConsumerTopicExchange(string routeKey, string messageType, object message)
    {
      IConnection connection = GetIConnection();
      IModel channel = connection.CreateModel();
      channel.ExchangeDeclare("SAGA-GMS-topic-exchange", ExchangeType.Topic);
      var queueName = channel.QueueDeclare().QueueName;
      channel.QueueBind(queue: queueName, exchange: "SAGA-GMS-topic-exchange", routingKey: routeKey);

      var consumer = new EventingBasicConsumer(channel);
      consumer.Received += (model, ea) =>
      {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var routingKey = ea.RoutingKey;
        string messageType = Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["MessageType"]);
        Console.WriteLine(" [x] Received '{0}':'{1}'|'{2}'",
                          routingKey, messageType,
                          ea.BasicProperties.CorrelationId?.Substring(0, 6));
      };
      channel.BasicConsume(queue: queueName,
                           autoAck: true,
                           consumer: consumer);
    }

    public IConnection GetIConnection()
    {
      return new ConnectionFactory
      {
        HostName = "localhost"
      }
      .CreateConnection();
    }

    private static IBasicProperties GetBasicProperties(string messageType, IModel channel)
    {
      IBasicProperties properties = channel.CreateBasicProperties();
      properties.Headers = new Dictionary<string, object>
            {
                {"MessageType", messageType}
            };
      return properties;
    }
  }
}