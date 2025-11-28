using tyre_reporting_app_api.Models;

namespace tyre_reporting_app_api.Interfaces
{
    public interface IVehicleLookupService
    {
        Task<VehicleDto> LookupByRegistration(string regNumber);
    }
}
