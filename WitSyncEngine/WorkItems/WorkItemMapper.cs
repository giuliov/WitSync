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
    partial class WorkItemMapper : SyncContext
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

            ValidateAndRollback(target, map.RollbackValidationErrors);

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

            ValidateAndRollback(target, map.RollbackValidationErrors);

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

        private void ValidateAndRollback(WorkItem workItem, bool rollbackOnFailure)
        {
            if (rollbackOnFailure)
            {
                var result = workItem.Validate();
                foreach (Field item in result)
                {
                    if (item.ReferenceName == "System.State"
                        && this.Mapping.Mode.HasFlag(WorkItemsStageConfiguration.Modes.CreateThenUpdate)
                        && item.Status == FieldStatus.InvalidListValue)
                        // skip this error
                        continue;
                    // rollback
                    this.EventSink.RollbackOnValidationError(item);
                    item.Value = item.OriginalValue;
                }
                Validate(workItem);
            }
            else
            {
                Validate(workItem);
            }
        }

        private bool Validate(WorkItem workItem)
        {
            var result = workItem.Validate();
            foreach (Field item in result)
            {
                if (item.ReferenceName == "System.State"
                    && this.Mapping.Mode.HasFlag(WorkItemsStageConfiguration.Modes.CreateThenUpdate)
                    && item.Status == FieldStatus.InvalidListValue)
                    // skip this error
                    continue;
                this.EventSink.ValidationError(item);
            }
            return result.Count == 0;
        }

        internal void CleanUp()
        {
            CleanUpAttachments();
        }
    }
}
