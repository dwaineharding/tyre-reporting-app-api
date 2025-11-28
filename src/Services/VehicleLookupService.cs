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
            _httpClient.DefaultRequestHeaders.Add("ocp-apim-subscription-key", "79a1bd3b76164ef4b2301de0c1b2e5bb");
            
            var response = await _httpClient.GetAsync($"https://api.confused.com/engagementtools/v1/vehicle-lookup/registration?registrationNumber={regNumber}");
            response.EnsureSuccessStatusCode();
            
            var vehicleData = await response.Content.ReadFromJsonAsync<VehicleLookupResponse>();

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
