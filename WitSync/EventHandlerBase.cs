using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace WitSync
{
    class EventHandlerBase
    {
        [Flags]
        protected enum OutputFlags
        {
            Debug = 1,
            Console = 2,
            All = 0xff
        }

        private bool verbose;

        public EventHandlerBase(bool verbose)
        {
            this.verbose = verbose;
        }

        protected void Verbose(string format, params object[] args)
        {
            OutputFlags outFlags = this.verbose ? OutputFlags.All : OutputFlags.Debug;
            Out(ConsoleColor.Gray, outFlags, "VERBOSE: ", format, args);
        }

        protected void UniqueVerbose(string format, params object[] args)
        {
            OutputFlags outFlags = this.verbose ? OutputFlags.All : OutputFlags.Debug;
            UniqueOut(ConsoleColor.Gray, outFlags, "VERBOSE: ", format, args);
        }

        protected void Info(string format, params object[] args)
        {
            Out(ConsoleColor.Cyan, OutputFlags.All, string.Empty, format, args);
        }

        protected void Warning(string format, params object[] args)
        {
            Out(ConsoleColor.Yellow, OutputFlags.All, "WARNING: ", format, args);
        }

        protected void Error(string format, params object[] args)
        {
            Out(ConsoleColor.Red, OutputFlags.All, "ERROR: ", format, args);
        }

        protected void UniqueWarning(string format, params object[] args)
        {
            UniqueOut(ConsoleColor.Yellow, OutputFlags.All, "WARNING: ", format, args);
        }

        protected void UniqueError(string format, params object[] args)
        {
            UniqueOut(ConsoleColor.Red, OutputFlags.All, "ERROR: ", format, args);
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

        static protected void Out(ConsoleColor color, OutputFlags outFlags, string prefix, string format, object[] args)
        {
            string message = args != null ? string.Format(format, args: args) : format;

            OutCore(color, outFlags, prefix, message);
        }

        static protected void OutCore(ConsoleColor color, OutputFlags outFlags, string prefix, string message)
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
        }

        static public void GlobalError(string format, params object[] args)
        {
            Out(ConsoleColor.Red, OutputFlags.All, "ERROR: ", format, args);
        }

        static public void GlobalVerbose(string format, params object[] args)
        {
            Out(ConsoleColor.Gray, OutputFlags.All, "VERBOSE: ", format, args);
        }
    }
}
