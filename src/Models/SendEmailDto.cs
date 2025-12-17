namespace tyre_reporting_app_api.Models
{
    public class SendEmailDto
    {
        public required string RegNumber { get; init; }
        public required DateTime Date { get; init; }
        public required string InvoiceName { get; init; }

    }
}