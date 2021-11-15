using System;

namespace DriveSafe.SqlPerfTest
{
    public interface IVehicleValidationRequest
    {
        string Id { get; }
        string VehicleStatusCode { get; }
        string Registration { get; }
        DateTime? RegistrationExpiry { get; }
    }
}
