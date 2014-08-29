using Plossum.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    [CommandLineManager(
        ApplicationName = "WitSync",
        Copyright = "Copyright (c) Giulio Vian",
        Version = "0.2.2.0",
        EnabledOptionStyles = OptionStyles.Windows | OptionStyles.ShortUnix | OptionStyles.LongUnix)]
    public class WitSyncCommandLineOptions
    {
        public enum Verbs
        {
            SyncWorkItems,
            SyncAreasAndIterations,
            GenerateSampleMappingFile
        }

        public WitSyncCommandLineOptions()
        {
        }

        [CommandLineOption(Name = "a", Aliases = "action"
            , MinOccurs = 1
            , Description = "Action, one of: SyncWorkItems,SyncAreasAndIterations.")]
        public Verbs Action { get; set; }

        [CommandLineOption(Name = "c", Aliases = "sourceCollection"
            , MinOccurs = 1
            , Description = "Source Collection Url, e.g. http://localhost:8080/tfs/DefaultCollection")]
        public string SourceCollectionUrl { get; set; }
        [CommandLineOption(Name = "p", Aliases = "sourceProject"
            , MinOccurs = 1
            , Description = "Source Project Name")]
        public string SourceProjectName { get; set; }
        [CommandLineOption(Name = "su", Aliases = "sourceUser"
            , Description = "Username connecting to Source")]
        public string SourceUser { get; set; }
        [CommandLineOption(Name = "sp", Aliases = "sourcePassword"
            , Description = "Password for Source user")]
        public string SourcePassword { get; set; }

        [CommandLineOption(Name = "d", Aliases = "destinationCollection targetCollection"
            , MinOccurs = 1
            , Description = "Destination Collection Url, e.g. http://localhost:8080/tfs/DefaultCollection")]
        public string DestinationCollectionUrl { get; set; }
        [CommandLineOption(Name = "q", Aliases = "destinationProject targetProject"
            , MinOccurs =1
            , Description = "Destination Project Name")]
        public string DestinationProjectName { get; set; }
        [CommandLineOption(Name = "du", Aliases = "destinationUser targetUser "
            , Description = "Username connecting to Destination")]
        public string DestinationUser { get; set; }
        [CommandLineOption(Name = "dp", Aliases = "destinationPassword targetPassword "
            , Description = "Password for Destination user")]
        public string DestinationPassword { get; set; }

        [CommandLineOption(Name = "m", Aliases = "map mapping mappingFile"
            , MinOccurs = 1
            , Description = "Mapping file, e.g. MyMappingFile.xml")]
        public string MappingFile { get; set; }

        [CommandLineOption(Name = "v", Aliases = "verbose"
            , BoolFunction = BoolFunction.TrueIfPresent
            , Description = "Prints detailed output")]
        public bool Verbose { get; set; }

        [CommandLineOption(Name = "t", Aliases = "test trial"
            , BoolFunction = BoolFunction.TrueIfPresent
            , Description = "Test and does not save changes to target")]
        public bool TestOnly { get; set; }

        [CommandLineOption(Description = "Displays this help text")]
        public bool Help = false;

        // Advanced options
        [CommandLineOption(BoolFunction = BoolFunction.FalseIfPresent
            , Description = "Enable Rule validation")]
        public bool CheckWorkItemStoreRules { get; set; }

        [CommandLineOption(BoolFunction = BoolFunction.FalseIfPresent
            , Description = "Algorithm used to determine when a field is updatable.")]
        public bool UseHeuristicForFieldUpdatability { get; set; }

        [CommandLineOption(BoolFunction = BoolFunction.FalseIfPresent
            , Description = "Use [WorkItem.Open] Method to make the WorkItem updatable.")]
        public bool DoNotOpenTargetWorkItem { get; set; }

        [CommandLineOption(BoolFunction = BoolFunction.TrueIfPresent
            , Description = "Use [WorkItem.PartialOpen] Method to make the WorkItem updatable.")]
        public bool PartialOpenTargetWorkItem { get; set; }

        [CommandLineOption(BoolFunction = BoolFunction.TrueIfPresent
            , Description = "WorkItems missing from the target are first added in the initial state specified by InitalStateOnDestination, then updated to reflect the state of the source.")]
        public bool CreateThenUpdate { get; set; }

        public WitSyncEngine.EngineOptions AdvancedOptions
        {
            get
            {
                WitSyncEngine.EngineOptions result = 0;

                if (!CheckWorkItemStoreRules)
                    result |= WitSyncEngine.EngineOptions.BypassWorkItemStoreRules;
                if (!UseHeuristicForFieldUpdatability)
                    result |= WitSyncEngine.EngineOptions.UseEditableProperty;
                if (!DoNotOpenTargetWorkItem)
                    result |= WitSyncEngine.EngineOptions.OpenTargetWorkItem;
                if (PartialOpenTargetWorkItem)
                    result |= WitSyncEngine.EngineOptions.PartialOpenTargetWorkItem;
                if (CreateThenUpdate)
                    result |= WitSyncEngine.EngineOptions.CreateThenUpdate;    

                return result;
            }
        }
    }
}
