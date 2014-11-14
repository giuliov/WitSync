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
    internal class WorkItemChangeEntry : SuccessEntry
    {
        internal enum Change { New, Update }

        internal WorkItemChangeEntry(int source, int target, Change change)
            : base("WorkItem", source.ToString(), target.ToString(), change.ToString())
            {}
    }

    internal class WorkItemFailureEntry : FailureEntry
    {
        internal WorkItemFailureEntry(int source, int target, string message)
            : base("WorkItem", source.ToString(), target.ToString(), message)
            {}
    }

    

    public class WorkItemsSyncEngine : EngineBase
    {
        [Flags]
        public enum EngineOptions
        {
            TestOnly = 0x1,
            BypassWorkItemStoreRules = 0x2,
            UseEditableProperty = 0x4,
            OpenTargetWorkItem = 0x8,
            PartialOpenTargetWorkItem = 0x10,
            CreateThenUpdate = 0x20,
        }

        public WorkItemsSyncEngine(TfsConnection source, TfsConnection dest, IEngineEvents eventHandler)
            : base(source, dest, eventHandler)
        {
            //no-op
        }

        public Func<ProjectMapping> MapGetter { get; set; }
        public EngineOptions Options { set { this.options = value; } }

        protected ProjectMapping mapping;
        protected EngineOptions options;

        protected WorkItemStore sourceWIStore;
        protected WorkItemStore destWIStore;
        internal ProjectMappingChecker checker;

        public override int Prepare(bool testOnly)
        {
            mapping = MapGetter();
            if (mapping == null)
                // SetDefaults will fill this in
                mapping = new ProjectMapping();

            sourceWIStore = sourceConn.Collection.GetService<WorkItemStore>();
            if (options.HasFlag(EngineOptions.BypassWorkItemStoreRules))
            {
                eventSink.BypassingRulesOnDestinationWorkItemStore(destConn);
                // this will turn off validation!
                destWIStore = new WorkItemStore(destConn.Collection, WorkItemStoreFlags.BypassRules);
            }
            else
            {
                destWIStore = destConn.Collection.GetService<WorkItemStore>();
            }//if

            mapping.SetDefaults(sourceConn, sourceWIStore, destConn, destWIStore);

            eventSink.DumpMapping(mapping);

            checker = new ProjectMappingChecker(sourceWIStore, sourceConn.ProjectName, destWIStore, destConn.ProjectName, eventSink);
            checker.AgnosticCheck(mapping);
            if (!checker.Passed)
                // abort
                return checker.ErrorCount;

            return 0;
        }

        public override int Execute(bool testOnly)
        {
            var sourceRunner = new QueryRunner(sourceWIStore, sourceConn.ProjectName);
            eventSink.ExecutingSourceQuery(mapping.SourceQuery, sourceConn);
            var sourceResult = sourceRunner.RunQuery(mapping.SourceQuery);
            if (sourceResult == null)
            {
                eventSink.SourceQueryNotFound(mapping.SourceQuery);
                return 3;
            }

            var destRunner = new QueryRunner(destWIStore, destConn.ProjectName);
            eventSink.ExecutingDestinationQuery(mapping.DestinationQuery, destConn);
            var destResult = destRunner.RunQuery(mapping.DestinationQuery);
            if (destResult == null)
            {
                eventSink.DestinationQueryNotFound(mapping.DestinationQuery);
                return 4;
            }

            // use query data for more thorough checks
            checker.Check(sourceResult, mapping, destResult);
            if (!checker.Passed)
                // abort
                return checker.ErrorCount;


            // this needs also connection to target, better after query execution, so we have warm caches
            var index = BuildIndex(destWIStore, destResult.WorkItems.Values, mapping);

            var context = new SyncContext(sourceConn, sourceWIStore, sourceConn.ProjectName, destWIStore, destConn.ProjectName, mapping, index, eventSink);

            var workItemMapper = new WorkItemMapper(context);
            // configure options
            workItemMapper.UseEditableProperty = options.HasFlag(EngineOptions.UseEditableProperty);
            workItemMapper.OpenTargetWorkItem = options.HasFlag(EngineOptions.OpenTargetWorkItem);
            workItemMapper.PartialOpenTargetWorkItem = options.HasFlag(EngineOptions.PartialOpenTargetWorkItem);

            List<WorkItem> newWorkItems;
            List<WorkItem> updatedWorkItems;
            workItemMapper.MapWorkItems(sourceResult, destResult, out newWorkItems, out updatedWorkItems);

            // from http://social.msdn.microsoft.com/Forums/vstudio/en-US/0cbc378b-09ad-4899-865d-b418aecb8375/work-item-links-error-message-unexplained
            // "It happens when you add a link when you are creating a new work item. If you add the link after the new work item is saved then it works OK."
            eventSink.SavingWorkItems(newWorkItems, updatedWorkItems);
            var validWorkItems = new List<WorkItem>();
            if (options.HasFlag(EngineOptions.CreateThenUpdate))
            {
                // uncommon path
                eventSink.UsingThreePassSavingAlgorithm();
                SaveWorkItems3Passes(mapping, index, testOnly, destWIStore, newWorkItems, updatedWorkItems, validWorkItems);
                // multi-pass records the same WI object multiple times
                validWorkItems = validWorkItems.DistinctBy(x => x.Id, null).ToList();
            }
            else
            {
                // normal path
                var changedWorkItems = newWorkItems.Concat(updatedWorkItems).ToList();
                var savedWorkItems = SaveWorkItems(mapping, index, destWIStore, changedWorkItems, testOnly);
                validWorkItems.AddRange(savedWorkItems);
            }//if

            workItemMapper.CleanUp();

            var linkMapper = new LinkMapper(context);
            var changedLinks = linkMapper.MapLinks(sourceResult.WorkItems.Values, validWorkItems);

            eventSink.SavingLinks(changedLinks, validWorkItems);
            SaveLinks(mapping, index, destWIStore, validWorkItems, testOnly);

            return saveErrors;
        }

        private void SaveWorkItems3Passes(ProjectMapping mapping, WitMappingIndex index, bool testOnly, WorkItemStore destWIStore, List<WorkItem> newWorkItems, List<WorkItem> updatedWorkItems, List<WorkItem> validWorkItems)
        {
            eventSink.SaveFirstPassSavingNewWorkItems(newWorkItems);
            //HACK: force all new workitems to the Initial state
            var realStates = new Dictionary<WorkItem, string>();
            newWorkItems.ForEach(w =>
            {
                realStates.Add(w, w.State);
                w.State = GetInitialState(w);
            });
            validWorkItems.AddRange(SaveWorkItems(mapping, index, destWIStore, newWorkItems, testOnly));

            eventSink.SaveSecondPassUpdatingNewWorkItemsState(newWorkItems);
            // and now update the no-more-new WI with the real state
            newWorkItems.ForEach(w =>
            {
                w.State = realStates[w];
            });
            validWorkItems.AddRange(SaveWorkItems(mapping, index, destWIStore, newWorkItems, testOnly));

            eventSink.SaveThirdPassSavingUpdatedWorkItems(updatedWorkItems);
            // existing WI do not need tricks
            validWorkItems.AddRange(SaveWorkItems(mapping, index, destWIStore, updatedWorkItems, testOnly));
        }

        Dictionary<WorkItemType, string> initialStates = new Dictionary<WorkItemType, string>();

        private string GetInitialState(WorkItem w)
        {
            var t = w.Type;
            string state;
            if (!initialStates.TryGetValue(t, out state))
            {
                state = t.NewWorkItem().State;
                initialStates.Add(t, state);
            }
            return state;
        }

        private List<WorkItem> SaveWorkItems(ProjectMapping mapping, WitMappingIndex index, WorkItemStore destWIStore, List<WorkItem> changedWorkItems, bool testOnly)
        {
            var failedWorkItems = new List<WorkItem>();
            if (testOnly)
            {
                eventSink.SavingSkipped();
            }
            else
            {
                var errors = destWIStore.BatchSave(changedWorkItems.ToArray(), SaveFlags.MergeAll);
                failedWorkItems = ExamineSaveErrors(errors, index);
            }//if

            var validWorkItems = changedWorkItems.Except(failedWorkItems);
            // some succeeded: their Ids could be changed, so refresh index
            if (!testOnly)
            {
                UpdateIndex(index, validWorkItems, mapping);
                foreach (var item in validWorkItems)
                {
                    this.ChangeLog.AddEntry(
                        new WorkItemChangeEntry(
                            index.GetSourceIdFromTargetId(item.Id),
                            item.Id,
                            item.IsNew ? WorkItemChangeEntry.Change.New : WorkItemChangeEntry.Change.Update));
                }//for
            }//if

            return validWorkItems.ToList();
        }

        private void SaveLinks(ProjectMapping mapping, WitMappingIndex index, WorkItemStore destWIStore, IEnumerable<WorkItem> changedWorkItems, bool testOnly)
        {
            if (testOnly)
            {
                eventSink.SavingSkipped();
            }
            else
            {
                var errors = destWIStore.BatchSave(changedWorkItems.ToArray(), SaveFlags.MergeAll);
                ExamineSaveErrors(errors, index);
            }//if
        }

        private List<WorkItem> ExamineSaveErrors(BatchSaveError[] errors, WitMappingIndex index)
        {
            // log what failed
            var failedWorkItems = new List<WorkItem>();
            foreach (var err in errors)
            {
                failedWorkItems.Add(err.WorkItem);
                eventSink.SaveError(err.Exception, err.WorkItem);
                foreach (Field f in err.WorkItem.Fields)
                {
                    if (!f.IsValid)
                    {
                        eventSink.SaveErrorInvalidField(err.WorkItem, f);
                    }
                }//for
                saveErrors++;

                // ChangeLog also
                int targetId = err.WorkItem.IsNew ? err.WorkItem.TemporaryId : err.WorkItem.Id;
                this.ChangeLog.AddEntry(
                    new WorkItemFailureEntry(
                        index.GetSourceIdFromTargetId(targetId),
                        targetId,
                        err.Exception.Message));
                
            }//for
            return failedWorkItems;
        }

        private WitMappingIndex BuildIndex(WorkItemStore destWIStore, IEnumerable<WorkItem> existingTargetWorkItems, ProjectMapping mapping)
        {
            var index = new WitMappingIndex();
            if (mapping.HasIndex)
            {
                if (!System.IO.File.Exists(mapping.IndexFile))
                {
                    //HACK on first run the file cannot exists, so create an empty one
                    index = WitMappingIndex.CreateEmpty(mapping.IndexFile);
                }
                else
                {
                    index = WitMappingIndex.Load(mapping.IndexFile, destWIStore);
                }//if
            }
            else
            {
                index.Clear();
                foreach (var targetWI in existingTargetWorkItems)
                {
                    var originatingFieldMap = mapping.FindIdFieldForTargetWorkItemType(targetWI.Type.Name);
                    var v = targetWI.Fields[originatingFieldMap.Destination].Value;
                    // could be that the destination exists, with no origin (e.g. manual intervention)
                    int originatingId = (int) (v ?? 0);
                    index.Add(originatingId, targetWI);
                }//for
            }//if

            return index;
        }

        private void UpdateIndex(WitMappingIndex index, IEnumerable<WorkItem> updatedWorkItems, ProjectMapping mapping)
        {
            if (mapping.HasIndex)
            {
                index.Update(updatedWorkItems);
                index.Save(mapping.IndexFile);
            }
            else
            {
                foreach (var dst in updatedWorkItems)
                {
                    var originatingFieldMap = mapping.FindIdFieldForTargetWorkItemType(dst.Type.Name);
                    int originatingId = (int)dst.Fields[originatingFieldMap.Destination].Value;
                    index.Update(originatingId, dst);
                }//for
            }
        }
    }
}
