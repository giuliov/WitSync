using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WitSync
{
    public interface IEngineTracing
    {
        void Trace(string format, params object[] args);
    }

    public interface IEngineEvents : IEngineTracing
    {
        void ConnectingSource(TfsConnection sourceConn);
        void SourceConnected(TfsConnection sourceConn);
        void ConnectingDestination(TfsConnection destConn);
        void DestinationConnected(TfsConnection destConn);
        void ExecutingSourceQuery(string queryName, TfsConnection sourceConn);
        void ExecutingDestinationQuery(string queryName, TfsConnection destConn);
        void SyncStarted();
        void SavingWorkItems(List<WorkItem> newWorkItems, List<WorkItem> updatedWorkItems);
        void SavingLinks(List<WorkItemLink> changedLinks, List<WorkItem> validWorkItems);
        void SaveError(Exception exception, WorkItem workItem);
        void SaveErrorInvalidField(WorkItem workItem, Field f);
        void MakingNewWorkItem(WorkItem source);
        void UpdatingExistingWorkItem(WorkItem source, WorkItem target);
        void NoTargetState(WorkItemMap map, object state);
        void NoWildcardAreaRule(WorkItemsStageConfiguration mapping, object sourceValue);
        void AreaPathNotFoundUsingWildcardRule(WorkItemsStageConfiguration mapping, object sourceValue);
        void NoWildcardIterationRule(WorkItemsStageConfiguration mapping, object sourceValue);
        void IterationPathNotFoundUsingWildcardRule(WorkItemsStageConfiguration mapping, object sourceValue);
        void NoWorkItemTypeMapping(WorkItem source);
        void MappingGenericValidationError(string message, object[] args);
        void TranslatorFunctionNotFoundUsingDefault(FieldMap rule);
        void MakingNewLink(WorkItemLink relationship);
        void ExistingWorkItemUpdated(WorkItem source, WorkItem target);
        void AnalyzingSourceLink(WorkItemLinkInfo queryLink);
        void LinkExists(WorkItemLinkInfo queryLink, WorkItemLinkInfo match);
        void LinkExists(WorkItemLinkInfo queryLink, WorkItemLink relationship);
        void TargetMissingForLink(WorkItemLinkInfo queryLink, int parentId, int childId);
        void SyncFinished(int totalErrors);
        void InvalidRule(FieldMap rule);
        void NoRuleFor(WorkItemType sourceType, string field);
        void SavingSkipped();
        void ValidationError(Field item);
        void BypassingRulesOnDestinationWorkItemStore(TfsConnection destConn);
        void UsingThreePassSavingAlgorithm();
        void SaveFirstPassSavingNewWorkItems(List<WorkItem> newWorkItems);
        void SaveSecondPassUpdatingNewWorkItemsState(List<WorkItem> newWorkItems);
        void SaveThirdPassSavingUpdatedWorkItems(List<WorkItem> updatedWorkItems);
        void SourceQueryNotFound(string queryName);
        void DestinationQueryNotFound(string queryName);
        void AnalyzingSourceLink(WorkItemLink sourceLink);
        void LinkExists(WorkItemLink sourceLink, WorkItemLink relationship);
        void TargetMissingForLink(WorkItemLink sourceLink, int parentId, int childId);
        void SkippingLink(WorkItemLink sourceLink);
        void MappingFileNotFoundAssumeDefaults(string path);
        void ExceptionWhileMappingLink(Exception ex, WorkItemLink sourceLink);
        void ExceptionWhileMappingWorkItem(Exception ex, WorkItem sourceWorkItem);
        void DumpOptions(WorkItemsStage.EngineOptions options);
        void ReadingGlobalListsFromSource();
        void GlobalListQueuedForUpdate(string glName);
        void UpdatingGlobalListsOnDestination();
        void SelectingGlobalLists();
        void BuildingGlobalListUpdateMessage();
        void GlobalListsUpdated();
        void SyncingIterations();
        void SyncingAreas();
        void ReadingAreaAndIterationInfoFromSource();
        void InternalError(Exception ex);
        void ExecutingStage(PipelineStage stage);
        void StageCompleted(PipelineStage stage, int stageErrors);
        void StageError(PipelineStage stage, Exception ex);
        void PreparingStage(PipelineStage stage);
        void StagePrepared(PipelineStage stage, int stageErrors);
        void StagePreparationError(PipelineStage stage, Exception ex);
        void DumpMapping(WorkItemsStageConfiguration mapping);
        void TraceRule(string msg, params object[] args);
        void ExceptionWhileRemovingAttachment(Exception ex, Attachment a, WorkItem target);
        void ExceptionWhileAddingAttachment(Exception ex, Attachment sourceAttachment, WorkItem source);
        void ExceptionWhileCopyingField(Exception ex, Field fromField);
        void ExceptionWhileActingOnField(Exception ex, string targetFieldName);
    }
}
