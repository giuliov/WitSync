using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WitSync;
using Plossum.CommandLine;
using System.Net;

namespace WitSync
{
    class Program
    {
        static int Main(string[] args)
        {
            // option to generate sample file
            if (args.Length > 0
                && string.Compare(args[0], "generate", true) == 0)
            {
                MappingFile.Generate().SaveTo("sample.yml");
                return 1;
            }//if

            int lastColumn;

            // nice output formatting
            try
            {
                lastColumn = Console.BufferWidth - 2;
            }
            catch
            {
                lastColumn = 78;
            }//try

            // parse command line
            var options = new WitSyncCommandLineOptions();

            CommandLineParser parser = new CommandLineParser(options);
            // display the version computed by the build
            var fileVersion = System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(System.Reflection.AssemblyFileVersionAttribute), false).FirstOrDefault() as System.Reflection.AssemblyFileVersionAttribute;
            parser.UsageInfo.ApplicationVersion = fileVersion.Version;
            string logHeader = parser.UsageInfo.GetHeaderAsString(lastColumn);
            Console.WriteLine(logHeader);
            parser.Parse();

            if (options.Help)
            {
                Console.WriteLine(parser.UsageInfo.GetOptionsAsString(lastColumn));
                return 0;
            }
            else if (parser.HasErrors)
            {
                Console.WriteLine(parser.UsageInfo.GetOptionsAsString(lastColumn));
                Console.WriteLine(parser.UsageInfo.GetErrorsAsString(lastColumn));
                return -1;
            }//if

            MappingFile configuration = null;
            if (System.IO.File.Exists(options.MappingFile))
            {
                configuration = MappingFile.LoadFrom(options.MappingFile);
                // merge options (hand-made)
                if (string.IsNullOrWhiteSpace(options.SourceCollectionUrl))
                    options.SourceCollectionUrl = configuration.SourceConnection.CollectionUrl;
                if (string.IsNullOrWhiteSpace(options.SourceProjectName))
                    options.SourceProjectName = configuration.SourceConnection.ProjectName;
                if (string.IsNullOrWhiteSpace(options.DestinationCollectionUrl))
                    options.DestinationCollectionUrl = configuration.DestinationConnection.CollectionUrl;
                if (string.IsNullOrWhiteSpace(options.DestinationProjectName))
                    options.DestinationProjectName = configuration.DestinationConnection.ProjectName;
                if (string.IsNullOrWhiteSpace(options.IndexFile))
                    options.IndexFile = configuration.IndexFile;
                if (string.IsNullOrWhiteSpace(options.ChangeLogFile))
                    options.ChangeLogFile = configuration.ChangeLogFile;
                if (string.IsNullOrWhiteSpace(options.LogFile))
                    options.LogFile = configuration.LogFile;
                if (options.Steps == 0)
                {
                    WitSyncCommandLineOptions.PipelineSteps steps = 0;
                    foreach (var step in configuration.PipelineSteps)
                    {
                        steps |= (WitSyncCommandLineOptions.PipelineSteps)Enum.Parse(typeof(WitSyncCommandLineOptions.PipelineSteps), step, true);
                    }//for
                    options.Steps = steps;
                }//if
                if (configuration.AdvancedOptions != null)
                {
                    WorkItemsStage.EngineOptions advanced = options.AdvancedOptions;
                    foreach (var oneOpt in configuration.AdvancedOptions)
                    {
                        advanced |= (WorkItemsStage.EngineOptions)Enum.Parse(typeof(WorkItemsStage.EngineOptions), oneOpt, true);
                    }//for
                    options.AdvancedOptions = advanced;
                }//if
            }
            else
            {
                Console.WriteLine("Mapping file '{0}' not found.", options.MappingFile);
                return -2;
            }//if

            // command line parsing succeeded
            if (options.TestOnly)
                Console.WriteLine("** TEST MODE: no data will be written on destination **");

            // with user's need in hand, build the pipeline
            var eventHandler = new EngineEventHandler(options.Verbose, options.LogFile);
            eventHandler.FirstMessage(logHeader);
            eventHandler.DumpOptions(options);

            TfsConnection source;
            TfsConnection dest;
            MakeConnection(options, out source, out dest);

            var pipeline = new SyncPipeline(source, dest, eventHandler);
            if (!System.IO.File.Exists(options.MappingFile))
            {
                eventHandler.MappingFileNotFoundAssumeDefaults(options.MappingFile);
                configuration = new MappingFile();
            }//if
            //TODO mapping validation

            var stageBuilder = new Dictionary<WitSyncCommandLineOptions.PipelineSteps,Func<PipelineStage>>();
            stageBuilder[WitSyncCommandLineOptions.PipelineSteps.Globallists] = () =>
            {
                var engine = new GlobalListsStage(source, dest, eventHandler);
                engine.MapGetter = () => { return configuration.GlobalListStage; };
                return engine;
            };
            stageBuilder[WitSyncCommandLineOptions.PipelineSteps.Areas] = () =>
            {
                var engine = new AreasAndIterationsStage(source, dest, eventHandler);
                engine.Options = AreasAndIterationsStage.EngineOptions.Areas;
                return engine;
            };
            stageBuilder[WitSyncCommandLineOptions.PipelineSteps.Iterations] = () =>
            {
                var engine = new AreasAndIterationsStage(source, dest, eventHandler);
                engine.Options = AreasAndIterationsStage.EngineOptions.Iterations;
                return engine;
            };
            stageBuilder[WitSyncCommandLineOptions.PipelineSteps.WorkItems] = () =>
            {
                var engine = new WorkItemsStage(source, dest, eventHandler);
                engine.MapGetter = () => {
                    if (configuration.WorkItemsStage == null)
                        // mapping file could be empty
                        configuration.WorkItemsStage = new WorkItemsStageConfiguration();
                    if (!string.IsNullOrEmpty(options.IndexFile))
                    {
                        // if specified both in the mapping and on the command line, latter wins
                        configuration.WorkItemsStage.IndexFile = options.IndexFile;
                    }
                    return configuration.WorkItemsStage;
                };
                engine.Options = options.AdvancedOptions;
                return engine;
            };//lambda

            foreach (WitSyncCommandLineOptions.PipelineSteps stage in Enum.GetValues(typeof(WitSyncCommandLineOptions.PipelineSteps)))
            {
                if ((options.Steps & stage) == stage)
                {
                    pipeline.AddStage(stageBuilder[stage]);
                }//if
            }//for

            int rc = pipeline.Execute(options.StopPipelineOnFirstError, options.TestOnly);

            if (!string.IsNullOrWhiteSpace(options.ChangeLogFile))
            {
                eventHandler.SavingChangeLogToFile(options.ChangeLogFile);
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(options.ChangeLogFile))
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

        private static void MakeConnection(WitSyncCommandLineOptions options, out TfsConnection source, out TfsConnection dest)
        {
            source = new TfsConnection()
            {
                CollectionUrl = new Uri(options.SourceCollectionUrl),
                ProjectName = options.SourceProjectName,
                Credential = new NetworkCredential(options.SourceUser, options.SourcePassword)
            };
            dest = new TfsConnection()
            {
                CollectionUrl = new Uri(options.DestinationCollectionUrl),
                ProjectName = options.DestinationProjectName,
                Credential = new NetworkCredential(options.DestinationUser, options.DestinationPassword)
            };
        }
    }
}
