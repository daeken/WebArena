using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Glob;
using static System.Console;

namespace Converter {
	public class AssetManager {
		public static readonly AssetManager Instance = new AssetManager();
		readonly List<ZipArchive> Paks = new List<ZipArchive>();

		public void AddSource(string dir) {
			foreach(var elem in new [] {"pk3", "PK3", "pK3", "Pk3"}.SelectMany(ext => new DirectoryInfo(dir).GlobFiles($"**/*.{ext}")))
				try {
					Paks.Add(ZipFile.OpenRead(elem.FullName));
				} catch {}
		}

		public IEnumerable<string> FindFilesByPrefix(string prefix) {
			foreach(var pak in Paks) {
				foreach(var file in pak.Entries)
					if(file.FullName.StartsWith(prefix))
						yield return file.FullName;
			}
		}

		public IEnumerable<string> FindFilesByExtension(string extension) {
			extension = "." + extension.ToLower();
			foreach(var pak in Paks) {
				foreach(var file in pak.Entries)
					if(file.FullName.EndsWith(extension))
						yield return file.FullName;
			}
		}

		public IEnumerable<string> FindFilesByNamePrefix(string prefix) {
			foreach(var pak in Paks) {
				foreach(var file in pak.Entries)
					if(Path.GetFileName(file.FullName).StartsWith(prefix))
						yield return file.FullName;
			}
		}

		public byte[] Open(string name) {
			foreach(var pak in Paks) {
				var entry = pak.GetEntry(name);
				if(entry != null) {
					var bytes = new byte[entry.Length];
					entry.Open().Read(bytes, 0, bytes.Length);
					return bytes;
				}
			}
			return null;
		}
	}
}