using System.Threading.Tasks;

namespace TGSentry.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new Startup().InitAndRun(args);
        }
    }
}