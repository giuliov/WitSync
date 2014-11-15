using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WitSync
{
    [XmlType("States")]
    public class StateList
    {
        [XmlElement("State")]
        public StateMap[] States { get; set; }
    }

    [XmlType("State")]
    public class StateMap
    {
        [XmlAttribute]
        public string Source { get; set; }
        [XmlAttribute]
        public string Destination { get; set; }
    }

    [XmlType]
    public class FieldMap
    {
        [XmlAttribute]
        public string Source { get; set; }
        [XmlAttribute]
        public string Destination { get; set; }
        [XmlAttribute]
        public string Translate { get; set; }
        [XmlAttribute]
        public string Set { get; set; }
        [XmlAttribute]
        public string SetIfNull { get; set; }

        [YamlIgnore]
        public bool IsWildcard { get { return Source == "*"; } }

        public override string ToString()
        {
            var buf = new StringBuilder();
            buf.AppendFormat("Source: {0}", Source);
            if (!string.IsNullOrEmpty(Destination))
                buf.AppendFormat(", Destination: {0}", Destination);
            if (!string.IsNullOrEmpty(Translate))
                buf.AppendFormat(", Translate: {0}", Translate);
            if (!string.IsNullOrEmpty(Set))
                buf.AppendFormat(", Set: {0}", Set);
            if (!string.IsNullOrEmpty(SetIfNull))
                buf.AppendFormat(", SetIfNull: {0}", SetIfNull);

            return buf.ToString();
        }
    }

    [XmlType("Area")]
    public class AreaMap
    {
        [XmlAttribute]
        public string SourcePath { get; set; }
        [XmlAttribute]
        public string DestinationPath { get; set; }
    }

    [XmlType("Iteration")]
    public class IterationMap
    {
        [XmlAttribute]
        public string SourcePath { get; set; }
        [XmlAttribute]
        public string DestinationPath { get; set; }
    }

    [XmlType]
    public class WorkItemMap
    {
        public WorkItemMap()
        {
            // default
            this.Attachments = AttachmentMode.Sync;
            this.DefaultRules = true;
        }

        [Flags]
        public enum AttachmentMode
        {
            DoNotSync = 0,
            AddAndUpdate = 1,
            RemoveIfAbsent = 2,
            ClearTarget = 4,
            Sync = AddAndUpdate | RemoveIfAbsent,
            FullSync = AddAndUpdate | ClearTarget
        }

        [XmlAttribute]
        public string SourceType { get; set; }
        [XmlAttribute]
        public string DestinationType { get; set; }
        [XmlElement]
        public FieldMap IDField { get; set; }
        [XmlElement("States")]
        public StateList StateList { get; set; }
        [XmlElement("Field")]
        public FieldMap[] Fields { get; set; }
        [XmlAttribute("Attachments")]
        public AttachmentMode Attachments { get; set; }
        [XmlAttribute("DefaultRules")]
        public bool DefaultRules { get; set; }

        [XmlIgnore]
        [YamlIgnore]
        public IEnumerable<FieldMap> AllFields
        {
            get
            {
                if (IDField != null)
                {
                    yield return IDField;
                }
                for (int i = 0; i < Fields.Length; i++)
                    yield return Fields[i];
            }
        }

        public FieldMap FindFieldRule(string sourceFieldName)
        {
            var x = AllFields.Where(f => f.Source == sourceFieldName).FirstOrDefault();
            if (x == null)
            {
                // wildcard ?
                x = Fields.Where(f => f.Source == "*").FirstOrDefault();
            }
            return x;
        }

        public StateMap FindMappedState(string state)
        {
            return StateList.States.Where(s => s.Source == state).FirstOrDefault();
        }
    }

    [XmlType("LinkType")]
    public class LinkTypeMap
    {
        [XmlAttribute]
        public string SourceType { get; set; }
        [XmlAttribute]
        public string DestinationType { get; set; }

        [YamlIgnore]
        public bool IsWildcard { get { return SourceType == "*"; } }
    }

    [XmlRoot("Mapping")]
    public class WorkItemsStageConfiguration : StageConfiguration
    {
        [Flags]
        public enum Modes
        {
            NOT_USED = 0x1,
            BypassWorkItemStoreRules = 0x2,
            UseEditableProperty = 0x4,
            OpenTargetWorkItem = 0x8,
            PartialOpenTargetWorkItem = 0x10,
            CreateThenUpdate = 0x20,
        }

        public Modes Mode { get; set; }

        [XmlElement]
        public string SourceQuery { get; set; }
        [XmlElement]
        public string DestinationQuery { get; set; }
        [XmlElement]
        public string IndexFile { get; set; }
        [XmlArray]
        public AreaMap[] AreaMap { get; set; }
        [XmlArray]
        public IterationMap[] IterationMap { get; set; }
        [XmlElement("WorkItemMap")]
        public WorkItemMap[] WorkItemMappings { get; set; }
        [XmlArray]
        public LinkTypeMap[] LinkTypeMap { get; set; }

        public bool HasIndex { get { return !string.IsNullOrWhiteSpace(this.IndexFile); } }

        public FieldMap FindIdFieldForTargetWorkItemType(string destWorkItemType)
        {
            WorkItemMap wiMap = WorkItemMappings.Where(m => m.DestinationType == destWorkItemType).FirstOrDefault();
            return wiMap.IDField;
        }

        public WorkItemMap FindWorkItemTypeMapping(string sourceWorkItemType)
        {
            return WorkItemMappings.Where(m => m.SourceType == sourceWorkItemType).FirstOrDefault();
        }

        public LinkTypeMap FindLinkRule(string sourceLinkTypeName)
        {
            var x = LinkTypeMap.Where(lt => lt.SourceType == sourceLinkTypeName).FirstOrDefault();
            if (x == null)
            {
                // wildcard ?
                x = LinkTypeMap.Where(lt => lt.IsWildcard).FirstOrDefault();
            }
            return x;
        }

        public AreaMap FindExactMappedAreaPath(string path)
        {
            return AreaMap.Where(a => a.SourcePath == path).FirstOrDefault();
        }
        public AreaMap GetDefaultAreaPathMapping()
        {
            return AreaMap.Where(a => a.SourcePath == "*").FirstOrDefault();
        }

        public IterationMap FindExactMappedIterationPath(string path)
        {
            return IterationMap.Where(a => a.SourcePath == path).FirstOrDefault();
        }
        public IterationMap GetDefaultIterationPathMapping()
        {
            return IterationMap.Where(a => a.SourcePath == "*").FirstOrDefault();
        }

        public void RebuildMappingIndexes()
        {
            //no-op
        }

        // Validation Error Count
        [XmlIgnore]
        [YamlIgnore]
        public int ErrorsCount { get { return ErrorMessage.Count; } }
        // Validation Error Message
        [XmlIgnore]
        [YamlIgnore]
        public List<string> ErrorMessage = new List<string>();

        public void ValidationHandler(object sender, ValidationEventArgs args)
        {
            ErrorMessage.Add(args.Message);
        }

        [Obsolete("Schema is no more up to date")]
        public void Validate(Stream documentStream, string schemaPath)
        {
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                XmlReaderSettings settings = new XmlReaderSettings();
                using (Stream schemaStream = asm.GetManifestResourceStream("WitSync." + schemaPath))
                {
                    using (XmlReader schemaReader = XmlReader.Create(schemaStream))
                    {
                        settings.Schemas.Add(null, schemaReader);
                    }
                }
                settings.ValidationType = ValidationType.Schema;

                XmlReader reader = XmlReader.Create(documentStream, settings);
                XmlDocument document = new XmlDocument();
                document.Load(reader);
                ValidationEventHandler eventHandler = new ValidationEventHandler(this.ValidationHandler);
                document.Validate(eventHandler);
            }
            catch (Exception error)
            {
                ErrorMessage.Add(error.Message);
            }//try
        }

        public static WorkItemsStageConfiguration Generate()
        {
            var self = new WorkItemsStageConfiguration()
            {
                SourceQuery = "source query",
                DestinationQuery = "dest query",
                IndexFile = "index",
                Mode = Modes.OpenTargetWorkItem | Modes.UseEditableProperty,
                AreaMap = new AreaMap[] {
                    new AreaMap() { SourcePath = "srcArea1", DestinationPath = "dstArea1" },
                    new AreaMap() { SourcePath = "srcArea2", DestinationPath = "dstArea2" }
                },
                IterationMap = new IterationMap[] {
                    new IterationMap() { SourcePath = "src", DestinationPath = "dst" },
                    new IterationMap() { SourcePath = "*", DestinationPath = "" }
                },
                WorkItemMappings = new WorkItemMap[] {
                    new WorkItemMap() {
                        SourceType = "srctype", DestinationType="desttype",
                        Attachments = WorkItemMap.AttachmentMode.Sync,
                        IDField = new FieldMap() { Source="srcID", Destination="dstID" },
                        StateList = new StateList() {
                        States = new StateMap[] {
                            new StateMap() { Source="srcstate1", Destination="deststate1"},
                            new StateMap() { Source="srcstate2", Destination="deststate2"}
                        }},
                        Fields = new FieldMap[] {
                            new FieldMap() { Source="src1", Destination="dst1"},
                            new FieldMap() { Source="src2", Destination="dst2", Translate="tranFunc2"},
                            new FieldMap() { Destination="dst3", Set="val3" },
                            new FieldMap() { Source="src4", Destination="dst4", SetIfNull="set4"},
                            new FieldMap() { Source="*", Destination="*"},
                            new FieldMap() { Source="*", Destination=""},
                        }
                    }
                },
                LinkTypeMap = new LinkTypeMap[] { 
                    new LinkTypeMap() {SourceType="srclnk1", DestinationType="dstlnk1" },
                    new LinkTypeMap() {SourceType="srclnk2", DestinationType="dstlnk2" },
                    new LinkTypeMap() {SourceType="*", DestinationType="*" }
                }
            };
            return self;
        }

        // TODO move this code out of this class to remove dependency on Microsoft.TeamFoundation.WorkItemTracking.Client
        internal void SetDefaults(TfsConnection sourceConn, WorkItemStore sourceWIStore, TfsConnection destConn, WorkItemStore destWIStore)
        {
            // add defaults
            this.SourceQuery = this.SourceQuery ?? string.Format("SELECT id FROM workitems WHERE [Team Project]='{0}'", sourceConn.ProjectName);
            this.DestinationQuery = this.DestinationQuery ?? string.Format("SELECT id FROM workitems WHERE [Team Project]='{0}'", destConn.ProjectName);
            this.AreaMap = this.AreaMap ?? new AreaMap[] { new AreaMap() { SourcePath = "*", DestinationPath = "*" } };
            this.IterationMap = this.IterationMap ?? new IterationMap[] { new IterationMap() { SourcePath = "*", DestinationPath = "*" } };
            var defaultFieldRules = new FieldMap[] {
                new FieldMap() { Source = "System.AreaId", Destination = "" },
                new FieldMap() { Source = "System.AreaPath", Destination = "System.AreaPath", Translate = "MapAreaPath"},
                new FieldMap() { Source = "System.IterationId", Destination = "" },
                new FieldMap() { Source = "System.IterationPath", Destination = "System.IterationPath", Translate = "MapIterationPath" },
                new FieldMap() { Source = "System.Reason", Destination = "" },
                new FieldMap() { Source = "Microsoft.VSTS.Common.StateChangeDate", Destination = "" },
                new FieldMap() { Source = "System.CreatedDate", Destination = "" },
                new FieldMap() { Source = "System.ChangedDate", Destination = "" },
                new FieldMap() { Source = "Microsoft.VSTS.Common.ActivatedDate", Destination = "" },
                new FieldMap() { Source = "System.Rev", Destination = "" },
                new FieldMap() { Source = "*", Destination = "*" }
            };
            var sourceWItypes = sourceWIStore.Projects[sourceConn.ProjectName].WorkItemTypes;
            if (this.WorkItemMappings == null)
            {
                var mappings = new List<WorkItemMap>();
                mappings.AddRange(sourceWItypes.ConvertAll(wit =>
                {
                    return new WorkItemMap()
                        {
                            SourceType = wit.Name,
                            DestinationType = wit.Name,
                            Attachments = WorkItemMap.AttachmentMode.Sync,
                            Fields = defaultFieldRules
                        };
                }));
                this.WorkItemMappings = mappings.ToArray();
            }
            else
            {
                this.WorkItemMappings.ForEach(m =>
                {
                    if (m.Fields == null)
                    {
                        m.Fields = defaultFieldRules;
                    }
                    else if (m.DefaultRules)
                    {
                        var mixin = new List<FieldMap>();
                        mixin.AddRange(m.Fields);
                        mixin.AddRange(defaultFieldRules);
                        m.Fields = mixin.ToArray();
                    }
                });
            }
            this.LinkTypeMap = this.LinkTypeMap ?? new LinkTypeMap[] { new LinkTypeMap() { SourceType = "*", DestinationType = "*" } };
        }
    }
}
