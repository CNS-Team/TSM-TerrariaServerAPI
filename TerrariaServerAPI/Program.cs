using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria.ID;
using ReLogic.OS;
using MonoMod.RuntimeDetour.HookGen;
using GameLauncher;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Text;

namespace TerrariaApi.Server
{
	public class Program
	{
		/// <summary>
		/// Initialises any internal values before any server initialisation begins
		/// </summary>
		public static void InitialiseInternals()
		{
			ItemID.Sets.Explosives = ItemID.Sets.Factory.CreateBoolSet(new int[]
			{
				// Bombs
				ItemID.Bomb,
				ItemID.StickyBomb,
				ItemID.BouncyBomb,
				ItemID.BombFish,
				ItemID.DirtBomb,
				ItemID.DirtStickyBomb,
				ItemID.ScarabBomb,
				// Launchers
				ItemID.GrenadeLauncher,
				ItemID.RocketLauncher,
				ItemID.SnowmanCannon,
				ItemID.Celeb2,
				// Rockets
				ItemID.RocketII,
				ItemID.RocketIV,
				ItemID.ClusterRocketII,
				ItemID.MiniNukeII,
				// The following are classified as explosives untill we can figure out a better way.
				ItemID.DryRocket,
				ItemID.WetRocket,
				ItemID.LavaRocket,
				ItemID.HoneyRocket,
				// Explosives & misc
				ItemID.Dynamite,
				ItemID.Explosives,
				ItemID.StickyDynamite
			});

			//Set corrupt tiles to true, as they aren't in vanilla
			TileID.Sets.Corrupt[TileID.CorruptGrass] = true;
			TileID.Sets.Corrupt[TileID.CorruptPlants] = true;
			TileID.Sets.Corrupt[TileID.CorruptThorns] = true;
			TileID.Sets.Corrupt[TileID.CorruptIce] = true;
			TileID.Sets.Corrupt[TileID.CorruptHardenedSand] = true;
			TileID.Sets.Corrupt[TileID.CorruptSandstone] = true;
			TileID.Sets.Corrupt[TileID.Ebonstone] = true;
			TileID.Sets.Corrupt[TileID.Ebonsand] = true;

			//Same again for crimson
			TileID.Sets.Crimson[TileID.FleshBlock] = true;
			TileID.Sets.Crimson[TileID.CrimsonGrass] = true;
			TileID.Sets.Crimson[TileID.FleshIce] = true;
			TileID.Sets.Crimson[TileID.CrimsonPlants] = true;
			TileID.Sets.Crimson[TileID.Crimstone] = true;
			TileID.Sets.Crimson[TileID.Crimsand] = true;
			TileID.Sets.Crimson[TileID.CrimsonVines] = true;
			TileID.Sets.Crimson[TileID.CrimsonThorns] = true;
			TileID.Sets.Crimson[TileID.CrimsonHardenedSand] = true;
			TileID.Sets.Crimson[TileID.CrimsonSandstone] = true;

			//And hallow
			TileID.Sets.Hallow[TileID.HallowedGrass] = true;
			TileID.Sets.Hallow[TileID.HallowedPlants] = true;
			TileID.Sets.Hallow[TileID.HallowedPlants2] = true;
			TileID.Sets.Hallow[TileID.HallowedVines] = true;
			TileID.Sets.Hallow[TileID.HallowedIce] = true;
			TileID.Sets.Hallow[TileID.HallowHardenedSand] = true;
			TileID.Sets.Hallow[TileID.HallowSandstone] = true;
			TileID.Sets.Hallow[TileID.Pearlsand] = true;
			TileID.Sets.Hallow[TileID.Pearlstone] = true;
		}

