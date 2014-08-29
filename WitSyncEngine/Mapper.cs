using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    class Mapper
    {
        protected IEngineEvents eventSink;
        private WorkItemStore sourceWIStore;
        private WorkItemStore destWIStore;
        private ProjectMapping mapping;
        private WitMappingIndex index;
        private string sourceProjectName;
        private string destProjectName;
        private MapperFunctions functions;

        internal Mapper(WorkItemStore sourceWIStore, string sourceProjectName, WorkItemStore destWIStore, string destProjectName, ProjectMapping mapping, WitMappingIndex index, IEngineEvents eventSink)
        {
            this.sourceWIStore = sourceWIStore;
            this.sourceProjectName = sourceProjectName;
            this.destWIStore = destWIStore;
            this.destProjectName = destProjectName;
            this.mapping = mapping;
            this.index = index;
            this.eventSink = eventSink;
            functions = new MapperFunctions(eventSink, sourceProjectName, destProjectName);
            // make sure we have needed indexes
            this.mapping.RebuildMappingIndexes();

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
                if (IsNewWorkItemId(sourceWorkItem.Id))
                {
                    var newWI = NewWorkItem(sourceWorkItem);
                    if (newWI != null)
                    {
                        newWorkItems.Add(newWI);
                        index.Add(sourceWorkItem.Id, newWI);
                    }
                }
                else
                {
                    var oldWI = index.GetWorkItemFromSourceId(sourceWorkItem.Id);
                    updatedWorkItems.Add(UpdateWorkItem(sourceWorkItem, oldWI));
                }//if
            }//for
        }

        private bool IsNewWorkItemId(int sourceId)
        {
            return !index.IsSourceIdPresent(sourceId);
        }

        protected virtual WorkItem NewWorkItem(WorkItem source)
        {
            this.eventSink.MakingNewWorkItem(source);

            var map = mapping.FindWorkItemTypeMapping(source.Type.Name);
            if (map == null)
            {
                eventSink.NoWorkItemTypeMapping(source);
                return null;
            }
            var targetType = destWIStore.Projects[this.destProjectName].WorkItemTypes[map.DestinationType];

            WorkItem target = new WorkItem(targetType);

            if (!mapping.HasIndex)
            {
                target.Fields[map.IDField.Destination].Value = source.Id;
            }

            SetWorkItemFields(source, map, target);

            Validate(target);

            return target;
        }

        protected virtual WorkItem UpdateWorkItem(WorkItem source, WorkItem target)
        {
            this.eventSink.UpdatingExistingWorkItem(source,target);

            var map = mapping.FindWorkItemTypeMapping(source.Type.Name);
            Debug.Assert(map.DestinationType == target.Type.Name);
            if (!mapping.HasIndex)
            {
                Debug.Assert(target.Fields[map.IDField.Destination].Value.ToString() == source.Id.ToString());
            }

            SetWorkItemFields(source, map, target);

            Validate(target);

            this.eventSink.ExistingWorkItemUpdated(source, target);
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
                    eventSink.NoRuleFor(source, fromField.Name);
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
                                eventSink.TranslatorFunctionNotFoundUsingDefault(rule);
                                // default: no translation
                                toField.Value = fromField.Value;
                            }
                            else
                            {
                                toField.Value = translatorMethod.Invoke(functions, new object[] { rule, map, mapping, fromField.Value });
                            }
                        }
                        else
                        {
                            eventSink.InvalidRule(rule);
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
                eventSink.ValidationError(item);
            }
            return result.Count == 0;
        }

        internal List<WorkItemLink> MapLinks(QueryResult sourceResult, QueryResult destResult, IEnumerable<WorkItem> validWorkItems)
        {
            var changedLinks = new List<WorkItemLink>();

            // get the link type for hierarchical relationships
            var parentChildlinkType = sourceWIStore.WorkItemLinkTypes[CoreLinkTypeReferenceNames.Hierarchy];
            var linkTypeIdToMatch = parentChildlinkType.ForwardEnd.Id;
            // check that all source links are included
            foreach (var queryLink in sourceResult.Links)
            {
                // only Parent-Child 
                if (queryLink.LinkTypeId == linkTypeIdToMatch && queryLink.SourceId != linkTypeIdToMatch)
                {
                    this.eventSink.AnalyzingSourceLink(queryLink);

                    // Parent-Child: we must be sure that link.SourceId and link.TargetId have a link in destination project
                    int parentId = index.GetIdFromSourceId(queryLink.SourceId);
                    int childId = index.GetIdFromSourceId(queryLink.TargetId);
                    if (parentId > 0 && childId > 0)
                    {
                        // assume that if missing from query, then must be added
                        var match = destResult.Links.FirstOrDefault(l => l.LinkTypeId == linkTypeIdToMatch && l.SourceId == parentId && l.TargetId == childId);
                        if (match == default(WorkItemLinkInfo))
                        {
                            // not found
                            var parent = validWorkItems.Where(w => w.Id == parentId).FirstOrDefault();
                            Debug.Assert(parent != null);
                            var lte = parentChildlinkType.ForwardEnd;
                            var relationship = new WorkItemLink(lte, parentId, childId);
                            parent.WorkItemLinks.Add(relationship);
                            //track
                            changedLinks.Add(relationship);

                            this.eventSink.MakingNewLink(relationship);
                        }
                        else
                        {
                            this.eventSink.LinkExists(queryLink, match);
                        }
                    }
                    else
                    {
                        this.eventSink.TargetMissingForLink(queryLink, parentId, childId);
                    }
                }
            }//for

            return changedLinks;
        }
    }
}
