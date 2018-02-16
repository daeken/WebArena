from Struct import *
import copy, json, sys
import numpy as np
from assets import AssetManager

@Struct
def Direntry():
	offset, length = int32[2]

@Struct
def Header():
	magic = string(4)
	version = int32
	direntries = Direntry()[17]

@Struct
def Texture():
	name = string(64)
	surface_flags = int32
	content_flags = int32

@Struct
def Plane():
	normal = vec3
	dist = float

@Struct
def Node():
	plane = int32
	children = int32[2]
	mins = int32[3]
	maxs = int32[3]

@Struct
def Leaf():
	cluster = int32
	area = int32
	mins = int32[3]
	maxs = int32[3]
	leafface = int32
	n_leaffaces = int32
	leafbrush = int32
	n_leafbrushes = int32

@Struct
def LeafFace():
	face = int32

@Struct
def LeafBrush():
	brush = int32

@Struct
def Model():
	mins = vec3
	maxs = vec3
	face = int32
	n_faces = int32
	brush = int32
	n_brushes = int32

@Struct
def Brush():
	brushside = int32
	n_brushsides = int32
	texture = int32

@Struct
def BrushSide():
	plane = int32
	texture = int32

@Struct
def Vertex():
	position = vec3
	texcoord, lmcoord = vec2, vec2
	normal = vec3
	color = uint8[4]

@Struct
def Meshvert():
	offset = int32

@Struct
def Effect(): # Called 'Fog' in the original source
	name = string(64)
	brush = int32
	visibleside = int32

@Struct
def Face():
	texture, effect, type = int32[3]
	vertex, n_vertices = int32[2]
	meshvert, n_meshverts = int32[2]
	lm_index = int32
	lm_start = int32[2]
	lm_size = int32[2]
	lm_origin = vec3
	lm_vec_S = vec3
	lm_vec_T = vec3
	normal = vec3
	size = int32[2]

@Struct
def Lightmap():
	pixels = uint8[128*128*3]

rotmat = np.array([
	[1.0000000,  0.0000000,  0.0000000], 
	[0.0000000,  0.0000000, -1.0000000], 
	[0.0000000,  1.0000000,  0.0000000]])

# ind, verts = tesselate(face.size, fv, fmv)
# this function is directly adapted from http://media.tojicode.com/q3bsp/js/q3bsp_worker.js
def tesselate(size, verts, meshverts):
	def getPoint(c0, c1, c2, dist):
		def sub(attr):
			v0, v1, v2 = map(np.array, (getattr(c0, attr), getattr(c1, attr), getattr(c2, attr)))
			b = 1 - dist

			vc = v0 * (b * b) + v1 * (2 * b * dist) + v2 * (dist * dist)
			if attr == 'normal':
				mag = np.linalg.norm(vc)
				if mag != 0:
					vc /= mag
			setattr(outvert, attr, vc.tolist())

		outvert = Vertex()
		sub('position')
		sub('texcoord')
		sub('lmcoord')
		sub('color')
		sub('normal')
		return outvert

	level = 5.
	L1 = int(level) + 1

	for py in xrange(0, size[1]-2, 2):
		for px in xrange(0, size[0]-2, 2):
			rowOff = py * size[0]

			c0, c1, c2 = verts[rowOff + px:rowOff + px + 3]
			rowOff += size[0]
			c3, c4, c5 = verts[rowOff + px:rowOff + px + 3]
			rowOff += size[0]
			c6, c7, c8 = verts[rowOff + px:rowOff + px + 3]

			indexOff = len(verts)

			for i in xrange(L1):
				a = i / level
				verts.append(getPoint(c0, c3, c6, a))

			for i in xrange(1, L1):
				a = i / level

				tc0 = getPoint(c0, c1, c2, a)
				tc1 = getPoint(c3, c4, c5, a)
				tc2 = getPoint(c6, c7, c8, a)

				for j in xrange(L1):
					b = j / level

					verts.append(getPoint(tc0, tc1, tc2, b))

			for row in xrange(int(level)):
				for col in xrange(int(level)):
					meshverts.append(indexOff + (row + 1) * L1 + col)
					meshverts.append(indexOff + row * L1 + col)
					meshverts.append(indexOff + row * L1 + col + 1)
					
					meshverts.append(indexOff + (row + 1) * L1 + col)
					meshverts.append(indexOff + row * L1 + col + 1)
					meshverts.append(indexOff + (row + 1) * L1 + col + 1)

	return meshverts, verts

