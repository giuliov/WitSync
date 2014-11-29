using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    public class TestPlansStageConfiguration : StageConfiguration
    {
        public List<string> include { get; set; }
        public List<string> exclude { get; set; }

        public bool IsIncluded(string name)
        {
            if (this.exclude == null)
            {
                // default exclude
                return include.Contains(name);
            }
            else if (this.include == null)
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

        public static TestPlansStageConfiguration Generate()
        {
            return new TestPlansStageConfiguration()
            {
                include = new List<string>() { "incl1", "incl2" },
                exclude = new List<string>() { "excl3", "excl4" }
            };
        }
    }
}
