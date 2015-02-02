using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    public class StageInfo
    {
        static private StageInfo[] possibleStages = new StageInfo[] {
                new StageInfo() { Order = 10, Type = typeof(GlobalListsStage) },
                new StageInfo() { Order = 20, Type = typeof(AreasAndIterationsStage) },
                new StageInfo() { Order = 21, Type = typeof(AreasStage), ConfigurationProperty = "AreasAndIterationsStageConfiguration" },
                new StageInfo() { Order = 22, Type = typeof(IterationsStage), ConfigurationProperty = "AreasAndIterationsStageConfiguration" },
                new StageInfo() { Order = 30, Type = typeof(WorkItemsStage) },
            };

        public int Order { get; private set; }
        public Type Type { get; private set; }
        public string Name
        {
            get
            {
                return this.Type.Name.Replace("Stage", "");
            }
        }
        public string ConfigurationProperty { get; private set; }

        public StageConfiguration GetConfiguration(PipelineConfiguration parent)
        {
            string propName = this.ConfigurationProperty ?? this.Type.Name;
            return parent.GetType().GetProperty(propName).GetValue(parent) as StageConfiguration;
        }

        public static void Build(PipelineConfiguration configuration, Action<StageInfo> make)
        {
            foreach (var info in possibleStages.OrderBy(x => x.Order))
            {
                if (configuration.PipelineStages.Contains(info.Name, StringComparer.InvariantCultureIgnoreCase))
                {
                    make(info);
                }//if
            }//for
        }

        public static StageInfo Get(string name)
        {
            var info = possibleStages.Where(x => x.Name == DeAlias(name)).FirstOrDefault();
            return info;
        }

        static private string DeAlias(string name)
        {
            switch (name)
            {
                case "Areas":
                    return "AreasAndIterations";
                case "Iterations":
                    return "AreasAndIterations";
                default:
                    return name;
            }
        }
    }//class
}
