using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static Serilog.Log;

namespace MILPLC.Views
{
    public partial class LadderEditorView : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<LadderElement> _elements;
        private ObservableCollection<Connection> _connections;
        private double _zoomLevel = 1.0;
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private LadderElement _draggedElement;
        private bool _snapToGrid = true;
        private const double GRID_SIZE = 20;

        // Pan kontrolü için yeni değişkenler
        private bool _isPanning = false;
        private Point _panStartPoint;
        private Point _scrollStartOffset;

        public ObservableCollection<LadderElement> Elements
        {
            get => _elements;
            set
            {
                _elements = value;
                OnPropertyChanged(nameof(Elements));
            }
        }

        public ObservableCollection<Connection> Connections
        {
            get => _connections;
            set
            {
                _connections = value;
                OnPropertyChanged(nameof(Connections));
            }
        }

        public LadderEditorView()
        {
            InitializeComponent();
            Elements = new ObservableCollection<LadderElement>();
            Connections = new ObservableCollection<Connection>();
            DataContext = this;

            InitializeEditor();
        }

        private void InitializeEditor()
        {
            // Add some initial ladder rungs
            for (int i = 0; i < 10; i++)
            {
                AddLadderRung(i * 100);
            }

            UpdateStatus("Editor initialized - Use Mouse Wheel to Zoom, Middle Button to Pan");
        }

        private void AddLadderRung(double yPosition)
        {
            // Left power rail
            var leftRail = new Line
            {
                X1 = 50,
                Y1 = yPosition,
                X2 = 50,
                Y2 = yPosition + 80,
                Stroke = Brushes.Black,
                StrokeThickness = 3
            };
            EditorCanvas.Children.Add(leftRail);

            // Right power rail
            var rightRail = new Line
            {
                X1 = 750,
                Y1 = yPosition,
                X2 = 750,
                Y2 = yPosition + 80,
                Stroke = Brushes.Black,
                StrokeThickness = 3
            };
            EditorCanvas.Children.Add(rightRail);
        }

