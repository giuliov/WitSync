using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    internal class ProjectMappingChecker
    {
        protected IEngineEvents eventSink;
        private WorkItemStore sourceWIStore;
        private WorkItemStore destWIStore;
        private string sourceProjectName;
        private string destProjectName;

        internal ProjectMappingChecker(WorkItemStore sourceWIStore, string sourceProjectName, WorkItemStore destWIStore, string destProjectName, IEngineEvents eventSink)
        {
            this.sourceWIStore = sourceWIStore;
            this.sourceProjectName = sourceProjectName;
            this.destWIStore = destWIStore;
            this.destProjectName = destProjectName;
            this.eventSink = eventSink;
        }

        private bool passed = true;
        internal bool Passed { get { return passed; } }

        // logging anything implies failure
        private void Log(string message, params object[] args)
        {
            passed = false;
            eventSink.MappingGenericValidationError(message, args);
        }

        internal void Check(QueryResult sourceResult, ProjectMapping mapping, QueryResult destResult)
        {
            var sourceWorkItems = sourceResult.WorkItems.Values.ToList();
            var destWorkItems = destResult.WorkItems.Values.ToList();

            var workItemMappings = mapping.WorkItemMappings.ToList();

            var sourceTypeNames = sourceWorkItems.ConvertAll(wi => wi.Type.Name).Distinct();
            var mappedSourceTypeNames = workItemMappings.ConvertAll(m => m.SourceType);
            sourceTypeNames.Except(mappedSourceTypeNames).ToList().ForEach(
                t => Log("Missing mapping for source type {0}", t)
                );

            var destTypeNames = destWorkItems.ConvertAll(wi => wi.Type.Name).Distinct();
            var mappedDestTypeNames = workItemMappings.ConvertAll(m => m.DestinationType);
            destTypeNames.Except(mappedDestTypeNames).ToList().ForEach(
                t => Log("Missing mapping for destination type {0}", t));

            var allSourceTypes = this.sourceWIStore.Projects[this.sourceProjectName].WorkItemTypes;
            var allDestTypes = this.destWIStore.Projects[this.destProjectName].WorkItemTypes;

            // check that all target types have an originating ID field
            workItemMappings
                .Where(m => !allDestTypes[m.DestinationType].FieldDefinitions.Contains(m.IDField.Destination))
                .ToList()
                .ForEach(t => Log("Destination WorkItem type '{0}' has no '{1}' Field to host source ID."
                    , t.DestinationType, t.IDField.Destination));
            // check that source ID field match
            workItemMappings
                .Where(m => !allSourceTypes[m.SourceType].FieldDefinitions.Contains(m.IDField.Source))
                .ToList()
                .ForEach(t => Log("Source WorkItem type '{0}' has no source '{1}' ID Field."
                    , t.SourceType, t.IDField.Source));

            // check Rules are valid
            foreach (var wiMapping in workItemMappings)
            {
                foreach (var fieldRule in wiMapping.Fields)
                {
                    // check combo, valid combos are: S+D S+D+T D+S
                    if (!string.IsNullOrWhiteSpace(fieldRule.Set)) {
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

                    // check on Set
                    if (!string.IsNullOrWhiteSpace(fieldRule.Set))
                    {
                        // TODO can fail!
                        var destFieldType = this.destWIStore.FieldDefinitions[fieldRule.Destination].FieldType;

                        bool parseOk = false;
                        switch (destFieldType)
                        {
                            case FieldType.Boolean:
                                bool _bool;
                                parseOk = bool.TryParse(fieldRule.Set, out _bool);
                                break;
                            case FieldType.DateTime:
                                DateTime _DateTime;
                                parseOk = DateTime.TryParse(fieldRule.Set, out _DateTime);
                                break;
                            case FieldType.Double:
                                double _double;
                                parseOk = double.TryParse(fieldRule.Set, out _double);
                                break;
                            case FieldType.Guid:
                                Guid _Guid;
                                parseOk = Guid.TryParse(fieldRule.Set, out _Guid);
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
                                parseOk = int.TryParse(fieldRule.Set, out _int);
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
                                Log("Destination field '{1}/{0}' has unknow type {2}."
                                    , fieldRule.Destination, wiMapping.DestinationType, destFieldType);
                                parseOk = false;
                                break;
                        }//switch
                        if (!parseOk)
                        {
                            Log("Cannot set destination field '{1}/{0}' to '{2}': invalid value."
                                , fieldRule.Destination, wiMapping.DestinationType, fieldRule.Set);
                        }//if
                    }//if
                }//for
            }//for

            //TODO more checks, e.g. on fields and functions
        }
    }
}
