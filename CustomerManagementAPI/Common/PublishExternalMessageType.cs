namespace CustomerManagementAPI.Common
{
    public class PublishExternalMessageType
    {
        public const string RegisterCustomerSucceed = "RegisterCustomerSucceed";
        public const string RegisterCustomerFailed = "RegisterCustomerFailed";

        public const string UndoRegisterCustomerSucceed = "UndoRegisterCustomerSucceed";
        public const string UndoRegisterCustomerFailed = "UndoRegisterCustomerFailed";
    }

    public class TopicRouteKey
    {
        public const string OrchestrationEngine = "OrchestrationEngine.#";
        public const string CustomerServices = "Customer.#";
        public const string VehicleServices = "Vehicle.#";
        public const string WorkshopManagementServices = "Workshop.#";
    }
}