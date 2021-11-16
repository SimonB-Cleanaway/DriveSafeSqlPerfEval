namespace DriveSafe.SqlPerfTest
{
    public record Vehicle(int VehicleId, string VehicleNo)
    {
        public BusinessUnit? BusinessUnit { get; set; } 
    }
}
