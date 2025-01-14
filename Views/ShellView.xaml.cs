using SPPLegionServerManager.Models;
using System.Windows;
using System.Windows.Controls;

namespace SPPLegionServerManager.Views
{
    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    public partial class ShellView : Window
    {
		public ShellView()
        {
			this.InitializeComponent();
			ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(System.Int32.MaxValue));
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// Save the window size/location when exiting
		
			GeneralSettingsManager.SaveSettings(GeneralSettingsManager.SettingsPath, GeneralSettingsManager.GeneralSettings);
		}
	}
}
