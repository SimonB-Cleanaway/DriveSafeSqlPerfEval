using System;

namespace ConsoleApp3
{
    public interface IVehicleValidationRequest
    {
        string Id { get; }
        string VehicleStatusCode { get; }
        string Registration { get; }
        DateTime? RegistrationExpiry { get; }
    }
}
