using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using MinDbg;
using MinDbg.CorDebug;
using MinDbg.SourceBinding;

namespace mindgbtest
{
    class Program
    {
        static void PrintUsage()
        {
            Console.WriteLine("Usage: mindbgtest.exe { -p pid | appname }");
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            if (String.Equals(args[0], "-p", StringComparison.Ordinal))
            {
                // attaching to the process
                if (args.Length != 2)
                {
                    PrintUsage();
                    return;
                }
                if (!Int32.TryParse(args[1], out var pid))
                {
                    PrintUsage();
                    return;
                }
                var debugger = DebuggingFacility.CreateDebuggerForProcess(pid);
                debugger.DebugActiveProcess(pid);
            }
            else
            {
                //Console.WriteLine("Calling DebuggingFacility.CreateDebuggerForExecutable()");
                var debugger = DebuggingFacility.CreateDebuggerForExecutable(args[0]);
                //Console.WriteLine("DebuggingFacility.CreateDebuggerForExecutable() completed");
                var process = debugger.CreateProcess(args[0]);
                //Console.WriteLine("debugger.CreateProcess() completed");

                process.OnBreakpoint += process_OnBreakpoint;
            }
            Console.ReadKey();
        }

        static Regex methodBreakpointRegex = new Regex(@"^((?<module>[\.\w\d]*)!)?(?<class>[\w\d\.]+)\.(?<method>[\w\d]+)$");
        static Regex codeBreakpointRegex = new Regex(@"^(?<filepath>[\\\.\S]+)\:(?<linenum>\d+)$");

        static void ProcessCommand(CorProcess process)
        {
            while (true)
            {
                Console.Write("> ");
                String command = Console.ReadLine();

                if (command.StartsWith("set-break", StringComparison.Ordinal))
                {
                    // setting breakpoint
                    command = command.Remove(0, "set-break".Length).Trim();

                    // try module!type.method location (simple regex used)
                    Match match = methodBreakpointRegex.Match(command);
                    if (match.Groups["method"].Length > 0)
                    {
                        Console.Write("Setting method breakpoint... ");

                        CorFunction func = process.ResolveFunctionName(match.Groups["module"].Value,
                                                                       match.Groups["class"].Value,
                                                                       match.Groups["method"].Value);
                        func.CreateBreakpoint().Activate(true);

                        Console.WriteLine("done.");
                    }
                    // try file code:line location
                    match = codeBreakpointRegex.Match(command);
                    if (match.Groups["filepath"].Length > 0)
                    {
                        Console.Write("Setting code breakpoint...");

                        CorCode code = process.ResolveCodeLocation(match.Groups["filepath"].Value,
                                                                   Int32.Parse(match.Groups["linenum"].Value),
                                                                   out var offset);
                        code.CreateBreakpoint(offset).Activate(true);

                        Console.WriteLine("done.");
                    }
                }
                else if (command.StartsWith("go", StringComparison.Ordinal))
                {
                    process.Continue(false);
                    break;
                }
            }
        }

        static void DisplayCurrentSourceCode(CorSourcePosition source)
        {
            SourceFileReader sourceReader = new SourceFileReader(source.Path);
            ConsoleColor oldcolor = Console.ForegroundColor;

            // Print three lines of code
            Debug.Assert(source.StartLine < sourceReader.LineCount && source.EndLine < sourceReader.LineCount);
            if (source.StartLine >= sourceReader.LineCount || 
                source.EndLine >= sourceReader.LineCount)
                return;

            var extraLines = 3;
            var startLine = Math.Max(source.StartLine - extraLines, 0);
            var endLine = Math.Min(source.EndLine + extraLines, sourceReader.LineCount);
            for (Int32 i = startLine; i <= endLine; i++)
            {
                String line = sourceReader[i];

                // for each line highlight the code
                for (Int32 col = 0; col < line.Length; col++)
                {
                    if ((i >= source.StartLine && i <= source.EndLine) && 
                        (source.EndColumn == 0 || col >= source.StartColumn - 1 && col <= source.EndColumn))
                    {
                        Console.ForegroundColor = ConsoleColor.Green; // Yellow;
                        Console.Write(line[col]);
                    }
                    else
                    {
                        // normal display
                        Console.ForegroundColor = oldcolor;
                        Console.Write(line[col]);
                    }
                }
                Console.ForegroundColor = oldcolor;
                Console.WriteLine();
            }
            Console.ForegroundColor = oldcolor;
            Console.WriteLine();
        }

        static void process_OnBreakpoint(CorBreakpointEventArgs ev)
        {
            Console.WriteLine("Breakpoint hit.");

            var source = ev.Thread.GetCurrentSourcePosition();

            DisplayCurrentSourceCode(source);

            ProcessCommand((ev.Controller is CorProcess process) ? process : ((CorAppDomain)ev.Controller).GetProcess());
        }
    }
}
