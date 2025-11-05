using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MILPLC.Views
{
    public partial class ComponentsView : UserControl
    {
        public ObservableCollection<ComponentItem> AllComponents { get; set; }
        public ObservableCollection<ComponentItem> FilteredComponents { get; set; }

        public ComponentsView()
        {
            InitializeComponent();
            InitializeComponents();
            DataContext = this;
        }

        private void InitializeComponents()
        {
            // Örnek komponent verileri
            AllComponents = new ObservableCollection<ComponentItem>
            {
                new ComponentItem
                {
                    Name = "TON - Timer On Delay",
                    Type = "Timer",
                    IconPath = "/Images/timer.png",
                    Description = "Giriş sinyali geldikten belirtilen süre sonunda çıkış veren timer.",
                    Parameters = new ObservableCollection<Parameter>
                    {
                        new Parameter { Name = "PT", Value = "TIME#0s" },
                        new Parameter { Name = "Q", Value = "BOOL" },
                        new Parameter { Name = "ET", Value = "TIME" }
                    }
                },
                new ComponentItem
                {
                    Name = "CTU - Counter Up",
                    Type = "Counter",
                    IconPath = "/Images/counter.png",
                    Description = "Yukarı sayım sayacı. Giriş sinyali her geldiğinde sayacı bir artırır.",
                    Parameters = new ObservableCollection<Parameter>
                    {
                        new Parameter { Name = "CU", Value = "BOOL" },
                        new Parameter { Name = "R", Value = "BOOL" },
                        new Parameter { Name = "PV", Value = "INT" },
                        new Parameter { Name = "Q", Value = "BOOL" },
                        new Parameter { Name = "CV", Value = "INT" }
                    }
                },
                new ComponentItem
                {
                    Name = "MOV - Move",
                    Type = "Data Transfer",
                    IconPath = "/Images/move.png",
                    Description = "Veri transfer fonksiyonu. Kaynak değeri hedefe kopyalar.",
                    Parameters = new ObservableCollection<Parameter>
                    {
                        new Parameter { Name = "IN", Value = "ANY" },
                        new Parameter { Name = "OUT", Value = "ANY" }
                    }
                },
                new ComponentItem
                {
                    Name = "ADD - Addition",
                    Type = "Matematik",
                    IconPath = "/Images/math.png",
                    Description = "İki değeri toplar ve sonucu çıkışa yazar.",
                    Parameters = new ObservableCollection<Parameter>
                    {
                        new Parameter { Name = "IN1", Value = "ANY_NUM" },
                        new Parameter { Name = "IN2", Value = "ANY_NUM" },
                        new Parameter { Name = "OUT", Value = "ANY_NUM" }
                    }
                }
            };

            FilteredComponents = new ObservableCollection<ComponentItem>(AllComponents);
            ComponentsListBox.ItemsSource = FilteredComponents;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text.ToLower();

            FilteredComponents.Clear();

            var filtered = string.IsNullOrEmpty(searchText)
                ? AllComponents
                : AllComponents.Where(c => c.Name.ToLower().Contains(searchText) ||
                                         c.Type.ToLower().Contains(searchText));

            foreach (var item in filtered)
            {
                FilteredComponents.Add(item);
            }
        }

        private void ComponentsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedComponent = ComponentsListBox.SelectedItem as ComponentItem;

            if (selectedComponent != null)
            {
                EmptyStateText.Visibility = Visibility.Collapsed;
                DetailsContent.Visibility = Visibility.Visible;

                // Detayları doldur
                SelectedComponentName.Text = selectedComponent.Name;
                SelectedComponentType.Text = selectedComponent.Type;
                ComponentDescription.Text = selectedComponent.Description;
                ParametersList.ItemsSource = selectedComponent.Parameters;
            }
            else
            {
                EmptyStateText.Visibility = Visibility.Visible;
                DetailsContent.Visibility = Visibility.Collapsed;
            }
        }

        private void AddComponentButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedComponent = ComponentsListBox.SelectedItem as ComponentItem;
            if (selectedComponent != null)
            {
                MessageBox.Show($"{selectedComponent.Name} komponenti eklendi!",
                              "Komponent Ekleme",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
        }
    }

    // Data Modelleri
    public class ComponentItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string IconPath { get; set; }
        public string Description { get; set; }
        public ObservableCollection<Parameter> Parameters { get; set; }
    }

    public class Parameter
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}