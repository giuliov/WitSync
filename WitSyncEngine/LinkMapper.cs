using Microsoft.TeamFoundation;
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
                MapWorkItemLinks(validWorkItems, changedLinks, sourceWI);
                MapOtherLinks(sourceWI);
            }//for

            return changedLinks;
        }

        private void MapOtherLinks(WorkItem sourceWI)
        {
            var cloneWI = this.Index.GetWorkItemFromSourceId(sourceWI.Id);
            foreach (Link link in sourceWI.Links)
            {
                if (link.BaseType == BaseLinkType.ExternalLink)
                {
                    // TODO these are meaningful only in the same Collection
#if false
                    var eLink = link as ExternalLink;
                    // something interesting?
                    RegisteredLinkType registeredType = sourceWI.Store.RegisteredLinkTypes[eLink.ArtifactLinkType];
                    var cloneLink = new ExternalLink(registeredType, eLink.LinkedArtifactUri);
                    cloneLink.Comment = eLink.Comment;
                    cloneWI.Links.Add(cloneLink);
#endif
                }
                else if (link.BaseType == BaseLinkType.RelatedLink)
                {
                    // TODO how they can be meaningful in different Collections?
#if false
                    var rLink = link as RelatedLink;
                    var cloneLink = new RelatedLink(rLink.LinkTypeEnd, rLink.RelatedWorkItemId);
                    cloneLink.Comment = rLink.Comment;
                    cloneWI.Links.Add(cloneLink);
#endif
                }
                else if (link.BaseType == BaseLinkType.Hyperlink)
                {
                    var hLink = link as Hyperlink;
                    var cloneLink = new Hyperlink(hLink.Location);
                    cloneLink.Comment = hLink.Comment;
                    cloneWI.Links.Add(cloneLink);
                }//if
            }//for
        }

        private void MapWorkItemLinks(List<WorkItem> validWorkItems, List<WorkItemLink> changedLinks, WorkItem sourceWI)
        {
            foreach (WorkItemLink sourceLink in sourceWI.WorkItemLinks)
            {
                WorkItemLink destinationLink = null;
                try
                {
                    destinationLink = MapSingleWorkItemLink(sourceLink, validWorkItems);
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
        }

        private WorkItemLink MapSingleWorkItemLink(WorkItemLink sourceLink, List<WorkItem> validWorkItems)
        {
            WorkItemLink destinationLink = null;

            // BUG: wildcard rule may get Changeset links also!!! How do you propagate???
            var rule = this.Mapping.FindLinkRule(sourceLink.LinkTypeEnd.Name);
            if (rule != null)
            {
                // rule: do not map
                if (string.IsNullOrWhiteSpace(rule.DestinationType))
                {
                    return null;
                }

                this.EventSink.AnalyzingSourceLink(sourceLink);

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