def rewind(data, xmod=1):
	return data
	out = []
	for i in xrange(0, len(data), 3):
		out += [xmod * data[i+0], data[i+2], data[i+1]]
	return out

def rotate(data):
	return data
	out = []
	for i in xrange(0, len(data), 3):
		vec = np.array(data[i:i+3])
		out += list(rotmat.dot(vec))
	return out

def interleave(count, *elems):
	out = []
	elemsize = [len(x) / count for x in elems]

	for i in xrange(count):
		for j, elem in enumerate(elems):
			size = elemsize[j]
			start = size * i
			out += elem[start:start+size]

	return out

def splitMesh(indices, vertices, stride):
	indmap = {}
	outindices = []
	outvertices = []

	for ind in indices:
		if ind in indmap:
			outindices.append(indmap[ind])
		else:
			i = len(indmap)
			start = ind * stride
			outvertices += vertices[start:start + stride]
			indmap[ind] = i
			outindices.append(i)
	return outindices, outvertices

def adjustBrightness(pixels):
	for i in xrange(0, len(pixels), 3):
		r, g, b = pixels[i:i+3]
		r, g, b = r * 4., g * 4., b * 4.
		scale = 1.
		if r > 255 and 255 / r < scale:
			scale = 255 / r
		if g > 255 and 255 / g < scale:
			scale = 255 / g
		if b > 255 and 255 / b < scale:
			scale = 255 / b
		pixels[i] = int(r * scale)
		pixels[i+1] = int(g * scale)
		pixels[i+2] = int(b * scale)
	return pixels

def flip(pixels, stride):
	return reduce(lambda a, x: a + x, [pixels[i - stride:i] for i in xrange(len(pixels), 0, -stride)])

