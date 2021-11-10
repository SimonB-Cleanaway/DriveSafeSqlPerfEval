using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConsoleApp3
{
    public interface ICmd
    {
        Task Run(IReadOnlyList<string> args);
    }
}
