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
        PipelineConfiguration configuration;

        public SyncPipeline(PipelineConfiguration configuration, IEngineEvents eventHandler)
        {
            this.configuration = configuration;
            eventSink = eventHandler;
        }

        List<ConstructorInfo> stageBuilders = new List<ConstructorInfo>();
        List<PipelineStage> preparedStages = new List<PipelineStage>();

        public void AddStage<TStage>()
            where TStage : PipelineStage
        {
            AddStage(typeof(TStage));
        }
        public void AddStage(Type t)
        {
            var ctor = t.GetConstructor(new Type[] { typeof(TfsConnection), typeof(TfsConnection), typeof(IEngineEvents) });
            stageBuilders.Add(ctor);
        }

        protected TfsConnection sourceConn;
        protected TfsConnection destConn;
        protected IEngineEvents eventSink;
        protected int syncErrors = 0;
        protected ChangeLog changeLog = new ChangeLog();

        public ChangeLog ChangeLog { get { return changeLog; } }

        public int Execute()
        {
            try
            {
                MakeConnections();
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

        private void PrepareStages()
        {
            preparedStages.Clear();

            foreach (var stageBuilder in stageBuilders)
            {
                // create Stage object
                var stage = stageBuilder.Invoke(new object[] { sourceConn, destConn, eventSink }) as PipelineStage;
                int stageErrors = -1;
                try
                {
                    eventSink.PreparingStage(stage);
                    StageConfiguration config = configuration.GetStageConfiguration(stage);
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
                    StageConfiguration config = configuration.GetStageConfiguration(stage);
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
    }
}
