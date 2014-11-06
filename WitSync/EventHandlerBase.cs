using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace WitSync
{
    class EventHandlerBase : IEngineTracing
    {
        [Flags]
        protected enum OutputFlags
        {
            Debug = 1,
            Console = 2,
            All = 0xff
        }

        protected const ConsoleColor DebugColor = ConsoleColor.DarkGray;
        protected const ConsoleColor VerboseColor = ConsoleColor.DarkGray;
        protected const ConsoleColor InfoColor = ConsoleColor.Cyan;
        protected const ConsoleColor SuccessColor = ConsoleColor.Green;
        protected const ConsoleColor WarningColor = ConsoleColor.Yellow;
        protected const ConsoleColor ErrorColor = ConsoleColor.Red;

        private bool verbose;
        private string logFile;

        public EventHandlerBase(bool verbose, string logFile)
        {
            this.verbose = verbose;
            this.logFile = logFile;
        }

        protected void Verbose(string format, params object[] args)
        {
            OutputFlags outFlags = this.verbose ? OutputFlags.All : OutputFlags.Debug;
            Out(VerboseColor, outFlags, "VERBOSE: ", format, args);
        }

        protected void UniqueVerbose(string format, params object[] args)
        {
            OutputFlags outFlags = this.verbose ? OutputFlags.All : OutputFlags.Debug;
            UniqueOut(VerboseColor, outFlags, "VERBOSE: ", format, args);
        }

        protected void Success(string format, params object[] args)
        {
            Out(SuccessColor, OutputFlags.All, string.Empty, format, args);
        }

        protected void Info(string format, params object[] args)
        {
            Out(InfoColor, OutputFlags.All, string.Empty, format, args);
        }

        protected void Warning(string format, params object[] args)
        {
            Out(WarningColor, OutputFlags.All, "WARNING: ", format, args);
        }

        protected void Error(string format, params object[] args)
        {
            Out(ErrorColor, OutputFlags.All, "ERROR: ", format, args);
        }

        protected void UniqueWarning(string format, params object[] args)
        {
            UniqueOut(WarningColor, OutputFlags.All, "WARNING: ", format, args);
        }

        protected void UniqueError(string format, params object[] args)
        {
            UniqueOut(ErrorColor, OutputFlags.All, "ERROR: ", format, args);
        }

        private HashSet<int> outputted = new HashSet<int>();

        protected void UniqueOut(ConsoleColor color, OutputFlags outFlags, string prefix, string format, object[] args)
        {
            string message = string.Format(format, args: args);
            int key = message.GetHashCode();

            if (!outputted.Contains(key))
            {
                OutCore(color, outFlags, prefix, message);
                outputted.Add(key);
            }//if
        }

        protected void RawOut(ConsoleColor color, OutputFlags outFlags, string format, params object[] args)
        {
            string message = args != null ? string.Format(format, args: args) : format;

            OutCore(color, outFlags, string.Empty, message);
        }

        protected void Out(ConsoleColor color, OutputFlags outFlags, string prefix, string format, object[] args)
        {
            string message = args != null ? string.Format(format, args: args) : format;

            OutCore(color, outFlags, prefix, message);
        }

        protected void OutCore(ConsoleColor color, OutputFlags outFlags, string prefix, string message)
        {
            if ((outFlags & OutputFlags.Debug) == OutputFlags.Debug)
            {
                System.Diagnostics.Debug.Write(prefix);
                System.Diagnostics.Debug.WriteLine(message);
            }

            if ((outFlags & OutputFlags.Console) == OutputFlags.Console)
            {
                var save = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.Write(prefix);
                Console.WriteLine(message);
                Console.ForegroundColor = save;
            }

            if (!string.IsNullOrWhiteSpace(this.logFile))
            {
                using (var file = System.IO.File.AppendText(this.logFile))
                {
                    file.Write(prefix);
                    file.WriteLine(message);
                }//using
            }
        }

        public void Trace(string format, params object[] args)
        {
            Out(DebugColor, OutputFlags.All, "DEBUG: ", format, args);
        }
    }
}