		/// <summary>
		/// 1.4.4.2 introduced another static variable, which needs to be setup before any Main calls
		/// </summary>
		static void PrepareSavePath(string[] args)
		{
			Terraria.Program.LaunchParameters = Terraria.Utils.ParseArguements(args);
			Terraria.Program.SavePath = (Terraria.Program.LaunchParameters.ContainsKey("-savedirectory")
				? Terraria.Program.LaunchParameters["-savedirectory"]
				: Platform.Get<IPathService>().GetStoragePath("Terraria"));
		}

		public static void Main(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += UnhandledException;
			try
			{
				ServerConfig config = JsonConvert.DeserializeObject<ServerConfig>(File.ReadAllText(args[0]));
				ServerApi.ServerPluginsDirectoryPath = args[1];
				string[] argsCreated = config.CreateArgs(args[2]);
				Process parent = Process.GetProcessById(int.Parse(args[4]));

				PrepareSavePath(argsCreated);
				InitialiseInternals();
				ServerApi.Hooks.AttachOTAPIHooks(argsCreated);

				On.Terraria.Main.Update += delegate (On.Terraria.Main.orig_Update orig, Terraria.Main self, GameTime time)
				{
					orig(self, time);
					if (parent.HasExited)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler3 = new DefaultInterpolatedStringHandler(38, 1);
						defaultInterpolatedStringHandler3.AppendLiteral("parent(pid #");
						defaultInterpolatedStringHandler3.AppendFormatted(parent.Id);
						defaultInterpolatedStringHandler3.AppendLiteral(") exited, server exiting..");
						Console.WriteLine(defaultInterpolatedStringHandler3.ToStringAndClear());
						Environment.Exit(114514);
					}
				};

				Console.InputEncoding = Encoding.UTF8;
				Console.OutputEncoding = Encoding.UTF8;
				Console.SetIn(new TextWrapper(Console.In));
				Console.ReadLine();

				// avoid any Terraria.Main calls here or the heaptile hook will not work.
				// this is because the hook is executed on the Terraria.Main static constructor,
				// and simply referencing it in this method will trigger the constructor.
				StartServer(argsCreated);

				ServerApi.DeInitialize();
			}
			catch (Exception ex)
			{
				ServerApi.LogWriter.ServerWriteLine("Server crashed due to an unhandled exception:\n" + ex, TraceLevel.Error);
			}
		}

		static void StartServer(string[] args)
		{
			Terraria.Main.SkipAssemblyLoad = true;
			if (args.Any(x => x == "-skipassemblyload"))
			{
				Terraria.Main.SkipAssemblyLoad = true;
			}

			Terraria.WindowsLaunch.Main(args);
		}

		/// <summary>
		/// TShock sets up its own unhandled exception handler; this one is just to catch possible
		/// startup exceptions
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Console.WriteLine($"Unhandled exception\n{e}");
		}


		#region Console Hooks

		static Program()
		{
			HookEndpointManager.Add(typeof(Console).GetProperty("ForegroundColor")?.SetMethod, new Action<ConsoleColor>(SetFColor));
			HookEndpointManager.Add(typeof(Console).GetProperty("BackgroundColor")?.SetMethod, new Action<ConsoleColor>(SetBColor));
			HookEndpointManager.Add(typeof(Console).GetProperty("Title")?.SetMethod, new Action<string>(SetTitle));
			HookEndpointManager.Add(typeof(Console).GetMethod("ResetColor"), new Action(ResetColor));
			ResetColor();
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void SetBColor(ConsoleColor value) => Console.WriteLine($"\u0001bgclr{value}");

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void SetFColor(ConsoleColor value)
		{
			switch (value)
			{
				case ConsoleColor.Gray:
					Console.WriteLine("\u0001fgclrLightGray");
					return;
				case ConsoleColor.DarkGray:
					Console.WriteLine("\u0001fgclrGray");
					return;
			}
			Console.WriteLine($"\u0001fgclr{value}");
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void SetTitle(string value) => Console.WriteLine($"\u0001title{value}");

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void ResetColor()
		{
			SetFColor(ConsoleColor.Gray);
			SetBColor(ConsoleColor.Black);
		}

		#endregion
	}
}
