using tyre_reporting_app_api.Models;

namespace tyre_reporting_app_api.Interfaces
{
    public interface IStorageInteractor
    {
        Task<bool> CreateContainer(string regNumber, string user, DateTime date);

        Task<bool> InsertJob(string regNumber, DateTime date, SaveJobDto saveJobDto);

        Task<Dictionary<string, List<DateTime>>> ListJobs();

        Task<JobReviewDto> GetJobDetails(string regNumber, DateTime date);
    }
}
