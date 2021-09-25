namespace VehicleManagementAPI.Common
{
    public class PublishExternalMessageType
    {
        public const string RegisterVehicleSucceed = "RegisterVehicleSucceed";
        public const string RegisterVehicleFailed = "RegisterVehicleFailed";

        public const string UndoRegisterVehicleSucceed = "UndoRegisterVehicleSucceed";
        public const string UndoRegisterVehicleFailed = "UndoRegisterVehicleFailed";
    }

    public class TopicRouteKey
    {
        public const string OrchestrationEngine = "OrchestrationEngine.#";
        public const string CustomerServices = "Customer.#";
        public const string VehicleServices = "Vehicle.#";
        public const string MaintenanceManagementServices = "Maintenance.#";
    }
}