using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    public class MappingBase { }

    public class SyncPipeline
    {
        public SyncPipeline(TfsConnection source, TfsConnection dest, IEngineEvents eventHandler)
        { 
            sourceConn = source;
            destConn = dest;
            eventSink = eventHandler;
        }

        List<Func<EngineBase>> stageBuilders = new List<Func<EngineBase>>();

        public void AddStage<TEngine>(Func<TEngine> engineBuilder)
            where TEngine : EngineBase
        {
            stageBuilders.Add(engineBuilder);
        }

        protected TfsConnection sourceConn;
        protected TfsConnection destConn;
        protected IEngineEvents eventSink;
        protected int syncErrors = 0;

        public int Execute(bool testOnly)
        {
            try
            {
                // connect
                eventSink.ConnectingSource(sourceConn);
                sourceConn.Connect();
                eventSink.SourceConnected(sourceConn);
                eventSink.ConnectingDestination(destConn);
                destConn.Connect();
                eventSink.DestinationConnected(destConn);

                // execute the stages in order
                eventSink.SyncStarted();

                foreach (var stageBuilder in stageBuilders)
                {
                    var stage = stageBuilder();
                    int stageErrors = stage.Execute(testOnly);
                }//for

                eventSink.SyncFinished(syncErrors);
                return syncErrors;
            }
            catch (Exception ex)
            {
                eventSink.InternalError(ex);                
                return -99;
            }//try
        }
    }
}
