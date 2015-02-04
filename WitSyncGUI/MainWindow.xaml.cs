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

namespace WitSyncGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainViewModel _ViewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize the Model Object
        }

        private void NewCommand(object sender, RoutedEventArgs e)
        {
            _ViewModel = new MainViewModel();
            _ViewModel.New();
            this.DataContext = _ViewModel;
        }

        private void OpenCommand(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var dlg = new Microsoft.Win32.OpenFileDialog();
            //dlg.FileName = ;
            dlg.DefaultExt = ".yml";
            dlg.Filter = "Configuration files (.yml)|*.yml|All Files|*.*";
            dlg.AddExtension = true;
            dlg.CheckFileExists = true;
            dlg.CheckPathExists = true;
            dlg.ValidateNames = true;
            dlg.Title = "Select Configuration file to Open";

            // Show open file dialog box
            var result = dlg.ShowDialog(this);

            // Process open file dialog box results
            if (result == true)
            {
                // discard what's in memory
                _ViewModel = new MainViewModel();
                _ViewModel.Open(dlg.FileName);
                this.DataContext = _ViewModel;
            }
        }

        private void SaveCommand(object sender, RoutedEventArgs e)
        {
            _ViewModel.Save();
        }

        private void SaveAsCommand(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".yml";
            dlg.Filter = "Configuration files (.yml)|*.yml|All Files|*.*";
            dlg.AddExtension = true;
            dlg.CheckPathExists = true;
            dlg.ValidateNames = true;
            dlg.Title = "Save Configuration file As";

            // Show open file dialog box
            var result = dlg.ShowDialog(this);

            // Process open file dialog box results
            if (result == true)
            {
                _ViewModel.SaveAs(dlg.FileName);
            }
        }

        private void CloseCommand(object sender, RoutedEventArgs e)
        {
            _ViewModel = new MainViewModel();
            this.DataContext = _ViewModel;
        }

        private void ExitCommand(object sender, RoutedEventArgs e)
        {
            //TODO check dirty flag and ask to save before exit
            this.Close();
        }

        private void AboutCommand(object sender, RoutedEventArgs e)
        {
            // Instantiate the dialog box
            var dlg = new WitSyncGUI.Windows.AboutWindow();

            // Configure the dialog box
            dlg.Owner = this;

            // Open the dialog box modally 
            dlg.ShowDialog();
        }
    }
}
