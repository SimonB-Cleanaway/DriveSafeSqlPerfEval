using System;

namespace ConsoleApp3
{
    public record VehicleValidationResult(string VehicleId, string RuleCode, NotLevel? Level, string Message, TimeSpan? RuleErrorRetention) : IVehicleValidationResult;
}
