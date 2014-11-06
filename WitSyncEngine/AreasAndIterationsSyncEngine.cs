using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WitSync
{
    internal class NodeChangeEntry : ChangeEntry
    {
        internal enum Change { Add }

        protected NodeChangeEntry(string source, string sourcePath, string targetPath, Change change)
            : base("AreasOrIteration", sourcePath, targetPath, change.ToString())
        { }
    }

    internal class AreaChangeEntry : NodeChangeEntry
    {
        internal AreaChangeEntry(string sourcePath, string targetPath, Change change)
            : base("Area", sourcePath, targetPath, change)
        { }
    }

    internal class IterationChangeEntry : NodeChangeEntry
    {
        internal IterationChangeEntry(string sourcePath, string targetPath, Change change)
            : base("Iteration", sourcePath, targetPath, change)
        { }
    }

    public class AreasAndIterationsSyncEngine : EngineBase
    {
        [Flags]
        public enum EngineOptions
        {
            TestOnly = 0x1,
            Areas = 0x2,
            Iterations = 0x4,
        }

        NodeInfo rootAreaNode = null;
        NodeInfo rootIterationNode = null;
        ICommonStructureService4 sourceCSS = null;
        ICommonStructureService4 destCSS = null;

        public AreasAndIterationsSyncEngine(TfsConnection source, TfsConnection dest, IEngineEvents eventHandler)
            : base(source, dest, eventHandler)
        {
            //no-op
        }

        public EngineOptions Options { set { this.options =value;}}

        protected EngineOptions options;

        public override int Prepare(bool testOnly)
        {
            return 0;
        }

        bool testMode = false;

        public override int Execute(bool testOnly)
        {
            bool areas = options.HasFlag(EngineOptions.Areas);
            bool iterations = options.HasFlag(EngineOptions.Iterations);
            this.testMode = testOnly;


            var sourceWIStore = sourceConn.Collection.GetService<WorkItemStore>();
            var destWIStore = destConn.Collection.GetService<WorkItemStore>();

            var sourceProject = sourceWIStore.Projects[sourceConn.ProjectName];
            var destProject = destWIStore.Projects[destConn.ProjectName];

            eventSink.ReadingAreaAndIterationInfoFromSource();

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
                eventSink.SyncingAreas();
                SyncNodes(sourceProject.AreaRootNodes);
            }
            if (iterations)
            {
                eventSink.SyncingIterations();
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
            if (!this.testMode) {
                destCSS.SetIterationDates(destNode.Uri, sourceNode.StartDate, sourceNode.FinishDate);
            }
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
                    if (!this.testMode)
                    {
                        destNodeUri = destCSS.CreateNode(nodePathWithoutRoot, rootNode.Uri);
                        if (node.IsAreaNode)
                        {
                            this.ChangeLog.AddEntry(new AreaChangeEntry(node.Name, destNodeUri, NodeChangeEntry.Change.Add));
                        }
                        else if (node.IsIterationNode)
                        {
                            this.ChangeLog.AddEntry(new IterationChangeEntry(node.Name, destNodeUri, NodeChangeEntry.Change.Add));
                        }
                    }
                }
                else
                {
                    int lastBackslash = nodePathWithoutRoot.LastIndexOf("\\");
                    NodeInfo parentNode = destCSS.GetNodeFromPath(rootNode.Path + "\\" + nodePathWithoutRoot.Substring(0, lastBackslash));
                    // TODO test mode!
                    if (!this.testMode)
                    {
                        destNodeUri = destCSS.CreateNode(nodePathWithoutRoot.Substring(lastBackslash + 1), parentNode.Uri);
                        if (node.IsAreaNode)
                        {
                            this.ChangeLog.AddEntry(new AreaChangeEntry(node.Name, destNodeUri, NodeChangeEntry.Change.Add));
                        }
                        else if (node.IsIterationNode)
                        {
                            this.ChangeLog.AddEntry(new IterationChangeEntry(node.Name, destNodeUri, NodeChangeEntry.Change.Add));
                        }
                    }
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
