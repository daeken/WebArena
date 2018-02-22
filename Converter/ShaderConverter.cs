using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using static System.Console;

namespace Converter {
	public class ShaderConverter {
		static Dictionary<string, List<List<string[]>>> ParseShaders(string code) {
			code = code.Replace("\r", "\n");
			code = Regex.Replace(code, "//.*?\n$", "\n", RegexOptions.Singleline | RegexOptions.Multiline);
			code = code.Replace("{", "\n{\n");
			code = Regex.Replace(code, @"^\s+", "", RegexOptions.Singleline | RegexOptions.Multiline);
			code = Regex.Replace(code, @"\s+$", "\n", RegexOptions.Singleline | RegexOptions.Multiline);
			code = Regex.Replace(code, "\n[\n \t]*", "\n", RegexOptions.Singleline | RegexOptions.Multiline);
			code = Regex.Replace(code, @"[ \t]+", " ", RegexOptions.Singleline | RegexOptions.Multiline);
			var data = code.Trim().Split('\n');

			var inside = false;
			List<string[]> stage = null;
			var instage = false;
			string ntex = null;
			var odict = new Dictionary<string, List<List<string[]>>>();
			for(var i = 0; i < data.Length; ++i) {
				if(data[i] == "") continue;
				if(!inside) {
					ntex = data[i++];
					Debug.Assert(data[i] == "{");
					odict[ntex] = new List<List<string[]>>();
					odict[ntex].Add(stage = new List<string[]>());
					inside = true;
				} else if(data[i] == "{") {
					odict[ntex].Add(stage = new List<string[]>());
					instage = true;
				} else if(data[i] == "}") {
					if(instage)
						instage = false;
					else
						inside = false;
				} else
					stage.Add(data[i].Split(' '));
			}
			
			return odict;
		}
		
		public static void Convert(byte[] istream, Stream ostream) {
			var shaders = ParseShaders(Encoding.ASCII.GetString(istream));
			foreach(var (k, v) in shaders) {
				WriteLine($"Shader '{k}'");
				foreach(var elems in v) {
					Write($"- ");
					foreach(var elem in elems) {
						Write(" ~~~ ");
						foreach(var sub in elem)
							Write($"'{sub}' ; ");
					}

					WriteLine();
				}
			}
		}
	}
}