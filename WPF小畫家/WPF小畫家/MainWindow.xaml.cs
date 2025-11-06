using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

// 解決 Path 衝突，與使用 WinForms ColorDialog
using IOPath = System.IO.Path;
using Forms = System.Windows.Forms;

namespace SimplePaint
{
    public partial class MainWindow : Window
    {
        public class ColorItem
        {
            public string Name { get; set; }
            public Brush Brush { get; set; }
        }

        private enum ToolMode { Select, Rectangle, Ellipse, Line, FreeDraw, Text, Eraser }
        private enum DrawingShape { Rectangle, Ellipse, Line, FreeDraw, Text }

        private ToolMode currentTool = ToolMode.Rectangle;
        private DrawingShape currentShape = DrawingShape.Rectangle;

        private bool isDrawing = false;
        private Point startPoint;
        private Shape currentElement;
        private Polyline currentPolyline;
        private TextBox currentTextBox;

        private Brush currentStrokeBrush = Brushes.Black;
        private Brush currentFillBrush = Brushes.Transparent;
        private double currentStrokeThickness = 2;
        private double currentTextSize = 24;

        // 橡皮擦大小（可自行調整）
        private double currentEraserSize => currentStrokeThickness * 10;

        private ObservableCollection<ColorItem> colorList;

        private readonly Stack<UIElement> undoStack = new();
        private readonly Stack<UIElement> redoStack = new();
        private List<UIElement> elementsErasedInStroke = new List<UIElement>();

        private UIElement selectedElement;
        private Rectangle selectionBox;

        public MainWindow()
        {
            InitializeComponent();
            InitializeColors();
            WireCommands();
            BuildSelectionBox();
            UpdateStatus();
        }

        private void InitializeColors()
        {
            colorList = new ObservableCollection<ColorItem>();
            colorList.Add(new ColorItem { Name = "Transparent", Brush = Brushes.Transparent });

            var colors = new[]
            {
                Colors.Black, Colors.Gray, Colors.DarkGray, Colors.LightGray, Colors.White,
                Colors.Red, Colors.DarkRed, Colors.IndianRed, Colors.Pink, Colors.LightPink,
                Colors.Orange, Colors.DarkOrange, Colors.Gold, Colors.Yellow, Colors.LightYellow,
                Colors.Green, Colors.DarkGreen, Colors.LightGreen, Colors.Lime, Colors.YellowGreen,
                Colors.Blue, Colors.DarkBlue, Colors.LightBlue, Colors.SkyBlue, Colors.Cyan,
                Colors.Purple, Colors.DarkMagenta, Colors.Magenta, Colors.Violet, Colors.Lavender,
                Colors.Brown, Colors.SaddleBrown, Colors.Peru, Colors.Tan, Colors.Beige
            };

            foreach (var c in colors)
                colorList.Add(new ColorItem { Name = c.ToString(), Brush = new SolidColorBrush(c) });

            cmbStrokeColor.ItemsSource = colorList;
            cmbFillColor.ItemsSource = colorList;

            cmbStrokeColor.SelectedIndex = 1; // Black
            cmbFillColor.SelectedIndex = 0;   // Transparent
        }

