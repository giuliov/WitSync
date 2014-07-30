using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    class EngineEventHandler : EventHandlerBase, IEngineEvents
    {
        public EngineEventHandler(bool verbose)
            : base(verbose)
        {
        }

        public void ConnectingSource(TfsConnection sourceConn)
        {
            this.Verbose("Connecting to source {0} as {1}.", sourceConn.CollectionUrl, GetUsername(sourceConn));
        }

        public void SourceConnected(TfsConnection sourceConn)
        {
            this.Info("Connected to source {0}\\{1}.", sourceConn.CollectionUrl, sourceConn.ProjectName);
        }

        public void ConnectingDestination(TfsConnection destConn)
        {
            this.Verbose("Connecting to destination {0} as {1}.", destConn.CollectionUrl, GetUsername(destConn));
        }

        public void DestinationConnected(TfsConnection destConn)
        {
            this.Info("Connected to source {0}\\{1}.", destConn.CollectionUrl, destConn.ProjectName);
        }

        protected string GetUsername(TfsConnection conn)
        {
            return string.IsNullOrWhiteSpace(conn.Username)
                // ? System.Threading.Thread.CurrentPrincipal.Identity.Name
                ? string.Format("{0}\\{1}", Environment.UserDomainName, Environment.UserName)
                : conn.Username;
        }

        public void SaveError(Exception exception, WorkItem workItem)
        {
            // error message has dot . at the end
            this.Error("While saving workitem #{1} '{2}': {0}", exception.Message, workItem.IsNew ? workItem.TemporaryId : workItem.Id, workItem.Title);
        }

        public void ExecutingSourceQuery(string queryName, TfsConnection sourceConn)
        {
            this.Info("Executing source query {2} on {0}\\{1}.", sourceConn.CollectionUrl, sourceConn.ProjectName, queryName);
        }

        public void ExecutingDestinationQuery(string queryName, TfsConnection destConn)
        {
            this.Info("Executing source query {2} on {0}\\{1}.", destConn.CollectionUrl, destConn.ProjectName, queryName);
        }

        protected DateTimeOffset syncStart;

        public void SyncStarted()
        {
            syncStart = DateTimeOffset.UtcNow;
            this.Info("Syncronization started.");
        }

        public void SyncFinished(int errors)
        {
            var elapsed = DateTimeOffset.UtcNow - syncStart;
            this.Info("Syncronization completed in {0} with {1} error(s).", elapsed, errors);
        }

        public void SavingWorkItems(List<WorkItem> newWorkItems, List<WorkItem> updatedWorkItems)
        {
            int add = newWorkItems.Count;
            int upd = updatedWorkItems.FindAll(w => w.IsDirty).Count;
            this.Info("Saving {0} workitems ({1} new, {2} updated).", add + upd, add, upd);
        }

        public void SavingLinks(List<WorkItemLink> changedLinks, List<WorkItem> validWorkItems)
        {
            // TODO there is more to show here
            int chg = validWorkItems.FindAll(w => w.IsDirty).Count;
            this.Info("Saving {0} links, or {1} workitems.", changedLinks.Count, chg);
        }

        public void SaveErrorInvalidField(WorkItem workItem, Field f)
        {
            this.Verbose("Field '{0}' has invalid value '{1}'.", f.Name, f.Value);
        }

        public void MakingNewWorkItem(WorkItem source)
        {
            this.Info("Making new Workitem for #{0} [{1}] '{2}'.", source.Id, source.Type.Name, source.Title);
        }

        public void UpdatingExistingWorkItem(WorkItem source, WorkItem target)
        {
            this.Info("Mapping source Workitem #{3} [{4}] '{5}' to target #{0} [{1}] '{2}'."
                , target.Id, target.Type.Name, target.Title
                , source.Id, source.Type.Name, source.Title);
        }

        public void ExistingWorkItemUpdated(WorkItem source, WorkItem target)
        {
            if (target.IsDirty)
            {
                this.Info("Workitem #{0} [{1}] '{2}' updated."
                    , target.Id, target.Type.Name, target.Title);
            }
            else
            {
                this.Info("No changes to Workitem #{0} [{1}] '{2}'."
                    , target.Id, target.Type.Name, target.Title);
            }
        }

        public void NoTargetState(WorkItemMap map, object state)
        {
            this.UniqueWarning("    State '{0}'\'{1}' not found in '{2}'.", map.SourceType, state, map.DestinationType);
        }

        public void NoWildcardAreaRule(ProjectMapping mapping, object sourceValue)
        {
            this.UniqueWarning("    No wildcard Area rule.");
        }

        public void AreaPathNotFoundUsingWildcardRule(ProjectMapping mapping, object sourceValue)
        {
            this.UniqueVerbose("    Area path '{0}' not found: using wildcard rule.", sourceValue);
        }

        public void NoWildcardIterationRule(ProjectMapping mapping, object sourceValue)
        {
            this.UniqueWarning("    No wildcard Iteration rule.");
        }

        public void IterationPathNotFoundUsingWildcardRule(ProjectMapping mapping, object sourceValue)
        {
            this.UniqueVerbose("    Iteration path '{0}' not found: using wildcard rule.", sourceValue);
        }

        public void NoWorkItemTypeMapping(WorkItem source)
        {
            this.Error("WorkItem type '{0}' has no mapping: correct the query or add a mapping.", source.Type.Name);
        }

        public void MappingGenericValidationError(string message, object[] args)
        {
            this.Error(message, args);
        }

        public void TranslatorFunctionNotFoundUsingDefault(FieldMap rule)
        {
            this.UniqueWarning("    Translator Function '{0}' not found, copying unaltered value.", rule.Translate);
        }

        public void AnalyzingSourceLink(WorkItemLinkInfo queryLink)
        {
            this.Info("Analyzing source WorkitemLink {0}->{1} (type {2}).", queryLink.SourceId, queryLink.TargetId, queryLink.LinkTypeId);
        }

        public void MakingNewLink(WorkItemLink relationship)
        {
            this.Info("Adding WorkitemLink from {0} to {1} ({2} End).", relationship.SourceId, relationship.TargetId, relationship.LinkTypeEnd.Name);
        }

        public void LinkExists(WorkItemLinkInfo queryLink, WorkItemLinkInfo match)
        {
            this.Info("WorkitemLink source {0}->{1} (type {2}) already maps to {3}->{4} (type {5})."
                , queryLink.SourceId, queryLink.TargetId, queryLink.LinkTypeId
                , match.SourceId, match.TargetId, match.LinkTypeId);
        }

        public void TargetMissingForLink(WorkItemLinkInfo queryLink, int parentId, int childId)
        {
            this.Info("Cannot add source WorkitemLink {0}->{1} (type {2}) as one of the target is not mapped."
                , queryLink.SourceId, queryLink.TargetId, queryLink.LinkTypeId);
        }

        public void InvalidRule(FieldMap rule)
        {
            this.UniqueWarning("    Invalid rule '{0}': copying unaltered value.", rule);
        }

        public void NoRuleFor(WorkItem source, string field)
        {
            this.UniqueWarning("    No rule for field '{0}\\{1}': skipping.", source.Type.Name, field);
        }

        public void SavingSkipped()
        {
            this.Warning("Saving skipped due to TestOnly option.");
        }

        public void ValidationError(Field item)
        {
            this.Warning("    Value '{1}' is not valid for field {0} (Status {2}).", item.Name, item.Value, item.Status);
        }

        public void BypassingRulesOnDestinationWorkItemStore(TfsConnection destConn)
        {
            this.Info("Bypassing validation rules on destination project {0}.", destConn.ProjectName);
        }

        public void UsingThreePassSavingAlgorithm()
        {
            this.Info("Using create-then-update algorithm to save work items on destination.");
        }

        public void SaveFirstPassSavingNewWorkItems(List<WorkItem> newWorkItems)
        {
            this.Verbose("First pass, saving new work items forcing initial state");
        }

        public void SaveSecondPassUpdatingNewWorkItemsState(List<WorkItem> newWorkItems)
        {
            this.Verbose("Second pass, saving new work items using correct state");
        }

        public void SaveThirdPassSavingUpdatedWorkItems(List<WorkItem> updatedWorkItems)
        {
            this.Verbose("Third pass, saving updated work items");
        }
    }
}
