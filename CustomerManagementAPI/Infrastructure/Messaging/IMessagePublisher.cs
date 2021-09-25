using RabbitMQ.Client;

namespace CustomerManagementAPI.Infrastructure.Messaging
{
    public interface IMessagePublisher
    {
        /// <summary>
        /// Most recommended approach
        /// Publishing to topic exchange is most easy pizzy approach
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        void PublishToTopicExchange(string correlationId, string routeKey, string messageType, object message);
        void PublishToFanoutExchange(string correlationId, string exchangeName, string messageType, object message);

        IConnection GetIConnection();
        IConnection GetIConnectionForDispatchConsumer();
    }
}