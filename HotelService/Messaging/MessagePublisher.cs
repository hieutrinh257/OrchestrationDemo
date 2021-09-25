using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

using HotelService.DataAccess;
using HotelService.Manager;
using HotelService.Models;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace WebApp.Infrastructure.Messaging
{
    public class MessagePublisher : IMessagePublisher
    {
        private readonly IDataAccess<Hotel> _dataAccess;

        public MessagePublisher(IDataAccess<Hotel> dataAccess)
        {
            _dataAccess = dataAccess;
        }

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
                string body = Encoding.UTF8.GetString(ea.Body.ToArray());
                string correlationId = ea.BasicProperties.CorrelationId;
                var hotel = JsonSerializer.Deserialize<HotelRequestModel>(body);
                try
                {
                    // Check Hotel availability
                    var result = _dataAccess.GetByName(hotel.Name).HasRoom;
                    // send success message
                    channel.BasicPublish(
                        exchange: "SAGA-GMS-topic-exchange", 
                        routingKey: "OrchestrationEngine.#", 
                        basicProperties: GetBasicProperties(correlationId, result ? "HotelAvaialble" : "HotelLocked", channel),
                        body: GetObjectBytes(hotel.Name));
                }
                catch (Exception e)
                {
                    channel.BasicPublish(
                        exchange: "SAGA-GMS-topic-exchange",
                        routingKey: "OrchestrationEngine.#",
                        basicProperties: GetBasicProperties(correlationId, "HotelLocked", channel),
                        body: GetObjectBytes(hotel.Name));
                }
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

        private static byte[] GetObjectBytes(object message)
        {
            string serializeObject = Newtonsoft.Json.JsonConvert.SerializeObject(message);
            byte[] body = Encoding.UTF8.GetBytes(serializeObject);
            return body;
        }

        private static IBasicProperties GetBasicProperties(string correlationId, string messageType, IModel channel)
        {
            IBasicProperties properties = channel.CreateBasicProperties();
            properties.CorrelationId = correlationId;
            properties.Headers = new Dictionary<string, object>
            {
                {"MessageType", messageType}
            };
            return properties;
        }
    }
}