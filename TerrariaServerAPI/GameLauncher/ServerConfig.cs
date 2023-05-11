using System.Collections.Generic;
using System.IO;
using System;

namespace GameLauncher;

public class ServerConfig
{
	public enum CultureName
	{
		English = 1,
		German,
		Italian,
		French,
		Spanish,
		Russian,
		Chinese,
		Portuguese,
		Polish
	}

	public string world = string.Empty;

	public CultureName lang = CultureName.English;

	public int maxPlayer = 8;

	public ushort port = 7777;

	public string ip = "0.0.0.0";

	public string password = string.Empty;

	public string[] parameters = Array.Empty<string>();

	public string[] plugins = Array.Empty<string>();

	public string[] CreateArgs(string worldDir)
	{
		List<string> list = new List<string>();
		list.Add("-ip");
		list.Add(ip);
		list.Add("-port");
		list.Add(port.ToString());
		list.Add("-lang");
		int num = (int)lang;
		list.Add(num.ToString());
		list.Add("-maxplayer");
		list.Add(maxPlayer.ToString());
		if (!string.IsNullOrEmpty(world))
		{
			list.Add("-world");
			list.Add(Path.Combine(worldDir, world) + ".wld");
		}
		if (!string.IsNullOrEmpty(password))
		{
			list.Add("-pass");
			list.Add(password);
		}
		list.AddRange(parameters);
		return list.ToArray();
	}
}
