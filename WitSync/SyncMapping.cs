using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    class SyncMapping
    {
        public GeneralConfig config { get; set; }
        public GlobalListMapping globallists { get; set; }
        public ProjectMapping workitems { get; set; }

        public static SyncMapping LoadFrom(string path)
        {
            var input = new System.IO.StreamReader(path);

            var deserializer = new YamlDotNet.Serialization.Deserializer(
                namingConvention: new YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention(), ignoreUnmatched: true);

            var mapping = deserializer.Deserialize<SyncMapping>(input);

            return mapping;
        }

        public void SaveTo(string path)
        {
            var output = new System.IO.StreamWriter(path);

            var serializer = new YamlDotNet.Serialization.Serializer(YamlDotNet.Serialization.SerializationOptions.Roundtrip, new YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention());

            serializer.Serialize(output, this);

            output.Close();
        }

        public static SyncMapping Generate()
        {
            var self = new SyncMapping()
            {
                config = new GeneralConfig()
                {
                    SourceConnection = new GeneralConfig.ConnectionInfo()
                    {
                        CollectionUrl = "http://localhost:8080/tfs/DefaultCollection",
                        ProjectName = "yourSourceProject",
                        User = "sourceUser",
                        Password = "***"
                    },
                    DestinationConnection = new GeneralConfig.ConnectionInfo()
                    {
                        CollectionUrl = "http://localhost:8080/tfs/DefaultCollection",
                        ProjectName = "yourTargetProject",
                        User = "targetUser",
                        Password = "***"
                    },
                    MappingFile = "yourMap.yml",
                    PipelineSteps = new List<string>() { "step1", "step2" },
                    StopPipelineOnFirstError = true,
                    TestOnly = true,
                    Logging = LoggingLevel.Diagnostic,
                    IndexFile = "index.xml",
                    ChangeLogFile = "changes.csv",
                    LogFile = "log.txt",
                    AdvancedOptions = new List<string>() { "opt1", "opt2" },
                },
                globallists = new GlobalListMapping()
                {
                    include = new List<string>() { "incl1", "incl2" },
                    exclude = new List<string>() { "excl3", "excl4" }
                },
                workitems = new ProjectMapping()
                {
                    SourceQuery = "source query",
                    DestinationQuery = "dest query",
                    IndexFile = "index",
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
                }
            };
            return self;
        }
    };
}
