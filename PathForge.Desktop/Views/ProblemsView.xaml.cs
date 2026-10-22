using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PathForge.Application.Problems;
using PathForge.Desktop.ViewModels;

namespace PathForge.Desktop.Views;

public partial class ProblemsView : UserControl
{
    public ProblemsView() => InitializeComponent();

    private async void ProblemList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ProblemsViewModel viewModel &&
            sender is ListBox listBox &&
            listBox.SelectedItem is ProblemListItem selectedProblem)
        {
            await viewModel.SelectProblemAsync(selectedProblem);
        }
    }

    private void CreateNewProblem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ProblemsViewModel viewModel)
            return;

        // Text fields commit on LostFocus to keep typing responsive. Flush the
        // active editor before clearing the form so its pending value cannot
        // overwrite the blank draft afterward.
        UpdateTextBindings(this, updateSource: true);
        Keyboard.ClearFocus();
        viewModel.FocusNewProblem();
        ProblemList.SelectedItem = null;
        UpdateTextBindings(this, updateSource: false);

        Dispatcher.BeginInvoke(new Action(() =>
        {
            TitleInput.Focus();
            TitleInput.CaretIndex = TitleInput.Text.Length;
        }), DispatcherPriority.Loaded);
    }

    private static void UpdateTextBindings(DependencyObject root, bool updateSource)
    {
        if (root is TextBox textBox)
        {
            var binding = textBox.GetBindingExpression(TextBox.TextProperty);
            if (updateSource)
                binding?.UpdateSource();
            else
                binding?.UpdateTarget();
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            UpdateTextBindings(VisualTreeHelper.GetChild(root, index), updateSource);
    }
}
