using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WitSyncGUI.Helpers;
using WitSyncGUI.Model;

namespace WitSyncGUI.ViewModel
{
    public class GlobalListsViewModel : StageViewModelBase
    {
        public GlobalListsViewModel()
        {
            DisplayName = "Global Lists";

            List<string> all = Repository.SourceExplorer.GetAllGlobalLists();
            _Included = new SelectionList<string>();
            _Excluded = new SelectionList<string>();
            foreach (string gl in all)
            {
                _Included.Add(
                    new SelectableListItem<string>(gl,
                        Repository.MappingFile.GlobalListsStage.IsIncluded(gl))
                    );
                _Excluded.Add(
                    new SelectableListItem<string>(gl,false)
                    );
            }//for
        }

        private SelectionList<string> _Included;
        public SelectionList<string> Included
        {
            get { return _Included; }
            set
            {
                _Included = value;
                RaisePropertyChanged("Included");
            }
        }

        private SelectionList<string> _Excluded;
        public SelectionList<string> Excluded
        {
            get { return _Excluded; }
            set
            {
                _Excluded = value;
                RaisePropertyChanged("Excluded");
            }
        }
    }
}
