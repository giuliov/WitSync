using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptionsBase = System.Int32;

namespace WitSync
{
    abstract public class EngineBase
    {
        protected TfsConnection sourceConn;
        protected TfsConnection destConn;
        protected IEngineEvents eventSink;
        protected int saveErrors = 0;

        public EngineBase(TfsConnection source, TfsConnection dest, IEngineEvents eventHandler)
        {
            sourceConn = source;
            destConn = dest;
            eventSink = eventHandler;
        }

        abstract public int Prepare(bool testOnly);
        abstract public int Execute(bool testOnly);

        public virtual string Name { get { return this.GetType().Name; } }
    }
}
