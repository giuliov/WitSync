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

        internal List<WorkItemLink> MapLinks(QueryResult sourceResult, QueryResult destResult, IEnumerable<WorkItem> validWorkItems)
        {
            var changedLinks = new List<WorkItemLink>();

            // get the link type for hierarchical relationships
            var parentChildlinkType = this.SourceStore.WorkItemLinkTypes[CoreLinkTypeReferenceNames.Hierarchy];
            var linkTypeIdToMatch = parentChildlinkType.ForwardEnd.Id;
            // check that all source links are included
            foreach (var queryLink in sourceResult.Links)
            {
                // only Parent-Child 
                if (queryLink.LinkTypeId == linkTypeIdToMatch && queryLink.SourceId != linkTypeIdToMatch)
                {
                    this.EventSink.AnalyzingSourceLink(queryLink);

                    // Parent-Child: we must be sure that link.SourceId and link.TargetId have a link in destination project
                    int parentId = this.Index.GetIdFromSourceId(queryLink.SourceId);
                    int childId = this.Index.GetIdFromSourceId(queryLink.TargetId);
                    if (parentId > 0 && childId > 0)
                    {
                        // assume that if missing from query, then should be added
                        var match = destResult.Links.FirstOrDefault(l => l.LinkTypeId == linkTypeIdToMatch && l.SourceId == parentId && l.TargetId == childId);
                        if (match == default(WorkItemLinkInfo))
                        {
                            // not found
                            var parent = validWorkItems.Where(w => w.Id == parentId).FirstOrDefault();
                            Debug.Assert(parent != null);
                            var lte = parentChildlinkType.ForwardEnd;
                            var relationship = new WorkItemLink(lte, parentId, childId);
                            // FIX a flat query do not materialize links, but they are present on the workitem object
                            if (!parent.WorkItemLinks.Contains(relationship))
                            {
                                parent.WorkItemLinks.Add(relationship);
                                //track
                                changedLinks.Add(relationship);

                                this.EventSink.MakingNewLink(relationship);
                            }
                            else
                            {
                                this.EventSink.LinkExists(queryLink, relationship);
                            }
                        }
                        else
                        {
                            this.EventSink.LinkExists(queryLink, match);
                        }
                    }
                    else
                    {
                        this.EventSink.TargetMissingForLink(queryLink, parentId, childId);
                    }
                }
            }//for

            return changedLinks;
        }
    }
}
