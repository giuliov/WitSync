using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    internal class SyncContext
    {
        private TfsConnection sourceConnection;
        private WorkItemStore sourceWIStore;
        private string sourceProjectName;
        private WorkItemStore destWIStore;
        private string destProjectName;
        private ProjectMapping mapping;
        private WitMappingIndex index;
        private IEngineEvents eventSink;

        internal SyncContext(TfsConnection sourceConnection, WorkItemStore sourceWIStore, string sourceProjectName, WorkItemStore destWIStore, string destProjectName, ProjectMapping mapping, WitMappingIndex index, IEngineEvents eventSink)
        {
            this.sourceConnection = sourceConnection;
            this.sourceWIStore = sourceWIStore;
            this.sourceProjectName = sourceProjectName;
            this.destWIStore = destWIStore;
            this.destProjectName = destProjectName;
            this.mapping = mapping;
            this.index = index;
            this.eventSink = eventSink;
        }

        internal SyncContext(SyncContext rhs)
        {
            this.sourceConnection = rhs.sourceConnection;
            this.sourceWIStore = rhs.sourceWIStore;
            this.sourceProjectName = rhs.sourceProjectName;
            this.destWIStore = rhs.destWIStore;
            this.destProjectName = rhs.destProjectName;
            this.mapping = rhs.mapping;
            this.index = rhs.index;
            this.eventSink = rhs.eventSink;
        }

        public TfsConnection SourceConnection { get { return this.sourceConnection; } }
        public WorkItemStore SourceStore { get { return this.sourceWIStore; } }
        public string SourceProjectName { get { return this.sourceProjectName; } }
        public WorkItemStore DestinationStore { get { return this.destWIStore; } }
        public string DestinationProjectName { get { return this.destProjectName; } }
        public ProjectMapping Mapping { get { return this.mapping; } }
        public WitMappingIndex Index { get { return this.index; } }
        public IEngineEvents EventSink { get { return this.eventSink; } }
    }
}
