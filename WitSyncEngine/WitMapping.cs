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
        [XmlAttribute("InitialStateOnDestination")]
        public string InitialStateOnDestination { get; set; }
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
            this.SyncAttachments = true;
            this.DefaultRules = true;
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
        public bool SyncAttachments { get; set; }
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
    public class ProjectMapping : MappingBase
    {
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


        public static ProjectMapping LoadFrom(string path)
        {
            var input = new StreamReader(path);

            var deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention());

            var mapping = deserializer.Deserialize<ProjectMapping>(input);

            return mapping;
        }

        public void RebuildMappingIndexes()
        {
            //no-op
        }

        public void SaveTo(string path)
        {
            var serializer = new XmlSerializer(typeof(ProjectMapping));
            using (var writer = new StreamWriter(path))
            {
                serializer.Serialize(writer, this);
            }
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

        public static void GenerateSampleMappingFile(string path)
        {
            //TODO (very poor now)
            var mapping = new ProjectMapping()
            {
                SourceQuery = "sq",
                DestinationQuery = "dq",
                WorkItemMappings = new WorkItemMap[] {
                    new WorkItemMap() {
                        SourceType = "st",
                        DestinationType = "dt",
                        IDField = new FieldMap() {
                            Source="id", Destination="destid"
                        },
                        Fields = new FieldMap[] {
                            new FieldMap() {
                                Source = "*", Destination="*"
                            }
                        }
                    }
                }
            };
            mapping.SaveTo(path);
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
                // HACK these names are OK for Scrum, but ...
                new FieldMap() { Source = "Area ID", Destination = "" },
                new FieldMap() { Source = "Area Path", Destination = "Area Path", Translate = "MapAreaPath"},
                new FieldMap() { Source = "Iteration ID", Destination = "" },
                new FieldMap() { Source = "Iteration Path", Destination = "Iteration Path", Translate = "MapIterationPath" },
                new FieldMap() { Source = "Reason", Destination = "" },
                new FieldMap() { Source = "State Change Date", Destination = "" },
                new FieldMap() { Source = "Created Date", Destination = "" },
                new FieldMap() { Source = "Changed Date", Destination = "" },
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
                            SyncAttachments = true,
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
