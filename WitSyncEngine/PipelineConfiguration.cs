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
        public GlobalListsStageConfiguration GlobalListStage { get; set; }
        public WorkItemsStageConfiguration WorkItemsStage { get; set; }

        internal StageConfiguration GetStageConfiguration(PipelineStage stage)
        {
            if (stage.GetType() == typeof(AreasAndIterationsStage)
                || stage.GetType() == typeof(AreasStage)
                || stage.GetType() == typeof(IterationsStage))
            {
                this.AreasAndIterationsStage.TestOnly = this.TestOnly;
                return this.AreasAndIterationsStage;
            }
            if (stage.GetType() == typeof(GlobalListsStage))
            {
                this.GlobalListStage.TestOnly = this.TestOnly;
                return this.GlobalListStage;
            }
            if (stage.GetType() == typeof(WorkItemsStage))
            {
                this.WorkItemsStage.TestOnly = this.TestOnly;
                return this.WorkItemsStage;
            }
            // catch design errors
            throw new ApplicationException("Forgot to add PipelineStage in PipelineConfiguration.GetStageConfiguration, please correct.");
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
                GlobalListStage = GlobalListsStageConfiguration.Generate(),
                WorkItemsStage = WorkItemsStageConfiguration.Generate()
            };
            return self;
        }
    }
}
