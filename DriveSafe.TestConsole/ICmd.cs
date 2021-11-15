using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DriveSafe.TestConsole
{
    public interface ICmd
    {
        Task Run(IReadOnlyList<string> args);
    }
}
