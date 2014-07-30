using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WitSync
{
    public class IdMap
    {
        public int OriginatingId;
        public int TargetId;
    }

    public class WitMappingIndex
    {
        private Dictionary<int, int> ForwardIndex = new Dictionary<int, int>();
        private Dictionary<int, int> BackwardIndex = new Dictionary<int, int>();
        private Dictionary<int, WorkItem> TargetIndex = new Dictionary<int, WorkItem>();

        internal void Clear()
        {
            ForwardIndex = new Dictionary<int, int>();
            BackwardIndex = new Dictionary<int, int>();
            TargetIndex = new Dictionary<int, WorkItem>();
        }

        public void Add(int originatingId, WorkItem targetWorkItem)
        {
            int targetId = targetWorkItem.TemporaryId;
            this.ForwardIndex.Add(originatingId, targetId);
            this.BackwardIndex.Add(targetId, originatingId);
            this.TargetIndex.Add(targetId, targetWorkItem);
        }

        public void Update(int originatingId, WorkItem updatedWorkItem)
        {
            int newTargetId = updatedWorkItem.Id;
            int oldTargetId = this.ForwardIndex[originatingId];
            if (newTargetId != oldTargetId)
            {
                this.ForwardIndex[originatingId] = newTargetId;
                this.BackwardIndex.Remove(oldTargetId);
                this.TargetIndex.Remove(oldTargetId);
                this.BackwardIndex.Add(newTargetId, originatingId);
                this.TargetIndex.Add(newTargetId, updatedWorkItem);
            }//if
        }

        public int GetIdFromSourceId(int originatingId)
        {
            return ForwardIndex[originatingId];
        }

        public WorkItem GetWorkItemFromSourceId(int originatingId)
        {
            return TargetIndex[ ForwardIndex[originatingId] ];
        }

        internal bool IsSourceIdPresent(int sourceId)
        {
            return ForwardIndex.ContainsKey(sourceId);
        }
    }
}
