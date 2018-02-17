using System;

namespace Converter {
	internal static class Program {
		static void Main(string[] args) {
			var bspc = new BspConverter(args[0]);
		}
	}
}