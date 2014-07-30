using Plossum.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    [CommandLineManager(
        ApplicationName = "WitSync",
        Copyright = "Copyright (c) Giulio Vian",
        Version="0.1.2.0")]
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
            this.Advanced = new string[0];
        }

        [CommandLineOption(Name = "a", MinOccurs =1, Description = "Action, one of: SyncWorkItems,SyncAreasAndIterations.")]
        public Verbs Action { get; set; }

        [CommandLineOption(Name = "c", MinOccurs = 1, Description = "Source Collection Url, e.g. http://localhost:8080/tfs/DefaultCollection")]
        public string SourceCollectionUrl { get; set; }
        [CommandLineOption(Name = "p", MinOccurs =1, Description = "Source Project Name")]
        public string SourceProjectName { get; set; }
        [CommandLineOption(Name = "d", MinOccurs = 1, Description = "Destination Collection Url, e.g. http://localhost:8080/tfs/DefaultCollection")]
        public string DestinationCollectionUrl { get; set; }
        [CommandLineOption(Name = "q", MinOccurs =1, Description = "Destination Project Name")]
        public string DestinationProjectName { get; set; }

        [CommandLineOption(Name = "m", MinOccurs = 1, Description = "Mapping file, e.g. MyMappingFile.xml")]
        public string MappingFile { get; set; }

        [CommandLineOption(Name = "v", BoolFunction = BoolFunction.TrueIfPresent, Description = "Prints detailed output")]
        public bool Verbose { get; set; }

        [CommandLineOption(Name = "t", BoolFunction = BoolFunction.TrueIfPresent, Description = "Test and does not save changes to target")]
        public bool TestOnly { get; set; }

        [CommandLineOption(Name = "x", MaxOccurs = 4, Description = "Advanced options, one or more of: BypassWorkItemStoreRules,UseEditableProperty,OpenTargetWorkItem,PartialOpenTargetWorkItem,CreateThenUpdate")]
        public string[] Advanced { get; set; }
        
        [CommandLineOption(Description = "Displays this help text")]
        public bool Help = false;

        public WitSyncEngine.EngineOptions AdvancedOptions
        {
            get
            {
                WitSyncEngine.EngineOptions result = 0;
                //List<string> -> enum
                foreach (string option in this.Advanced)
                {
                    WitSyncEngine.EngineOptions value;
                    if (Enum.TryParse<WitSyncEngine.EngineOptions>(option, true, out value))
                        result |= value;
                }//for
                return result;
            }
        }
    }
}