        private void WireCommands()
        {
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, (s, e) => DoUndo(), (s, e) => e.CanExecute = undoStack.Count > 0));
            InputBindings.Add(new KeyBinding(ApplicationCommands.Undo, new KeyGesture(Key.Z, ModifierKeys.Control)));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, (s, e) => DoRedo(), (s, e) => e.CanExecute = redoStack.Count > 0));
            InputBindings.Add(new KeyBinding(ApplicationCommands.Redo, new KeyGesture(Key.Y, ModifierKeys.Control)));

            var saveCmd = new RoutedUICommand("SaveCanvas", "SaveCanvas", typeof(MainWindow));
            CommandBindings.Add(new CommandBinding(saveCmd, (s, e) => SaveCanvas_Click(s, null)));
            InputBindings.Add(new KeyBinding(saveCmd, new KeyGesture(Key.S, ModifierKeys.Control)));

            var newCmd = new RoutedUICommand("NewCanvas", "NewCanvas", typeof(MainWindow));
            CommandBindings.Add(new CommandBinding(newCmd, (s, e) => NewCanvas_Click(s, null)));
            InputBindings.Add(new KeyBinding(newCmd, new KeyGesture(Key.N, ModifierKeys.Control)));
        }

        private void RefreshCommands() => CommandManager.InvalidateRequerySuggested();

        private void BuildSelectionBox()
        {
            selectionBox = new Rectangle
            {
                Stroke = Brushes.Black,
                StrokeDashArray = new DoubleCollection { 2, 2 },
                StrokeThickness = 1,
                Fill = Brushes.Transparent,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            DrawingCanvas.Children.Add(selectionBox);
        }

        private void PushHistoryErased(List<UIElement> erasedElements)
        {
            var marker = new Rectangle { Tag = erasedElements, Opacity = 0, Width = 0, Height = 0 };
            undoStack.Push(marker);
            redoStack.Clear();
            RefreshCommands();
        }

        private void PushHistoryAdd(UIElement el)
        {
            undoStack.Push(el);
            redoStack.Clear();
            RefreshCommands();
        }

        private void DoUndo()
        {
            if (undoStack.Count == 0) return;
            var el = undoStack.Pop();

            if (el is Rectangle marker && marker.Tag is List<UIElement> erasedElements)
            {
                foreach (var erasedEl in erasedElements)
                    DrawingCanvas.Children.Add(erasedEl);
                redoStack.Push(el);
            }
            else
            {
                DrawingCanvas.Children.Remove(el);
                redoStack.Push(el);
            }

            UpdateStatus("Undo");
            RefreshCommands();
        }

        private void DoRedo()
        {
            if (redoStack.Count == 0) return;
            var el = redoStack.Pop();

            if (el is Rectangle marker && marker.Tag is List<UIElement> erasedElements)
            {
                foreach (var erasedEl in erasedElements)
                    DrawingCanvas.Children.Remove(erasedEl);
                undoStack.Push(el);
            }
            else
            {
                DrawingCanvas.Children.Add(el);
                undoStack.Push(el);
            }

            UpdateStatus("Redo");
            RefreshCommands();
        }

        private void SetTool(ToolMode mode)
        {
            currentTool = mode;
            if (mode != ToolMode.Select) ClearSelection();
            DrawingCanvas.Focus();
            UpdateStatus($"Tool: {mode}");
        }

        private void SyncShapeCombo(DrawingShape shape)
        {
            currentShape = shape;
            switch (shape)
            {
                case DrawingShape.Rectangle: cmbShape.SelectedIndex = 0; break;
                case DrawingShape.Ellipse: cmbShape.SelectedIndex = 1; break;
                case DrawingShape.Line: cmbShape.SelectedIndex = 2; break;
                case DrawingShape.FreeDraw: cmbShape.SelectedIndex = 3; break;
                case DrawingShape.Text: cmbShape.SelectedIndex = 4; break;
            }
        }

        private UIElement HitVisual(Point p)
        {
            UIElement hit = null;
            VisualTreeHelper.HitTest(
                DrawingCanvas,
                v => HitTestFilterBehavior.Continue,
                result =>
                {
                    var el = result.VisualHit as UIElement;
                    while (el != null && VisualTreeHelper.GetParent(el) != DrawingCanvas)
                        el = VisualTreeHelper.GetParent(el) as UIElement;

                    if (el != null && el != selectionBox && DrawingCanvas.Children.Contains(el))
                    {
                        hit = el;
                        return HitTestResultBehavior.Stop;
                    }
                    return HitTestResultBehavior.Continue;
                },
                new PointHitTestParameters(p));
            return hit;
        }

        // ✅ 修正：用 TransformBounds 取得元素在 Canvas 的實際邊界（支援 Line/Polyline/未設 Left/Top 的元素）
        private List<UIElement> HitVisualsInArea(Rect area)
        {
            var hitElements = new List<UIElement>();
            VisualTreeHelper.HitTest(
                DrawingCanvas,
                v => HitTestFilterBehavior.Continue,
                result =>
                {
                    var el = result.VisualHit as UIElement;
                    while (el != null && VisualTreeHelper.GetParent(el) != DrawingCanvas)
                        el = VisualTreeHelper.GetParent(el) as UIElement;

                    if (el != null && el != selectionBox && DrawingCanvas.Children.Contains(el) && !hitElements.Contains(el))
                    {
                        Rect localBounds = VisualTreeHelper.GetDescendantBounds(el);
                        // 重要：把元素的本地邊界轉成相對 Canvas 的座標系
                        Rect boundsOnCanvas = el.TransformToAncestor(DrawingCanvas).TransformBounds(localBounds);

                        if (boundsOnCanvas.IntersectsWith(area))
                            hitElements.Add(el);
                    }
                    return HitTestResultBehavior.Continue;
                },
                new GeometryHitTestParameters(new RectangleGeometry(area)));
            return hitElements;
        }

        private void ShowSelection(UIElement el)
        {
            selectedElement = el;
            if (el == null) { selectionBox.Visibility = Visibility.Collapsed; return; }

            Rect local = VisualTreeHelper.GetDescendantBounds(el);
            Rect bounds = el.TransformToAncestor(DrawingCanvas).TransformBounds(local);

            Canvas.SetLeft(selectionBox, bounds.Left - 3);
            Canvas.SetTop(selectionBox, bounds.Top - 3);
            selectionBox.Width = bounds.Width + 6;
            selectionBox.Height = bounds.Height + 6;
            selectionBox.Visibility = Visibility.Visible;
        }

        private void ClearSelection()
        {
            selectedElement = null;
            selectionBox.Visibility = Visibility.Collapsed;
        }

        // ===== Canvas Events =====
        private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (currentTextBox != null) { FinalizeTextBox(commit: true); return; }

            var pos = e.GetPosition(DrawingCanvas);

            if (currentTool == ToolMode.Select)
            {
                var el = HitVisual(pos);
                ShowSelection(el);
                UpdateStatus(el != null ? "Selected" : "No selection");
                return;
            }

            if (currentTool == ToolMode.Eraser)
            {
                isDrawing = true;
                startPoint = pos;
                DrawingCanvas.CaptureMouse();
                elementsErasedInStroke.Clear();

                EraseAtPosition(pos); // 立即擦一下點擊位置
                UpdateStatus("Erasing...");
                return;
            }

            isDrawing = true;
            startPoint = pos;
            DrawingCanvas.CaptureMouse();

            switch (currentTool)
            {
                case ToolMode.Rectangle:
                    currentElement = new Rectangle { Stroke = currentStrokeBrush, Fill = currentFillBrush, StrokeThickness = currentStrokeThickness };
                    Canvas.SetLeft(currentElement, startPoint.X);
                    Canvas.SetTop(currentElement, startPoint.Y);
                    DrawingCanvas.Children.Add(currentElement);
                    break;

                case ToolMode.Ellipse:
                    currentElement = new Ellipse { Stroke = currentStrokeBrush, Fill = currentFillBrush, StrokeThickness = currentStrokeThickness };
                    Canvas.SetLeft(currentElement, startPoint.X);
                    Canvas.SetTop(currentElement, startPoint.Y);
                    DrawingCanvas.Children.Add(currentElement);
                    break;

                case ToolMode.Line:
                    currentElement = new Line
                    {
                        Stroke = currentStrokeBrush,
                        StrokeThickness = currentStrokeThickness,
                        X1 = startPoint.X,
                        Y1 = startPoint.Y,
                        X2 = startPoint.X,
                        Y2 = startPoint.Y
                    };
                    DrawingCanvas.Children.Add(currentElement);
                    break;

                case ToolMode.FreeDraw:
                    currentPolyline = new Polyline
                    {
                        Stroke = currentStrokeBrush,
                        StrokeThickness = currentStrokeThickness,
                        StrokeLineJoin = PenLineJoin.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    };
                    currentPolyline.Points.Add(startPoint);
                    DrawingCanvas.Children.Add(currentPolyline);
                    break;

                case ToolMode.Text:
                    currentTextBox = new TextBox
                    {
                        Text = "",
                        FontSize = currentTextSize,
                        Foreground = (currentStrokeBrush as SolidColorBrush) ?? Brushes.Black,
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(1),
                        MinWidth = 40,
                        AcceptsReturn = true
                    };
                    Canvas.SetLeft(currentTextBox, startPoint.X);
                    Canvas.SetTop(currentTextBox, startPoint.Y);
                    DrawingCanvas.Children.Add(currentTextBox);
                    currentTextBox.Focus();
                    currentTextBox.LostFocus += (s2, e2) => FinalizeTextBox(commit: true);
                    currentTextBox.KeyDown += (s2, e2) =>
                    {
                        if (e2.Key == Key.Escape) { FinalizeTextBox(commit: false); }
                        else if (e2.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                        {
                            e2.Handled = true; FinalizeTextBox(commit: true);
                        }
                    };
                    isDrawing = false;
                    DrawingCanvas.ReleaseMouseCapture();
                    UpdateStatus("Text editing...");
                    return;
            }

            UpdateStatus("Drawing...");
        }

        private void EraseAtPosition(Point pos)
        {
            double r = currentEraserSize / 2.0;
            Rect eraseArea = new Rect(pos.X - r, pos.Y - r, currentEraserSize, currentEraserSize);
            var elementsToErase = HitVisualsInArea(eraseArea);

            foreach (var el in elementsToErase)
            {
                if (DrawingCanvas.Children.Contains(el))
                {
                    DrawingCanvas.Children.Remove(el);
                    if (!elementsErasedInStroke.Contains(el))
                        elementsErasedInStroke.Add(el);
                }
            }
        }

        private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point pt = e.GetPosition(DrawingCanvas);
            txtStatusRight.Text = $"X: {pt.X:F0}, Y: {pt.Y:F0}";

            if (!isDrawing) return;

            if (currentTool == ToolMode.Eraser)
            {
                EraseAtPosition(pt);
                return;
            }

            switch (currentTool)
            {
                case ToolMode.Rectangle:
                case ToolMode.Ellipse:
                    if (currentElement != null)
                    {
                        double w = Math.Abs(pt.X - startPoint.X);
                        double h = Math.Abs(pt.Y - startPoint.Y);
                        double l = Math.Min(startPoint.X, pt.X);
                        double t = Math.Min(startPoint.Y, pt.Y);

                        currentElement.Width = w;
                        currentElement.Height = h;
                        Canvas.SetLeft(currentElement, l);
                        Canvas.SetTop(currentElement, t);
                    }
                    break;

                case ToolMode.Line:
                    if (currentElement is Line line)
                    {
                        line.X2 = pt.X;
                        line.Y2 = pt.Y;
                    }
                    break;

                case ToolMode.FreeDraw:
                    currentPolyline?.Points.Add(pt);
                    break;
            }
        }

        private void DrawingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDrawing) return;

            isDrawing = false;
            DrawingCanvas.ReleaseMouseCapture();

            if (currentTool == ToolMode.Eraser)
            {
                if (elementsErasedInStroke.Count > 0)
                    PushHistoryErased(elementsErasedInStroke);
                elementsErasedInStroke = new List<UIElement>();
            }
            else
            {
                if (currentElement != null) PushHistoryAdd(currentElement);
                else if (currentPolyline != null) PushHistoryAdd(currentPolyline);
            }

            currentElement = null;
            currentPolyline = null;
            UpdateStatus("Ready");
        }

        private void DrawingCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isDrawing) DrawingCanvas_MouseLeftButtonUp(sender, null);
            txtStatusRight.Text = "Ready";
        }

        private void DrawingCanvas_LostMouseCapture(object sender, MouseEventArgs e)
        {
            isDrawing = false;
            currentElement = null;
            currentPolyline = null;
            UpdateStatus("Ready");
        }

        private void FinalizeTextBox(bool commit)
        {
            if (currentTextBox == null) return;

            var tb = currentTextBox;
            currentTextBox = null;

            if (commit && !string.IsNullOrWhiteSpace(tb.Text))
            {
                var text = new TextBlock
                {
                    Text = tb.Text,
                    FontSize = tb.FontSize,
                    Foreground = tb.Foreground,
                    TextWrapping = TextWrapping.Wrap
                };
                Canvas.SetLeft(text, Canvas.GetLeft(tb));
                Canvas.SetTop(text, Canvas.GetTop(tb));

                DrawingCanvas.Children.Remove(tb);
                DrawingCanvas.Children.Add(text);
                PushHistoryAdd(text);
            }
            else
            {
                DrawingCanvas.Children.Remove(tb);
            }

            UpdateStatus("Ready");
        }

        private void ToolToggle_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var tb in new[] { tbSelect, tbLine, tbRect, tbEllipse, tbFree, tbEraser })
                if (tb != null && !ReferenceEquals(tb, sender)) tb.IsChecked = false;

            if (ReferenceEquals(sender, tbSelect)) SetTool(ToolMode.Select);
            else if (ReferenceEquals(sender, tbLine)) { SetTool(ToolMode.Line); SyncShapeCombo(DrawingShape.Line); }
            else if (ReferenceEquals(sender, tbRect)) { SetTool(ToolMode.Rectangle); SyncShapeCombo(DrawingShape.Rectangle); }
            else if (ReferenceEquals(sender, tbEllipse)) { SetTool(ToolMode.Ellipse); SyncShapeCombo(DrawingShape.Ellipse); }
            else if (ReferenceEquals(sender, tbFree)) { SetTool(ToolMode.FreeDraw); SyncShapeCombo(DrawingShape.FreeDraw); }
            else if (ReferenceEquals(sender, tbEraser)) SetTool(ToolMode.Eraser);
        }

        private void ToolToggle_Unchecked(object sender, RoutedEventArgs e) { }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedElement != null)
            {
                DrawingCanvas.Children.Remove(selectedElement);
                PushHistoryAdd(selectedElement);
                ClearSelection();
            }
            else
            {
                if (DrawingCanvas.Children.Count > 0)
                {
                    var last = DrawingCanvas.Children[DrawingCanvas.Children.Count - 1];
                    if (last == selectionBox && DrawingCanvas.Children.Count > 1)
                        last = DrawingCanvas.Children[DrawingCanvas.Children.Count - 2];

                    if (last != selectionBox)
                    {
                        DrawingCanvas.Children.Remove(last);
                        PushHistoryAdd(last);
                    }
                }
            }
        }

        private void cmbStrokeColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbStrokeColor.SelectedItem is ColorItem item)
            {
                currentStrokeBrush = item.Brush;
                UpdateStatus();
            }
        }

        private void cmbFillColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFillColor.SelectedItem is ColorItem item)
            {
                currentFillBrush = item.Brush;
                UpdateStatus();
            }
        }

        private void cmbShape_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbShape?.SelectedItem is ComboBoxItem item)
            {
                if (item.Content?.ToString() == "文字")
                {
                    currentShape = DrawingShape.Text;
                    SetTool(ToolMode.Text);
                    foreach (var tb in new[] { tbSelect, tbLine, tbRect, tbEllipse, tbFree, tbEraser })
                        if (tb != null) tb.IsChecked = false;
                }
                else
                {
                    switch (item.Content?.ToString())
                    {
                        case "矩形": currentShape = DrawingShape.Rectangle; SetTool(ToolMode.Rectangle); if (tbRect != null) tbRect.IsChecked = true; break;
                        case "橢圓": currentShape = DrawingShape.Ellipse; SetTool(ToolMode.Ellipse); if (tbEllipse != null) tbEllipse.IsChecked = true; break;
                        case "線段": currentShape = DrawingShape.Line; SetTool(ToolMode.Line); if (tbLine != null) tbLine.IsChecked = true; break;
                        case "自由筆": currentShape = DrawingShape.FreeDraw; SetTool(ToolMode.FreeDraw); if (tbFree != null) tbFree.IsChecked = true; break;
                    }
                }
                UpdateStatus();
            }
        }

        private void cmbStroke_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbStroke?.SelectedItem is ComboBoxItem item &&
                double.TryParse(item.Content.ToString(), out double thickness))
            {
                currentStrokeThickness = thickness;
                UpdateStatus();
            }
        }

        private void cmbTextSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTextSize?.SelectedItem is ComboBoxItem item &&
                double.TryParse(item.Content.ToString(), out double size))
            {
                currentTextSize = size;
                UpdateStatus();
            }
        }

        private void btnStrokeAdvanced_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Forms.ColorDialog();
            if (dlg.ShowDialog() == Forms.DialogResult.OK)
            {
                var c = dlg.Color;
                var color = Color.FromArgb(c.A, c.R, c.G, c.B);
                currentStrokeBrush = new SolidColorBrush(color);
                var newItem = new ColorItem { Name = color.ToString(), Brush = currentStrokeBrush };
                colorList.Add(newItem);
                cmbStrokeColor.SelectedItem = newItem;
            }
        }

        private void btnFillAdvanced_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Forms.ColorDialog();
            if (dlg.ShowDialog() == Forms.DialogResult.OK)
            {
                var c = dlg.Color;
                var color = Color.FromArgb(c.A, c.R, c.G, c.B);
                currentFillBrush = new SolidColorBrush(color);
                var newItem = new ColorItem { Name = color.ToString(), Brush = currentFillBrush };
                colorList.Add(newItem);
                cmbFillColor.SelectedItem = newItem;
            }
        }

        private void NewCanvas_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("確定要清除畫布嗎？", "開啟新畫布",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                DrawingCanvas.Children.Clear();
                BuildSelectionBox();
                undoStack.Clear();
                redoStack.Clear();
                UpdateStatus("New canvas created");
                RefreshCommands();
            }
        }

        private void SaveCanvas_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|Bitmap Image|*.bmp",
                FileName = "MyDrawing"
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    int width = (int)Math.Ceiling(DrawingCanvas.ActualWidth);
                    int height = (int)Math.Ceiling(DrawingCanvas.ActualHeight);
                    if (width <= 0) width = 800;
                    if (height <= 0) height = 600;

                    DrawingCanvas.Measure(new Size(width, height));
                    DrawingCanvas.Arrange(new Rect(new Size(width, height)));
                    DrawingCanvas.UpdateLayout();

                    var rtb = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Pbgra32);
                    var wasVisible = selectionBox.Visibility;
                    selectionBox.Visibility = Visibility.Collapsed;
                    rtb.Render(DrawingCanvas);
                    selectionBox.Visibility = wasVisible;

                    BitmapEncoder encoder;
                    string ext = IOPath.GetExtension(dlg.FileName).ToLowerInvariant();
                    switch (ext)
                    {
                        case ".jpg":
                        case ".jpeg": encoder = new JpegBitmapEncoder(); break;
                        case ".bmp": encoder = new BmpBitmapEncoder(); break;
                        default: encoder = new PngBitmapEncoder(); break;
                    }
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    using (var fs = new FileStream(dlg.FileName, FileMode.Create))
                        encoder.Save(fs);

                    MessageBox.Show("畫布已儲存！", "儲存成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateStatus($"Saved to {IOPath.GetFileName(dlg.FileName)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"儲存失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateStatus(string message = null)
        {
            if (message != null)
            {
                txtStatusLeft.Text = message;
            }
            else
            {
                txtStatusLeft.Text =
                    $"Shape: {currentShape} | Tool: {currentTool} | " +
                    $"Stroke: {GetColorName(currentStrokeBrush)} | Fill: {GetColorName(currentFillBrush)} | " +
                    $"Thickness: {currentStrokeThickness} | TextSize: {currentTextSize}";
            }
        }

        private string GetColorName(Brush brush)
        {
            if (brush is SolidColorBrush sb)
            {
                if (sb.Color == Colors.Transparent) return "Transparent";
                return sb.Color.ToString();
            }
            return "Unknown";
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (cmbShape != null) cmbShape.SelectionChanged += cmbShape_SelectionChanged;
            if (cmbStroke != null) cmbStroke.SelectionChanged += cmbStroke_SelectionChanged;
            if (cmbTextSize != null) cmbTextSize.SelectionChanged += cmbTextSize_SelectionChanged;

            if (cmbShape != null) cmbShape_SelectionChanged(cmbShape, null);
            if (cmbStroke != null) cmbStroke_SelectionChanged(cmbStroke, null);
            if (cmbTextSize != null) cmbTextSize_SelectionChanged(cmbTextSize, null);

            DrawingCanvas.Focus();
        }
    }
}
