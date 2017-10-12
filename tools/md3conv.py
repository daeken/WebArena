import math, sys
from Struct import *

@Struct
def Frame():
	mins, maxs = vec3, vec3
	local_origin = vec3
	radius = float
	name = string(16)

@Struct
def Tag():
	name = string(64)
	origin = vec3
	axis = float[9]

@Struct
def Shader():
	name = string(64)
	index = uint32

@Struct
def Triangle():
	indices = int32[3]

@Struct
def TexCoord():
	st = float[2]

@Struct
def Vertex():
	coord = int16[3]
	normal = uint8[2]

@Struct
def Surface(self):
	magic = string(4).ignore
	name = string(64)
	flags = int32
	num_frames, num_shaders, num_verts, num_triangles = uint32[4].ignore
	ofs_triangles, ofs_shaders, ofs_st, ofs_xyznormal, ofs_end = uint32[5].ignore

	with struct_seek(self.ofs_shaders, STRUCT_RELATIVE):
		shaders = Shader()[self.num_shaders]
	with struct_seek(self.ofs_triangles, STRUCT_RELATIVE):
		triangles = Triangle()[self.num_triangles]
	with struct_seek(self.ofs_st, STRUCT_RELATIVE):
		texcoords = TexCoord()[self.num_verts]
	with struct_seek(self.ofs_xyznormal, STRUCT_RELATIVE):
		vertices = Vertex()[lambda self: self.num_frames * self.num_verts]
	struct_seek(self.ofs_end, STRUCT_RELATIVE)

@Struct
def Header(self):
	magic = string(4).ignore
	version = int32.ignore
	name = string(64)
	flags = int32
	num_frames, num_tags, num_surfaces, num_skins = uint32[4].ignore
	ofs_frames, ofs_tags, ofs_surfaces, ofs_eof = uint32[4].ignore

	with struct_seek(self.ofs_frames, STRUCT_RELATIVE):
		frames = Frame()[self.num_frames]
	with struct_seek(self.ofs_tags, STRUCT_RELATIVE):
		tags = Tag()[lambda self: self.num_frames * self.num_tags]
	with struct_seek(self.ofs_surfaces, STRUCT_RELATIVE):
		surfaces = Surface()[self.num_surfaces]
	struct_seek(self.ofs_eof, STRUCT_RELATIVE)

def rewind(data):
	out = []
	for i in xrange(0, len(data), 3):
		out += [data[i+0], data[i+2], data[i+1]]
	return out

def convert(inp):
	data = Header(inp)

	tags = {}
	for i in xrange(data.num_tags):
		name = data.tags[i].name
		this = []
		tags[name] = this

		for j in xrange(data.num_frames):
			tag = data.tags[j * data.num_tags + i]
			assert tag.name == name
			this.append((tag.origin, tag.axis))

	meshes = []
	for surface in data.surfaces:
		this = dict(
			frames=[], 
			texcoords=[], 
			indices=[]
		)
		meshes.append(this)
		for texcoord in surface.texcoords:
			this['texcoords'] += texcoord.st

		off = 0
		for i in xrange(data.num_frames):
			vertices = []
			normals = []
			for j in xrange(surface.num_verts):
				vert = surface.vertices[off]
				off += 1
				vertices += [x / 64. for x in vert.coord]
				"""lat = vert.normal[0] * 2 * math.pi / 255.
				long = vert.normal[1] * 2 * math.pi / 255.
				normals += [
					math.cos(long) * math.sin(lat), 
					math.sin(long) * math.sin(lat), 
					math.cos(lat)
				]"""
				normals += vert.normal
			this['frames'].append((vertices, normals))

		for triangle in surface.triangles:
			this['indices'] += rewind(triangle.indices)

	return dict(
		tags=tags, 
		meshes=meshes, 
	)

def main(fn, ofn=None):
	fp = file(fn, 'rb')
	#header = Header(fp)
	
	data = convert(fp)
	if ofn is not None:
		import json
		json.encoder.FLOAT_REPR = lambda o: format(o, '.4f')
		json.dump(data, file(ofn, 'wb'))

if __name__=='__main__':
	main(*sys.argv[1:])
