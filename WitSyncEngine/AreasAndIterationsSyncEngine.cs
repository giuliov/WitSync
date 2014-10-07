using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WitSync
{

    public class AreasAndIterationsSyncEngine
    {
        protected TfsConnection sourceConn;
        protected TfsConnection destConn;
        protected IEngineEvents eventSink;
        protected int saveErrors = 0;

        public AreasAndIterationsSyncEngine(TfsConnection source, TfsConnection dest, IEngineEvents eventHandler)
        {
            this.sourceConn = source;
            this.destConn = dest;
            this.eventSink = eventHandler;
        }

        NodeInfo rootAreaNode = null;
        NodeInfo rootIterationNode = null;
        ICommonStructureService4 sourceCSS = null;
        ICommonStructureService4 destCSS = null;

        public int Sync(bool areas, bool iterations, bool testOnly)
        {
            eventSink.ConnectingSource(sourceConn);
            sourceConn.Connect();
            eventSink.SourceConnected(sourceConn);
            eventSink.ConnectingDestination(destConn);
            destConn.Connect();
            eventSink.DestinationConnected(destConn);

            var sourceWIStore = sourceConn.Collection.GetService<WorkItemStore>();
            var destWIStore = destConn.Collection.GetService<WorkItemStore>();

            var sourceProject = sourceWIStore.Projects[sourceConn.ProjectName];
            var destProject = destWIStore.Projects[destConn.ProjectName];

            sourceCSS = sourceConn.Collection.GetService<ICommonStructureService4>();
            destCSS = destConn.Collection.GetService<ICommonStructureService4>();
            foreach (NodeInfo info in destCSS.ListStructures(destProject.Uri.ToString()))
            {
                if (info.StructureType == "ProjectModelHierarchy")
                {
                    rootAreaNode = info;
                }
                else if (info.StructureType == "ProjectLifecycle")
                {
                    rootIterationNode = info;
                }
            }

            if (areas)
            {
                SyncNodes(sourceProject.AreaRootNodes);
            }
            if (iterations)
            {
                SyncNodes(sourceProject.IterationRootNodes);
            }

            return saveErrors;
        }

        // TODO: how do you manage a node that moved????
        private void SyncNodes(NodeCollection nodes)
        {
            foreach (Node node in nodes)
            {
                if (node.IsAreaNode)
                {
                    SyncAreaNode(node);
                }
                else if (node.IsIterationNode)
                {
                    SyncIterationNode(node);
                }
                else
                {
                    //TODO error
                }

                if (node.ChildNodes.Count > 0)
                {
                    SyncNodes(node.ChildNodes);
                }
            }
        }

        private void SyncAreaNode(Node node)
        {
            CreateNodeIfMissing(node, rootAreaNode);
        }

        private void SyncIterationNode(Node node)
        {
            string newNodeUri = CreateNodeIfMissing(node, rootIterationNode);

            var destNode = destCSS.GetNode(newNodeUri);
            var sourceNode = sourceCSS.GetNode(node.Uri.AbsoluteUri);
            destCSS.SetIterationDates(destNode.Uri, sourceNode.StartDate, sourceNode.FinishDate);
        }

        private string CreateNodeIfMissing(Node node, NodeInfo rootNode)
        {
            // we should never account for Root node, as it has a fixed name project dependant
            string nodePathWithoutRoot = node.Path.Remove(0, node.Path.IndexOf("\\") + 1);
            string nodePathOnDest = rootNode.Path + "\\" + nodePathWithoutRoot;
            string destNodeUri = string.Empty;
            var destNode = GetNodeIfExists(nodePathOnDest);
            if (destNode == null)
            {
                if (!nodePathWithoutRoot.Contains("\\"))
                {
                    // first level nodes
                    destNodeUri = destCSS.CreateNode(nodePathWithoutRoot, rootNode.Uri);
                }
                else
                {
                    int lastBackslash = nodePathWithoutRoot.LastIndexOf("\\");
                    NodeInfo parentNode = destCSS.GetNodeFromPath(rootNode.Path + "\\" + nodePathWithoutRoot.Substring(0, lastBackslash));
                    destNodeUri = destCSS.CreateNode(nodePathWithoutRoot.Substring(lastBackslash + 1), parentNode.Uri);
                }//if
            }
            else
            {
                destNodeUri = destNode.Uri;
            }//if
            return destNodeUri;
        }

        private NodeInfo GetNodeIfExists(string nodePathOnDest)
        {
            try
            {
                return destCSS.GetNodeFromPath(nodePathOnDest);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
