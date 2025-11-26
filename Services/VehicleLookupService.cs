using tyre_reporting_app_api.Interfaces;
using tyre_reporting_app_api.Models;

namespace tyre_reporting_app_api.Services
{
    public class VehicleLookupService : IVehicleLookupService
    {
        public VehicleDto LookupByRegistration(string regNumber)
        {
            return new VehicleDto(regNumber, "Toyota", "Corolla", 2020, "White", "Diesel");
        }
    }
}
