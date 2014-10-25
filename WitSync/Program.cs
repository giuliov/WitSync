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
        /*
         --Globallists --Areas --Iterations  --WorkItems --sourceCollection http://localhost:8080/tfs/WitSync --sourceProject "WitSyncSrc" --destinationCollection http://localhost:8080/tfs/WitSync --destinationProject "WitSyncDest" --indexFile test01.idx --mappingFile "Sample Mappings\test01.yml" --verbose --stopOnError --test
         -Globallists -c http://localhost:8080/tfs/WitSync -p "WitSyncSrc" -d http://localhost:8080/tfs/WitSync -q "WitSyncDest" -m "Sample Mappings\globallists.yml" -v
         */
        static int Main(string[] args)
        {
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
            var fileVersion = System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(System.Reflection.AssemblyFileVersionAttribute), false).FirstOrDefault() as System.Reflection.AssemblyFileVersionAttribute;
            parser.UsageInfo.ApplicationVersion = fileVersion.Version;
            Console.WriteLine(parser.UsageInfo.GetHeaderAsString(lastColumn));
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

            // command line parsing succeeded
            if (options.TestOnly)
                Console.WriteLine("** TEST MODE: no data will be written on destination **");

            if (options.Verbose)
            {
                EventHandlerBase.GlobalVerbose(options.ToString());
            }//if

            // TODO use an option to generate sample file
            // SyncMapping.Generate().SaveTo("generated.yaml");
            //var x = SyncMapping.LoadFrom("generated.yaml");
            //var x = SyncMapping.LoadFrom("Sample Mappings\\test01.yml");

            // with user's need in hand, build the pipeline
            var eventHandler = new EngineEventHandler(options.Verbose);
            TfsConnection source;
            TfsConnection dest;
            MakeConnection(options, out source, out dest);

            var pipeline = new SyncPipeline(source, dest, eventHandler);
            SyncMapping map;
            if (System.IO.File.Exists(options.MappingFile))
            {
                map = SyncMapping.LoadFrom(options.MappingFile);
            }
            else
            {
                eventHandler.MappingFileNotFoundAssumeDefaults(options.MappingFile);
                map = new SyncMapping();
            }//if
            //TODO mapping validation

            var stageBuilder = new Dictionary<WitSyncCommandLineOptions.Verbs,Func<EngineBase>>();
            stageBuilder[WitSyncCommandLineOptions.Verbs.SyncGloballists] = () =>
            {
                var engine = new GlobalListsSyncEngine(source, dest, eventHandler);
                engine.MapGetter = () => { return map.globallists; };
                return engine;
            };
            stageBuilder[WitSyncCommandLineOptions.Verbs.SyncAreas] = () =>
            {
                var engine = new AreasAndIterationsSyncEngine(source, dest, eventHandler);
                engine.Options = AreasAndIterationsSyncEngine.EngineOptions.Areas;
                return engine;
            };
            stageBuilder[WitSyncCommandLineOptions.Verbs.SyncIterations] = () =>
            {
                var engine = new AreasAndIterationsSyncEngine(source, dest, eventHandler);
                engine.Options = AreasAndIterationsSyncEngine.EngineOptions.Iterations;
                return engine;
            };
            stageBuilder[WitSyncCommandLineOptions.Verbs.SyncWorkItems] = () =>
            {
                var engine = new WitSyncEngine(source, dest, eventHandler);
                engine.MapGetter = () => {
                    if (map.workitems == null)
                        // mapping file could be empty
                        map.workitems = new ProjectMapping();
                    if (!string.IsNullOrEmpty(options.IndexFile))
                    {
                        // if specified both in the mapping and on the command line, latter wins
                        map.workitems.IndexFile = options.IndexFile;
                    } 
                    return map.workitems;
                };
                engine.Options = options.AdvancedOptions;
                return engine;
            };//lambda

            foreach (WitSyncCommandLineOptions.Verbs stage in Enum.GetValues(typeof(WitSyncCommandLineOptions.Verbs)))
            {
                if ((options.Action & stage) == stage)
                {
                    pipeline.AddStage(stageBuilder[stage]);
                }//if
            }//for

            return pipeline.Execute(options.StopPipelineOnFirstError, options.TestOnly);
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
