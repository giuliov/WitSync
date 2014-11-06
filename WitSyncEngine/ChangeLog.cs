using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WitSync
{
    public class ChangeLog
    {
        internal ChangeLog() { }

        List<ChangeEntry> entries = new List<ChangeEntry>();

        internal void AddEntry(ChangeEntry entry)
        {
            entries.Add(entry);
        }

        public System.Collections.Generic.IEnumerable<ChangeEntry> GetEntries()
        {
            foreach (var entry in entries)
            {
                yield return entry;
            }
        }

        public void Append(ChangeLog x)
        {
            this.entries.AddRange(x.entries);
        }

        public int Count { get { return this.entries.Count; } }
    }

    public class ChangeEntry
    {
        protected string source;
        protected string sourceId;
        protected string targetId;
        protected string changeType;

        protected ChangeEntry(string source, string sourceId, string targetId, string changeType)
        {
            this.source = source;
            this.sourceId = sourceId;
            this.targetId = targetId;
            this.changeType = changeType;
        }

        public string Source { get { return this.source; } }
        public string SourceId { get { return this.sourceId; } }
        public string TargetId { get { return this.targetId; } }
        public string ChangeType { get { return this.changeType; } }
    }
}
