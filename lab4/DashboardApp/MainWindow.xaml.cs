using Contracts;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DashboardApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _widgetsPath;
        private CompositionContainer _container;
        private DirectoryCatalog _dirCatalog;
        private IEventAggregator _eventAggregator;
        private FileSystemWatcher _watcher;

        public MainWindow()
        {
            InitializeComponent();

            _widgetsPath = Path.Combine(AppContext.BaseDirectory, "Plugins");
            Directory.CreateDirectory(_widgetsPath);

            InitializeMef();
            LoadWidgets();
            WatchPluginsFolder();
        }

        private void InitializeMef()
        {
            _dirCatalog = new DirectoryCatalog(_widgetsPath, "*.dll");
            _container = new CompositionContainer(_dirCatalog);

            _eventAggregator = _container.GetExportedValues<IEventAggregator>().FirstOrDefault()
                ?? new SimpleEventAggregator();

            _container.ComposeExportedValue<IEventAggregator>(_eventAggregator);
        }

        private void LoadWidgets()
        {
            WidgetsTab.Items.Clear();
            var widgets = _container.GetExports<IWidget, IDictionary<string, object>>();
            foreach (var w in widgets)
            {
                string? name = w.Metadata?.TryGetValue("Name", out var v) == true ? v as string : "Unknown";

                var widget = w.Value;
                WidgetsTab.Items.Add(new TabItem { Header = $"{widget.Name} ({name})", Content = widget.View });
            }
        }

        private void WatchPluginsFolder()
        {
            _watcher = new FileSystemWatcher(_widgetsPath, "*.dll")
            {
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };
            _watcher.Created += (_, __) => Dispatcher.Invoke(ReloadPlugins);
            _watcher.Deleted += (_, __) => Dispatcher.Invoke(ReloadPlugins);
            _watcher.Changed += (_, __) => Dispatcher.Invoke(ReloadPlugins);
        }

        private void ReloadPlugins()
        {
            _dirCatalog.Refresh();
            LoadWidgets();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string text = TextBox.Text ?? string.Empty;
            _eventAggregator.Publish(new DataSubmittedEvent(text));
        }
    }
}