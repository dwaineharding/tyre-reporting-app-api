using tyre_reporting_app_api.Interfaces;
using tyre_reporting_app_api.Models;

namespace tyre_reporting_app_api.Services
{
    public class VehicleLookupService : IVehicleLookupService
    {
        private readonly HttpClient _httpClient;

        public VehicleLookupService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<VehicleDto> LookupByRegistration(string regNumber)
        {
            // TODO: If and when required, use the client to call an external API.

            var vehicleData = new VehicleLookupResponse
            {
                CarInfo = new CarInfo("Toyota", "Corolla", "Petrol", 2020)
            };

            if (vehicleData is null)
            {
                throw new Exception("Failed to retrieve vehicle data.");
            }

            return new VehicleDto
            (
                regNumber,
                vehicleData.CarInfo.ManufacturerBrandName,
                vehicleData.CarInfo.AbiDescription,
                vehicleData.CarInfo.RegistrationYear,
                "N/A",
                vehicleData.CarInfo.PrimaryFuelTypeName
            );
        }
    }
}
