using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    public abstract class StageConfiguration : ConfigurationBase
    {
        public bool TestOnly { get; set; }
    }
}
