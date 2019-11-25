using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace LaunchDarkly.Build.Helpers
{
    public static class DumpDllTypes
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("must be called with a single argument (DLL path)");
                Environment.ExitCode = 1;
                return;
            }
            var dllPath = args[0];
            Console.Error.WriteLine(dllPath);
            Type[] types;
            try
            {
                var assembly = Assembly.LoadFrom(dllPath);
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                Console.Error.WriteLine(e);
                foreach (var e1 in e.LoaderExceptions)
                {
                    Console.Error.WriteLine(e1);
                }
                types = e.Types;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                Environment.ExitCode = 1;
                return;
            }
            var typeNames = types.Where(t => t != null)
                .Select(t => t.FullName)
                .Where(name => !name.Contains("+")) // filters out auto-generated anonymous classes for lambdas etc.
                .ToList();
            typeNames.Sort();
            typeNames.ForEach(Console.Out.WriteLine);
        }
    }
}
