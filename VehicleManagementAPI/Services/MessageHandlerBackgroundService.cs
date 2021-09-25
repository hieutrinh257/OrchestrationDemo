using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

using Serilog;

using VehicleManagementAPI.Common;
using VehicleManagementAPI.Infrastructure.Messaging;
using VehicleManagementAPI.Models;

using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace VehicleManagementAPI.Services
{
    /// <summary>
    /// Running Background service by using BackgroundService class
    /// </summary>
    public class MessageHandlerBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly ILogger<MessageHandlerBackgroundService> _logger;
        private readonly IVehicleService _vehicleService;
        private readonly IMessagePublisher _messagePublisher;

        public MessageHandlerBackgroundService(
            ILogger<MessageHandlerBackgroundService> logger,
            IVehicleService vehicleService,
            IMessagePublisher messagePublisher)
        {
            _logger = logger;
            _vehicleService = vehicleService;
            _messagePublisher = messagePublisher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            IConnection connection = _messagePublisher.GetIConnectionForDispatchConsumer();
            IModel channel = connection.CreateModel();

            channel.ExchangeDeclare("SAGA-GMS-topic-exchange", ExchangeType.Topic);
            channel.QueueDeclare(queue: "SAGA-GMS-VehicleManagementAPI-topic-queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            channel.QueueBind(exchange: "SAGA-GMS-topic-exchange", queue: "SAGA-GMS-VehicleManagementAPI-topic-queue", routingKey: TopicRouteKey.VehicleServices);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (bc, ea) =>
            {
                string message = Encoding.UTF8.GetString(ea.Body.ToArray());
                string messageType = Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["MessageType"]);
                _logger.LogInformation($"message received - messageType:{messageType} - messageBody {message}");
                try
                {
                    await HandleEvent(ea);
                    await Task.Delay(3 * 1000, stoppingToken);
                    //channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (JsonException)
                {
                    _logger.LogError($"JSON Parse Error: '{message}'.");
                    channel.BasicNack(ea.DeliveryTag, false, false);
                }
                catch (AlreadyClosedException)
                {
                    _logger.LogInformation("RabbitMQ is closed!");
                }
                catch (Exception e)
                {
                    _logger.LogError(default, e, e.Message);
                }
            };

            channel.BasicConsume(queue: "SAGA-GMS-VehicleManagementAPI-topic-queue", autoAck: true, consumer: consumer);

            await Task.CompletedTask;
        }

        private Task<bool> HandleEvent(BasicDeliverEventArgs ea)
        {
            string messageType = Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["MessageType"]);
            string body = Encoding.UTF8.GetString(ea.Body.ToArray());
            string correlationId = ea.BasicProperties.CorrelationId;
            var responseMessageType = EnumUtil.ParseEnum<ServiceResponseMessageType>(messageType);
            return HandleMessageAsync(correlationId, responseMessageType, body);
        }
        public async Task<bool> HandleMessageAsync(string correlationId, ServiceResponseMessageType messageType, string message)
        {
            try
            {
                switch (messageType)
                {
                    case ServiceResponseMessageType.RegisterVehicle:
                        await HandleAsync(correlationId, JsonSerializer.Deserialize<Vehicle>(message));
                        break;
                    case ServiceResponseMessageType.UndoRegisterVehicle:
                        await HandleAsync(correlationId, licenseNumber: message);
                        break;
                }
            }
            catch (Exception ex)
            {
                //string messageId = messageObject.Property("MessageId") != null ? messageObject.Property("MessageId").Value<string>() : "[unknown]";
                //Log.Error(ex, "Error while handling {MessageType} message with id {MessageId}.", messageType, messageId);
            }

            // always acknowledge message - any errors need to be dealt with locally.
            return true;
        }

        private async Task<bool> HandleAsync(string correlationId, Vehicle inputVehicle)
        {
            bool result = false;

            Log.Information($@"Register LicenseNumber: {inputVehicle.LicenseNumber}, {inputVehicle.OwnerId}, 
                        {inputVehicle.Brand}, GenerateDemoError - {inputVehicle.GenerateDemoError}");
            try
            {
                var vehicle = new Vehicle
                {
                    LicenseNumber = inputVehicle.LicenseNumber,
                    OwnerId = inputVehicle.OwnerId,
                    Brand = inputVehicle.Brand,
                    Type = inputVehicle.Type,
                    GenerateDemoError = inputVehicle.GenerateDemoError,
                };

                result = await _vehicleService.RegisterAsync(vehicle);
                _messagePublisher
                    .PublishToTopicExchange(
                        correlationId,
                        TopicRouteKey.OrchestrationEngine,
                        result
                            ? PublishExternalMessageType.RegisterVehicleSucceed
                            : PublishExternalMessageType.RegisterVehicleFailed,
                        vehicle.LicenseNumber);
            }
            catch (Exception)
            {
                Log.Warning($"Skipped adding Vehicle with LicenseNumber {inputVehicle.LicenseNumber}.");
            }
            return result;
        }

        private async Task<bool> HandleAsync(string correlationId, string licenseNumber)
        {
            bool result = false;
            licenseNumber = JsonConvert.DeserializeObject(licenseNumber)?.ToString();
            Log.Information($"UndoRegister Vehicle {licenseNumber} ");

            try
            {
                result = await _vehicleService.UndoRegisterAsync(licenseNumber);

                _messagePublisher
                    .PublishToTopicExchange(
                    correlationId,
                        TopicRouteKey.OrchestrationEngine,
                        result
                            ? PublishExternalMessageType.UndoRegisterVehicleSucceed
                            : PublishExternalMessageType.UndoRegisterVehicleFailed,
                        licenseNumber);
            }
            catch (Exception)
            {
                Log.Warning($"UndoRegisterAsync failed for {licenseNumber}");
            }
            return result;
        }

    }
}