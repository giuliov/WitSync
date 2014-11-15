using NDesk.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    class CommandLineParser
    {
        static private T GetCustomAttribute<T>()
            where T : Attribute
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
        }

        static internal string GetHeader()
        {
            var title = GetCustomAttribute<System.Reflection.AssemblyTitleAttribute>();
            var descr = GetCustomAttribute<System.Reflection.AssemblyDescriptionAttribute>();
            var copy = GetCustomAttribute<System.Reflection.AssemblyCopyrightAttribute>();
            var fileVersion = GetCustomAttribute<System.Reflection.AssemblyFileVersionAttribute>();
            var infoVersion = GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>();

            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1}", title.Title, infoVersion.InformationalVersion);
            sb.AppendLine();
            sb.AppendLine(descr.Description);
            sb.AppendFormat("Build: {0}", fileVersion.Version);
            sb.AppendLine();
            sb.AppendLine(copy.Copyright);

            return sb.ToString();
        }

        static internal CommandLineArgs InitalParse(string[] args)
        {
            var options = new CommandLineArgs();

            bool show_help = false;

            var p = new OptionSet()
            {
            { "h|help",  "Shows this message and exit", 
              value => show_help = value != null },
            { "m|configuration:",  "Configuration & Mapping file", 
              value => options.MappingFile = value },
            { "g|generate",  "Generate sample configuration file", 
              value => options.Generate = value != null },
            };

            try
            {
                p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }//try

            if (show_help)
            {
                p.WriteOptionDescriptions(Console.Out);
                return null;
            }
            return options;
        }

        internal static MappingFile ParseAndMerge(string[] args, MappingFile configuration)
        {
            configuration.FixNulls();

            var options = new CommandLineArgs();

            var p = new OptionSet()
            {
            { "m|configuration=",  "Configuration & Mapping file", 
              value => options.MappingFile = value },
            // pipeline behavior
            { "e|stopOnError",  "Test and does not save changes to target", 
              value => configuration.StopPipelineOnFirstError = value != null },
            { "t|test",  "Stops if pipeline stage fails", 
              value => configuration.TestOnly = value != null },
            //logging
            { "l|log:",  "Write complete log to file", 
              value => configuration.LogFile = value },
            { "v|verbosity:",  string.Format("Verbosity level: {0}", string.Join(",", Enum.GetNames(typeof(LoggingLevel)))), 
              value => configuration.Logging = (LoggingLevel)Enum.Parse(typeof(LoggingLevel),value) },
            // data files
            { "i|index:",  "Index file, e.g. MyIndex.xml", 
              value => configuration.WorkItemsStage.IndexFile = value },
            { "c|changeLog:",  "ChangeLog file, e.g. ChangeLog.csv", 
              value => configuration.ChangeLogFile = value },
            // connection group
            { "sc|sourceCollection:",  "Source Collection Url, e.g. http://localhost:8080/tfs/DefaultCollection", 
              value => configuration.SourceConnection.CollectionUrl = value },
            { "dc|destinationCollection:",  "Destination Collection Url, e.g. http://localhost:8080/tfs/DefaultCollection", 
              value => configuration.DestinationConnection.CollectionUrl = value },
            { "sp|sourceProject:",  "Source Project Name", 
              value => configuration.SourceConnection.ProjectName = value },
            { "dp|destinationProject:",  "Destination Project Name", 
              value => configuration.DestinationConnection.ProjectName = value },
            { "su|sourceUser:",  "Username connecting to Source", 
              value => configuration.SourceConnection.User = value },
            { "du|destinationUser:",  "Username connecting to Destination", 
              value => configuration.DestinationConnection.User = value },
            { "sw|sourcePassword:",  "Password for Source user", 
              value => configuration.SourceConnection.Password = value },
            { "dw|destinationPassword:",  "Password for Destination user", 
              value => configuration.DestinationConnection.Password = value },
            };

            try
            {
                p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }//try
            
            return configuration;
        }
    }
}
