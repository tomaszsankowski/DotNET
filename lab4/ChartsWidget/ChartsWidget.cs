using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Contracts;

namespace ChartsWidget
{
    [Export(typeof(IWidget))]
    [ExportMetadata("Name", "Charts Widget")]
    public class ChartsWidget : IWidget
    {
        public string Name => "Charts Widget";
        public object View { get; }

        private readonly StackPanel _barsPanel;

        [ImportingConstructor]
        public ChartsWidget(IEventAggregator aggregator)
        {
            var root = new StackPanel { Margin = new Thickness(8) };
            root.Children.Add(new TextBlock { Text = "Prosty wykres liczb", FontWeight = FontWeights.Bold });
            _barsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
            root.Children.Add(_barsPanel);
            View = new UserControl { Content = root };

            aggregator.Subscribe<DataSubmittedEvent>(OnDataReceived);
        }

        private void OnDataReceived(DataSubmittedEvent e)
        {
            var numbers = ParseNumbers(e.Data);
            ((UserControl)View).Dispatcher.Invoke(() =>
            {
                _barsPanel.Children.Clear();
                if (!numbers.Any()) return;

                double max = numbers.Max();
                foreach (var n in numbers)
                {
                    double height = (n / max) * 150;
                    var rect = new Rectangle
                    {
                        Width = 20,
                        Height = height,
                        Margin = new Thickness(3),
                        Fill = Brushes.SteelBlue,
                        VerticalAlignment = VerticalAlignment.Bottom
                    };
                    _barsPanel.Children.Add(rect);
                }
            });
        }

        private List<double> ParseNumbers(string text)
        {
            var list = new List<double>();
            foreach (var part in text.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                if (double.TryParse(part, out double val)) list.Add(val);
            return list;
        }
    }
}
