using System.Threading.Tasks;

namespace ConsoleApp3
{
    public interface IValidationRule
    {
        Task<IVehicleValidationResult> Validate(IVehicleValidationRequest request);
    }
}
