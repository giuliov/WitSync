using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace WitSync
{
    class LinkMapper : SyncContext
    {
        internal LinkMapper(SyncContext context)
            : base(context)
        {
        }

        internal List<WorkItemLink> MapLinks(ICollection<WorkItem> sourceWorkItems, List<WorkItem> validWorkItems)
        {
            var changedLinks = new List<WorkItemLink>();

            foreach (var sourceWI in sourceWorkItems)
            {
                foreach (WorkItemLink sourceLink in sourceWI.WorkItemLinks)
                {
                    WorkItemLink destinationLink = null;
                    try
                    {
                        destinationLink = MapSingleLink(sourceLink, validWorkItems);
                    }
                    catch (Exception ex)
                    {
                        this.EventSink.ExceptionWhileMappingLink(ex, sourceLink);
                    }
                    if (destinationLink != null)
                    {
                        changedLinks.Add(destinationLink);
                    }
                }//for
            }//for

            return changedLinks;
        }

        private WorkItemLink MapSingleLink(WorkItemLink sourceLink, List<WorkItem> validWorkItems)
        {
            WorkItemLink destinationLink = null;

            // BUG: wildcard rule may get Changest links also!!! How do you propagate???
            var rule = this.Mapping.FindLinkRule(sourceLink.LinkTypeEnd.Name);
            if (rule != null)
            {
                this.EventSink.AnalyzingSourceLink(sourceLink);

                // rule: do not map
                if (string.IsNullOrWhiteSpace(rule.DestinationType))
                    return null;

                // we must be sure that link.SourceId and link.TargetId have a link in destination project
                int sourceIdOnDest = this.Index.GetIdFromSourceId(sourceLink.SourceId);
                int targetIdOnDest = this.Index.GetIdFromSourceId(sourceLink.TargetId);
                if (sourceIdOnDest > 0 && targetIdOnDest > 0)
                {
                    var sourceWIOnDest = validWorkItems.Where(w => w.Id == sourceIdOnDest).FirstOrDefault();
                    Debug.Assert(sourceWIOnDest != null);

                    WorkItemLinkTypeEnd destLinkType = null;
                    if (rule.IsWildcard)
                    {
                        destLinkType = sourceLink.LinkTypeEnd;
                    }
                    else
                    {
                        // HACK
                        destLinkType = this.DestinationStore.WorkItemLinkTypes.Where(t => t.ForwardEnd.Name == rule.DestinationType).FirstOrDefault().ForwardEnd;
                    }//if
                    var relationship = new WorkItemLink(destLinkType, sourceIdOnDest, targetIdOnDest);
                    if (!sourceWIOnDest.WorkItemLinks.Contains(relationship))
                    {
                        sourceWIOnDest.WorkItemLinks.Add(relationship);
                        //track
                        destinationLink = relationship;

                        this.EventSink.MakingNewLink(relationship);
                    }
                    else
                    {
                        this.EventSink.LinkExists(sourceLink, relationship);
                    }//if
                }
                else
                {
                    this.EventSink.TargetMissingForLink(sourceLink, sourceIdOnDest, targetIdOnDest);
                }//if
            }
            else
            {
                this.EventSink.SkippingLink(sourceLink);
            }//if

            return destinationLink;
        }
    }
}
