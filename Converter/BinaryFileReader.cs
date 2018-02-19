using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using MoreLinq;

namespace Converter {
	public interface ICustomBinaryUnpacker<T> {
		T Unpack(BinaryFileReader bread);
	}
	
	public class BinaryFileReader {
		readonly byte[] Data;
		int offset;
		public BinaryFileReader(byte[] data) => Data = data;

		public void Seek(int addr) => offset = addr;
		public int Tell => offset;

		public byte[] ReadBytes(int count) => Data.Slice((offset += count) - count, count).ToArray();

		public T ReadStruct<T>(int addr = -1, bool raw = false) where T : struct {
			if(addr != -1) Seek(addr);
			if(!raw && typeof(ICustomBinaryUnpacker<T>).IsAssignableFrom(typeof(T)))
				return ((ICustomBinaryUnpacker<T>) new T()).Unpack(this);
			
			var bytes = ReadBytes(Marshal.SizeOf<T>());
			var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			var ret = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
			handle.Free();
			return ret;

		}

		public T[] ReadStructs<T>(int count, int addr = -1) where T : struct => Enumerable.Range(0, count).Select(i => ReadStruct<T>(i == 0 ? addr : -1)).ToArray();
		public T[] ReadMaxStructs<T>(int size, int addr = -1) where T : struct => ReadStructs<T>(size / Marshal.SizeOf<T>(), addr);
	}
}