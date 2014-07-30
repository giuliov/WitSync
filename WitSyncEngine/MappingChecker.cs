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
                .ForEach(t => Log("Destination WorkItem type '{0}' has no '{1}' Field to host source ID.", t.DestinationType, t.IDField.Destination));
            // check that source ID field match
            workItemMappings
                .Where(m => !allSourceTypes[m.SourceType].FieldDefinitions.Contains(m.IDField.Source))
                .ToList()
                .ForEach(t => Log("Source WorkItem type '{0}' has no source '{1}' ID Field.", t.SourceType, t.IDField.Source));
            // check on Translator
            foreach (var wim in workItemMappings)
            {
                foreach (var f in wim.Fields)
                {
                    if (!string.IsNullOrWhiteSpace(f.Translate))
                    {
                        var bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                        var translatorMethod = typeof(MapperFunctions).GetMethod(f.Translate, bindingFlags);
                        if (translatorMethod == null)
                            Log("Translator {0} does not exists.", f.Translate);
                    }//if
                }//for
            }//for

            //TODO more checks, e.g. on fields and functions
        }
    }
}
