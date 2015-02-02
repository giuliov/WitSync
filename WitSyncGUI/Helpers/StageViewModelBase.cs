using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WitSyncGUI.Helpers;

namespace WitSyncGUI.Helpers
{
    public class StageViewModelBase : ViewModelBase
    {
        private bool _Enabled = true;
        public bool Enabled
        {
            get { return _Enabled; }
            set
            {
                if (_Enabled != value)
                {
                    _Enabled = value;
                    RaisePropertyChanged("Enabled");
                }
            }
        }

        private string _StageName = "** UNKNOWN **";
        public string StageName
        {
            get { return _StageName; }
            set
            {
                if (_StageName != value)
                {
                    _StageName = value;
                    RaisePropertyChanged("StageName");
                }
            }
        }
    }

}
