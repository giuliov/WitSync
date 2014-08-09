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
            int lastColumn = Console.BufferWidth - 2;

            var options = new WitSyncCommandLineOptions();

            CommandLineParser parser = new CommandLineParser(options);
            parser.Parse();
            Console.WriteLine(parser.UsageInfo.GetHeaderAsString(lastColumn));

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
            }

            // passed

            try
            {
                switch (options.Action)
                {
                    case WitSyncCommandLineOptions.Verbs.SyncWorkItems:
                        return RunSync(options);
                    case WitSyncCommandLineOptions.Verbs.SyncAreasAndIterations:
                        return RunSyncAreasAndIterations(options);
                    case WitSyncCommandLineOptions.Verbs.GenerateSampleMappingFile:
                        return GenerateSampleMappingFile(options);
                }//switch
            }
            catch (Exception ex)
            {
                EventHandlerBase.GlobalError("Internal error: {0}", ex.Message);
                return -99;
            }//try

            // should never get here...
            return -1;
        }

        private static int RunSyncAreasAndIterations(WitSyncCommandLineOptions options)
        {
            var eventHandler = new EngineEventHandler(options.Verbose);

            TfsConnection source;
            TfsConnection dest;
            Connect(options, out source, out dest);

            var engine = new AreasAndIterationsSyncEngine(source, dest, eventHandler);
            return engine.Sync(options.TestOnly);
        }


        private static int GenerateSampleMappingFile(WitSyncCommandLineOptions options)
        {
            ProjectMapping.GenerateSampleMappingFile(options.MappingFile);
            return 0;
        }

        private static int RunSync(WitSyncCommandLineOptions options)
        {
            var eventHandler = new EngineEventHandler(options.Verbose);

            var mapping = ProjectMapping.LoadFrom(options.MappingFile);
            if (mapping.ErrorsCount > 0)
            {
                foreach (var err in mapping.ErrorMessage)
                {
                    eventHandler.MappingGenericValidationError(err, null);
                }
                return 100 + mapping.ErrorsCount;
            }//if

            var index = new WitMappingIndex();

            TfsConnection source;
            TfsConnection dest;
            Connect(options, out source, out dest);

            var engine = new WitSyncEngine(source, dest, eventHandler);
            return engine.Sync(mapping, index, options.TestOnly, options.AdvancedOptions);
        }

        private static void Connect(WitSyncCommandLineOptions options, out TfsConnection source, out TfsConnection dest)
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
