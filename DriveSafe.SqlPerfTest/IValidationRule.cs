using System.Threading.Tasks;

namespace DriveSafe.SqlPerfTest
{
    public interface IValidationRule
    {
        Task<IVehicleValidationResult> Validate(IVehicleValidationRequest request);
    }
}
