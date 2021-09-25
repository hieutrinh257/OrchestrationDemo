using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebApp.RESTClients
{
    public class MaintenanceManagementAPI : RESTClientsBase, IMaintenanceManagementAPI
    {
        private readonly ILogger<MaintenanceManagementAPI> _logger;

        public MaintenanceManagementAPI(IConfiguration config, ILogger<MaintenanceManagementAPI> logger) 
            : base(config, "MaintenanceManagementAPI")
        {
            _logger = logger;
        }

        public async Task<HttpResponseMessage> SendMaintenanceJobScheduleDetailEmail(string emailAddress)
        {
            HttpResponseMessage httpResponse= await Get("MaintenancePlanning/SendMaintenanceJobScheduleDetailEmail/", emailAddress);
            _logger.LogInformation($"Maintenance Job Schedule Detail Email sent to {emailAddress}");

            return httpResponse;
        }
    }
}
