using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    public enum LoggingLevel
    {
        Normal,
        Verbose,
        Diagnostic
    }

    public class PipelineConfiguration
    {
        public class ConnectionInfo
        {
            public string CollectionUrl { get; set; }
            public string ProjectName { get; set; }
            public string User { get; set; }
            public string Password { get; set; }
        }

        public ConnectionInfo SourceConnection { get; set; }
        public ConnectionInfo DestinationConnection { get; set; }

        public List<string> PipelineSteps { get; set; }

        public string MappingFile { get; set; }
        public string IndexFile { get; set; }
        public string ChangeLogFile { get; set; }
        public string LogFile { get; set; }

        public LoggingLevel Logging { get; set; }
        public bool StopPipelineOnFirstError { get; set; }
        public bool TestOnly { get; set; }
        public List<string> AdvancedOptions { get; set; }

        public AreasAndIterationsStageConfiguration AreasAndIterationsStage { get; set; }
        public GlobalListStageConfiguration GlobalListStage { get; set; }
        public WorkItemsStageConfiguration WorkItemsStage { get; set; }

        public static T Generate<T>()
            where T : PipelineConfiguration, new()
        {
            var self = new T()
            {
                SourceConnection = new ConnectionInfo()
                {
                    CollectionUrl = "http://localhost:8080/tfs/DefaultCollection",
                    ProjectName = "yourSourceProject",
                    User = "sourceUser",
                    Password = "***"
                },
                DestinationConnection = new ConnectionInfo()
                {
                    CollectionUrl = "http://localhost:8080/tfs/DefaultCollection",
                    ProjectName = "yourTargetProject",
                    User = "targetUser",
                    Password = "***"
                },
                MappingFile = null, // MUST come from command line
                PipelineSteps = new List<string>() { "step1", "step2" },
                StopPipelineOnFirstError = true,
                TestOnly = true,
                Logging = LoggingLevel.Diagnostic,
                IndexFile = "index.xml",
                ChangeLogFile = "changes.csv",
                LogFile = "log.txt",
                AdvancedOptions = new List<string>() { "opt1", "opt2" },
                // let them say
                AreasAndIterationsStage = AreasAndIterationsStageConfiguration.Generate(),
                GlobalListStage = GlobalListStageConfiguration.Generate(),
                WorkItemsStage = WorkItemsStageConfiguration.Generate()
            };
            return self;
        }
    }
}
