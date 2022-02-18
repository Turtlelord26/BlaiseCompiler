using System;
using System.IO;
using System.Runtime.Loader;
using Blaise2.Internal;
using Mobius.ILasm.Core;

//
// This file includes the more technical part of the CalcC compiler.
// At this time, you needn't worry about what's going on in here,
// though I've commented it in case you're curious.
//

namespace Blaise2
{
    public sealed partial class Compiler : IDisposable
    {
        // Synchronization object.  Fun fact: all .NET objects
        // can act as monitor mutexes!
        private static readonly object monitor = new();

        // A MemoryStream (like an in-memory file) to hold the
        // compiled bytes after the CIL is assembled.
        public MemoryStream ObjectCodeStream { get; private set; }

        // Does the work of assembling the CIL we generated
        // into the actual bytes of a .NET assembly.
        public void AssembleToObjectCode()
        {
            lock (monitor)
            {
                var logger = new MobiusLogger();
                var driver = new Driver(logger, Driver.Target.Dll, showParser: false, debuggingInfo: false, showTokens: false);

                ObjectCodeStream = new MemoryStream();
                driver.Assemble(new[] { Cil }, ObjectCodeStream);
                ObjectCodeStream.Seek(0, SeekOrigin.Begin);
            }
        }

        // Not used right now, but if you want to actually write
        // out your program to disk, call
        //  `compiler.WriteDll("/tmp", "MyProgram")`
        // This will write MyProgram.dll to the /tmp directory,
        // as well as a runtime config file required to run
        // crossplatform .NET programs (which this is).  After this,
        // you could run `dotnet /tmp/MyProgram.dll`.
        public void WriteDll(string destinationDirectory, string moduleName)
        {
            var dllFilename = Path.Combine(destinationDirectory, $"{moduleName}.dll");
            var runtimeFilename = Path.Combine(destinationDirectory, $"{moduleName}.runtimeconfig.json");
            using var file = new FileStream(dllFilename, FileMode.Create, FileAccess.Write);
            ObjectCodeStream.WriteTo(file);
            File.WriteAllText(runtimeFilename, @"{
  ""runtimeOptions"": {
    ""tfm"": ""net5.0"",
    ""framework"": {
      ""name"": ""Microsoft.NETCore.App"",
      ""version"": ""5.0.0""
    }
  }
}");
        }

        // A utility function that runs the assembled object code
        // on the fly and captures the output to a string.  This is
        // pretty neat code here... it really shows off the dynamic
        // underpinnings of .NET.
        public string ExecuteObjectCode()
        {
            lock (monitor)
            {
                if (ObjectCodeStream is null)
                {
                    throw new InvalidOperationException("Must call CompileToObjectCode() first");
                }

                // Set up an execution context, load our assembled object code,
                // and find the entrypoint (Main).
                var assemblyContext = new AssemblyLoadContext(null);
                using (assemblyContext.EnterContextualReflection())
                {
                    var assembly = assemblyContext.LoadFromStream(ObjectCodeStream);
                    var entryPoint = assembly.EntryPoint;

                    // Capture the current STDOUT stream, then redirect STDOUT to a
                    // memory stream so we can capture the output of our object code.
                    var oldStdout = Console.Out;
                    using var memStream = new MemoryStream();
                    using var memWriter = new StreamWriter(memStream) { AutoFlush = true };
                    Console.SetOut(memWriter);

                    // Execute the compiled program.
                    entryPoint?.Invoke(null, Array.Empty<object>());

                    // Set STDOUT back to normal.
                    Console.SetOut(oldStdout);

                    // Rewind the memory stream to the beginning, then read what's in it.
                    memStream.Seek(0, SeekOrigin.Begin);
                    using var memReader = new StreamReader(memStream);
                    var result = memReader.ReadLine();

                    return result;
                }
            }
        }

        // MemoryStream is an IDisposable, so we need to make sure it gets
        // disposed of properly
        public void Dispose()
        {
            ObjectCodeStream?.Dispose();
        }
    }
}