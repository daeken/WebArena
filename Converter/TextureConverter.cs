using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using Pfim;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static System.Console;

namespace Converter {
	public class TextureConverter {
		public static readonly TextureConverter Instance = new TextureConverter();
		readonly Dictionary<string, string> FileMap = new Dictionary<string, string>();

		public string Convert(string ifn) {
			if(ifn == null) return null;
			if(FileMap.ContainsKey(ifn))
				return FileMap[ifn];
			else if(FileMap.ContainsValue(ifn))
				return ifn;
			
			var data = AssetManager.Instance.Open(ifn);
			if(data == null) {
				data = AssetManager.Instance.Open(Path.ChangeExtension(ifn, "tga"));
				if(data == null) {
					data = AssetManager.Instance.Open(Path.ChangeExtension(ifn, "jpg"));
					if(data == null) {
						WriteLine($"Could not find file {ifn}");
						return null;
					}
				}
			}

			var ofn = ConvertTga(data);
			if(ofn != null)
				return ofn;
			ofn = ConvertJpeg(data);
			return ofn;
		}

		string AddFile(byte[] data, string ext) {
			var fn = string.Join("", MD5.Create().ComputeHash(data).Select(x => $"{x:X02}")) + "." + ext;
			using(var fp = Program.CreateFile($"output/textures/{fn}"))
				fp.Write(data, 0, data.Length);
			return fn;
		}
		
		string ConvertTga(byte[] data) {
			try {
				var tga = Targa.Create(new MemoryStream(data));
				var im = Image.LoadPixelData<Rgba32>(tga.Data, tga.Width, tga.Height);
				for(var x = 0; x < im.Width; ++x) {
					for(var y = 0; y < im.Height; ++y) {
						var p = im[x, y];
						im[x, y] = new Rgba32 { R = p.B, G = p.G, B = p.R, A = p.A };
					}
				}
				using(var ms = new MemoryStream()) {
					im.SaveAsPng(ms);
					return AddFile(ms.GetBuffer(), "png");
				}
			} catch {
				try {
					var tga = Targa.Create(new MemoryStream(data));
					var im = Image.LoadPixelData<Rgb24>(tga.Data, tga.Width, tga.Height);
					for(var x = 0; x < im.Width; ++x) {
						for(var y = 0; y < im.Height; ++y) {
							var p = im[x, y];
							im[x, y] = new Rgb24 { R = p.B, G = p.G, B = p.R };
						}
					}
					using(var ms = new MemoryStream()) {
						im.SaveAsPng(ms);
						return AddFile(ms.GetBuffer(), "png");
					}

				} catch {
					return null;
				}
			}
		}

		string ConvertJpeg(byte[] data) {
			return AddFile(data, "jpg");
		}
	}
}