using tyre_reporting_app_api.Models;

namespace tyre_reporting_app_api.Interfaces
{
    public interface IStorageInteractor
    {
        Task<bool> CreateContainer(string regNumber, DateTime date);

        Task<bool> InsertJob(string regNumber, DateTime date, SaveJobDto saveJobDto);

        Task<Dictionary<string, List<DateTime>>> ListJobs();

        Task<List<TyreChangeViewDto>> GetJobDetails(string regNumber, DateTime date);
    }
}
