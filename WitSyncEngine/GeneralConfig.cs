using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    public enum LoggingLevel
    {
        Normal,
        Verbose,
        Diagnostic
    }
    public class GeneralConfig
    {
        public class ConnectionInfo
        {
            public string CollectionUrl { get; set; }
            public string ProjectName { get; set; }
            public string User { get; set; }
            public string Password { get; set; }
        }

        public ConnectionInfo SourceConnection { get; set; }
        public ConnectionInfo DestinationConnection { get; set; }

        public List<string> PipelineSteps { get; set; }

        public string MappingFile { get; set; }
        public string IndexFile { get; set; }

        public LoggingLevel Logging { get; set; }
        public bool StopPipelineOnFirstError { get; set; }
        public bool TestOnly { get; set; }
    }
}
