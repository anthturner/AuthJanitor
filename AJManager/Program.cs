using AJManager.Verbs;
using CommandLine;
using System.Threading.Tasks;

namespace AJManager
{
    class Program
    {
        static void Main(string[] args) => MainAsync(args).Wait();
        static async Task MainAsync(string[] args)
        {
            await CommandLine.Parser.Default.ParseArguments<CreateVerb, ListVerb, ManageVerb, RotateVerb, UnmanageVerb>(args)
                .MapResult(
                    async (CreateVerb opts) => await opts.Execute(),
                    async (ListVerb opts) => await opts.Execute(),
                    async (ManageVerb opts) => await opts.Execute(),
                    async (RotateVerb opts) => await opts.Execute(),
                    async (UnmanageVerb opts) => await opts.Execute(),
                  errs => Task.FromResult(1));
        }
    }
}
