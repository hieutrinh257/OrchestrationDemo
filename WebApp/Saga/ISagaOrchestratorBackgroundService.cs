using System.Threading.Tasks;
using WebApp.ViewModels;

namespace WebApp.Saga
{
    public interface ISagaOrchestratorBackgroundService
    {
        Task StartProcessing(MaintenanceManagementNewVM inputModel);
        Task<RegisterAndPlanJobSagaModel> GetDetailOnSagaComplete(string emailAddress);
    }
}