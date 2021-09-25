using System.Net.Http;
using System.Threading.Tasks;

namespace WebApp.RESTClients
{
    public interface IMaintenanceManagementAPI
    {
        Task<HttpResponseMessage> SendMaintenanceJobScheduleDetailEmail(string emailAddress);
    }
}
