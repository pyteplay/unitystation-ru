using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace SecureStuff
{
	public static class HubValidation
	{
		internal static bool? trustedMode = null;

		private static NamedPipeClientStream clientPipe;
		private static StreamReader reader;
		private static StreamWriter writer;

		private static List<string> allowedAPIHosts;
		private static List<string> AllowedAPIHosts
		{
			get
			{
				if (allowedAPIHosts == null)
				{
					LoadCashedURLConfiguration();
				}

				return allowedAPIHosts;
			}
		}


		private static List<string> allowedOpenHosts;
		private static List<string> AllowedOpenHosts
		{
			get
			{
				if (allowedOpenHosts == null)
				{
					LoadCashedURLConfiguration();
				}

				return allowedOpenHosts;
			}
		}

		private enum ClientRequest
		{
			URL = 1,
			API_URL = 2,
			Host_Trust_Mode = 3,
		}

		private class URLData
		{
			public List<string> AllowedOpenHosts = new List<string>();
			public List<string> AllowedAPIHosts = new List<string>();
		}

		private static async Task<bool> SetUp(string OnFailText, string URLClipboard = "")
		{
			int timeout = 5000;
			clientPipe = new NamedPipeClientStream(".", "Unitystation_Hub_Build_Communication", PipeDirection.InOut);
			var task = clientPipe.ConnectAsync();
			if (await Task.WhenAny(task, Task.Delay(timeout)) == task) {
				reader = new StreamReader(clientPipe);
				writer = new StreamWriter(clientPipe);
				return true;
			} else {
				HubNotConnectedPopUp.Instance.SetUp(OnFailText, URLClipboard);
				return false;
			}

		}

		private static void LoadCashedURLConfiguration()
		{
			var path = Path.Combine(Application.persistentDataPath, AccessFile.ForkName, "TrustedURLs.json");


			// Check if the file already exists
			if (File.Exists(path) == false)
			{
				// Create the file at the specified path
				File.Create(path).Close();
				File.WriteAllText(path, JsonConvert.SerializeObject(new URLData()));
			}

			var data = JsonConvert.DeserializeObject<URLData>(File.ReadAllText(path));
			allowedOpenHosts = data.AllowedOpenHosts;
			allowedAPIHosts = data.AllowedAPIHosts;
		}

		private static void SaveCashedURLConfiguration(URLData URLData)
		{
			var path = Path.Combine(Application.persistentDataPath, AccessFile.ForkName, "TrustedURLs.json");

			Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, AccessFile.ForkName));

			File.WriteAllText(path, JsonConvert.SerializeObject(URLData));

		}

		private static void AddTrustedHost(string Host, bool IsAPI)
		{
			if (IsAPI)
			{
				AllowedAPIHosts.Add(Host);
			}
			else
			{
				AllowedOpenHosts.Add(Host);
			}

			SaveCashedURLConfiguration(new URLData()
			{
				AllowedAPIHosts = allowedAPIHosts,
				AllowedOpenHosts = AllowedOpenHosts
			});
		}

		public static bool TrustedMode
		{
			get
			{
				return true;
#if UNITY_EDITOR
				return true;
#endif

				if (trustedMode == null)
				{
					string[] commandLineArgs = Environment.GetCommandLineArgs();
					trustedMode = commandLineArgs.Any(x => x == "--trusted");
				}

				return trustedMode.Value;
			}
		}

		public static async Task<bool> RequestAPIURL(Uri URL, string JustificationReason, bool addAsTrusted)
		{
			if (TrustedMode) return true;
			if (AllowedAPIHosts.Contains(URL.Host))
			{
				return true;
			}

			var AbleToConnect = true;
			if (writer == null || (clientPipe != null && clientPipe.IsConnected == false))
			{
				AbleToConnect = await SetUp($" Wasn't able to connect the hub to Evaluate new domain for API Request URL {URL}, The hub is used as a secure method for getting user input ");
			}

			if (AbleToConnect == false)
			{
				return false;
			}


			writer.WriteLine($"{ClientRequest.API_URL},{URL},{JustificationReason}");
			writer.Flush();

			var APIURL = bool.Parse(await reader.ReadLineAsync());
			if (APIURL && addAsTrusted)
			{
				AddTrustedHost(URL.Host, true);
			}


			return APIURL;
		}

		public static async Task<bool> RequestOpenURL(Uri URL, string justificationReason, bool addAsTrusted)
		{
			if (TrustedMode) return true;
			if (AllowedOpenHosts.Contains(URL.Host))
			{
				return true;
			}

			var AbleToConnect = true;
			if (writer == null || (clientPipe != null && clientPipe.IsConnected == false))
			{
				AbleToConnect = await SetUp(
					$" Wasn't able to connect the hub to Get user input on open URL {URL}, The hub is used as a secure method for getting user input ",
					URL.ToString());
			}

			if (AbleToConnect == false)
			{
				return false;
			}

			writer.WriteLine($"{ClientRequest.URL},{URL},{justificationReason}");
			writer.Flush();

			var openURL = bool.Parse(reader.ReadLine());
			if (openURL && addAsTrusted)
			{
				AddTrustedHost(URL.Host, false);
			}

			return openURL;
		}


		public static async Task<bool> RequestTrustedMode(string JustificationReason)
		{
			if (TrustedMode) return true;
			var AbleToConnect = true;
			if (writer == null || (clientPipe != null && clientPipe.IsConnected == false))
			{
				AbleToConnect = await SetUp($" Wasn't able to connect the hub to Turn on trusted mode " +
				                            $"(Access to Verbal viewer on client side, automatic yes to API and open URL requests)," +
				                            $" The hub is used as a secure method for getting user input ");;
			}

			if (AbleToConnect == false)
			{
				return false;
			}

			await writer.WriteLineAsync($"{ClientRequest.Host_Trust_Mode},{JustificationReason}");
			await writer.FlushAsync();


			bool IsTrusted = bool.Parse(await reader.ReadLineAsync());
			if (IsTrusted)
			{
				trustedMode = true;
			}
			else
			{
				trustedMode = false;
			}

			return IsTrusted;
		}
	}
}