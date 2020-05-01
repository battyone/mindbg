using System;

namespace TestApp_NetFramework
{
    class AnotherClassProgram { }
    class YetAnotherClassProgram { }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Debugging test application 1");

            Console.WriteLine("Running under .NET {0}", Environment.Version);

            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("We are being debugged");
            }
            else
            {
                Console.WriteLine("We are not being debugged");
            }

            try
            {
                throw new InvalidOperationException("Testing");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            System.Diagnostics.Debug.WriteLine("Hello");
            System.Diagnostics.Debugger.Break();
            Console.WriteLine("Running again (Press <Enter> to exit)");

            Console.ReadLine();
            Console.WriteLine("Exiting");
        }
    }
}
