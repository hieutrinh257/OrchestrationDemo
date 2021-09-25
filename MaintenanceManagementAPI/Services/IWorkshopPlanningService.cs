using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MaintenanceManagementAPI.Models;

namespace MaintenanceManagementAPI.Services
{
    public interface IMaintenancePlanningService
    {
        Task<bool> RegisterAsync([FromBody] PlanMaintenanceJob planMaintenanceJob);
        Task<bool> UndoPlanMaintenanceJobAsync(Guid jobId);
        bool SendMaintenanceJobScheduleDetailEmail(string emailAddress);
    }
}