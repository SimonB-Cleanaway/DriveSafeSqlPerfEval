﻿using System;

namespace ConsoleApp3
{
    public record ValidationRequest(
        string Id, 
        string BusUnitCode, 
        string VehicleStatusCode, 
        DateTimeOffset Timestamp, 
        string Registration, 
        DateTime? RegistrationExpiry, 
        float Lat, 
        float Lng, 
        int Speed, 
        int Dir)
        : IVehicleValidationRequest;
}
