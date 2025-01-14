using SPPLegionServerManager.Models;
using Microsoft.Win32;
using Stylet;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.ComponentModel;
using Windows.Security.Authentication.Identity.Core;
using System.Threading;

namespace SPPLegionServerManager.ViewModels
{
	public class MainViewModel : Screen, INotifyPropertyChanged
	{
		internal const int CTRL_C_EVENT = 0;
		[DllImport("kernel32.dll")]
		internal static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool AttachConsole(uint dwProcessId);
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		internal static extern bool FreeConsole();
		[DllImport("kernel32.dll")]
		static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);
		
		// Delegate type to be used as the Handler Routine for SCCH
		delegate bool ConsoleCtrlDelegate(uint CtrlType);

		public string RepackPath
		{
			get { return GeneralSettingsManager.GeneralSettings.RepackPath; }
			set { GeneralSettingsManager.GeneralSettings.RepackPath = value; }
		}

		// Auto start/restart
		public bool AutoStart
		{
			get { return GeneralSettingsManager.GeneralSettings.AutoStart; }
			set { GeneralSettingsManager.GeneralSettings.AutoStart = value; }
		}
		public bool AutoRestart
		{
			get { return GeneralSettingsManager.GeneralSettings.AutoRestart; }
			set { GeneralSettingsManager.GeneralSettings.AutoRestart = value; }
		}

		// Handle start/restart responses
		public bool DBStatus { get; set; } = false;
		public bool BNetStatus { get; set; } = false;
		public bool WorldStatus { get; set; } = false;
		public string DBInfo { get; set; } = string.Empty;

		// Handle button visibility
		public bool ShowDBStartButton { get { return !DBStatus; } } 
		public bool ShowDBStopButton { get { return DBStatus; } }
		public bool ShowBNetStartButton { get { return !BNetStatus; } }
		public bool ShowBNetStopButton { get { return BNetStatus; } }
		public bool ShowWorldStartButton { get { return !WorldStatus; } }
		public bool ShowWorldStopButton { get { return WorldStatus; } }

		private DateTime lastUpdate = DateTime.Now;
		private DateTime lastButton = DateTime.Now;
		private int _DBProcessId = -1;
		private int _BNetProcessId = -1;
		private int _WorldProcessId = -1;
		private static bool dbNeedsStarted = false;
		private static bool dbNeedsStopped = false;
		private static bool bnetNeedsStarted = false;
		private static bool bnetNeedsStopped = false;
		private static bool worldNeedsStarted = false;
		private static bool worldNeedsStopped = false;

		public MainViewModel()
		{
			Models.GeneralSettingsManager.LoadGeneralSettings();
			CheckProcesses();
			UpdateStatus();
			if (AutoStart)
				StartAllServers();
		}

		public void BrowsePath()
		{
			var folderDialog = new OpenFolderDialog
			{
				Title = "Select Folder",
				InitialDirectory = RepackPath
			};

			if (folderDialog.ShowDialog() == true)
			{
				RepackPath = folderDialog.FolderName;
			}
		}

		private void StartServer(string service)
		{
			// If our default, then just drop. If someone actually
			// put the repack in the root then they're a dumbass anyway
			if (RepackPath == "C:\\")
				return;

			Task StartServer = Task.Run(() =>
			{
				// Setup the child process.
				Process p = new Process();
				p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				p.StartInfo.UseShellExecute = false;

				switch (service)
				{
					case "DB":
						p.StartInfo.Arguments = "--defaults-file=" + RepackPath + "\"\\Database\\SPP-Database.ini\" --console --standalone --log_syslog=0 --explicit_defaults_for_timestamp --sql-mode=\"\" --log_error_verbosity=1";
						p.StartInfo.FileName = RepackPath + "\\Database\\bin\\mysqld.exe";
						break;
					case "BNET":
						p.StartInfo.WorkingDirectory = RepackPath + "\\Servers";
						p.StartInfo.FileName = p.StartInfo.WorkingDirectory + "\\bnetserver.exe";
						break;
					case "WORLD":
						p.StartInfo.WorkingDirectory = RepackPath + "\\Servers";
						p.StartInfo.FileName = p.StartInfo.WorkingDirectory + "\\worldserver.exe";
						break;
					default:
						return;
				}

				if (!File.Exists(p.StartInfo.FileName))
					return;

				try
				{
					p.Start();
					p.WaitForExitAsync();
				}
				catch (Exception ex) { Console.WriteLine(ex); };
			});
			while (!StartServer.IsCompleted) { Thread.Sleep(1); }
		}

