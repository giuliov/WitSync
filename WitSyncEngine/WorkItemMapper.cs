using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    class WorkItemMapper : SyncContext
    {
        private MapperFunctions functions;

        internal WorkItemMapper(SyncContext context)
            : base(context)
        {
            functions = new MapperFunctions(this.EventSink, this.SourceProjectName, this.DestinationProjectName);

            // make sure we have needed indexes
            this.Mapping.RebuildMappingIndexes();

            // explicit default
            this.UseEditableProperty = false;
            this.OpenTargetWorkItem = false;
            this.PartialOpenTargetWorkItem = false;
        }

        public bool UseEditableProperty { get; set; }
        public bool OpenTargetWorkItem { get; set; }
        public bool PartialOpenTargetWorkItem { get; set; }

        internal void MapWorkItems(QueryResult sourceResult, QueryResult destResult, out List<WorkItem> newWorkItems, out List<WorkItem> updatedWorkItems)
        {
            // results
            newWorkItems = new List<WorkItem>();
            updatedWorkItems = new List<WorkItem>();

            var sourceWorkItems = sourceResult.WorkItems.Values;
            var destWorkItems = destResult.WorkItems.Values;

            // First pass: workitems
            foreach (var sourceWorkItem in sourceWorkItems)
            {
                try
                {
                    if (IsNewWorkItemId(sourceWorkItem.Id))
                    {
                        var newWI = NewWorkItem(sourceWorkItem);
                        if (newWI != null)
                        {
                            newWorkItems.Add(newWI);
                            this.Index.Add(sourceWorkItem.Id, newWI);
                        }
                    }
                    else
                    {
                        var oldWI = this.Index.GetWorkItemFromSourceId(sourceWorkItem.Id);
                        updatedWorkItems.Add(UpdateWorkItem(sourceWorkItem, oldWI));
                    }//if
                }
                catch (Exception ex)
                {
                    this.EventSink.ExceptionWhileMappingWorkItem(ex, sourceWorkItem);
                }
            }//for
        }

        private bool IsNewWorkItemId(int sourceId)
        {
            return !this.Index.IsSourceIdPresent(sourceId);
        }

        protected virtual WorkItem NewWorkItem(WorkItem source)
        {
            this.EventSink.MakingNewWorkItem(source);

            var map = this.Mapping.FindWorkItemTypeMapping(source.Type.Name);
            if (map == null)
            {
                this.EventSink.NoWorkItemTypeMapping(source);
                return null;
            }
            var targetType = this.DestinationStore.Projects[this.DestinationProjectName].WorkItemTypes[map.DestinationType];

            WorkItem target = new WorkItem(targetType);

            if (!this.Mapping.HasIndex)
            {
                target.Fields[map.IDField.Destination].Value = source.Id;
            }

            SetWorkItemFields(source, map, target);
            SetAttachments(source, map, target);

            Validate(target);

            return target;
        }

        protected virtual WorkItem UpdateWorkItem(WorkItem source, WorkItem target)
        {
            this.EventSink.UpdatingExistingWorkItem(source,target);

            var map = this.Mapping.FindWorkItemTypeMapping(source.Type.Name);
            Debug.Assert(map.DestinationType == target.Type.Name);
            if (!this.Mapping.HasIndex)
            {
                Debug.Assert(target.Fields[map.IDField.Destination].Value.ToString() == source.Id.ToString());
            }

            SetWorkItemFields(source, map, target);
            SetAttachments(source, map, target);

            Validate(target);

            this.EventSink.ExistingWorkItemUpdated(source, target);
            return target;
        }

        Dictionary<WorkItemMap, FieldCopier> copiers = new Dictionary<WorkItemMap, FieldCopier>();

        private FieldCopier GetCopier(WorkItemType sourceType, WorkItemMap map, WorkItemType targetType)
        {
            if (copiers.ContainsKey(map))
                return copiers[map];

            var copier = new FieldCopier(this.Mapping, this.functions, this.UseEditableProperty, sourceType, map, targetType, this.EventSink);
            //cache
            copiers[map] = copier;
            return copier;
        }

        protected virtual void SetWorkItemFields(WorkItem source, WorkItemMap map, WorkItem target)
        {
            var copier = GetCopier(source.Type, map, target.Type);

            if (this.OpenTargetWorkItem)
            {
                // force load and edit mode
                target.Open();
            } else if (this.PartialOpenTargetWorkItem)
            {
                // force load and edit mode
                target.PartialOpen();
            }

            copier.Copy(source, target, this.EventSink);
        }

        private bool CanAssign(Field fromField, Field toField)
        {
            if (this.UseEditableProperty)
            {
                // set only when changed
                // and only if writable field
                return toField.IsEditable && toField.Value != fromField.Value;
            }
            else
            {
                if (toField.IsComputed)
                    return false;
                if (toField.ReferenceName == "System.CreatedBy")
                    return false;
                return toField.Value != fromField.Value;
            }
        }

        private void SetAttachments(WorkItem source, WorkItemMap map, WorkItem target)
        {
            // TODO check SourceStore.MaxBulkUpdateBatchSize vs DestinationStore.MaxBulkUpdateBatchSize

            if (map.SyncAttachments && source.AttachedFileCount > 0)
            {
                bool matchFound = false;
                // see http://stackoverflow.com/questions/3507939/how-can-i-add-an-attachment-via-the-sdk-to-a-work-item-without-using-a-physical
                foreach (Attachment sourceAttachment in source.Attachments)
                {
                    foreach (Attachment targetAttachment in target.Attachments)
                    {
                        if (targetAttachment.Name == sourceAttachment.Name
                            && targetAttachment.Length == targetAttachment.Length)
                        {
                            matchFound = true;
                            break;
                        }
                    }

                    if (matchFound)
                    {
                        this.EventSink.Trace("Found attachment '{0}' with same name and lenght: skipping.", sourceAttachment.Name);
                    }
                    else
                    {
                        // not found
                        string tempFile = DownloadAttachment(sourceAttachment);
                        Attachment newAttachment = new Attachment(tempFile, sourceAttachment.Comment);
                        target.Attachments.Add(newAttachment);
                    }
                }//for
            }//if
        }

        private string DownloadAttachment(Attachment sourceAttachment)
        {
            var wc = new System.Net.WebClient();
            var cred = this.SourceConnection.Credential;
            if (cred != null
                && !string.IsNullOrWhiteSpace(cred.UserName))
            {
                wc.Credentials = this.SourceConnection.Credential;
                wc.UseDefaultCredentials = false;
            }
            else
            {
                wc.UseDefaultCredentials = true;
            }

            // two attachments may have the same name, so we generate a unique path (suboptimal)
            string tempFolder = Path.Combine(GetTemporaryAttachmentFolder(), sourceAttachment.FileGuid);
            Directory.CreateDirectory(tempFolder);
            string tempFile = Path.Combine(tempFolder, sourceAttachment.Name);

            wc.DownloadFile(sourceAttachment.Uri, tempFile);
            return tempFile;
        }

        private bool Validate(WorkItem workItem)
        {
            var result = workItem.Validate();
            foreach (Field item in result)
            {
                this.EventSink.ValidationError(item);
            }
            return result.Count == 0;
        }

        internal void CleanUp()
        {
            if (Directory.Exists(GetTemporaryAttachmentFolder()))
            {
                // cleanup attachment temp
                Directory.Delete(GetTemporaryAttachmentFolder(), true);
            }
        }

        private string GetTemporaryAttachmentFolder()
        {
            return Path.Combine(Path.GetTempPath(), "WitSyncAttachments");
        }
    }
}
