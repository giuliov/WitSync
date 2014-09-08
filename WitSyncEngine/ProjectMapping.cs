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

namespace WitSync
{
    [XmlType("States")]
    public class StateList
    {
        [XmlAttribute("InitialStateOnDestination")]
        public string InitialStateOnDestination;
        [XmlElement("State")]
        public StateMap[] States;
    }

    [XmlType("State")]
    public class StateMap
    {
        [XmlAttribute]
        public string Source;
        [XmlAttribute]
        public string Destination;
    }

    [XmlType]
    public class FieldMap
    {
        [XmlAttribute]
        public string Source;
        [XmlAttribute]
        public string Destination;
        [XmlAttribute]
        public string Translate;
        [XmlAttribute]
        public string Set;

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

            return buf.ToString();
        }
    }

    [XmlType("Area")]
    public class AreaMap
    {
        [XmlAttribute]
        public string SourcePath;
        [XmlAttribute]
        public string DestinationPath;
    }

    [XmlType("Iteration")]
    public class IterationMap
    {
        [XmlAttribute]
        public string SourcePath;
        [XmlAttribute]
        public string DestinationPath;
    }

    [XmlType]
    public class WorkItemMap
    {
        [XmlAttribute]
        public string SourceType;
        [XmlAttribute]
        public string DestinationType;
        [XmlElement]
        public FieldMap IDField;
        [XmlElement("States")]
        public StateList StateList;
        [XmlElement("Field")]
        public FieldMap[] Fields;

        [XmlIgnore]
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
        public string SourceType;
        [XmlAttribute]
        public string DestinationType;

        public bool IsWildcard { get { return SourceType == "*"; } }
    }

    [XmlRoot("Mapping")]
    public class ProjectMapping
    {
        [XmlElement]
        public string SourceQuery;
        [XmlElement]
        public string DestinationQuery;
        [XmlElement]
        public string IndexFile;
        [XmlArray]
        public AreaMap[] AreaMap;
        [XmlArray]
        public IterationMap[] IterationMap;
        [XmlElement("WorkItemMap")]
        public WorkItemMap[] WorkItemMappings;
        [XmlArray]
        public LinkTypeMap[] LinkTypeMap;

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
            var serializer = new XmlSerializer(typeof(ProjectMapping));
            using (var reader = new StreamReader(path))
            {
                var mapping = (ProjectMapping)serializer.Deserialize(reader);
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                mapping.Validate(reader.BaseStream, "Mapping.xsd");
                return mapping;
            }
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
        public int ErrorsCount { get { return ErrorMessage.Count; } }
        // Validation Error Message
        [XmlIgnore]
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

        internal void SetDefaults(TfsConnection sourceConn, WorkItemStore sourceWIStore, TfsConnection destConn, WorkItemStore destWIStore)
        {
            // add defaults
            this.SourceQuery = this.SourceQuery ?? string.Format("SELECT id FROM workitems WHERE [Team Project]='{0}'", sourceConn.ProjectName);
            this.DestinationQuery = this.DestinationQuery ?? string.Format("SELECT id FROM workitems WHERE [Team Project]='{0}'", destConn.ProjectName);
            this.AreaMap = this.AreaMap ?? new AreaMap[] { new AreaMap() { SourcePath = "*", DestinationPath = "*" } };
            this.IterationMap = this.IterationMap ?? new IterationMap[] { new IterationMap() { SourcePath = "*", DestinationPath = "*" } };
            if (this.WorkItemMappings == null)
            {
                var mappings = new List<WorkItemMap>();
                foreach (WorkItemType wit in sourceWIStore.Projects[sourceConn.ProjectName].WorkItemTypes)
                {
                    mappings.Add(new WorkItemMap()
                    {
                        SourceType = wit.Name,
                        DestinationType = wit.Name,
                        Fields = new FieldMap[] {
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
                        }
                    });
                }//for
                this.WorkItemMappings = mappings.ToArray();
            }
            this.LinkTypeMap = this.LinkTypeMap ?? new LinkTypeMap[] { new LinkTypeMap() { SourceType = "*", DestinationType = "*" } };
        }
    }
}
