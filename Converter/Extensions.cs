using System.Collections.Generic;
using System.Linq;

namespace Converter {
	public static class Extensions {
		public static Dictionary<KeyT, ValueT> ToDictionary<KeyT, ValueT>(this IEnumerable<(KeyT Key, ValueT Value)> seq) =>
			seq.ToDictionary(x => x.Key, x => x.Value);

		public static string Join(this IEnumerable<string> seq, string joiner) => string.Join(joiner, seq);

		public static IEnumerable<(int, T)> Enumerate<T>(this IEnumerable<T> seq) {
			var i = 0;
			foreach(var elem in seq)
				yield return (i++, elem);
		}
	}
}