def main(path, mapname):
	global am
	am = AssetManager(path)
	materials = json.load(file('materials.json'))

	def decode(lump, cls):
		size = len(cls())
		fp.seek(header.direntries[lump].offset)
		return [cls(unpack=fp) for i in xrange(header.direntries[lump].length / size)]

	fp = am.fileFuzzy(mapname + '.bsp', 'rb')
	header = Header(unpack=fp)

	fp.seek(header.direntries[0].offset)
	entities = fp.read(header.direntries[0].length)
	
	textures = decode(1, Texture)
	planes = decode(2, Plane)
	nodes = decode(3, Node)
	leafs = decode(4, Leaf)
	leaffaces = decode(5, LeafFace)
	leafbrushes = decode(6, LeafBrush)
	models = decode(7, Model)
	brushes = decode(8, Brush)
	brushsides = decode(9, BrushSide)
	vertices = decode(10, Vertex)
	meshverts = decode(11, Meshvert)
	effects = decode(12, Effect)
	faces = decode(13, Face)
	lightmaps = decode(14, Lightmap)

	outindices = {}
	outpositions = []
	outnormals = []
	outtexcoords = []
	outlmcoords = []

	model = models[0]
	numverts = 0
	for face in faces[model.face:model.face+model.n_faces]:
		if face.texture not in outindices:
			outindices[face.texture] = {}
		if face.lm_index not in outindices[face.texture]:
			outindices[face.texture][face.lm_index] = []
		fmv = [x.offset for x in meshverts[face.meshvert:face.meshvert+face.n_meshverts]]
		fv = vertices[face.vertex:face.vertex+face.n_vertices]
		if face.type == 1 or face.type == 3:
			outindices[face.texture][face.lm_index] += rewind([mv + numverts for mv in fmv], 1)
			for vert in fv:
				outpositions += rotate(vert.position)
				outnormals += rotate(vert.normal)
				outtexcoords += vert.texcoord
				outlmcoords += vert.lmcoord
			numverts += len(fv)
		elif face.type == 2:
			fmv, fv = tesselate(face.size, fv, fmv)
			outindices[face.texture][face.lm_index] += rewind([mv + numverts for mv in fmv], 1)
			for vert in fv:
				outpositions += rotate(vert.position)
				outnormals += rotate(vert.normal)
				outtexcoords += vert.texcoord
				outlmcoords += vert.lmcoord
			numverts += len(fv)
		elif face.type == 4:
			pass
		else:
			print 'other', face.type

	outplanes = []
	for plane in planes:
		outplanes.append(dict(Normal=rotate(plane.normal), Distance=plane.dist))
	outbrushes = []
	print len(brushes), model.brush, model.n_brushes
	for brush in brushes[model.brush:model.brush+model.n_brushes]:
		texture = textures[brush.texture]
		# XXX: Rewrite file so non-solid brushes are nuked.
		collidable = (texture.content_flags & 1) == 1
		sides = brushsides[brush.brushside:brush.brushside+brush.n_brushsides]
		outbrushes.append(dict(Collidable=collidable, Planes=[side.plane for side in sides]))

	def btree(ind):
		def reminmax(mins, maxs):
			mins, maxs = rotate(mins), rotate(maxs)
			return [min(mins[0], maxs[0]), min(mins[1], maxs[1]), min(mins[2], maxs[2])], [max(mins[0], maxs[0]), max(mins[1], maxs[1]), max(mins[2], maxs[2])]
		if ind >= 0:
			node = nodes[ind]
			mins, maxs = reminmax(node.mins, node.maxs)
			return dict(Leaf=False, Plane=node.plane, Mins=mins, Maxs=maxs, Left=btree(node.children[0]), Right=btree(node.children[1]))
		else:
			leaf = leafs[-(ind + 1)]
			mins, maxs = reminmax(leaf.mins, leaf.maxs)
			return dict(Leaf=True, Plane=-1, Mins=mins, Maxs=maxs, Brushes=[x.brush for x in leafbrushes[leaf.leafbrush:leaf.leafbrush+leaf.n_leafbrushes]])

	outvertices = interleave(len(outpositions) / 3, outpositions, outnormals, outtexcoords, outlmcoords)

	outmeshes = []
	outmaterials = {}
	for mkey in outindices:
		name = textures[mkey].name
		if name in materials:
			material = materials[name]
			"""
			for stage in materials[name]:
				if 'discard' in stage['FragShader']:
					print name
					print stage['FragShader']
					break
			"""
		elif am.exists(name + '.tga'):
			material = copy.deepcopy(materials['plaintexture'])
			material[0]['Texture'] = name + '.tga'
		elif am.exists(name + '.jpg'):
			material = copy.deepcopy(materials['plaintexture'])
			material[0]['Texture'] = name + '.jpg'
		else:
			print 'Material not found:', name
			material = copy.deepcopy(materials['plaintexture'])
			material[0]['Texture'] = '404'

		outmaterials[mkey] = material

		for lmkey in outindices[mkey]:
			indices, vertices = splitMesh(outindices[mkey][lmkey], outvertices, 3 + 3 + 2 + 2)
			outmeshes.append(dict(
				MaterialIndex=mkey, 
				LightmapIndex=lmkey, 
				Indices=indices, 
				Vertices=vertices
			))

	tree = btree(0)

	outlm = [adjustBrightness(x.pixels) for x in lightmaps]

	outfp = file(mapname + '.json', 'wb')
	outdata = dict(Materials=outmaterials, Meshes=outmeshes, Planes=outplanes, Brushes=outbrushes, Tree=tree, Lightmaps=outlm)
	json.dump(outdata, outfp)

if __name__=='__main__':
	main(*sys.argv[1:])
