namespace tyre_reporting_app_api.Models
{
    public class StoreInvoiceDto
    {
        public required string RegNumber { get; set; }
        public required DateTime Date { get; set; }
        public required IFormFile File { get; set; }
        public required string InvoiceNumber { get; set; }
    }
}
