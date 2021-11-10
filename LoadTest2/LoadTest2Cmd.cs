using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConsoleApp3
{
    public class LoadTest2Cmd : ICmd
    {



        public Task Run(IReadOnlyList<string> args)
        {
            return Task.CompletedTask;
        }
    }
}