        // Mouse Wheel Zoom - UserControl seviyesinde handle ediyoruz
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                // Ctrl + Mouse Wheel ile zoom
                double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
                ZoomAtPosition(zoomFactor, e.GetPosition(EditorCanvas));
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.None)
            {
                // Sadece Mouse Wheel ile dikey scroll
                MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }

            base.OnMouseWheel(e);
        }

        private void ZoomAtPosition(double zoomFactor, Point zoomCenter)
        {
            double newZoom = _zoomLevel * zoomFactor;

            // Zoom sınırları
            if (newZoom < 0.1) newZoom = 0.1;
            if (newZoom > 5.0) newZoom = 5.0;

            // Zoom merkezine göre ölçekleme
            double scale = newZoom / _zoomLevel;

            // Scroll viewer pozisyonunu ayarla
            double offsetX = (zoomCenter.X * scale - zoomCenter.X) + MainScrollViewer.HorizontalOffset;
            double offsetY = (zoomCenter.Y * scale - zoomCenter.Y) + MainScrollViewer.VerticalOffset;

            _zoomLevel = newZoom;
            ApplyZoom();

            // Scroll pozisyonunu güncelle
            MainScrollViewer.ScrollToHorizontalOffset(offsetX);
            MainScrollViewer.ScrollToVerticalOffset(offsetY);

            UpdateStatus($"Zoom: {(_zoomLevel * 100):0}%");
        }

        // Middle Mouse Button Pan
        private void EditorCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                // Middle click ile pan başlat
                _isPanning = true;
                _panStartPoint = e.GetPosition(MainScrollViewer);
                _scrollStartOffset = new Point(MainScrollViewer.HorizontalOffset, MainScrollViewer.VerticalOffset);
                EditorCanvas.Cursor = Cursors.SizeAll;
                Mouse.Capture(EditorCanvas);
                e.Handled = true;
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Mevcut left click davranışı
                var position = e.GetPosition(EditorCanvas);
                var element = FindElementAtPosition(position);

                if (element != null)
                {
                    _isDragging = true;
                    _draggedElement = element;
                    _dragStartPoint = position;
                    element.Visual.CaptureMouse();
                    UpdateStatus($"Dragging {element.Type}");
                }
            }
        }

        private void EditorCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released && _isPanning)
            {
                // Pan bitir
                _isPanning = false;
                EditorCanvas.Cursor = Cursors.Arrow;
                Mouse.Capture(null);
                e.Handled = true;
            }

            if (e.LeftButton == MouseButtonState.Released && _isDragging && _draggedElement != null)
            {
                // Mevcut drag bitirme
                _draggedElement.Visual.ReleaseMouseCapture();
                _isDragging = false;
                UpdateStatus($"Placed {_draggedElement.Type}");
                _draggedElement = null;
            }
        }

        private void EditorCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(EditorCanvas);
            CoordinatesText.Text = $" | X: {(int)position.X}, Y: {(int)position.Y}";

            if (_isPanning)
            {
                // Pan işlemi
                Point currentPoint = e.GetPosition(MainScrollViewer);
                Vector delta = _panStartPoint - currentPoint;

                MainScrollViewer.ScrollToHorizontalOffset(_scrollStartOffset.X + delta.X);
                MainScrollViewer.ScrollToVerticalOffset(_scrollStartOffset.Y + delta.Y);

                UpdateStatus("Panning...");
            }
            else if (_isDragging && _draggedElement != null)
            {
                // Mevcut drag işlemi
                var currentPosition = e.GetPosition(EditorCanvas);
                var delta = currentPosition - _dragStartPoint;

                if (_snapToGrid)
                {
                    currentPosition = SnapToGrid(currentPosition);
                }

                Canvas.SetLeft(_draggedElement.Visual, currentPosition.X - _draggedElement.Width / 2);
                Canvas.SetTop(_draggedElement.Visual, currentPosition.Y - _draggedElement.Height / 2);

                UpdateConnections(_draggedElement);
            }
        }

        // Drag and Drop handlers
        private void EditorCanvas_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent("LadderComponent"))
                {
                    var componentType = e.Data.GetData("LadderComponent") as string;
                    var dropPosition = e.GetPosition(EditorCanvas);

                    if (_snapToGrid)
                    {
                        dropPosition = SnapToGrid(dropPosition);
                    }

                    AddLadderElement(componentType, dropPosition);
                    UpdateStatus($"Added {componentType} at ({dropPosition.X}, {dropPosition.Y})");
                }
            }
            catch (Exception ex)
            {
                Error(ex, "Error in drag drop operation");
            }
        }

        private void EditorCanvas_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        // Element management
        private void AddLadderElement(string type, Point position)
        {
            var element = new LadderElement(type, position);
            Elements.Add(element);

            var visual = CreateElementVisual(element);
            element.Visual = visual;

            EditorCanvas.Children.Add(visual);
        }

        private FrameworkElement CreateElementVisual(LadderElement element)
        {
            Border border = new Border
            {
                Width = element.Width,
                Height = element.Height,
                Background = Brushes.LightBlue,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3)
            };

            TextBlock textBlock = new TextBlock
            {
                Text = element.Type,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold
            };

            border.Child = textBlock;

            Canvas.SetLeft(border, element.Position.X - element.Width / 2);
            Canvas.SetTop(border, element.Position.Y - element.Height / 2);

            // Make draggable
            border.MouseLeftButtonDown += (s, e) =>
            {
                _isDragging = true;
                _draggedElement = element;
                _dragStartPoint = e.GetPosition(EditorCanvas);
                border.CaptureMouse();
                e.Handled = true;
            };

            return border;
        }

        private LadderElement FindElementAtPosition(Point position)
        {
            return Elements.FirstOrDefault(e =>
                position.X >= e.Position.X - e.Width / 2 &&
                position.X <= e.Position.X + e.Width / 2 &&
                position.Y >= e.Position.Y - e.Height / 2 &&
                position.Y <= e.Position.Y + e.Height / 2);
        }

        // Connection management
        private void UpdateConnections(LadderElement element)
        {
            foreach (var connection in Connections.Where(c => c.SourceElement == element || c.TargetElement == element))
            {
                UpdateConnectionVisual(connection);
            }
        }

        private void UpdateConnectionVisual(Connection connection)
        {
            if (connection.Visual != null)
            {
                EditorCanvas.Children.Remove(connection.Visual);
            }

            var line = new Line
            {
                X1 = connection.SourceElement.Position.X,
                Y1 = connection.SourceElement.Position.Y,
                X2 = connection.TargetElement.Position.X,
                Y2 = connection.TargetElement.Position.Y,
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                StrokeDashArray = connection.IsVirtual ? new DoubleCollection { 4, 2 } : null
            };

            connection.Visual = line;
            EditorCanvas.Children.Add(line);
        }

        // Utility methods
        private Point SnapToGrid(Point point)
        {
            return new Point(
                Math.Round(point.X / GRID_SIZE) * GRID_SIZE,
                Math.Round(point.Y / GRID_SIZE) * GRID_SIZE
            );
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
            Information($"Ladder Editor: {message}");
        }

        // Toolbar button handlers
        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomAtPosition(1.1, new Point(EditorCanvas.ActualWidth / 2, EditorCanvas.ActualHeight / 2));
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomAtPosition(0.9, new Point(EditorCanvas.ActualWidth / 2, EditorCanvas.ActualHeight / 2));
        }

        private void ToggleSnapToGrid_Click(object sender, RoutedEventArgs e)
        {
            _snapToGrid = !_snapToGrid;
            SnapToGridButton.Content = _snapToGrid ? "Snap: ON" : "Snap: OFF";
            UpdateStatus($"Snap to grid: {(_snapToGrid ? "ON" : "OFF")}");
        }

        private void ApplyZoom()
        {
            var scale = new ScaleTransform(_zoomLevel, _zoomLevel);
            EditorCanvas.LayoutTransform = scale;
            ZoomText.Text = $" | Zoom: {(_zoomLevel * 100):0}%";
        }

        // Public methods for external interaction
        public void AddComponentFromLibrary(string componentType)
        {
            var center = new Point(EditorCanvas.ActualWidth / 2, EditorCanvas.ActualHeight / 2);
            AddLadderElement(componentType, center);
        }

        public void ClearEditor()
        {
            Elements.Clear();
            Connections.Clear();
            EditorCanvas.Children.Clear();
            InitializeEditor();
            UpdateStatus("Editor cleared");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Data Models
    public class LadderElement : INotifyPropertyChanged
    {
        public string Type { get; set; }
        public Point Position { get; set; }
        public double Width { get; set; } = 60;
        public double Height { get; set; } = 40;
        public FrameworkElement Visual { get; set; }

        public LadderElement(string type, Point position)
        {
            Type = type;
            Position = position;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Connection : INotifyPropertyChanged
    {
        public LadderElement SourceElement { get; set; }
        public LadderElement TargetElement { get; set; }
        public bool IsVirtual { get; set; }
        public Line Visual { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}