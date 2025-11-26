namespace tyre_reporting_app_api.Models
{
    public class SaveJobDto
    {
        public required string RegNumber { get; set; }
        public required List<TyreChangeDto> TyreChanges { get; set; }

        public class TyreChangeDto
        {
            public required string TyrePosition { get; set; }
            public required IFormFile PreImage { get; set; }
            public required IFormFile PostImage { get; set; }
        }
    }
}