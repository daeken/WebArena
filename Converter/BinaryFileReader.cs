using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using MoreLinq;

namespace Converter {
	public class BinaryFileReader {
		readonly byte[] Data;
		int offset;
		public BinaryFileReader(byte[] data) => Data = data;

		public void Seek(int addr) => offset = addr;

		public byte[] ReadBytes(int count) => Data.Slice((offset += count) - count, count).ToArray();

		public T ReadStruct<T>(int addr = -1) {
			if(addr != -1) Seek(addr);
			var bytes = ReadBytes(Marshal.SizeOf<T>());
			var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			var ret = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
			handle.Free();
			return ret;
		}

		public T[] ReadStructs<T>(int count, int addr = -1) => Enumerable.Range(0, count).Select(i => ReadStruct<T>(i == 0 ? addr : -1)).ToArray();
		public T[] ReadMaxStructs<T>(int size, int addr = -1) => ReadStructs<T>(size / Marshal.SizeOf<T>(), addr);
	}
}