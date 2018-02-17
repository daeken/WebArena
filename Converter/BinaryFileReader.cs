using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Converter {
	public class BinaryFileReader {
		readonly FileStream Fp;
		public BinaryFileReader(string fn) {
			Fp = File.OpenRead(fn);
		}

		public void Seek(int addr) => Fp.Seek(addr, SeekOrigin.Begin);

		public byte[] ReadBytes(int count) {
			var ret = new byte[count];
			Fp.Read(ret, 0, count);
			return ret;
		}

		public T ReadStruct<T>(int addr = -1) {
			if(addr != -1) Seek(addr);
			var bytes = new byte[Marshal.SizeOf<T>()];
			Fp.Read(bytes, 0, bytes.Length);
			var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			var ret = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
			handle.Free();
			return ret;
		}

		public T[] ReadStructs<T>(int count, int addr = -1) => Enumerable.Range(0, count).Select(i => ReadStruct<T>(i == 0 ? addr : -1)).ToArray();
		public T[] ReadMaxStructs<T>(int size, int addr = -1) => ReadStructs<T>(size / Marshal.SizeOf<T>(), addr);
	}
}