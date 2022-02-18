using System;
using Mobius.ILasm.interfaces;
using Mono.ILASM;

namespace Blaise2.Internal
{
    internal class MobiusLogger : ILogger
    {
        public void Error(string message)
        {
            Console.Error.WriteLine($"* ERR {message}");
        }

        public void Error(Location location, string message)
        {
            Console.Error.WriteLine($"* ERR {message} :{location.line}:{location.column}");
        }

        public void Info(string message)
        {
            // Only show errors and warnings.
            // Console.Error.WriteLine($"* INF {message}");
        }

        public void Warning(string message)
        {
            Console.Error.WriteLine($"* WRN {message}");
        }

        public void Warning(Location location, string message)
        {
            Console.Error.WriteLine($"* WRN {message} :{location.line}:{location.column}");
        }
    }
}