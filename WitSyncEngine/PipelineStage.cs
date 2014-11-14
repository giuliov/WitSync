using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptionsBase = System.Int32;

namespace WitSync
{
    // represent a phase in the pipeline
    abstract public class PipelineStage
    {
        protected TfsConnection sourceConn;
        protected TfsConnection destConn;
        protected IEngineEvents eventSink;
        protected int saveErrors = 0;
        private ChangeLog changeLog = new ChangeLog();

        public PipelineStage(TfsConnection source, TfsConnection dest, IEngineEvents eventHandler)
        {
            sourceConn = source;
            destConn = dest;
            eventSink = eventHandler;
        }

        abstract public int Prepare(bool testOnly);
        abstract public int Execute(bool testOnly);

        public ChangeLog ChangeLog { get { return changeLog; } }

        public virtual string Name { get { return this.GetType().Name.Replace("SyncEngine",""); } }
    }
}
