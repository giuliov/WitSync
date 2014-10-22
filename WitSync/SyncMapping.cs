using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    class SyncMapping
    {
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
                globallists = new GlobalListMapping()
                {
                    include = new List<string>() { "a", "b" },
                    exclude = new List<string>() { "a", "b" }
                },
                workitems = new ProjectMapping()
                {
                    SourceQuery = "source query",
                    DestinationQuery = "dest query",
                    IndexFile = "index",
                    AreaMap = new AreaMap[] {
                        new AreaMap() { SourcePath = "src", DestinationPath = "dst" }
                    },
                    IterationMap = new IterationMap[] {
                        new IterationMap() { SourcePath = "src", DestinationPath = "dst" }
                    },
                    WorkItemMappings = new WorkItemMap[] {
                        new WorkItemMap() {
                            SourceType = "srctype", DestinationType="desttype",
                            SyncAttachments = true,
                            IDField = new FieldMap() { Source="src", Destination="dst", Set="set", Translate="tran"},
                            StateList = new StateList() { InitialStateOnDestination="init",
                            States = new StateMap[] {
                                new StateMap() { Source="srcstate", Destination="deststate"}
                            }},
                            Fields = new FieldMap[] {
                                new FieldMap() { Source="src1", Destination="dst1", Set="set1", Translate="tran1"},
                                new FieldMap() { Source="src2", Destination="dst2", Set="set2", Translate="tran2"}
                            }
                        }
                    },
                    LinkTypeMap = new LinkTypeMap[] { 
                        new LinkTypeMap() {SourceType="srclnk", DestinationType="dstlnk" }
                    }
                }
            };
            return self;
        }
    };
}
