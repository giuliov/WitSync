using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            Validate(target);

            this.EventSink.ExistingWorkItemUpdated(source, target);
            return target;
        }

        protected virtual void SetWorkItemFields(WorkItem source, WorkItemMap map, WorkItem target)
        {
            if (this.OpenTargetWorkItem)
            {
                // force load and edit mode
                target.Open();
            } else if (this.PartialOpenTargetWorkItem)
            {
                // force load and edit mode
                target.PartialOpen();
            }

            foreach (Field fromField in source.Fields)
            {
                Debug.WriteLine("Source field '{0}' has value '{1}'", fromField.Name, fromField.Value);

                var rule = map.FindFieldRule(fromField.Name);
                if (rule == null)
                {
                    // if no rule -> skip field
                    this.EventSink.NoRuleFor(source, fromField.Name);
                    continue;
                }
                string targetFieldName
                    = rule.IsWildcard
                    ? fromField.Name : rule.Destination;
                // good source with destination?
                if (fromField.IsValid
                    && !string.IsNullOrWhiteSpace(rule.Destination)
                    && target.Fields.Contains(targetFieldName))
                {
                    var toField = target.Fields[targetFieldName];
                    if (CanAssign(fromField, toField))
                    {
                        if (rule.IsWildcard)
                        {
                            toField.Value = fromField.Value;
                        }
                        else if (!string.IsNullOrWhiteSpace(rule.Set)) 
                        {
                            // fixed value
                            switch (toField.FieldDefinition.FieldType)
                            {
                                // TODO source field is not needed
                                // Parse always succeeds, as value is already validated by Checker
                                case FieldType.Boolean:
                                    toField.Value = bool.Parse(rule.Set);
                                    break;
                                case FieldType.DateTime:
                                    toField.Value = DateTime.Parse(rule.Set);
                                    break;
                                case FieldType.Double:
                                    toField.Value = double.Parse(rule.Set);
                                    break;
                                case FieldType.Guid:
                                    toField.Value = Guid.Parse(rule.Set);
                                    break;
                                case FieldType.Integer:
                                    toField.Value = int.Parse(rule.Set);
                                    break;
                                default:
                                    toField.Value = rule.Set;
                                    break;
                            }//switch
                        }
                        else if (!string.IsNullOrWhiteSpace(rule.Translate))
                        {
                            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                            // TODO optimize
                            var translatorMethod = functions.GetType().GetMethod(rule.Translate, flags);
                            if (translatorMethod == null)
                            {
                                this.EventSink.TranslatorFunctionNotFoundUsingDefault(rule);
                                // default: no translation
                                toField.Value = fromField.Value;
                            }
                            else
                            {
                                toField.Value = translatorMethod.Invoke(functions, new object[] { rule, map, this.Mapping, fromField.Value });
                            }
                        }
                        else
                        {
                            this.EventSink.InvalidRule(rule);
                            // crossing fingers
                            toField.Value = fromField.Value;
                        }//if
                    }//if
                }//if has dest
            }//for fields
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

        private bool Validate(WorkItem workItem)
        {
            var result = workItem.Validate();
            foreach (Field item in result)
            {
                this.EventSink.ValidationError(item);
            }
            return result.Count == 0;
        }
    }
}
