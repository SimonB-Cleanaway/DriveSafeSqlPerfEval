using System;
using System.Threading.Tasks;

namespace DriveSafe.SqlPerfTest
{
    public abstract class ValidationRule : IValidationRule
    {
        public ValidationRule(string code, string name, NotLevel level)
        {
            Code = code;
            Name = name;
            Level = level;
        }

        public int Id { get; set; }
        public string Code { get; }
        public string Name { get; }
        public NotLevel Level { get; }

        public abstract Task<IVehicleValidationResult> Validate(IVehicleValidationRequest request);
    }

    public class ValidationRuleSim : ValidationRule, IValidationRule
    {
        private readonly Random _random;
        private readonly double _prob;

        public ValidationRuleSim(string code, string name, NotLevel level, Random random, double prob)
            : base(code, name, level)
        {
            _random = random;
            _prob = prob;
        }

        public override Task<IVehicleValidationResult> Validate(IVehicleValidationRequest request)
        {
            if (_random.NextDouble() >= _prob)
                return Task.FromResult<IVehicleValidationResult>(null);

            return Task.FromResult<IVehicleValidationResult>(new VehicleValidationResult(request.Id, Code, Level, "Rule " + Code, null));
        }
    }
}
