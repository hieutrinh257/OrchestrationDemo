using RabbitMQ.Client;

namespace WebApp.Infrastructure.Messaging
{
    public interface IMessagePublisher
    {
        void ConsumerTopicExchange(string routeKey, string messageType, object message);

        IConnection GetIConnection();
    }
}