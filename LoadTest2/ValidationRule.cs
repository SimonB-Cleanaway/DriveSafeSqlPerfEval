using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConsoleApp3
{
    public class ValidationRule : IValidationRule
    {
        public ValidationRule()
        {

        }

        public Task<IVehicleValidationResult> Validate(IVehicleValidationRequest request)
        {
            return Task.FromResult<IVehicleValidationResult>(null);
        }



        // publ
    }
}
