using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    public class TestPlansStageConfiguration : StageConfiguration
    {
        public string SourceQuery { get; set; }
        public string DestinationQuery { get; set; }

        public static TestPlansStageConfiguration Generate()
        {
            return new TestPlansStageConfiguration()
            {
                SourceQuery = "source query",
                DestinationQuery = "dest query",
            };
        }
    }
}
