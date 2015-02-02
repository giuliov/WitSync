using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WitSync;

namespace WitSyncGUI.Model
{
    // TODO this class should be factored in the Engine project
    public class MappingFile : PipelineConfiguration
    {
        // TODO look at file extensions
        public static MappingFile LoadFrom(string path)
        {
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

        public static MappingFile Generate()
        {
            return PipelineConfiguration.Generate<MappingFile>();
        }
    }
}
