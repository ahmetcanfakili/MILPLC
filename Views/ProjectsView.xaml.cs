using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.IO;
using System.Globalization;

namespace MILPLC.Views
{
    public partial class ProjectsView : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<ProjectItem> _openProjects;
        private ProjectItem _activeProject;

        public ObservableCollection<ProjectItem> OpenProjects
        {
            get => _openProjects;
            set
            {
                _openProjects = value;
                OnPropertyChanged(nameof(OpenProjects));
            }
        }

        public ProjectItem ActiveProject
        {
            get => _activeProject;
            set
            {
                _activeProject = value;
                OnPropertyChanged(nameof(ActiveProject));
                UpdateActiveProject();
            }
        }

        public ProjectsView()
        {
            InitializeComponent();
            OpenProjects = new ObservableCollection<ProjectItem>();
            DataContext = this;
            UpdateEmptyState();
        }

        // MainWindow'dan çağrılacak metodlar
        public void AddNewProject(string filePath)
        {
            var newProject = new ProjectItem
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(filePath),
                Path = filePath,
                Status = ProjectStatus.Active,
                LastModified = DateTime.Now,
                IsActive = true
            };

            // Önceki aktif projeyi pasif yap
            foreach (var project in OpenProjects)
            {
                project.IsActive = false;
                project.Status = ProjectStatus.Opened;
            }

            OpenProjects.Add(newProject);
            ActiveProject = newProject;
            UpdateEmptyState();
        }

        public void AddExistingProject(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"File not found: {filePath}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Aynı proje zaten açık mı kontrol et
            var existingProject = OpenProjects.FirstOrDefault(p => p.Path.Equals(filePath, StringComparison.OrdinalIgnoreCase));
            if (existingProject != null)
            {
                // Zaten açık, sadece aktif yap
                foreach (var project in OpenProjects)
                {
                    project.IsActive = project == existingProject;
                }
                ActiveProject = existingProject;
                return;
            }

            var fileInfo = new FileInfo(filePath);
            var projectItem = new ProjectItem
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(filePath),
                Path = filePath,
                Status = ProjectStatus.Active,
                LastModified = fileInfo.LastWriteTime,
                IsActive = true
            };

            // Önceki aktif projeyi pasif yap
            foreach (var project in OpenProjects)
            {
                project.IsActive = false;
                project.Status = ProjectStatus.Opened;
            }

            OpenProjects.Add(projectItem);
            ActiveProject = projectItem;
            UpdateEmptyState();
        }

        private void NewProjectBtn_Click(object sender, RoutedEventArgs e)
        {
            // MainWindow'daki New butonunu tetikle (File Dialog açılacak)
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.NewButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void OpenProjectBtn_Click(object sender, RoutedEventArgs e)
        {
            // MainWindow'daki Open butonunu tetikle (File Dialog açılacak)
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.OpenButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void CloseProjectBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedProject = ProjectsListBox.SelectedItem as ProjectItem;
            if (selectedProject != null)
            {
                CloseProject(selectedProject);
            }
            else
            {
                MessageBox.Show("Please select a project to close.", "No Selection",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CloseSingleProjectBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var project = button?.Tag as ProjectItem;
            if (project != null)
            {
                CloseProject(project);
            }
        }

        private void CloseProject(ProjectItem project)
        {
            // Değişiklik kontrolü (basit implementasyon)
            if (project.Status == ProjectStatus.Modified)
            {
                var result = MessageBox.Show(
                    $"Project '{project.Name}' has unsaved changes. Save before closing?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // MainWindow'daki Save butonunu tetikle
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    mainWindow?.SaveButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return; // Kapatma işlemini iptal et
                }
            }

            OpenProjects.Remove(project);

            // Eğer kapatılan proje aktif projeyse, başka bir projeyi aktif yap
            if (project.IsActive && OpenProjects.Count > 0)
            {
                OpenProjects.Last().IsActive = true;
                ActiveProject = OpenProjects.Last();
            }

            UpdateEmptyState();
        }

        private void ProjectsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProject = ProjectsListBox.SelectedItem as ProjectItem;
            if (selectedProject != null && !selectedProject.IsActive)
            {
                // Seçilen projeyi aktif yap
                foreach (var project in OpenProjects)
                {
                    project.IsActive = project == selectedProject;
                }
                ActiveProject = selectedProject;
            }
        }

        private void UpdateActiveProject()
        {
            // Aktif proje değiştiğinde yapılacak işlemler
            foreach (var project in OpenProjects)
            {
                project.Status = project.IsActive ? ProjectStatus.Active : ProjectStatus.Opened;
            }
        }

        private void UpdateEmptyState()
        {
            EmptyState.Visibility = OpenProjects.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ProjectsListBox.Visibility = OpenProjects.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        // Proje durumunu değiştirilen olarak işaretle (örneğin editörde değişiklik yapıldığında)
        public void MarkProjectAsModified(string projectPath)
        {
            var project = OpenProjects.FirstOrDefault(p => p.Path.Equals(projectPath, StringComparison.OrdinalIgnoreCase));
            if (project != null && project.Status != ProjectStatus.Active)
            {
                project.Status = ProjectStatus.Modified;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Data Modelleri
    public class ProjectItem : INotifyPropertyChanged
    {
        private string _name;
        private string _path;
        private ProjectStatus _status;
        private DateTime _lastModified;
        private bool _isActive;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Path
        {
            get => _path;
            set { _path = value; OnPropertyChanged(nameof(Path)); }
        }

        public ProjectStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public DateTime LastModified
        {
            get => _lastModified;
            set { _lastModified = value; OnPropertyChanged(nameof(LastModified)); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum ProjectStatus
    {
        Active,
        Opened,
        Modified,
        Saved
    }

    // Converter - ProjectsView class'ının içine veya ayrı bir class olarak
    public class ProjectStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProjectStatus status)
            {
                return status switch
                {
                    ProjectStatus.Active => new SolidColorBrush(Colors.Green),
                    ProjectStatus.Opened => new SolidColorBrush(Colors.Blue),
                    ProjectStatus.Modified => new SolidColorBrush(Colors.Orange),
                    ProjectStatus.Saved => new SolidColorBrush(Colors.Gray),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}