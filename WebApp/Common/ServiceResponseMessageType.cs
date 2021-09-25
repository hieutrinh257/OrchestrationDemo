namespace WebApp.Common
{
    public enum ServiceResponseMessageType
    {
        HotelAvaialble = 1,
        HotelLocked = 2,

        RegisterCustomerSucceed = 3,
        RegisterCustomerFailed = 4,
        
        RegisterVehicleSucceed = 5,
        RegisterVehicleFailed = 6,

        PlanMaintenanceJobSucceed = 7,
        PlanMaintenanceJobFailed = 8,
        
        UndoRegisterCustomerSucceed = 9,
        UndoRegisterCustomerFailed = 10,
        UndoRegisterVehicleFailed = 11,
        UndoRegisterVehicleSucceed = 12,
        UndoPlanMaintenanceJobFailed = 13,
        UndoPlanMaintenanceJobSucceed = 14,

    }
}