using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WitSync
{
    class MappingFile : PipelineConfiguration
    {
        public static MappingFile LoadFrom(string path)
        {
            // TODO Xml support!
            var input = new System.IO.StreamReader(path);

            var deserializer = new YamlDotNet.Serialization.Deserializer(
                namingConvention: new YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention(), ignoreUnmatched: true);

            var mapping = deserializer.Deserialize<MappingFile>(input);

            return mapping;
        }

        public void SaveAsYaml(string path)
        {
            var output = new System.IO.StreamWriter(path);

            var serializer = new YamlDotNet.Serialization.Serializer(YamlDotNet.Serialization.SerializationOptions.Roundtrip, new YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention());

            serializer.Serialize(output, this);

            output.Close();
        }

        public void SaveAsXml(string path)
        {
            var serializer = new XmlSerializer(typeof(WorkItemsStageConfiguration));
            using (var writer = new StreamWriter(path))
            {
                serializer.Serialize(writer, this);
            }
        }

        public static MappingFile Generate()
        {
            return PipelineConfiguration.Generate<MappingFile>();
        }
    };
}
