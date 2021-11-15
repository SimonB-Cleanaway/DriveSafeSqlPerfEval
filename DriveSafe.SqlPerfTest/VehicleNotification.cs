using System;

namespace DriveSafe.SqlPerfTest
{
    public record VehicleNotification(int? Id, int VehicleId, int ValidationRuleId, int NotificationLevelId, string Message, DateTimeOffset CreateDate, DateTimeOffset? ExpiryDate, bool Active)
    {
        public Vehicle Vehicle { get; set; }
        public ValidationRule ValidationRule { get; set; }
        public NotLevel NotificationLevel { get; set; }
    }
}
