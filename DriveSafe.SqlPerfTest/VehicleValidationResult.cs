using System;

namespace DriveSafe.SqlPerfTest
{
    public record VehicleValidationResult(string VehicleId, string RuleCode, NotLevel? Level, string Message, TimeSpan? RuleErrorRetention) : IVehicleValidationResult;
}