		private async void StopServer(int processId)
		{
			// If this was called, then we need to unset the auto restart
			// or things will just keep restarting
			AutoRestart = false;

			Task StopServer = Task.Run(() =>
			{
				foreach (Process process in Process.GetProcesses())
				{
					if (process.Id != processId)
						continue;

					if (!AttachConsole((uint)process.Id))
						return;

					SetConsoleCtrlHandler(null, true);
					try
					{
						if (!GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0))
							return;
						process.WaitForExit();
					}
					finally
					{
						
					}
				}
			});
			while (!StopServer.IsCompleted) { await Task.Delay(1); }
			SetConsoleCtrlHandler(null, false);
			FreeConsole();
		}

		public void StartAllServers()
		{
			dbNeedsStarted = true;
			bnetNeedsStarted = true;
			worldNeedsStarted = true;
		}

		public void StopAllServers()
		{
			worldNeedsStopped = true;
			bnetNeedsStopped = true;
			dbNeedsStopped = true;
		}

		public void StartDBServer()
		{
			dbNeedsStarted = true;
		}

		public void StopDBServer()
		{
			dbNeedsStopped = true;
		}

		public void StartBNetServer()
		{
			bnetNeedsStarted = true;
		}

		public void StopBNetServer()
		{
			bnetNeedsStopped = true;
		}

		public void StartWorldServer()
		{
			worldNeedsStarted = true;
		}

		public void StopWorldServer()
		{
			worldNeedsStopped = true;
		}

		// If processes are already running, then find them and populate
		void CheckProcesses()
		{
			foreach(Process process in Process.GetProcesses())
			{
				try
				{
					// only processes we can access - may not allow access to admin priviledge
					// processes or some of the lower processes like idle/system
					if (process.MainModule == null)
						continue;

					// If this wasn't in our repack path, then no need to process
					if (!process.MainModule.FileName.Contains(RepackPath, StringComparison.OrdinalIgnoreCase))
						continue;
				}
				catch (Exception e) { continue; }

				// If we're here then we have a matching path to our repack path
				switch (process.ProcessName)
				{
					case "mysqld":
						_DBProcessId = process.Id;
						break;
					case "bnetserver":
						_BNetProcessId = process.Id;
						break;
					case "worldserver":
						_WorldProcessId = process.Id;
						break;
					default:
						break;
				}
			}
		}

		// See if any are marked to start - any action taken if a server
		// is starting will return back to reprocess the rest once completed
		private void CheckStartingServices()
		{
			// Handle starting services
			if (dbNeedsStarted)
			{
				if (DBStatus)
				{
					dbNeedsStarted = false;
					return;
				}

				StartServer("DB");
				return;
			}

			if (bnetNeedsStarted)
			{
				// Handle launch
				if (BNetStatus)
				{
					bnetNeedsStarted = false;
					return;
				}

				// DB needs handled first, set and check next pass
				if (!DBStatus && !dbNeedsStarted)
				{
					dbNeedsStarted = true;
					return;
				}

				// Still waiting on DB
				if (!DBStatus)
					return;

				StartServer("BNET");
				return;
			}

			if (worldNeedsStarted)
			{
				// Handle launch
				if (WorldStatus)
				{
					worldNeedsStarted = false;
					return;
				}

				// DB needs handled first, set and check next pass
				if (!DBStatus && !worldNeedsStarted)
				{
					dbNeedsStarted = true;
					return;
				}

				// Still waiting on DB
				if (!DBStatus)
					return;

				StartServer("WORLD");
			}
		}

		// See if any are marked to stop - any action taken if a server
		// is stopping will return back to reprocess the rest once completed
		private void CheckStoppingServices()
		{
			if (worldNeedsStopped)
			{
				if (WorldStatus)
					StopServer(_WorldProcessId);
				else
					worldNeedsStopped = false;

				return;
			}

			if (bnetNeedsStopped)
			{
				if (BNetStatus)
					StopServer(_BNetProcessId);
				else
					bnetNeedsStopped = false;

				return;
			}

			if (dbNeedsStopped)
			{
				if (WorldStatus)
				{
					StopServer(_WorldProcessId);
					return;
				}

				if (BNetStatus)
				{
					StopServer(_BNetProcessId);
					return;
				}

				if (DBStatus)
					StopServer(_DBProcessId);
				else
					dbNeedsStopped = false;
			}
		}

		// This will constantly run to update the status
		// or start/stop any services as requested
		private async void UpdateStatus()
		{
			// Keep this running always
			while (1 == 1)
			{
				// Every 2 seconds we want to check for updates in the even the Database Server status or file locations change
				if (lastUpdate.AddSeconds(1) < DateTime.Now)
				{
					lastUpdate = DateTime.Now;

					// Set process IDs if able
					CheckProcesses();

					// Set status if processes accessible
					try
					{
						Process process = Process.GetProcessById(_DBProcessId);

						if (process.ProcessName.Length > 0)
						{
							DBStatus = true;
							DBInfo = "Database access:\nPort: 3310\nUser: spp_user\nPass: 123456";
						}
					}
					catch (Exception ex)
					{
						// exception if the process was unreachable for any reason
						Console.WriteLine(ex);
						_DBProcessId = -1;
						DBStatus = false;
						DBInfo = string.Empty;
						if (AutoRestart)
							StartServer("DB");
					}

					try
					{
						Process process = Process.GetProcessById(_BNetProcessId);

						if (process.ProcessName.Length > 0)
							BNetStatus = true;
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex);
						_BNetProcessId = -1;
						BNetStatus = false;
						if (AutoRestart && DBStatus)
							StartServer("BNET");
					}

					try
					{
						Process process = Process.GetProcessById(_WorldProcessId);

						if (process.ProcessName.Length > 0)
							WorldStatus = true;
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex);
						_WorldProcessId = -1;
						WorldStatus = false;
						if (AutoRestart && DBStatus)
							StartServer("WORLD");
					}

					if (!dbNeedsStopped && !bnetNeedsStopped && !worldNeedsStopped)
						CheckStartingServices();

					if (!dbNeedsStarted && !bnetNeedsStarted && !worldNeedsStarted)
						CheckStoppingServices();
				}

				await Task.Delay(1);
			}
		}
	}
}
