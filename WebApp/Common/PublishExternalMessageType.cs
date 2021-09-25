namespace WebApp.Common
{
    public class PublishExternalMessageType
    {
        public const string CheckHotelAvailability = "CheckHotelAvailability";
        public const string RegisterCustomer = "RegisterCustomer";
        public const string RegisterVehicle = "RegisterVehicle";
        public const string PlanMaintenanceJob = "PlanMaintenanceJob";

        public const string UndoRegisterCustomer = "UndoRegisterCustomer";
        public const string UndoRegisterVehicle = "UndoRegisterVehicle";
        public const string UndoPlanMaintenanceJob = "UndoPlanMaintenanceJob";

    }

    public class TopicRouteKey
    {
        public const string OrchestrationEngine = "OrchestrationEngine.#";
        public const string HotelServices = "Hotel.#";
        public const string CustomerServices = "Customer.#";
        public const string VehicleServices = "Vehicle.#";
        public const string MaintenanceManagementServices = "Maintenance.#";
    }
}