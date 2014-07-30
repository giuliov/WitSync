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
        [XmlAttribute("InitalStateOnDestination")]
        public string InitalStateOnDestination;
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
                yield return IDField;
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

    [XmlRoot("Mapping")]
    public class ProjectMapping
    {
        [XmlElement]
        public string SourceQuery;
        [XmlElement]
        public string DestinationQuery;
        [XmlArray]
        public AreaMap[] AreaMap;
        [XmlArray]
        public IterationMap[] IterationMap;
        [XmlElement("WorkItemMap")]
        public WorkItemMap[] WorkItemMappings;

        public FieldMap FindIdFieldForTargetWorkItemType(string destWorkItemType)
        {
            WorkItemMap wiMap = WorkItemMappings.Where(m => m.DestinationType == destWorkItemType).FirstOrDefault();
            return wiMap.IDField;
        }

        public WorkItemMap FindWorkItemTypeMapping(string sourceWorkItemType)
        {
            return WorkItemMappings.Where(m => m.SourceType == sourceWorkItemType).FirstOrDefault();
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
    }
}
