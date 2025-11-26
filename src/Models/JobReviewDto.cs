namespace tyre_reporting_app_api.Models
{
    public class TyreChangeViewDto
    {
        public required string TyrePosition { get; set; }

        public string? PreImageUrl { get; set; }

        public string? PostImageUrl { get; set; }

    }
}
