using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using AutoMapper;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

using Serilog;

using WebApp.Common;
using WebApp.Infrastructure.Messaging;
using WebApp.Models;
using WebApp.ViewModels;

using JsonException = System.Text.Json.JsonException;

namespace WebApp.Saga
{
    /// <summary>
    /// Running Background service by using BackgroundService class
    /// </summary>
    public class SagaOrchestratorBackgroundService : Microsoft.Extensions.Hosting.BackgroundService, ISagaOrchestratorBackgroundService
    {
        private readonly ILogger<SagaOrchestratorBackgroundService> _logger;
        private readonly IMessagePublisher _messagePublisher;
        private readonly ISagaMemoryStorage _sagaMemoryStorage;

        public SagaOrchestratorBackgroundService(
            ILogger<SagaOrchestratorBackgroundService> logger,
            IMessagePublisher messagePublisher,
            ISagaMemoryStorage memorySagaStorage)
        {
            _logger = logger;
            _messagePublisher = messagePublisher;
            _sagaMemoryStorage = memorySagaStorage;
        }


        public async Task StartProcessing(WorkShopManagementNewVM inputModel)
        {
            Guid jobId = Guid.NewGuid();

            //First add in memory for state tracking purpose
            var registerWorkingModel = new RegisterAndPlanJobSagaModel
            {
                CorrelationID = jobId.ToString(),
                HotelName = inputModel.Hotel.Name,
                EmailAddress = inputModel.Customer.EmailAddress,
                LicenseNumber = inputModel.Vehicle.LicenseNumber,
                JobId = jobId.ToString(),
                SagaStartTimeStamp = DateTime.Now,
            };

            _sagaMemoryStorage.Add(registerWorkingModel);

            //Here pushing message to all services
            var hotel = Mapper.Map<Hotel>(inputModel.Hotel);
            _messagePublisher.PublishToTopicExchange(
                registerWorkingModel.CorrelationID,
                TopicRouteKey.HotelServices,
                PublishExternalMessageType.CheckHotelAvailability,
                hotel);

            if (!await CheckHotelAvailability(inputModel.Hotel.Name))
                return;

            var customer = Mapper.Map<Customer>(inputModel.Customer);
            _messagePublisher.PublishToTopicExchange(
                registerWorkingModel.CorrelationID,
                TopicRouteKey.CustomerServices,
                PublishExternalMessageType.RegisterCustomer,
                customer);

            if (!await RegisterCustomerSuccess(inputModel.Customer.EmailAddress))
                return;

            var vehicle = Mapper.Map<Vehicle>(inputModel.Vehicle);
            vehicle.OwnerId = customer.EmailAddress;
            _messagePublisher.PublishToTopicExchange(
                registerWorkingModel.CorrelationID,
                TopicRouteKey.VehicleServices,
                PublishExternalMessageType.RegisterVehicle,
                vehicle);

            if (!await RegisterVehicleSuccess(inputModel.Vehicle.LicenseNumber))
                return;

            var maintenanceJob = Mapper.Map<MaintenanceJob>(inputModel.MaintenanceJob);
            maintenanceJob.JobId = jobId;
            maintenanceJob.OwnerId = customer.EmailAddress;
            maintenanceJob.LicenseNumber = vehicle.LicenseNumber;
            maintenanceJob.PlanningDate = DateTime.Now;
            _messagePublisher.PublishToTopicExchange(
                registerWorkingModel.CorrelationID,
                TopicRouteKey.WorkshopManagementServices,
                PublishExternalMessageType.PlanMaintenanceJob,
                maintenanceJob);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            IConnection connection = _messagePublisher.GetIConnectionForDispatchConsumer();
            IModel channel = connection.CreateModel();

            channel.ExchangeDeclare("SAGA-GMS-topic-exchange", ExchangeType.Topic);
            channel.QueueDeclare(queue: "SAGA-GMS-AllAPI-topic-queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            channel.QueueBind(exchange: "SAGA-GMS-topic-exchange", queue: "SAGA-GMS-AllAPI-topic-queue", routingKey: TopicRouteKey.OrchestrationEngine);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (bc, ea) =>
            {
                string message = Encoding.UTF8.GetString(ea.Body.ToArray());
                string messageType = Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["MessageType"]);


                _logger.LogInformation($"message received in webapp- messageType:{messageType} - messageBody {message}");

                try
                {
                    await HandleEvent(ea);
                    await Task.Delay(2 * 1000, stoppingToken);
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

            channel.BasicConsume(queue: "SAGA-GMS-AllAPI-topic-queue", autoAck: true, consumer: consumer);

            await Task.CompletedTask;
        }

        private Task HandleEvent(BasicDeliverEventArgs ea)
        {
            string body = Encoding.UTF8.GetString(ea.Body.ToArray());
            string messageType = Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["MessageType"]);
            string correlationId = ea.BasicProperties.CorrelationId;
            var responseMessageType = EnumUtil.ParseEnum<ServiceResponseMessageType>(messageType);

            switch (responseMessageType)
            {
                case ServiceResponseMessageType.UndoRegisterCustomerSucceed:
                case ServiceResponseMessageType.UndoRegisterVehicleSucceed:
                case ServiceResponseMessageType.UndoPlanMaintenanceJobSucceed:
                    return HandleMessageForCompensatingResult(responseMessageType, body);

                case ServiceResponseMessageType.UndoRegisterCustomerFailed:
                case ServiceResponseMessageType.UndoRegisterVehicleFailed:
                case ServiceResponseMessageType.UndoPlanMaintenanceJobFailed:
                    return HandleMessageForCompensatingResult(responseMessageType, body);
            }

            return HandleMessageAsync(correlationId, responseMessageType, body);
        }

        public async Task HandleMessageAsync(string correlationId, ServiceResponseMessageType messageType, string message)
        {
            try
            {
                RegisterAndPlanJobSagaModel sagaModel = _sagaMemoryStorage.Get()
                    .FirstOrDefault(r => r.CorrelationID == correlationId);

                Log.Information($"webapp - {messageType} for message: {message}");

                if (sagaModel == null) return;

                if (HandleCheckHotelResponse(messageType, sagaModel)) return;
                if (HandleRegisterCustomerResponse(messageType, sagaModel)) return;
                if (HandleRegisterVehicleResponse(messageType, sagaModel)) return;
                if (HandlePlanMaintenanceJobResponse(messageType, sagaModel)) return;
            }
            catch (Exception ex)
            {
                //string messageId = messageObject.Property("MessageId") != null ? messageObject.Property("MessageId").Value<string>() : "[unknown]";
                //Log.Error(ex, "Error while handling {MessageType} message with id {MessageId}.", messageType, messageId);
            }

            // always acknowledge message - any errors need to be dealt with locally.
        }

        private bool HandleCheckHotelResponse(ServiceResponseMessageType messageType, RegisterAndPlanJobSagaModel sagaModel)
        {
            switch (messageType)
            {
                case ServiceResponseMessageType.HotelAvaialble:
                    sagaModel.isHotelAvailable = true;
                    return true;
                case ServiceResponseMessageType.HotelLocked:
                    sagaModel.isHotelAvailable = false;
                    return true;
                default:
                    return false;
            }
        }

        private bool HandleRegisterCustomerResponse(ServiceResponseMessageType messageType, RegisterAndPlanJobSagaModel sagaModel)
        {
            switch (messageType)
            {
                case ServiceResponseMessageType.RegisterCustomerSucceed:
                    sagaModel.RegisterCustomerSucceed = true;
                    return true;
                case ServiceResponseMessageType.RegisterCustomerFailed:
                    sagaModel.RegisterCustomerSucceed = false;
                    return true;
                default:
                    return false;
            }
        }

        private bool HandleRegisterVehicleResponse(ServiceResponseMessageType messageType, RegisterAndPlanJobSagaModel sagaModel)
        {
            switch (messageType)
            {
                case ServiceResponseMessageType.RegisterVehicleSucceed:
                    sagaModel.RegisterVehicleSucceed = true;
                    return true;
                case ServiceResponseMessageType.RegisterVehicleFailed:
                    sagaModel.RegisterVehicleSucceed = false;
                    _messagePublisher.PublishToTopicExchange(
                        sagaModel.CorrelationID,
                        TopicRouteKey.CustomerServices,
                        PublishExternalMessageType.UndoRegisterCustomer,
                        sagaModel.EmailAddress);
                    return true;
                default:
                    return false;
            }
        }

        private bool HandlePlanMaintenanceJobResponse(ServiceResponseMessageType messageType, RegisterAndPlanJobSagaModel sagaModel)
        {
            switch (messageType)
            {
                case ServiceResponseMessageType.PlanMaintenanceJobSucceed:
                    sagaModel.PlanMaintenanceJobSucceed = true;
                    return true;
                case ServiceResponseMessageType.PlanMaintenanceJobFailed:
                    sagaModel.PlanMaintenanceJobSucceed = false;
                    _messagePublisher.PublishToTopicExchange(
                        sagaModel.CorrelationID,
                        TopicRouteKey.WorkshopManagementServices,
                        PublishExternalMessageType.UndoPlanMaintenanceJob, 
                        sagaModel.JobId);
                    _messagePublisher.PublishToTopicExchange(
                        sagaModel.CorrelationID,
                        TopicRouteKey.VehicleServices,
                        PublishExternalMessageType.UndoRegisterVehicle,
                        sagaModel.LicenseNumber);
                    _messagePublisher.PublishToTopicExchange(
                        sagaModel.CorrelationID,
                        TopicRouteKey.CustomerServices,
                        PublishExternalMessageType.UndoRegisterCustomer,
                        sagaModel.EmailAddress);
                    return true;
                default:
                    return false;
            }
        }

        public async Task HandleMessageForCompensatingResult(ServiceResponseMessageType messageType, string message)
        {
            //TODO:To be completed with retry approach
            message = JsonConvert.DeserializeObject(message)?.ToString();
            return;
        }

        private async Task<bool> CheckHotelAvailability(string hotelName)
        {
            RegisterAndPlanJobSagaModel sagaModel = _sagaMemoryStorage.GetByHotelName(hotelName);
            while (sagaModel.isHotelAvailable == null)
            {
                _logger.LogInformation($"......In loop {sagaModel.isHotelAvailable}");
                await Task.Delay(1 * 1000);
            }

            return sagaModel.isHotelAvailable != null && (bool)sagaModel.isHotelAvailable;
        }

        private async Task<bool> RegisterCustomerSuccess(string emailAddress)
        {
            RegisterAndPlanJobSagaModel sagaModel = _sagaMemoryStorage.GetByEmailAddress(emailAddress);
            while (sagaModel.RegisterCustomerSucceed == null)
            {
                _logger.LogInformation($"......In loop {sagaModel.RegisterCustomerSucceed}");
                await Task.Delay(1 * 1000);
            }

            return sagaModel.RegisterCustomerSucceed != null && (bool)sagaModel.RegisterCustomerSucceed;
        }

        private async Task<bool> RegisterVehicleSuccess(string licenseNumber)
        {
            RegisterAndPlanJobSagaModel sagaModel = _sagaMemoryStorage.GetByLicenseNumber(licenseNumber);
            while (sagaModel.RegisterVehicleSucceed == null)
            {
                _logger.LogInformation($"......In loop {sagaModel.RegisterVehicleSucceed}");
                await Task.Delay(1 * 1000);
            }

            return sagaModel.RegisterVehicleSucceed != null && (bool)sagaModel.RegisterVehicleSucceed;
        }

        public async Task<RegisterAndPlanJobSagaModel> GetDetailOnSagaComplete(string emailAddress)
        {
            RegisterAndPlanJobSagaModel sagaModel = _sagaMemoryStorage.GetByEmailAddress(emailAddress);

            while (!sagaModel.IsSagaCompleted)
            {
                _logger.LogInformation($"......In loop {sagaModel.IsSagaSuccessful}");
                await Task.Delay(1 * 1000);
            }
            sagaModel.SagaCompleteTimeStamp = DateTime.Now;
            RegisterAndPlanJobSagaModel result = sagaModel;

            //SAGA Completed hence removing in-memory object as it is of no use anymore
            _sagaMemoryStorage.Remove(sagaModel);

            return result;
        }

    }
}