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

    public abstract class ChangeEntry
    {
        protected string source;
        protected string sourceId;
        protected bool succeeded;
        protected string targetId;
        protected string changeType;
        protected string message;

        protected ChangeEntry(string source, string sourceId, bool succeeded, string targetId, string changeType, string message)
        {
            this.source = source;
            this.sourceId = sourceId;
            this.succeeded = succeeded;
            this.targetId = targetId;
            this.changeType = changeType;
            this.message = message;
        }

        public string Source { get { return this.source; } }
        public string SourceId { get { return this.sourceId; } }
        public bool Succeeded { get { return this.succeeded; } }
        public string TargetId { get { return this.targetId; } }
        public string ChangeType { get { return this.changeType; } }
        public string Message { get { return this.message; } }
    }

    public abstract class SuccessEntry : ChangeEntry
    {
        protected SuccessEntry(string source, string sourceId, string targetId, string changeType)
            : base(source, sourceId, true, targetId, changeType, string.Empty)
        {
        }
    }

    public abstract class FailureEntry : ChangeEntry
    {
        protected FailureEntry(string source, string sourceId, string targetId, string message)
            : base(source, sourceId, false, targetId, string.Empty, message)
        {
        }
    }
}
