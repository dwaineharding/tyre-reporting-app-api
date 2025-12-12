namespace tyre_reporting_app_api.Models
{
    public class JobReviewDto
    {
        public List<string>? JobDescriptions { get; set; }

        public List<TyreChangeViewDto>? TyreChanges { get; set; }
    }
}
