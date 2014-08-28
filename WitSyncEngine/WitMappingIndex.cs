using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace WitSync
{

    public class WitMappingIndex
    {
        private Dictionary<int, int> ForwardIndex;
        private Dictionary<int, int> BackwardIndex;
        private Dictionary<int, WorkItem> TargetIndex;

        public WitMappingIndex()
        {
            Clear();
        }

        public static WitMappingIndex CreateEmpty(string pathToDataFile)
        {
            var mapping = new WitMappingIndex();
            mapping.Save(pathToDataFile);
            return mapping;
        }

        public static WitMappingIndex Load(string pathToDataFile, WorkItemStore store)
        {
            using (var reader = new XmlTextReader(pathToDataFile))
            {
                var mapping = new WitMappingIndex();

                LoadIndex(reader, "ForwardIndex", mapping.ForwardIndex);
                LoadIndex(reader, "BackwardIndex", mapping.BackwardIndex);

                mapping.Rebuild(store);

                return mapping;
            }//using
        }

        private void Rebuild(WorkItemStore targetStore)
        {
            foreach (int targetId in this.BackwardIndex.Keys)
            {
                var targetWorkItem = targetStore.GetWorkItem(targetId);
                this.TargetIndex.Add(targetId, targetWorkItem);                
            }
        }

        public void Save(string pathToDataFile)
        {
            using (var writer = new XmlTextWriter(pathToDataFile, Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented; // indent the Xml so it's human readable

                writer.WriteStartDocument();
                writer.WriteComment("WitSync index file: maps source WorkItem IDs to target WorkItem IDs.");
                writer.WriteStartElement("MappingIndex");

                SaveIndex(writer, "ForwardIndex", ForwardIndex);
                SaveIndex(writer, "BackwardIndex", BackwardIndex);

                writer.WriteEndElement();
                writer.WriteEndDocument();

                writer.Flush();
            }//using
        }

        private static void LoadIndex(XmlTextReader reader, string indexName, Dictionary<int, int> index)
        {
            reader.ReadToFollowing(indexName);

            bool found = reader.ReadToFollowing("Map");
            while (found)
            {
                reader.ReadAttributeValue();
                int from = int.Parse(reader.GetAttribute("from"));
                int to = int.Parse(reader.GetAttribute("to"));
                index.Add(from, to);
                found = reader.ReadToNextSibling("Map");
            }
        }

        private void SaveIndex(XmlTextWriter writer, string indexName, Dictionary<int, int> index)
        {
            writer.WriteStartElement(indexName);
            foreach (KeyValuePair<int, int> item in index)
            {
                // sanity check: values *must* be positive
                if (item.Key > 0 && item.Value > 0)
                {
                    writer.WriteStartElement("Map");
                    writer.WriteAttributeString("from", item.Key.ToString());
                    writer.WriteAttributeString("to", item.Value.ToString());
                    writer.WriteEndElement();
                }//if
            }//for
            writer.WriteEndElement();
        }

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

        internal void Update(IEnumerable<WorkItem> updatedWorkItems)
        {
            // build temporary lookup
            var reverse = new Dictionary<WorkItem,int>();
            foreach (var item in this.TargetIndex)
            {
                reverse.Add(item.Value, item.Key);
            }//for

            foreach (var updatedWorkItem in updatedWorkItems)
            {
                int newTargetId = updatedWorkItem.Id;
                int oldTargetId = reverse[updatedWorkItem];
                int originatingId = this.BackwardIndex[oldTargetId];

                this.ForwardIndex[originatingId] = newTargetId;
                this.BackwardIndex.Remove(oldTargetId);
                this.TargetIndex.Remove(oldTargetId);
                this.BackwardIndex.Add(newTargetId, originatingId);
                this.TargetIndex.Add(newTargetId, updatedWorkItem);
            }
        }
    }
}
