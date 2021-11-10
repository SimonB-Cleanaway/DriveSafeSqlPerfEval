using System;

namespace ConsoleApp3
{
    public record VehicleValidationResult : IVehicleValidationResult
    {
        public string VehicleId { get; init; }
        public string RuleCode { get; init; }
        public NotificationLevel? Level { get; init; }
        public string Message { get; init; }
        public TimeSpan? RuleErrorRetention { get; init; }
    }
}
