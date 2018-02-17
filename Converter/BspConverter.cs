using System;
using System.Runtime.InteropServices;
using static System.Console;
using System.Diagnostics;

namespace Converter {
	public class BspConverter {
		[StructLayout(LayoutKind.Sequential)]
		struct BspHeader {
			public readonly uint Magic;
			public readonly int Version;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=17)]
			public readonly BspDirEntry[] DirEntries;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct BspDirEntry {
			public readonly int Offset, Length;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		struct BspTexture {
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)]
			public readonly string Name;
			public readonly int SurfaceFlags, ContentFlags;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct BspPlane {
			public readonly Vec3 Normal;
			public readonly float Distance;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct BspNode {
			public readonly int Plane;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public readonly int[] Children;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public readonly int[] Mins;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public readonly int[] Maxs;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct BspLeaf {
			public readonly int Cluster, Area;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public int[] Mins;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public int[] Maxs;
			public readonly int LeafFace, LeafFaceCount;
			public readonly int LeafBrush, LeafBrushCount;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspLeafFace {
			public readonly int Face;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspLeafBrush {
			public readonly int Brush;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspModel {
			public readonly Vec3 Mins, Maxs;
			public readonly int Face, FaceCount;
			public readonly int Brush, BrushCount;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspBrush {
			public readonly int BrushSide, BrushSideCount;
			public readonly int Texture;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspBrushSide {
			public readonly int Plane, Texture;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspVertex {
			public readonly Vec3 Position;
			public readonly Vec2 TexCoord, LmCoord;
			public readonly Vec3 Normal;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public readonly byte[] Color;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspMeshVert {
			public readonly int Offset;
		}
		
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		struct BspEffect {
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
			public readonly string Name;
			public readonly int brush, VisibleSide;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspFace {
			public readonly int Texture, Effect, Type;
			public readonly int Vertex, VertexCount;
			public readonly int MeshVert, MeshVertCount;
			public readonly int LmIndex;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public readonly int[] LmStart, LmSize;
			public readonly Vec3 LmOrigin, LmVecS, LmVecT;
			public readonly Vec3 Normal;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public readonly int[] Size;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspLightmap {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128 * 128 * 3)]
			public readonly byte[] Pixels;
		}

		public BspConverter(string fn) {
			var bread = new BinaryFileReader(fn);
			var header = bread.ReadStruct<BspHeader>();
			Debug.Assert(header.Magic == 0x50534249);

			T[] GetLump<T>(int lump) => bread.ReadMaxStructs<T>(header.DirEntries[lump].Length, header.DirEntries[lump].Offset);
			
			var textures = GetLump<BspTexture>(1);
			var planes = GetLump<BspPlane>(2);
			var nodes = GetLump<BspNode>(3);
			var leafs = GetLump<BspLeaf>(4);
			var leaffaces = GetLump<BspLeafFace>(5);
			var leafbrushes = GetLump<BspLeafBrush>(6);
			var models = GetLump<BspModel>(7);
			var brushes = GetLump<BspBrush>(8);
			var brushsides = GetLump<BspBrushSide>(9);
			var vertices = GetLump<BspVertex>(10);
			var meshverts = GetLump<BspMeshVert>(11);
			var effects = GetLump<BspEffect>(12);
			var faces = GetLump<BspFace>(13);
			var lightmaps = GetLump<BspLightmap>(14);
		}
	}
}