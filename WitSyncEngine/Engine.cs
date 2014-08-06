﻿using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    public class WitSyncEngine
    {
        protected TfsConnection sourceConn;
        protected TfsConnection destConn;
        protected IEngineEvents eventSink;
        protected int saveErrors = 0;

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

        public WitSyncEngine(TfsConnection source, TfsConnection dest, IEngineEvents eventHandler)
        {
            sourceConn = source;
            destConn = dest;
            eventSink = eventHandler;
        }

        public int Sync(ProjectMapping mapping, WitMappingIndex index, bool testOnly, EngineOptions options)
        {
            saveErrors = 0;
            testOnly |= options.HasFlag(EngineOptions.TestOnly);

            eventSink.ConnectingSource(sourceConn);
            sourceConn.Connect();
            eventSink.SourceConnected(sourceConn);
            eventSink.ConnectingDestination(destConn);
            destConn.Connect();
            eventSink.DestinationConnected(destConn);

            var sourceWIStore = sourceConn.Collection.GetService<WorkItemStore>();
            WorkItemStore destWIStore = null;
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

            var sourceRunner = new QueryRunner(sourceWIStore, sourceConn.ProjectName);
            eventSink.ExecutingSourceQuery(mapping.SourceQuery, sourceConn);
            var sourceResult = sourceRunner.RunQuery(mapping.SourceQuery);

            var destRunner = new QueryRunner(destWIStore, destConn.ProjectName);
            eventSink.ExecutingDestinationQuery(mapping.DestinationQuery, destConn);
            var destResult = destRunner.RunQuery(mapping.DestinationQuery);

            var checker = new ProjectMappingChecker(sourceWIStore, sourceConn.ProjectName, destWIStore, destConn.ProjectName, eventSink);
            checker.Check(sourceResult, mapping, destResult);
            if (!checker.Passed)
                // abort
                return 2;

            BuildIndex(index, destResult.WorkItems.Values, mapping);

            var mapper = new Mapper(sourceWIStore, sourceConn.ProjectName, destWIStore, destConn.ProjectName, mapping, index, eventSink);
            // configure options
            mapper.UseEditableProperty = options.HasFlag(EngineOptions.UseEditableProperty);
            mapper.OpenTargetWorkItem = options.HasFlag(EngineOptions.OpenTargetWorkItem);
            mapper.PartialOpenTargetWorkItem = options.HasFlag(EngineOptions.PartialOpenTargetWorkItem);

            eventSink.SyncStarted();
            List<WorkItem> newWorkItems;
            List<WorkItem> updatedWorkItems;
            mapper.MapWorkItems(sourceResult, destResult, out newWorkItems, out updatedWorkItems);

            // from http://social.msdn.microsoft.com/Forums/vstudio/en-US/0cbc378b-09ad-4899-865d-b418aecb8375/work-item-links-error-message-unexplained
            // "It happens when you add a link when you are creating a new work item. If you add the link after the new work item is saved then it works OK."
            eventSink.SavingWorkItems(newWorkItems, updatedWorkItems);
            var validWorkItems = new List<WorkItem>();
            if (options.HasFlag(EngineOptions.CreateThenUpdate))
            {
                eventSink.UsingThreePassSavingAlgorithm();
                SaveWorkItems3Passes(mapping, index, testOnly, destWIStore, newWorkItems, updatedWorkItems, validWorkItems);
            }
            else
            {
                var changedWorkItems = newWorkItems.Concat(updatedWorkItems).ToList();
                validWorkItems.AddRange(SaveWorkItems(mapping, index, destWIStore, changedWorkItems, testOnly));
            }//if

            var changedLinks = mapper.MapLinks(sourceResult, destResult, validWorkItems);

            eventSink.SavingLinks(changedLinks, validWorkItems);
            SaveLinks(mapping, index, destWIStore, validWorkItems, testOnly);

            eventSink.SyncFinished(saveErrors);
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
                w.State = mapping.FindWorkItemTypeMapping(w.Type.Name).StateList.InitialStateOnDestination;
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
                failedWorkItems = ExamineSaveErrors(errors);
            }//if

            var validWorkItems = changedWorkItems.Where(candidate => !failedWorkItems.Contains(candidate));
            // some succeded: their Ids could be changed, so refresh index
            if (!testOnly)
            {
                UpdateIndex(index, changedWorkItems, mapping);
            }

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
                ExamineSaveErrors(errors);
            }//if
        }

        private List<WorkItem> ExamineSaveErrors(BatchSaveError[] errors)
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
            }//for
            return failedWorkItems;
        }

        private void BuildIndex(WitMappingIndex index, IEnumerable<WorkItem> existingTargetWorkItems, ProjectMapping mapping)
        {
            index.Clear();
            foreach (var targetWI in existingTargetWorkItems)
            {
                var originatingFieldMap = mapping.FindIdFieldForTargetWorkItemType(targetWI.Type.Name);
                int originatingId = (int)targetWI.Fields[originatingFieldMap.Destination].Value;
                index.Add(originatingId, targetWI);
            }//for
        }

        private void UpdateIndex(WitMappingIndex index, IEnumerable<WorkItem> updatedWorkItems, ProjectMapping mapping)
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
