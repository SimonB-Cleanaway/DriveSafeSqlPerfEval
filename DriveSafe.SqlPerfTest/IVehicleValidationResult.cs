using System;

namespace DriveSafe.SqlPerfTest
{
    public interface IVehicleValidationResult
    {
        string VehicleId { get; }
        string RuleCode { get; }
        NotLevel? Level { get; }
        string Message { get; }
        TimeSpan? RuleErrorRetention { get; }
    }
}
