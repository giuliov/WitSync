using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    class MapperFunctions
    {
        private IEngineEvents eventSink;
        private string sourceProjectName;
        private string destProjectName;

        internal MapperFunctions(IEngineEvents eventSink, string sourceProjectName, string destProjectName)
        {
            this.eventSink = eventSink;
            this.sourceProjectName = sourceProjectName;
            this.destProjectName = destProjectName;
        }

        internal object MapState(FieldMap rule, WorkItemMap map, ProjectMapping mapping, object sourceValue)
        {
            var x = map.FindMappedState(sourceValue.ToString());
            if (x != null)
                return x.Destination;
            else
            {
                eventSink.NoTargetState(map, sourceValue);
                return string.Empty;
            }
        }

        internal object MapAreaPath(FieldMap rule, WorkItemMap map, ProjectMapping mapping, object sourceValue)
        {
            var path = sourceValue.ToString();
            // search suitable mapping
            var x = mapping.FindExactMappedAreaPath(path);
            if (x == null)
            {
                x = mapping.GetDefaultAreaPathMapping();
                if (x == null)
                {
                    eventSink.NoWildcardAreaRule(mapping, sourceValue);
                }
                else
                {
                    eventSink.AreaPathNotFoundUsingWildcardRule(mapping, sourceValue);
                }
            }
            if (x != null)
            {
                if (x.DestinationPath == "*")
                {
                    // replace project name
                    path = this.destProjectName + path.Substring(this.sourceProjectName.Length);
                }
                else if (!string.IsNullOrWhiteSpace(x.DestinationPath))
                {
                    path = x.DestinationPath;
                }
                else
                {
                    path = this.destProjectName;
                }
            }
            return path;
        }

        internal object MapIterationPath(FieldMap rule, WorkItemMap map, ProjectMapping mapping, object sourceValue)
        {
            var path = sourceValue.ToString();
            // search suitable mapping
            var x = mapping.FindExactMappedIterationPath(path);
            if (x == null)
            {
                x = mapping.GetDefaultIterationPathMapping();
                if (x == null)
                {
                    eventSink.NoWildcardIterationRule(mapping, sourceValue);
                }
                else
                {
                    eventSink.IterationPathNotFoundUsingWildcardRule(mapping, sourceValue);
                }
            }
            if (x != null)
            {
                if (x.DestinationPath == "*")
                {
                    // replace project name
                    path = this.destProjectName + path.Substring(this.sourceProjectName.Length);
                }
                else if (!string.IsNullOrWhiteSpace(x.DestinationPath))
                {
                    path = x.DestinationPath;
                }
                else
                {
                    path = this.destProjectName;
                }
            }
            return path;
        }
    }
}
