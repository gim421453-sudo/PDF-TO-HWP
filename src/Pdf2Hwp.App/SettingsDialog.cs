using Microsoft.Win32;
using Pdf2Hwp.Core;
using System.Windows;
using System.Windows.Controls;

namespace Pdf2Hwp.App;

public sealed class SettingsDialog : Window
{
    private readonly TextBox _outputDirectory;
    private readonly ComboBox _renderQuality;
    private readonly CheckBox _rememberLastFolder;
    public AppSettings Value { get; private set; }

    public SettingsDialog(AppSettings current)
    {
        Value = current;
        Title = "PDF2HWP 설정"; Width = 500; Height = 240; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new DockPanel { Margin = new Thickness(18) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        DockPanel.SetDock(buttons, Dock.Bottom);
        var cancel = new Button { Content = "취소", MinWidth = 84, Margin = new Thickness(0, 12, 8, 0), IsCancel = true };
        var apply = new Button { Content = "저장", MinWidth = 84, Margin = new Thickness(0, 12, 0, 0), IsDefault = true };
        apply.Click += (_, _) => SaveAndClose(); buttons.Children.Add(cancel); buttons.Children.Add(apply); root.Children.Add(buttons);

        var form = new Grid(); form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) }); form.ColumnDefinitions.Add(new ColumnDefinition());
        form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddLabel(form, "출력 폴더", 0); _outputDirectory = new TextBox { Text = current.LastOutputDirectory, Margin = new Thickness(0, 4, 0, 8), VerticalContentAlignment = VerticalAlignment.Center };
        var folder = new Button { Content = "찾기…", Margin = new Thickness(8, 4, 0, 8), MinWidth = 72 };
        var outputRow = new DockPanel(); DockPanel.SetDock(folder, Dock.Right); outputRow.Children.Add(folder); outputRow.Children.Add(_outputDirectory); Grid.SetRow(outputRow, 0); Grid.SetColumn(outputRow, 1); form.Children.Add(outputRow);
        folder.Click += (_, _) => { var dialog = new OpenFolderDialog { Title = "출력 폴더 선택" }; if (dialog.ShowDialog() == true) _outputDirectory.Text = dialog.FolderName; };

        AddLabel(form, "렌더 품질", 1); _renderQuality = new ComboBox { ItemsSource = Enum.GetValues<RenderQuality>(), SelectedItem = current.RenderQuality, Margin = new Thickness(0, 4, 0, 8) }; Grid.SetRow(_renderQuality, 1); Grid.SetColumn(_renderQuality, 1); form.Children.Add(_renderQuality);
        AddLabel(form, "폴더 기억", 2); _rememberLastFolder = new CheckBox { Content = "마지막 출력 폴더 기억", IsChecked = current.RememberLastFolder, Margin = new Thickness(0, 8, 0, 4) }; Grid.SetRow(_rememberLastFolder, 2); Grid.SetColumn(_rememberLastFolder, 1); form.Children.Add(_rememberLastFolder);
        root.Children.Add(form); Content = root;
    }

    private static void AddLabel(Grid grid, string text, int row)
    { var label = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 4, 8, 8) }; Grid.SetRow(label, row); grid.Children.Add(label); }

    private void SaveAndClose()
    {
        if (_renderQuality.SelectedItem is not RenderQuality quality) { MessageBox.Show(this, "렌더 품질을 선택하세요.", "설정", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        Value = Value with { LastOutputDirectory = _outputDirectory.Text.Trim(), RenderQuality = quality, RememberLastFolder = _rememberLastFolder.IsChecked == true };
        DialogResult = true;
    }
}
