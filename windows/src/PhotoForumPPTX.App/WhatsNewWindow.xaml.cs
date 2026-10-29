using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.App;

public partial class WhatsNewWindow : Window
{
    public WhatsNewWindow(IReadOnlyList<ReleaseNote> notes)
    {
        InitializeComponent();
        SubtitleText.Text = notes.Count > 0 ? $"v{notes[0].Version} にアップデートしました" : "";

        foreach (var note in notes)
        {
            NotesPanel.Children.Add(new TextBlock
            {
                Text = $"v{note.Version}",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 12, 0, 4),
            });
            foreach (var item in note.Items)
            {
                NotesPanel.Children.Add(new TextBlock { Text = "・" + item, TextWrapping = TextWrapping.Wrap });
            }
            if (note.Image != null)
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Resources", "whatsnew", note.Image + ".png");
                if (File.Exists(path))
                {
                    NotesPanel.Children.Add(new Image
                    {
                        Source = new BitmapImage(new Uri(path)),
                        Stretch = Stretch.Uniform,
                        MaxWidth = 460,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(0, 4, 0, 0),
                    });
                }
            }
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
