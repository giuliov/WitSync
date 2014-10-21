using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WitSync
{
    public class GlobalListMapping : MappingBase
    {
        public static GlobalListMapping LoadFrom(string path)
        {
            var input = new StreamReader(path);

            var deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention());

            var mapping = deserializer.Deserialize<GlobalListMapping>(input);
            if (mapping.exclude == null && mapping.include == null)
                throw new IndexOutOfRangeException("At least one of exclude/include must be present.");
            return mapping;
        }

        public List<string> include { get; set; }
        public List<string> exclude { get; set; }

        public bool IsIncluded(string name)
        {
            if (this.exclude == null)
            {
                // default exclude
                return include.Contains(name);
            } else if (this.include == null)
            {
                // default include
                return !exclude.Contains(name);
            }
            else
            {
                // both defined???
                return include.Contains(name) && !exclude.Contains(name);
            }
            throw new IndexOutOfRangeException("At least one of exclude/include must be present.");
        }
    }
}
