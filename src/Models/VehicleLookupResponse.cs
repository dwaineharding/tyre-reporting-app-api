namespace tyre_reporting_app_api.Models
{
    public class VehicleLookupResponse
    {
        public required CarInfo CarInfo { get; set; }
    }

    public record CarInfo(string ManufacturerBrandName, string AbiDescription, string PrimaryFuelTypeName, int RegistrationYear);

}
