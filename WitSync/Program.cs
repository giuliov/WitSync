using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WitSync;
using System.Net;

namespace WitSync
{
    class Program
    {
        static int Main(string[] args)
        {
            string logHeader = CommandLineParser.GetHeader();
            Console.WriteLine(logHeader);

            var initialOptions = CommandLineParser.InitalParse(args);
            if (initialOptions == null)
                // parsing failed
                return -1;

            if (initialOptions.Generate)
            {
                MappingFile.Generate().SaveAsYaml("sample.yml");
                return 1;
            }

            MappingFile configuration = new MappingFile();
            if (System.IO.File.Exists(initialOptions.MappingFile))
            {
                configuration = MappingFile.LoadFrom(initialOptions.MappingFile);
            }
            else
            {
                Console.WriteLine("Mapping file '{0}' not found.", initialOptions.MappingFile);
                return -2;
            }//if

            configuration = CommandLineParser.ParseAndMerge(args, configuration);
            if (configuration == null) {
                // parsing failed
                return -1;
            }

            // command line parsing succeeded
            if (configuration.TestOnly)
                Console.WriteLine("** TEST MODE: no data will be written on destination **");


            // with user's need in hand, build the pipeline
            var eventHandler = new EngineEventHandler(configuration.Logging != LoggingLevel.Normal, configuration.LogFile);
            eventHandler.FirstMessage(logHeader);
            //TODO eventHandler.DumpOptions(configuration);

            if (!configuration.Validate())
            {
                return -1;
            }

            // build the pipeline
            var pipeline = new SyncPipeline(configuration, eventHandler);
            int rc = pipeline.Execute();

            if (!string.IsNullOrWhiteSpace(configuration.ChangeLogFile))
            {
                eventHandler.SavingChangeLogToFile(configuration.ChangeLogFile);
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(configuration.ChangeLogFile))
                {
                    //CSV header
                    file.WriteLine("Source,SourceId,TargetId,ChangeType,Message");
                    foreach (var entry in pipeline.ChangeLog.GetEntries())
                    {
                        file.WriteLine("{0},{1},{2},{3},\"{4}\"",
                            entry.Source,
                            entry.SourceId,
                            entry.TargetId,
                            entry.Succeeded ? entry.ChangeType : "Failure",
                            entry.Message);
                    }//for
                }//using
                eventHandler.SavedChangeLog(pipeline.ChangeLog.Count);
            }//if

            eventHandler.LastMessage(rc);
            return rc;
        }
    }
}
