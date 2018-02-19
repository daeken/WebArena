using System;
using System.IO;
using System.Linq;
using System.Text;
using static System.Console;

namespace Converter {
	internal static class Program {
		internal static FileStream CreateFile(string fn) {
			Directory.CreateDirectory(Path.GetDirectoryName(fn));
			return File.Open(fn, FileMode.Create);
		}

		internal static string BytesToString(byte[] data) => Encoding.ASCII.GetString(data).Split('\0', 2)[0];
		
		static void Main(string[] args) {
			AssetManager.Instance.AddSource("../tools/baseq3");
			foreach(var fn in AssetManager.Instance.FindFilesByExtension("bsp")) {
				WriteLine($"Converting {fn}");
				BspConverter.Convert(AssetManager.Instance.Open(fn), CreateFile($"output/{Path.ChangeExtension(fn, null)}.json"));
			}
			foreach(var fn in AssetManager.Instance.FindFilesByExtension("md3")) {
				WriteLine($"Converting {fn}");
				Md3Converter.Convert(fn, AssetManager.Instance.Open(fn), CreateFile($"output/{Path.ChangeExtension(fn, null)}.json"));
			}
		}
	}
}