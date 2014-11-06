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
//        Version = "0.5.0.0",
        EnabledOptionStyles = OptionStyles.Group | OptionStyles.Windows | OptionStyles.ShortUnix | OptionStyles.LongUnix)]
    [CommandLineOptionGroup("commands", Name = "Commands")]
    [CommandLineOptionGroup("connection", Name = "Connection")]
    [CommandLineOptionGroup("options", Name = "Options")]
    [CommandLineOptionGroup("advanced", Name = "Advanced Options")]
    public class WitSyncCommandLineOptions
    {
        [Flags]
        public enum PipelineSteps
        {
            // value order is important!
            Globallists = 1,
            Areas = 2,
            Iterations = 4,
            WorkItems = 8
        }

        public WitSyncCommandLineOptions()
        {
        }

        // commands
        [CommandLineOption(GroupId = "commands"
            , Name="g", Aliases="gl globallists"
            , Description = "Syncronize GlobalLists data (use mapping file to filter)")]
        public bool SyncGloballists { get; set; }
        [CommandLineOption(GroupId = "commands"
            , Name = "a", Aliases = "area areas"
            , Description = "Syncronize Areas (see documentation for limits)")]
        public bool SyncAreas { get; set; }
        [CommandLineOption(GroupId = "commands"
            , Name = "i", Aliases = "iteration iterations"
            , Description = "Syncronize Iterations (see documentation for limits)")]
        public bool SyncIterations { get; set; }
        [CommandLineOption(GroupId = "commands"
            , Name = "w", Aliases = "wi workitem workitems"
            , Description = "Syncronize WorkItems")]
        public bool SyncWorkItems { get; set; }

        private PipelineSteps steps = 0;
        public PipelineSteps Steps
        {
            get
            {
                PipelineSteps result = steps;
                if (SyncGloballists)
                    result |= PipelineSteps.Globallists;
                if (SyncAreas)
                    result |= PipelineSteps.Areas;
                if (SyncIterations)
                    result |= PipelineSteps.Iterations;
                if (SyncWorkItems)
                    result |= PipelineSteps.WorkItems;
                return result;
            }
            set { steps = value; }
        }

        // options

        [CommandLineOption(GroupId = "connection"
            , Name = "c", Aliases = "sourceCollection"
            , Description = "Source Collection Url, e.g. http://localhost:8080/tfs/DefaultCollection")]
        public string SourceCollectionUrl { get; set; }
        [CommandLineOption(GroupId = "connection"
            , Name = "p", Aliases = "sourceProject"
            , Description = "Source Project Name")]
        public string SourceProjectName { get; set; }
        [CommandLineOption(GroupId = "connection"
            , Name = "su", Aliases = "sourceUser"
            , Description = "Username connecting to Source")]
        public string SourceUser { get; set; }
        [CommandLineOption(GroupId = "connection"
            , Name = "sp", Aliases = "sourcePassword"
            , Description = "Password for Source user")]
        public string SourcePassword { get; set; }

        [CommandLineOption(GroupId = "connection"
            , Name = "d", Aliases = "destinationCollection targetCollection"
            , Description = "Destination Collection Url, e.g. http://localhost:8080/tfs/DefaultCollection")]
        public string DestinationCollectionUrl { get; set; }
        [CommandLineOption(GroupId = "connection"
            , Name = "q", Aliases = "destinationProject targetProject"
            , Description = "Destination Project Name")]
        public string DestinationProjectName { get; set; }
        [CommandLineOption(GroupId = "connection"
            , Name = "du", Aliases = "destinationUser targetUser "
            , Description = "Username connecting to Destination")]
        public string DestinationUser { get; set; }
        [CommandLineOption(GroupId = "connection"
            , Name = "dp", Aliases = "destinationPassword targetPassword "
            , Description = "Password for Destination user")]
        public string DestinationPassword { get; set; }

        [CommandLineOption(GroupId = "options"
            , Name = "m", Aliases = "map mapping mappingFile"
            , MinOccurs = 1
            , Description = "Mapping file, e.g. MyMappingFile.xml")]
        public string MappingFile { get; set; }
        [CommandLineOption(GroupId = "options"
            , Name = "ix", Aliases = "idx index indexFile"
            , MinOccurs = 0 //HACK is mandatory if ...
            , Description = "Index file, e.g. MyIndex.xml")]
        public string IndexFile { get; set; }
        [CommandLineOption(GroupId = "options"
            , Name = "cl", Aliases = "chg changes changeLogFile"
            , Description = "ChangeLog file, e.g. ChangeLog.csv")]
        public string ChangeLogFile { get; set; }

        [CommandLineOption(GroupId = "options"
            , Name = "v", Aliases = "verbose"
            , BoolFunction = BoolFunction.TrueIfPresent
            , Description = "Prints detailed output")]
        public bool Verbose { get; set; }
        [CommandLineOption(GroupId = "options"
            , Name = "l", Aliases = "log logFile"
            , BoolFunction = BoolFunction.TrueIfPresent
            , Description = "Write complete log to file")]
        public string LogFile { get; set; }

        [CommandLineOption(GroupId = "options"
            , Name = "e", Aliases = "stopOnError"
            , BoolFunction = BoolFunction.TrueIfPresent
            , Description = "Stops if pipeline stage fails")]
        public bool StopPipelineOnFirstError { get; set; }
        [CommandLineOption(GroupId = "options"
            , Name = "t", Aliases = "test trial"
            , BoolFunction = BoolFunction.TrueIfPresent
            , Description = "Test and does not save changes to target")]
        public bool TestOnly { get; set; }

        [CommandLineOption(GroupId = "options", Description = "Displays this help text")]
        public bool Help = false;

        // Advanced options
        [CommandLineOption(GroupId = "advanced"
            , BoolFunction = BoolFunction.TrueIfPresent
            , Description = "Disable Rule validation")]
        public bool BypassWorkItemValidation { get; set; }

        [CommandLineOption(GroupId = "advanced"
            , BoolFunction = BoolFunction.TrueIfPresent
            , Description = "Algorithm used to determine when a field is updatable.")]
        public bool UseHeuristicForFieldUpdatability { get; set; }

        [CommandLineOption(GroupId = "advanced"
            , BoolFunction = BoolFunction.TrueIfPresent
            , Description = "Use [WorkItem.Open] Method to make the WorkItem updatable.")]
        public bool DoNotOpenTargetWorkItem { get; set; }

        [CommandLineOption(GroupId = "advanced"
            , BoolFunction = BoolFunction.TrueIfPresent
            , Description = "Use [WorkItem.PartialOpen] Method to make the WorkItem updatable.")]
        public bool PartialOpenTargetWorkItem { get; set; }

        [CommandLineOption(GroupId = "advanced"
            , BoolFunction = BoolFunction.TrueIfPresent
            , Description = "WorkItems missing from the target are first added in the initial state specified by InitalStateOnDestination, then updated to reflect the state of the source.")]
        public bool CreateThenUpdate { get; set; }

        public WitSyncEngine.EngineOptions AdvancedOptions
        {
            get
            {
                WitSyncEngine.EngineOptions result = 0;

                if (BypassWorkItemValidation)
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

        public override string ToString()
        {
            var buf = new StringBuilder();
            buf.AppendLine();
            buf.AppendFormat("  Action: {0}", this.Steps);
            buf.AppendLine();
            buf.AppendFormat("  Mapping file: {0}", this.MappingFile);
            buf.AppendLine();
            buf.AppendFormat("  Index file: {0}", this.IndexFile);
            buf.AppendLine();
            buf.AppendFormat("  ChangeLog file: {0}", this.ChangeLogFile);
            buf.AppendLine();
            buf.AppendFormat("  Log file: {0}", this.LogFile);
            buf.AppendLine();
            buf.AppendFormat("  Advanced options: {0}", this.AdvancedOptions);
            buf.AppendLine();
            return buf.ToString();
        }
    }
}
