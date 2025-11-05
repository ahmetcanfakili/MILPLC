using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Serilog.Log;

namespace MILPLC
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Information("MainWindow loaded successfully.");
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            Information("New file creation process started.");
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            Information("File open dialog displayed.");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Information("File saved successfully.");
            }
            catch (Exception ex)
            {
                Error(ex, "An unexpected error occurred during the save operation.");
            }
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            Warning("Save As operation started. There might be a risk of overwriting the existing file.");
        }
    }
}