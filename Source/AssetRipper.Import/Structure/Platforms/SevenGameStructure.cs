﻿using AssetRipper.Import.Logging;
using AssetRipper.Import.Structure.Assembly;

namespace AssetRipper.Import.Structure.Platforms
{
	public class SevenGameStructure : PlatformGameStructure
	{
		// only decompile Assembly-CSharp.dll

		#region Fields

		public const string GameName = "7DaysToDie.exe"; // the game we want to rip, awesome game <3
		public const string ExeExtension = ".exe";
		public const string GameUnityDataDir = "7DaysToDie_Data";
		public const string ConfigDataName = "Data"; // where all the xml config files are and asset bundles
		public const string ModsDir = "Mods";
		public const string AddressablesDir = "Addressables";
		public const string BundlesDir = "Bundles";
		public const string PluginsDir = "Plugins";

		#endregion Fields

		#region Properties

		public string? ConfigDataPath { get; protected set; }

		#endregion Properties

		#region Constructors

		/// <summary>
		/// The root path should be where the main executable lies
		/// </summary>
		/// <param name="rootPath"></param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="Exception"></exception>
		public SevenGameStructure(string rootPath)
		{
			if (string.IsNullOrEmpty(rootPath))
			{
				throw new ArgumentNullException(nameof(rootPath));
			}

			if (IsGame7Structure(rootPath))
			{
				Logger.Info("yay");
				m_root = new DirectoryInfo(rootPath);
			}
			else if (IsExecutableFile(rootPath))
			{
				Logger.Info(LogCategory.Import, "7 Days game executable found. Setting root to parent directory");
				m_root = new FileInfo(rootPath).Directory ?? throw new Exception("File has no directory");
			}

			// if is directory where main executable is check here

			// if not found throw

			if (!GetUnityDataDirectory(m_root, out string? dataPath, out string? name))
			{
				throw new Exception($"7 Days Unity Data directory wasn't found");
			}

			Name = name;
			RootPath = m_root.FullName;
			GameDataPath = dataPath;
			StreamingAssetsPath = Path.Combine(GameDataPath, StreamingName);
			ResourcesPath = Path.Combine(GameDataPath, ResourcesName);
			ManagedPath = Path.Combine(GameDataPath, ManagedName);
			UnityPlayerPath = Path.Combine(RootPath, DefaultUnityPlayerName);
			Version = null;

			ConfigDataPath = GetConfigDataPath();

			if (HasMonoAssemblies(ManagedPath))
			{
				Backend = ScriptingBackend.Mono;
			}
			else
			{
				Backend = ScriptingBackend.Unknown;
			}

			DataPaths = new string[] { dataPath };
		}

		#endregion Constructors

		#region Methods

		public string GetConfigDataPath()
		{
			string configDataDir = Path.Combine(RootPath, ConfigDataName);

			if (!Directory.Exists(configDataDir))
			{
				throw new Exception("Failed to find the Config Data folder.");
			}

			return "";
		}

		public override void CollectFiles(bool skipStreamingAssets)
		{
			foreach (string dataPath in DataPaths)
			{
				DirectoryInfo dataDirectory = new DirectoryInfo(dataPath);
				CollectGameFiles(dataDirectory, Files);
			}

			var addressablesDir = new DirectoryInfo(Path.Combine(RootPath, "Data", "Addressables", "Standalone"));
			CollectAddressablesBundles(addressablesDir, Files);

			var bundlesDir = new DirectoryInfo(Path.Combine(RootPath, "Data", "Bundles", "Entities"));
			CollectMiscBundles(bundlesDir, Files);

			//var pluginsDir = new DirectoryInfo(Path.Combine(RootPath, "7DaysToDie_Data", "Plugins", "x86_64"));
			//CollectPlugins(pluginsDir, Files);

			CollectMainAssemblies();

			// we know there is a streaming assets folder
			CollectStreamingAssets(Files);
		}

		protected void CollectAddressablesBundles(DirectoryInfo root, IDictionary<string, string> files)
		{
			// search recursively all sub folders here
			// Should find 15 files currently as of V1.0

			foreach (FileInfo file in root.EnumerateFiles("*.bundle", SearchOption.AllDirectories))
			{
				//if (file.Extension == AssetBundleExtension || file.Extension == AlternateBundleExtension)
				if (IsAddressableBundle(file.FullName))
				{
					string name = Path.GetFileNameWithoutExtension(file.Name).ToLowerInvariant();
					AddAssetBundle(files, name, file.FullName);
				}
			}
		}

