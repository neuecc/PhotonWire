using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using Reactive.Bindings;

namespace PhotonWire.HubInvoker
{
    public partial class MainWindow : MetroWindow
    {
        static readonly List<Window> openingWindows = new List<Window>();
        static bool isAlignVertical = true;

        public MainWindow()
            : this(new MainWindowViewModel())
        {
        }

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
            SetConfigurationMenu(viewModel);

            LogText.TextChanged += (sender, e) =>
            {
                LogText.ScrollToEnd();
            };
            KeyDown += (sender, e) =>
            {
                if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.W)
                {
                    this.Close();
                }
            };

            openingWindows.Add(this);
        }

        void SetConfigurationMenu(MainWindowViewModel viewModel)
        {
            ConfigurationContextMenu.Items.Clear();
            foreach (var item in viewModel.Configrations)
            {
                ConfigurationContextMenu.Items.Add(new MenuItem { FontSize = 12, Header = item.Item1, Command = item.Item2 });
            }
            ConfigurationContextMenu.Items.Add(new Separator());
            var saveCommand = new ReactiveCommand();
            saveCommand.Subscribe(_ =>
            {
                var dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.FilterIndex = 1;
                dialog.Filter = "JSON Configuration|*.json";
                dialog.InitialDirectory = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "configuration");

                if (dialog.ShowDialog() == true)
                {
                    var fName = dialog.FileName;
                    if (!fName.EndsWith(".json")) fName = fName + ".json";
                    viewModel.SaveCurrentConfiguration(fName);
                    viewModel.LoadConfigurations();
                    SetConfigurationMenu(viewModel); // reset
                }
            });
            ConfigurationContextMenu.Items.Add(new MenuItem { FontSize = 12, Header = "Save...", Command = saveCommand });
        }

        protected override void OnClosed(EventArgs e)
        {
            openingWindows.Remove(this);

            // Dispose MainWindowViewModel
            var d = this.DataContext as IDisposable;
            d.Dispose();
            base.OnClosed(e);
        }

        private void EnableScrollViewerMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
        }

        private void ConfigurationButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).ContextMenu.IsEnabled = true;
            (sender as Button).ContextMenu.PlacementTarget = (sender as Button);
            (sender as Button).ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            (sender as Button).ContextMenu.IsOpen = true;
        }

        private void AlignWindow_Click(object sender, RoutedEventArgs e)
        {
            AlignWindows(openingWindows);
        }

        private void DuplicateWindow_Click(object sender, RoutedEventArgs e)
        {
            var window = new MainWindow();
            window.Width = this.Width;
            window.Height = this.Height;
            window.Show();
        }

        static void AlignWindows(IList<Window> windows)
        {
            // Basis is most left window
            var leftWindow = windows.OrderBy(x => x.Left).First();

            // Take the monitor is important for Multi Window
            var windowRect = new System.Drawing.Rectangle((int)leftWindow.Left, (int)leftWindow.Top, (int)leftWindow.Width, (int)leftWindow.Height);
            var monitor = System.Windows.Forms.Screen.FromRectangle(windowRect);

            // Width is max window width
            var maxWidth = windows.Max(x => x.Width);

            // Height is half of monitor
            var windowHeight = (isAlignVertical)
                ? monitor.WorkingArea.Height / 2
                : monitor.WorkingArea.Height;

            var left = leftWindow.Left;
            var index = 0;
            foreach (var window in windows)
            {
                window.WindowState = WindowState.Normal;
                window.Width = maxWidth;
                window.Height = windowHeight;
                if (isAlignVertical)
                {
                    window.Top = (index % 2 == 0) ? monitor.WorkingArea.Top : (monitor.WorkingArea.Top + windowHeight);
                }
                else
                {
                    window.Top = monitor.WorkingArea.Top;
                }
                window.Left = left;
                window.Activate();

                if (isAlignVertical)
                {
                    if (index % 2 == 1) left += maxWidth;
                }
                else
                {
                    left += maxWidth;
                }
                index++;
            }
            windows.First().Activate();

            isAlignVertical = !isAlignVertical;
        }
    }
}