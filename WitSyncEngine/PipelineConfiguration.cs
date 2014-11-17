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

    public class PipelineConfiguration : ConfigurationBase
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

        public List<string> PipelineStages { get; set; }

        public string ChangeLogFile { get; set; }
        public string LogFile { get; set; }

        public LoggingLevel Logging { get; set; }
        public bool StopPipelineOnFirstError { get; set; }
        public bool TestOnly { get; set; }

        public AreasAndIterationsStageConfiguration AreasAndIterationsStage { get; set; }
        public GlobalListsStageConfiguration GlobalListsStage { get; set; }
        public WorkItemsStageConfiguration WorkItemsStage { get; set; }

        public void FixNulls()
        {
            SourceConnection = SourceConnection ?? new ConnectionInfo();
            DestinationConnection = DestinationConnection ?? new ConnectionInfo();
            PipelineStages = PipelineStages ?? new List<string>();
            AreasAndIterationsStage = AreasAndIterationsStage ?? new AreasAndIterationsStageConfiguration();
            GlobalListsStage = GlobalListsStage ?? new GlobalListsStageConfiguration();
            WorkItemsStage = WorkItemsStage ?? new WorkItemsStageConfiguration();
        }

        public bool Validate()
        {
            if (this.WorkItemsStage == null)
            {
                // mapping file could be empty
                this.WorkItemsStage = new WorkItemsStageConfiguration();
            }

            foreach (var stageName in this.PipelineStages)
            {
                //TODO
            }
            // TODO more and more

            return true;
        }

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
                PipelineStages = new List<string>() { "step1", "step2" },
                StopPipelineOnFirstError = true,
                TestOnly = true,
                Logging = LoggingLevel.Diagnostic,
                ChangeLogFile = "changes.csv",
                LogFile = "log.txt",
                // let them say
                AreasAndIterationsStage = AreasAndIterationsStageConfiguration.Generate(),
                GlobalListsStage = GlobalListsStageConfiguration.Generate(),
                WorkItemsStage = WorkItemsStageConfiguration.Generate()
            };
            return self;
        }
    }
}
