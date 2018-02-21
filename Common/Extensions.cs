using System.IO;
using System.Text;

namespace WebArena {
	public static class CommonExtensions {
		public static Vec2 ReadVec2(this BinaryReader br) => new Vec2(br.ReadSingle(), br.ReadSingle());
		public static void Write(this BinaryWriter bw, Vec2 vec) {
			bw.Write((float) vec.X);
			bw.Write((float) vec.Y);
		}
		
		public static Vec3 ReadVec3(this BinaryReader br) => new Vec3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
		public static void Write(this BinaryWriter bw, Vec3 vec) {
			bw.Write((float) vec.X);
			bw.Write((float) vec.Y);
			bw.Write((float) vec.Z);
		}
		
		public static Vec4 ReadVec4(this BinaryReader br) => new Vec4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
		public static void Write(this BinaryWriter bw, Vec4 vec) {
			bw.Write((float) vec.X);
			bw.Write((float) vec.Y);
			bw.Write((float) vec.Z);
			bw.Write((float) vec.W);
		}

		public static string ReadWaString(this BinaryReader br) {
			var len = br.ReadInt32();
			if(len == -1)
				return null;
			return Encoding.UTF8.GetString(br.ReadBytes(len));
		}
		public static void WaWrite(this BinaryWriter bw, string value) {
			if(value == null) {
				bw.Write(-1);
				return;
			}
			var temp = Encoding.UTF8.GetBytes(value);
			bw.Write(temp.Length);
			bw.Write(temp);
		}
	}
}