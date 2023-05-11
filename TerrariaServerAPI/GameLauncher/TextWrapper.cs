using System.IO;
using System;

namespace GameLauncher;

internal class TextWrapper : TextReader
{
	private readonly TextReader orig;

	public TextWrapper(TextReader orig)
	{
		this.orig = orig;
	}

	public override string ReadLine()
	{
		Console.Out.WriteLine();
		return orig.ReadLine();
	}
}
