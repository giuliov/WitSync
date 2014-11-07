using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    public class WorkItemNode
    {
        public WorkItem WorkItem { get; set; }
        public string RelationshipToParent { get; set; }
        public List<WorkItemNode> Children { get; set; }
    }
}
