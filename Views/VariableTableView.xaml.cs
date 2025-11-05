using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static Serilog.Log;

namespace MILPLC.Views
{
    public partial class VariableTableView : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<VariableItem> _variables;
        private int _selectedIndex = -1;

        public ObservableCollection<VariableItem> Variables
        {
            get => _variables;
            set
            {
                _variables = value;
                OnPropertyChanged(nameof(Variables));
            }
        }

        public ObservableCollection<string> VariableClasses { get; set; }
        public ObservableCollection<string> VariableTypes { get; set; }

        public VariableTableView()
        {
            InitializeComponent();
            InitializeData();
            DataContext = this;

            // Bind the DataGrid's ItemsSource
            VariablesDataGrid.ItemsSource = Variables;
        }

        private void InitializeData()
        {
            // Sample data - based on the visible table content
            Variables = new ObservableCollection<VariableItem>
            {
                new VariableItem { No = 1, Name = "PBI", Class = "Local", Type = "BOOL", Location = "%IX100.0", InitialValue = "" },
                new VariableItem { No = 2, Name = "LED", Class = "Local", Type = "BOOL", Location = "%CX100.0", InitialValue = "" },
                new VariableItem { No = 3, Name = "my_pt", Class = "Local", Type = "TIME", Location = "", InitialValue = "T#2000ms" },
                new VariableItem { No = 4, Name = "my_ton_in", Class = "Local", Type = "BOOL", Location = "", InitialValue = "" },
                new VariableItem { No = 5, Name = "my_ton", Class = "Local", Type = "TON", Location = "", InitialValue = "" },
                new VariableItem { No = 6, Name = "my_ton_q", Class = "Local", Type = "BOOL", Location = "", InitialValue = "" },
                new VariableItem { No = 7, Name = "my_tof", Class = "Local", Type = "TOF", Location = "", InitialValue = "" },
                new VariableItem { No = 8, Name = "my_tof_q", Class = "Local", Type = "BOOL", Location = "", InitialValue = "" }
            };

            // Variable Classes
            VariableClasses = new ObservableCollection<string>
            {
                "Local",
                "Global",
                "Input",
                "Output",
                "Memory",
                "Constant"
            };

            // Variable Types
            VariableTypes = new ObservableCollection<string>
            {
                "BOOL",
                "BYTE",
                "WORD",
                "DWORD",
                "INT",
                "DINT",
                "REAL",
                "TIME",
                "DATE",
                "STRING",
                "TON",
                "TOF",
                "TP",
                "CTU",
                "CTD"
            };
        }

        private void AddRowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("AddRowButton_Click called");
                Information("Adding new variable row."); 

                int newNo = Variables.Count > 0 ? Variables.Max(v => v.No) + 1 : 1;
                var newVariable = new VariableItem
                {
                    No = newNo,
                    Name = $"var{newNo}",
                    Class = "Local",
                    Type = "BOOL",
                    Location = "",
                    InitialValue = ""
                };

                Console.WriteLine($"New variable created: {newVariable.Name}"); 

                Variables.Add(newVariable);
                Console.WriteLine($"Variable added to collection. Total variables: {Variables.Count}");

                // Select the newly added row
                VariablesDataGrid.SelectedItem = newVariable;
                VariablesDataGrid.ScrollIntoView(newVariable);
                Console.WriteLine("Row selected and scrolled into view");

                Information($"New variable added: {newVariable.Name}"); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}"); 
                Error(ex, "Error while adding new row!");
                MessageBox.Show("Error adding new row: " + ex.Message, "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteRowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedVariable = VariablesDataGrid.SelectedItem as VariableItem;
                if (selectedVariable != null)
                {
                    Information($"Deleting variable: {selectedVariable.Name}"); 

                    Variables.Remove(selectedVariable);
                    UpdateRowNumbers();
                    Information($"Variable deleted: {selectedVariable.Name}"); 
                }
                else
                {
                    Warning("No row selected for deletion."); 
                    MessageBox.Show("Please select a row to delete.", "No Selection",
                                     MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Error(ex, "Error while deleting row!");
                MessageBox.Show("Error deleting row: " + ex.Message, "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedVariable = VariablesDataGrid.SelectedItem as VariableItem;
                if (selectedVariable != null)
                {
                    int currentIndex = Variables.IndexOf(selectedVariable);
                    if (currentIndex > 0)
                    {
                        Debug($"Moving variable up: {selectedVariable.Name}"); 

                        Variables.Move(currentIndex, currentIndex - 1);
                        UpdateRowNumbers();

                        // Keep the same row selected
                        VariablesDataGrid.SelectedItem = selectedVariable;
                        VariablesDataGrid.ScrollIntoView(selectedVariable);

                        Information($"Variable moved up: {selectedVariable.Name}"); 
                    }
                }
                else
                {
                    Warning("No row selected to move up."); 
                    MessageBox.Show("Please select a row to move up.", "No Selection",
                                     MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Error(ex, "Error while moving row up!");
                MessageBox.Show("Error moving row up: " + ex.Message, "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedVariable = VariablesDataGrid.SelectedItem as VariableItem;
                if (selectedVariable != null)
                {
                    int currentIndex = Variables.IndexOf(selectedVariable);
                    if (currentIndex < Variables.Count - 1)
                    {
                        Debug($"Moving variable down: {selectedVariable.Name}");

                        Variables.Move(currentIndex, currentIndex + 1);
                        UpdateRowNumbers();

                        // Keep the same row selected
                        VariablesDataGrid.SelectedItem = selectedVariable;
                        VariablesDataGrid.ScrollIntoView(selectedVariable);

                        Information($"Variable moved down: {selectedVariable.Name}"); 
                    }
                }
                else
                {
                    Warning("No row selected to move down.");
                    MessageBox.Show("Please select a row to move down.", "No Selection",
                                     MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Error(ex, "Error while moving row down!"); 
                MessageBox.Show("Error moving row down: " + ex.Message, "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateRowNumbers()
        {
            for (int i = 0; i < Variables.Count; i++)
            {
                Variables[i].No = i + 1;
            }
        }

        private void VariablesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedVariable = VariablesDataGrid.SelectedItem as VariableItem;
            _selectedIndex = selectedVariable != null ? Variables.IndexOf(selectedVariable) : -1;

            // Update button states
            UpdateButtonStates();
        }

        private void VariablesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var variable = e.Row.Item as VariableItem;
                if (variable != null)
                {
                    Information($"Variable updated: {variable.Name}");
                }
            }
        }

        private void UpdateButtonStates()
        {
            // Enable/disable buttons based on the selected row
            DeleteRowButton.IsEnabled = _selectedIndex >= 0;
            MoveUpButton.IsEnabled = _selectedIndex > 0;
            MoveDownButton.IsEnabled = _selectedIndex >= 0 && _selectedIndex < Variables.Count - 1;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Data Model
    public class VariableItem : INotifyPropertyChanged
    {
        private int _no;
        private string _name;
        private string _class;
        private string _type;
        private string _location;
        private string _initialValue;

        public int No
        {
            get => _no;
            set { _no = value; OnPropertyChanged(nameof(No)); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Class
        {
            get => _class;
            set { _class = value; OnPropertyChanged(nameof(Class)); }
        }

        public string Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(nameof(Type)); }
        }

        public string Location
        {
            get => _location;
            set { _location = value; OnPropertyChanged(nameof(Location)); }
        }

        public string InitialValue
        {
            get => _initialValue;
            set { _initialValue = value; OnPropertyChanged(nameof(InitialValue)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}