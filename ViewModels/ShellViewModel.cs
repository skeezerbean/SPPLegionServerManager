using SPPLegionServerManager.Models;
using Stylet;
using System.Reflection;

namespace SPPLegionServerManager.ViewModels
{
	public class ShellViewModel : Conductor<IScreen>.StackNavigation
	{
		public string AppTitle { get; set; } = $"SPP Legion Server Manager v{Assembly.GetExecutingAssembly().GetName().Version}";
		public static double WindowTop
		{
			get { return GeneralSettingsManager.GeneralSettings.WindowTop; }
			set { GeneralSettingsManager.GeneralSettings.WindowTop = value; }
		}
		public static double WindowLeft
		{
			get { return GeneralSettingsManager.GeneralSettings.WindowLeft; }
			set { GeneralSettingsManager.GeneralSettings.WindowLeft = value; }
		}
		public static double WindowHeight
		{
			get { return GeneralSettingsManager.GeneralSettings.WindowHeight; }
			set { GeneralSettingsManager.GeneralSettings.WindowHeight = value; }
		}
		public static double WindowWidth
		{
			get { return GeneralSettingsManager.GeneralSettings.WindowWidth; }
			set { GeneralSettingsManager.GeneralSettings.WindowWidth = value; }
		}

		public ShellViewModel(MainViewModel searchViewModel)
		{
			this.DisplayName = string.Empty;
			this.ActivateItem(searchViewModel);
		}
	}
}
