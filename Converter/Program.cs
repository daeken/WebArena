using System;
using System.IO;
using System.Linq;
using static System.Console;

namespace Converter {
	internal static class Program {
		static void Main(string[] args) {
			AssetManager.Instance.AddSource("../tools/baseq3");
			foreach(var fn in AssetManager.Instance.FindFilesByExtension("bsp")) {
				WriteLine($"Converting {fn}");
				BspConverter.Convert(AssetManager.Instance.Open(fn),
					File.Open("output/" + fn.Split('/').Last().Split('.', 2)[0] + ".json", FileMode.Create));
			}
		}
	}
}