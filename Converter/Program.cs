using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using Newtonsoft.Json;
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
			using(var sfp = CreateFile("output/materials.json")) {
				var comb = new Dictionary<string, JLayer[]>();
				foreach(var fn in AssetManager.Instance.FindFilesByExtension("shader")) {
					WriteLine($"Converting {fn}");
					foreach(var (k, v) in ShaderConverter.Convert(AssetManager.Instance.Open(fn)))
						comb[k] = v;
				}

				using(var tw = new StreamWriter(sfp))
					tw.Write(JsonConvert.SerializeObject(comb));
			}

			AssetManager.Instance.FindFilesByExtension("bsp").OrderBy(x => x).Distinct().ForEach(fn => {
				WriteLine($"Converting {fn}");
				FileStream fp;
				lock(AssetManager.Instance)
					fp = CreateFile($"output/{Path.ChangeExtension(fn, null)}.json");
				BspConverter.Convert(AssetManager.Instance.Open(fn), fp);
			});
			
			/*foreach(var fn in AssetManager.Instance.FindFilesByExtension("md3")) {
				WriteLine($"Converting {fn}");
				Md3Converter.Convert(fn, AssetManager.Instance.Open(fn), CreateFile($"output/{Path.ChangeExtension(fn, null)}.json"));
			}*/
		}
	}
}