		protected void CollectMiscBundles(DirectoryInfo root, IDictionary<string, string> files)
		{
			// only search for two misc bundles here: Entities and Trees
			// the only two files without an extension

			foreach (FileInfo file in root.EnumerateFiles())
			{
				//if (file.Extension == AssetBundleExtension || file.Extension == AlternateBundleExtension)
				if (IsMiscBundle(file.FullName))
				{
					string name = Path.GetFileNameWithoutExtension(file.Name).ToLowerInvariant();
					AddAssetBundle(files, name, file.FullName);
				}
			}
		}

		protected void CollectPlugins(DirectoryInfo root, IDictionary<string, string> files)
		{
			foreach (FileInfo file in root.EnumerateFiles())
			{
				//if (file.Extension == AssetBundleExtension || file.Extension == AlternateBundleExtension)
				if (IsMiscBundle(file.FullName))
				{
					string name = Path.GetFileNameWithoutExtension(file.Name).ToLowerInvariant();
					AddAssetBundle(files, name, file.FullName);
				}
			}
		}

		#endregion Methods


		#region Static Methods

		public static bool IsAddressableBundle(string path)
		{
			Logger.Info($"IsAddressableBundle: {path}");

			return true;
		}

		public static bool IsMiscBundle(string path)
		{
			Logger.Info($"IsMiscBundle: {path}");

			if (path.ToLower().Contains("manifest"))
				return false;

			return true;
		}

		public static bool IsDLL(string path)
		{
			Logger.Info($"IsDLL: {path}");

			return true;
		}

		public static bool IsGame7Structure(string path)
		{
			DirectoryInfo dirInfo;

			if (IsExecutableFile(path)) // first check if the executable is in this director/folder
			{
				dirInfo = new FileInfo(path).Directory ?? throw new Exception("File has no directory");
			}

			// check for unity data dir 7DaysToDie_Data


			// check for config data dir

			//     addressables - 15 bundles, *.bundle

			//     bundles - 2, no extension - trees, entities? empty??

			return true;

			/*
			else if (IsUnityDataDirectory(path))
			{
				return true;
			}
			else
			{
				dinfo = new DirectoryInfo(path);
			}

			if (!dinfo.Exists)
			{
				return false;
			}
			else
			{
				return IsRootGameDirectory(dinfo);
			}*/

		}

		private static bool IsUnityDataDirectory(string folderPath)
		{
			if (string.IsNullOrEmpty(folderPath) || !folderPath.EndsWith($"_{DataFolderName}"))
			{
				return false;
			}

			DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
			if (!directoryInfo.Exists || directoryInfo.Parent == null)
			{
				return false;
			}

			string folderName = directoryInfo.Name;
			string gameName = folderName.Substring(0, folderName.IndexOf($"_{DataFolderName}"));
			string rootPath = directoryInfo.Parent.FullName;

			if (File.Exists(Path.Combine(rootPath, gameName + ExeExtension)))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		private static bool IsExecutableFile(string filePath)
		{
			// looking for 7DaysToDie.exe
			return !string.IsNullOrEmpty(filePath) && File.Exists(Path.Combine(filePath, GameName));
		}

		private static bool IsRootGameDirectory(DirectoryInfo rootDirectory)
		{
			return GetUnityDataDirectory(rootDirectory, out string? _, out string? _);
		}

		private static bool GetUnityDataDirectory(DirectoryInfo rootDirectory, [NotNullWhen(true)] out string? dataPath, [NotNullWhen(true)] out string? name)
		{
			name = "";
			int exeCount = 0;

			Logger.Info($"Found {rootDirectory.EnumerateFiles().Count()} files in this directory.");

			foreach (FileInfo fileInfo in rootDirectory.EnumerateFiles())
			{
				// we're just looking at all the executables and checking if one of them is the Data folder
				// we know what exe's are in the dir already
				// 7DaysToDie.exe
				// 7DaysToDie_EAC.exe
				// 7dLauncher.exe
				// UnityCrashHandler64.exe

				if (fileInfo.Extension == ExeExtension)
				{
					exeCount++;
					name = Path.GetFileNameWithoutExtension(fileInfo.Name);
					string dataFolder = $"{name}_{DataFolderName}"; // this should be 7DaysToDie_Data
					dataPath = Path.Combine(rootDirectory.FullName, dataFolder);

					if (Directory.Exists(dataPath))
					{
						return true;
					}
				}
			}

			if (exeCount > 0)
			{
				name = exeCount == 1 ? name : rootDirectory.Name;
				dataPath = Path.Combine(rootDirectory.FullName, DataFolderName);

				if (Directory.Exists(dataPath))
				{
					return true;
				}
			}

			name = null;
			dataPath = null;
			return false;
		}

		#endregion Static Methods

	}

}
