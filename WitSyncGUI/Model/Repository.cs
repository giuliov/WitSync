using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSyncGUI.Model
{
    static class Repository
    {
        public static string Filename { get; private set; }

        private static MappingFile instance = null;
        public static MappingFile MappingFile
        {
            get
            {
                if (instance != null)
                    return instance;
                return instance;
            }
        }

        internal static void New(string pathToConfigurationFile)
        {
            instance = MappingFile.Generate();

            Filename = pathToConfigurationFile;
        }

        internal static void Open(string pathToConfigurationFile)
        {
            if (!File.Exists(pathToConfigurationFile))
            {
                throw new InvalidOperationException("Configuration filename not found");
            }

            instance = MappingFile.LoadFrom(pathToConfigurationFile);
            instance.FixNulls();

            Filename = pathToConfigurationFile;
        }
    }
}
