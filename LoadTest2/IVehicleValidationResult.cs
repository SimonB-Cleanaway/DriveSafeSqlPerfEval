using System;

namespace ConsoleApp3
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
