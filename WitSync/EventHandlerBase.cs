using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace WitSync
{
    class EventHandlerBase : IEngineTracing
    {
        public enum TraceLevel
        {
            Normal = 10,
            Verbose = 20,
            Diagnostic = 30,
        }

        [Flags]
        public enum TraceDevice
        {
            Debug = 0x40,
            Console = 0x80,
            All = 0xff
        }

        protected const ConsoleColor DebugColor = ConsoleColor.DarkGray;
        protected const ConsoleColor VerboseColor = ConsoleColor.DarkGray;
        protected const ConsoleColor InfoColor = ConsoleColor.Cyan;
        protected const ConsoleColor SuccessColor = ConsoleColor.Green;
        protected const ConsoleColor WarningColor = ConsoleColor.Yellow;
        protected const ConsoleColor ErrorColor = ConsoleColor.Red;

        private TraceLevel minLevel;
        private TraceDevice device;
        private string logFile;

        public EventHandlerBase(TraceLevel level, TraceDevice device, string logFile)
        {
            this.minLevel = level;
            this.device = device;
            this.logFile = logFile;
        }

        protected void Verbose(string format, params object[] args)
        {
            Out(VerboseColor, TraceLevel.Verbose, "VERBOSE: ", format, args);
        }

        protected void UniqueVerbose(string format, params object[] args)
        {
            UniqueOut(VerboseColor, TraceLevel.Verbose, "VERBOSE: ", format, args);
        }

        protected void Success(string format, params object[] args)
        {
            Out(SuccessColor, TraceLevel.Normal, string.Empty, format, args);
        }

        protected void Info(string format, params object[] args)
        {
            Out(InfoColor, TraceLevel.Normal, string.Empty, format, args);
        }

        protected void Warning(string format, params object[] args)
        {
            Out(WarningColor, TraceLevel.Normal, "WARNING: ", format, args);
        }

        protected void Error(string format, params object[] args)
        {
            Out(ErrorColor, TraceLevel.Normal, "ERROR: ", format, args);
        }

        protected void UniqueWarning(string format, params object[] args)
        {
            UniqueOut(WarningColor, TraceLevel.Normal, "WARNING: ", format, args);
        }

        protected void UniqueError(string format, params object[] args)
        {
            UniqueOut(ErrorColor, TraceLevel.Normal, "ERROR: ", format, args);
        }

        private HashSet<int> outputted = new HashSet<int>();

        protected void UniqueOut(ConsoleColor color, TraceLevel level, string prefix, string format, object[] args)
        {
            string message = string.Format(format, args: args);
            int key = message.GetHashCode();

            if (!outputted.Contains(key))
            {
                OutCore(color, level, prefix, message);
                outputted.Add(key);
            }//if
        }

        protected void RawOut(ConsoleColor color, TraceLevel level, string format, params object[] args)
        {
            string message = args != null ? string.Format(format, args: args) : format;
            OutCore(color, level, string.Empty, message);
        }

        protected void Out(ConsoleColor color, TraceLevel level, string prefix, string format, object[] args)
        {
            string message = args != null ? string.Format(format, args: args) : format;
            OutCore(color, level, prefix, message);
        }

        protected void OutCore(ConsoleColor color, TraceLevel level, string prefix, string message)
        {
            if (level > this.minLevel)
                return;

            if ((device & TraceDevice.Debug) == TraceDevice.Debug)
            {
                System.Diagnostics.Debug.Write(prefix);
                System.Diagnostics.Debug.WriteLine(message);
            }

            if ((device & TraceDevice.Console) == TraceDevice.Console)
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
            Out(DebugColor, TraceLevel.Diagnostic, "DEBUG: ", format, args);
        }
    }
}
