using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    public class SyncPipeline
    {
        public class StageInfo
        {
            public int Order { get; set; }
            public Type Type { get; set; }
            public string Name
            {
                get
                {
                    return this.Type.Name.Replace("Stage", "");
                }
            }
            public string ConfigurationProperty { get; set; }
            public StageConfiguration GetConfiguration(PipelineConfiguration parent)
            {
                string propName = this.ConfigurationProperty ?? this.Type.Name;
                return parent.GetType().GetProperty(propName).GetValue(parent) as StageConfiguration;
            }
        }//class

        static private StageInfo[] possibleStages = new StageInfo[] {
            new StageInfo() { Order = 10, Type = typeof(GlobalListsStage) },
            new StageInfo() { Order = 20, Type = typeof(AreasAndIterationsStage) },
            new StageInfo() { Order = 21, Type = typeof(AreasStage), ConfigurationProperty = "AreasAndIterationsStageConfiguration" },
            new StageInfo() { Order = 22, Type = typeof(IterationsStage), ConfigurationProperty = "AreasAndIterationsStageConfiguration" },
            new StageInfo() { Order = 30, Type = typeof(WorkItemsStage) },
        };

        private string DeAlias(string name)
        {
            switch (name)
            {
                case "Areas":
                    return "AreasAndIterations";
                case "Iterations":
                    return "AreasAndIterations";
                default:
                    return name;
            }
        }

        PipelineConfiguration configuration;

        List<PipelineStage> requestedStages = new List<PipelineStage>();
        List<PipelineStage> preparedStages = new List<PipelineStage>();

        protected TfsConnection sourceConn;
        protected TfsConnection destConn;
        protected IEngineEvents eventSink;
        protected int syncErrors = 0;
        protected ChangeLog changeLog = new ChangeLog();

        public ChangeLog ChangeLog { get { return changeLog; } }

        public SyncPipeline(PipelineConfiguration configuration, IEngineEvents eventHandler)
        {
            this.configuration = configuration;
            eventSink = eventHandler;
        }

        public int Execute()
        {
            try
            {
                MakeConnections();
                BuildPipeline();
                Connect();

                PrepareStages();

                // execute the stages in order
                eventSink.SyncStarted();

                ExecuteStages();

                eventSink.SyncFinished(syncErrors);
                return syncErrors;
            }
            catch (Exception ex)
            {
                eventSink.InternalError(ex);                
                return -99;
            }//try
        }

        private void BuildPipeline()
        {
            foreach (var info in possibleStages.OrderBy(x => x.Order))
            {
                if (configuration.PipelineStages.Contains(info.Name, StringComparer.InvariantCultureIgnoreCase))
                {
                    //build
                    var ctor = info.Type.GetConstructor(new Type[] { typeof(TfsConnection), typeof(TfsConnection), typeof(IEngineEvents) });
                    var stage = ctor.Invoke(new object[] { sourceConn, destConn, eventSink }) as PipelineStage;
                    requestedStages.Add(stage);
                }//if
            }//for

        }

        private void PrepareStages()
        {
            preparedStages.Clear();

            foreach (var stage in requestedStages)
            {
                int stageErrors = -1;
                try
                {
                    eventSink.PreparingStage(stage);
                    StageConfiguration config = GetStageConfiguration(stage);
                    stageErrors = stage.Prepare(config);
                    eventSink.StagePrepared(stage, stageErrors);
                    // only succeeded stages will be executed
                    preparedStages.Add(stage);
                }
                catch (Exception ex)
                {
                    if (configuration.StopPipelineOnFirstError)
                        throw;
                    else
                        eventSink.StagePreparationError(stage, ex);
                }//try
            }//for
        }

        private void MakeConnections()
        {
            sourceConn = new TfsConnection()
            {
                CollectionUrl = new Uri(configuration.SourceConnection.CollectionUrl),
                ProjectName = configuration.SourceConnection.ProjectName,
                Credential = new NetworkCredential(configuration.SourceConnection.User, configuration.SourceConnection.Password)
            };
            destConn = new TfsConnection()
            {
                CollectionUrl = new Uri(configuration.DestinationConnection.CollectionUrl),
                ProjectName = configuration.DestinationConnection.ProjectName,
                Credential = new NetworkCredential(configuration.DestinationConnection.User, configuration.DestinationConnection.Password)
            };
        }

        private void Connect()
        {
            // connect
            eventSink.ConnectingSource(sourceConn);
            sourceConn.Connect();
            eventSink.SourceConnected(sourceConn);
            eventSink.ConnectingDestination(destConn);
            destConn.Connect();
            eventSink.DestinationConnected(destConn);
        }

        private void ExecuteStages()
        {
            foreach (var stage in preparedStages)
            {
                int stageErrors = -1;
                try
                {
                    eventSink.ExecutingStage(stage);
                    StageConfiguration config = GetStageConfiguration(stage);
                    config.TestOnly = configuration.TestOnly;
                    stageErrors = stage.Execute(config);
                    eventSink.StageCompleted(stage, stageErrors);

                    syncErrors += stageErrors;
                }
                catch (Exception ex)
                {
                    if (configuration.StopPipelineOnFirstError)
                        throw;
                    else
                    {
                        eventSink.StageError(stage, ex);
                        syncErrors++;
                    }
                } finally {
                    this.changeLog.Append(stage.ChangeLog);
                }//try
            }//for
        }

        internal StageConfiguration GetStageConfiguration(PipelineStage stage)
        {
            var info = possibleStages.Where(x => x.Name == DeAlias(stage.Name)).FirstOrDefault();
            if (info != null)
                return info.GetConfiguration(configuration);
            // catch design errors
            throw new ApplicationException("Forgot to add PipelineStage in PipelineConfiguration.GetStageConfiguration, please correct.");
        }
    }
}
