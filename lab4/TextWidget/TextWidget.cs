using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using Contracts;

namespace TextWidget
{
    [Export(typeof(IWidget))]
    [ExportMetadata("Name", "Text Widget")]
    public class TextWidget : IWidget
    {
        public string Name => "Text Widget";
        public object View { get; }

        private readonly TextBlock _chars;
        private readonly TextBlock _words;

        [ImportingConstructor]
        public TextWidget(IEventAggregator aggregator)
        {
            var panel = new StackPanel { Margin = new Thickness(8) };
            panel.Children.Add(new TextBlock { Text = "Statystyki Tekstu", FontWeight = FontWeights.Bold });
            _chars = new TextBlock { Text = "Znaki: 0" };
            _words = new TextBlock { Text = "S³owa: 0" };
            panel.Children.Add(_chars);
            panel.Children.Add(_words);

            View = new UserControl { Content = panel };

            aggregator.Subscribe<DataSubmittedEvent>(OnDataReceived);
        }

        private void OnDataReceived(DataSubmittedEvent e)
        {
            var text = e.Data;
            var chars = text.Length;
            var words = string.IsNullOrWhiteSpace(text)
                ? 0
                : text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;

            ((UserControl)View).Dispatcher.Invoke(() =>
            {
                _chars.Text = $"Znaki: {chars}";
                _words.Text = $"S³owa: {words}";
            });
        }
    }
}