using System;
using System.IO;

namespace vstat
{
	public static class Utils
	{
		public static void WriteLineError(string format, params object[] args)
		{
			ConsoleWriteLine(ConsoleColor.Red, format, args);
		}

		public static void ConsoleWriteLine(ConsoleColor color, string format, params object[] args)
		{
			ConsoleColor initialColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(format, args);
			Console.ForegroundColor = initialColor;
		}

		public static void ConsoleWrite(ConsoleColor color, string format, params object[] args)
		{
			ConsoleColor initialColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.Write(format, args);
			Console.ForegroundColor = initialColor;
		}

		public static bool CheckFileIsWritable(string file)
		{
			try
			{
				FileInfo fileInfo = new FileInfo(file);
				
				if (!fileInfo.Exists)
				{
					return true;
				}

				using (FileStream stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.None))
				{
					stream.Close();
				}
				return true;
			}
			catch (IOException)
			{
				return false;
			}
		}
	}
}
