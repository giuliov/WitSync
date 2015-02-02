using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WitSyncGUI.ViewModel;

namespace WitSyncGUI.View
{
    /// <summary>
    /// Interaction logic for GeneralView.xaml
    /// </summary>
    public partial class GeneralView : UserControl
    {
        GeneralViewModel _ViewModel;

        public GeneralView()
        {
            InitializeComponent();

            // Initialize the View Model Object
            _ViewModel = (GeneralViewModel)this.Resources["viewModel"];
        }
    }
}
