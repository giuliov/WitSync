using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WitSync;

namespace WitSyncGUI.Model
{
    static class Repository
    {
        public static string Filename { get; private set; }

        private static MappingFile configuration = null;
        public static MappingFile MappingFile
        {
            get
            {
                return configuration;
            }
        }

        internal static void New(string pathToConfigurationFile)
        {
            configuration = MappingFile.Generate();

            Filename = pathToConfigurationFile;
        }

        internal static void Open(string pathToConfigurationFile)
        {
            if (!File.Exists(pathToConfigurationFile))
            {
                throw new InvalidOperationException("Configuration filename not found");
            }

            configuration = MappingFile.LoadFrom(pathToConfigurationFile);
            configuration.FixNulls();

            Filename = pathToConfigurationFile;
        }

        private static TfsExplorer sourceExplorer = null;
        public static TfsExplorer SourceExplorer
        {
            get
            {
                if (sourceExplorer != null)
                    return sourceExplorer;
                sourceExplorer = new TfsExplorer();
                sourceExplorer.Connect(MappingFile.SourceConnection);
                return sourceExplorer;
            }
        }

        private static TfsExplorer destinationExplorer = null;
        public static TfsExplorer DestinationExplorer
        {
            get
            {
                if (destinationExplorer != null)
                    return destinationExplorer;
                destinationExplorer = new TfsExplorer();
                destinationExplorer.Connect(MappingFile.DestinationConnection);
                return destinationExplorer;
            }
        }
    }
}
