using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    internal class WorkItemsStageConfigurationChecker
    {
        protected IEngineEvents eventSink;
        private WorkItemStore sourceWIStore;
        private WorkItemStore destWIStore;
        private string sourceProjectName;
        private string destProjectName;

        internal WorkItemsStageConfigurationChecker(WorkItemStore sourceWIStore, string sourceProjectName, WorkItemStore destWIStore, string destProjectName, IEngineEvents eventSink)
        {
            this.sourceWIStore = sourceWIStore;
            this.sourceProjectName = sourceProjectName;
            this.destWIStore = destWIStore;
            this.destProjectName = destProjectName;
            this.eventSink = eventSink;
        }

        private int errorCount = 0;
        internal bool Passed { get { return errorCount == 0; } }
        internal int ErrorCount { get { return errorCount; } }

        // logging anything implies failure
        private void Log(string message, params object[] args)
        {
            errorCount++;
            eventSink.MappingGenericValidationError(message, args);
        }

        internal void Check(QueryResult sourceResult, WorkItemsStageConfiguration mapping, QueryResult destResult)
        {
            var workItemMappings = mapping.WorkItemMappings.ToList();

            var sourceWorkItems = sourceResult.WorkItems.Values.ToList();
            var destWorkItems = destResult.WorkItems.Values.ToList();

            var sourceTypeNames = sourceWorkItems.ConvertAll(wi => wi.Type.Name).Distinct();
            var mappedSourceTypeNames = workItemMappings.ConvertAll(m => m.SourceType);
            sourceTypeNames.Except(mappedSourceTypeNames).ToList().ForEach(
                t => Log("Missing mapping for source type {0}", t)
                );

            var destTypeNames = destWorkItems.ConvertAll(wi => wi.Type.Name).Distinct();
            var mappedDestTypeNames = workItemMappings.ConvertAll(m => m.DestinationType);
            destTypeNames.Except(mappedDestTypeNames).ToList().ForEach(
                t => Log("Missing mapping for destination type {0}", t));
        }

        internal void AgnosticCheck(WorkItemsStageConfiguration mapping)
        {
            if (mapping.HasIndex && System.IO.File.Exists(mapping.IndexFile))
            {
                //TODO check indexFile is valid
            }//if

            var workItemMappings = mapping.WorkItemMappings.ToList();

            var allSourceTypes = this.sourceWIStore.Projects[this.sourceProjectName].WorkItemTypes;
            var allDestTypes = this.destWIStore.Projects[this.destProjectName].WorkItemTypes;

            if (mapping.HasIndex)
            {
                // IDField is wrong
                workItemMappings.Where(
                    m => m.IDField != null
                )
                .ToList()
                .ForEach(t => Log("IDField cannot be used with IndexFile: found on WorkItem type '{0}' .", t.SourceType));
            }
            else
            {
                workItemMappings
                    .Where(m => m.IDField==null || string.IsNullOrWhiteSpace(m.IDField.Destination))
                    .ToList()
                    .ForEach(t => Log("Invalid ID Field Destination on WorkItem type '{0}'."
                        , t.DestinationType));
                workItemMappings
                    .Where(m => m.IDField == null || string.IsNullOrWhiteSpace(m.IDField.Source))
                    .ToList()
                    .ForEach(t => Log("Invalid ID Field Source on WorkItem type '{0}'."
                        , t.SourceType));

                // check that all target types have an originating ID field
                workItemMappings
                    .Where(m =>
                        m.IDField != null
                        && !string.IsNullOrWhiteSpace(m.IDField.Destination)
                        && !allDestTypes[m.DestinationType].FieldDefinitions.Contains(m.IDField.Destination))
                    .ToList()
                    .ForEach(t => Log("Destination WorkItem type '{0}' has no '{1}' Field to host source ID."
                        , t.DestinationType, t.IDField.Destination));
                // check that source ID field match
                workItemMappings
                    .Where(m => 
                        m.IDField != null
                        && !string.IsNullOrWhiteSpace(m.IDField.Source)
                        && !allSourceTypes[m.SourceType].FieldDefinitions.Contains(m.IDField.Source))
                    .ToList()
                    .ForEach(t => Log("Source WorkItem type '{0}' has no source '{1}' ID Field."
                        , t.SourceType, t.IDField.Source));
            }

            // check Rules are valid
            foreach (var wiMapping in workItemMappings)
            {
                //TODO if MapState function then States is mandatory!

                foreach (var fieldRule in wiMapping.Fields)
                {
                    bool isSetRule = !string.IsNullOrWhiteSpace(fieldRule.Set);
                    bool isSetIfNullRule = !string.IsNullOrWhiteSpace(fieldRule.SetIfNull);

                    // check combo, valid combos are: S+D S+D+T D+S D+Sif
                    if (isSetRule || isSetIfNullRule)
                    {
                        // Set rule
                        if (!string.IsNullOrWhiteSpace(fieldRule.Source))
                            Log("Invalid Set rule for destination field '{1}/{0}'."
                                , fieldRule.Destination, wiMapping.DestinationType);
                        if (!string.IsNullOrWhiteSpace(fieldRule.Source))
                            Log("Invalid Set rule for destination field '{1}/{0}'."
                                , fieldRule.Destination, wiMapping.DestinationType);
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(fieldRule.Translate))
                        {
                            // Copy rule
                            if (string.IsNullOrWhiteSpace(fieldRule.Source))
                                Log("Invalid Copy rule for destination field '{1}/{0}'."
                                    , fieldRule.Destination, wiMapping.DestinationType);
                        }
                        else
                        {
                            // Translate rule
                            if (string.IsNullOrWhiteSpace(fieldRule.Source))
                                Log("Invalid Translate rule for destination field '{1}/{0}'."
                                    , fieldRule.Destination, wiMapping.DestinationType);
                        }
                    }//if

                    // check on Translator
                    if (!string.IsNullOrWhiteSpace(fieldRule.Translate))
                    {
                        var bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                        var translatorMethod = typeof(MapperFunctions).GetMethod(fieldRule.Translate, bindingFlags);
                        if (translatorMethod == null)
                            Log("Translator {0} does not exists.", fieldRule.Translate);
                    }//if

                    // check on Set & SetIfNull
                    if (isSetRule || isSetIfNullRule)
                    {
                        // TODO can fail!
                        var destFieldType = this.destWIStore.FieldDefinitions[fieldRule.Destination].FieldType;

                        string setValue = isSetRule ? fieldRule.Set : fieldRule.SetIfNull;

                        bool parseOk = false;
                        switch (destFieldType)
                        {
                            case FieldType.Boolean:
                                bool _bool;
                                parseOk = bool.TryParse(setValue, out _bool);
                                break;
                            case FieldType.DateTime:
                                DateTime _DateTime;
                                parseOk = DateTime.TryParse(setValue, out _DateTime);
                                break;
                            case FieldType.Double:
                                double _double;
                                parseOk = double.TryParse(setValue, out _double);
                                break;
                            case FieldType.Guid:
                                Guid _Guid;
                                parseOk = Guid.TryParse(setValue, out _Guid);
                                break;
                            case FieldType.History:
                                Log("Destination field '{1}/{0}' has {2} type and cannot be set."
                                    , fieldRule.Destination, wiMapping.DestinationType, destFieldType);
                                parseOk = false;
                                break;
                            case FieldType.Html:
                                // string-like
                                parseOk = true;
                                break;
                            case FieldType.Integer:
                                int _int;
                                parseOk = int.TryParse(setValue, out _int);
                                break;
                            case FieldType.Internal:
                                Log("Destination field '{1}/{0}' has {2} type and cannot be set."
                                    , fieldRule.Destination, wiMapping.DestinationType, destFieldType);
                                parseOk = false;
                                break;
                            case FieldType.PlainText:
                                // string-like
                                parseOk = true;
                                break;
                            case FieldType.String:
                                parseOk = true;
                                break;
                            case FieldType.TreePath:
                                // string-like
                                parseOk = true;
                                break;
                            default:
                                Log("Destination field '{1}/{0}' has unknown type {2}."
                                    , fieldRule.Destination, wiMapping.DestinationType, destFieldType);
                                parseOk = false;
                                break;
                        }//switch
                        if (!parseOk)
                        {
                            Log("Cannot set destination field '{1}/{0}' to '{2}': invalid value."
                                , fieldRule.Destination, wiMapping.DestinationType, setValue);
                        }//if
                    }//if
                }//for
            }//for

            var allSourceLinkTypes = this.sourceWIStore.WorkItemLinkTypes;
            var allDestLinkTypes = this.destWIStore.WorkItemLinkTypes;
            foreach (var linkMap in mapping.LinkTypeMap)
            {
                if (linkMap.IsWildcard)
                {
                    // * -> * === same name
                    // * -> '' === not mapped
                    if (linkMap.DestinationType != "*"
                        && !string.IsNullOrWhiteSpace(linkMap.DestinationType))
                        Log("Invalid Link wildcard rule.");
                }

                if (!linkMap.IsWildcard)
                {
                    if (!allSourceLinkTypes.Any(st=> st.ForwardEnd.Name == linkMap.SourceType))
                        Log("Source link type '{0}' does not exist.", linkMap.SourceType);
                    if (!allDestLinkTypes.Any(st=> st.ForwardEnd.Name == linkMap.DestinationType))
                        Log("Destination link type '{0}' does not exist.", linkMap.DestinationType);
                    // TODO check if mapping is sensible
                }//if
            }

            //TODO more checks, e.g. on fields and functions
        }
    }
}
