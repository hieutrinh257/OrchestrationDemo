namespace MaintenanceManagementAPI.Common
{
    public class PublishExternalMessageType
    {
        public const string PlanMaintenanceJobSucceed = "PlanMaintenanceJobSucceed";
        public const string PlanMaintenanceJobFailed = "PlanMaintenanceJobFailed";

        public const string UndoPlanMaintenanceJobSucceed = "UndoPlanMaintenanceJobSucceed";
        public const string UndoPlanMaintenanceJobFailed = "UndoPlanMaintenanceJobFailed";
    }

    public class TopicRouteKey
    {
        public const string OrchestrationEngine = "OrchestrationEngine.#";
        public const string CustomerServices = "Customer.#";
        public const string VehicleServices = "Vehicle.#";
        public const string MaintenanceManagementServices = "Maintenance.#";
    }
}