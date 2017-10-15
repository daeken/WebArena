import json, re, sys
from assets import AssetManager
from pprint import pprint

class Consumer(object):
	def __init__(self, data):
		self.i = 0
		self.data = data

	@property
	def end(self):
		return self.i == len(self.data)

	@property
	def peek(self):
		return self.data[self.i]

	@property
	def next(self):
		ti = self.i
		self.i += 1
		return self.data[ti]

def parseShader(data):
	data = data.replace('\r', '\n')
	data = re.sub(r'//.*?\n$', '\n', data, flags=re.S | re.M)
	data = data.replace('{', '\n{\n')
	data = re.sub(r'^\s+', '', data, flags=re.S | re.M)
	data = re.sub(r'\s+$', '\n', data, flags=re.S | re.M)
	data = re.sub(r'\n[\n \t]*', '\n', data, flags=re.S | re.M)
	data = re.sub(r'[ \t]+', ' ', data, flags=re.S | re.M)
	data = data.strip()
	if not data:
		return {}
	data = Consumer(data.split('\n'))

	materials = {}
	inMat = False
	inStage = False
	while not data.end:
		cur = data.next
		assert ' ' not in cur and cur != '{'
		name = cur
		materials[name] = material = [[]]
		tl = material[0]

		assert data.next == '{'
		inMat = True

		while not data.end:
			cur = data.next
			if cur == '{':
				inStage = True
				stage = []
				material.append(stage)
				while not data.end:
					cur = data.next
					if cur == '}':
						inStage = False
						break
					stage.append(cur.split(' '))
			elif cur == '}':
				inMat = False
				break
			else:
				tl.append(cur.split(' '))

	assert not inMat and not inStage
	return materials

def preprocess(stage):
	for elem in stage:
		elem = [elem[0].lower()] + elem[1:]
		if elem[0].startswith('q3map_') or elem[0].startswith('qer_'):
			continue
		yield elem

def findFile(name):
	if am.exists(name):
		#print 'Found TGA'
		return name
	elif am.exists(name[:-3] + 'jpg'):
		#print 'Found JPEG'
		return name[:-3] + 'jpg'
	else:
		pass#print 'Could not find texture:', name

blendModes = dict(
	GL_ZERO=0, 
	GL_ONE=1, 
	GL_SRC_COLOR=0x0300, 
	GL_ONE_MINUS_SRC_COLOR=0x0301, 
	GL_SRC_ALPHA=0x0302, 
	GL_ONE_MINUS_SRC_ALPHA=0x0303, 
	GL_DST_ALPHA=0x0304, 
	GL_ONE_MINUS_DST_ALPHA=0x0305, 
	GL_DST_COLOR=0x0306, 
	GL_ONE_MINUS_DST_COLOR=0x0307, 
	GL_SRC_ALPHA_SATURATE=0x0308, 
	GL_CONSTANT_COLOR=0x8001, 
	GL_ONE_MINUS_CONSTANT_COLOR=0x8002, 
	GL_CONSTANT_ALPHA=0x8003, 
	GL_ONE_MINUS_CONSTANT_ALPHA=0x8004
)

class Codegen(object):
	def __init__(self):
		self.uniforms = []
		self.varyings = []
		self.functions = []
		self.body = []

	def uniform(self, name, type):
		self.uniforms.append((type, name))

	def varying(self, name, type):
		self.varyings.append((type, name))

	def function(self, code):
		self.functions.append(code.strip())

	def stmt(self, stmt):
		if stmt[-1] != ';':
			stmt += ';'
		self.body.append(stmt)

	def __str__(self):
		out = []
		for type, name in self.uniforms:
			out.append('uniform %s %s;' % (type, name))
		for type, name in self.varyings:
			out.append('varying %s %s;' % (type, name))
		for elem in self.functions:
			out.append(elem)
		out.append('void main() {')
		for stmt in self.body:
			out.append('\t' + stmt)
		out.append('}')
		return '\n'.join(out)

prebuilts = {}
prebuilts['inversesawtooth'] = '''
float inversesawtoothWave(float base, float amp, float phase, float freq) {
	return clamp(base + (1.0 - mod(uTime * freq + phase, 1.)) * amp, 0., 1.);
}
'''
prebuilts['noise'] = '''
'''
prebuilts['sawtooth'] = '''
float sawtoothWave(float base, float amp, float phase, float freq) {
	return clamp(base + mod(uTime * freq + phase, 1.) * amp, 0., 1.);
}
'''
prebuilts['sin'] = '''
float sinWave(float base, float amp, float phase, float freq) {
	return clamp(base + sin(uTime * 3.14159 * 2. * freq + phase) * amp, 0., 1.);
}
'''
prebuilts['square'] = '''
float squareWave(float base, float amp, float phase, float freq) {
	return clamp(base + sign(sin(uTime * 3.14159 * 2. * freq + phase)) * amp, 0., 1.);
}
'''
prebuilts['triangle'] = '''
float triangleWave(float base, float amp, float phase, float freq) {
	return clamp(base + (1. - (abs(mod(uTime * freq + phase, 1.) - 0.5) * 2.)) * amp, 0., 1.);
}
'''
prebuilts['rotate'] = '''
vec2 rotate(vec2 v, float deg) {
	float a = deg * 3.14159 / 180.;
	return vec2(v.x * cos(a) - v.y * sin(a), v.y * cos(a) + v.x * sin(a));
}
'''

ensureFloat = lambda x: '%s.' % x if '.' not in x else x

def compile(stages):
	top, stages = stages[0], stages[1:]
	outStages = []
	for stageI, stage in enumerate(stages):
		blendMode = None
		alphaTest = None
		tcSource = 'vTexCoord'
		tcMods = []
		vertexColor = 'vec3(1.)'
		texture = None
		clamp = False
		animTex = None
		texColor = 'vec4(1.)'
		lmColor = 'vec4(texture2D(uLmSampler, vLmCoord).xyz, 1.0)'
		functions = set()

		for elem in stage:
			cmd, ops = elem[0], elem[1:]

			if cmd == 'map' or cmd == 'clampmap':
				name, = ops
				if name.lower() == '$whiteimage' or name.lower() == '*white':
					texColor = 'vec4(1.)'
				elif name.lower() == '$lightmap' or name.lower() == '$lightmapt':
					texColor = lmColor
				else:
					texture = findFile(name)
					if texture is not None:
						texColor = 'texture2D(uTexSampler, texcoord)'
					clamp = cmd == 'clampmap'
			elif cmd == 'animmap':
				animTex = ops
				texColor = 'texture2D(uTexSampler, texcoord)'
			elif cmd == 'blendfunc':
				if len(ops) == 1:
					ops = [ops[0].lower()]
					if ops[0] == 'add' or ops[0] == 'gl_add':
						ops = 'gl_one', 'gl_one'
					elif ops[0] == 'filter':
						ops = 'gl_dst_color', 'gl_zero'
					elif ops[0] == 'blend':
						ops = 'gl_src_alpha', 'gl_one_minus_src_alpha'
				blendMode = blendModes[ops[0].upper()], blendModes[ops[1].upper()]
			elif cmd == 'rgbgen':
				type, ops = ops[0].lower(), ops[1:]
				if type == 'identity':
					pass
				elif type == 'identitylighting':
					pass # ?
				elif type == 'vertex' or type == 'exactvertex':
					pass # XXX: UNIMPLEMENTED
				elif type == 'entity' or type == 'oneminusentity':
					pass # XXX: UNIMPLEMENTED
				elif type == 'lightingdiffuse':
					pass # XXX: UNIMPLEMENTED
				elif type == 'wave':
					func, ops = ops[0].lower(), ops[1:]
					functions.add(func)
					vertexColor = 'vec3(%sWave(%s))' % (func, ', '.join(ensureFloat(x) for x in ops))
				else:
					print 'Unsupported rgbgen:', type, ops
			elif cmd == 'tcmod':
				func, ops = ops[0].lower(), ops[1:]
				ops = tuple(map(ensureFloat, ops))
				if func == 'scroll':
					tcMods.append('texcoord += vec2(%s, %s) * uTime' % (ops[0], ops[1]))
				elif func == 'rotate':
					tcMods.append('texcoord = rotate(texcoord, %s * uTime)' % ops[0])
					functions.add('rotate')
				elif func == 'scale':
					tcMods.append('texcoord /= vec2(%s, %s)' % (ops[0], ops[1]))
				elif func == 'transform':
					m00, m01, m10, m11, t0, t1 = ops
					tcMods.append('texcoord = vec2(texcoord.x * %s + texcoord.y * %s + %s, texcoord.x * %s + texcoord.y * %s + %s)' % (m00, m10, t0, m01, m11, t1))
				else:
					pass#print func
			elif cmd == 'alphafunc':
				alphaTest = ops[0].upper()
			else:
				pass#print cmd

		assert not (texture is not None and animTex is not None)

		code = Codegen()
		for func in functions:
			code.function(prebuilts[func])
		code.uniform('uTime', 'float')
		if texture is not None or animTex is not None:
			code.uniform('uTexSampler', 'sampler2D')
			code.varying('vTexCoord', 'vec2')
			code.stmt('vec2 texcoord = ' + tcSource)
			for stmt in tcMods:
				code.stmt(stmt)
		code.uniform('uLmSampler', 'sampler2D')
		code.varying('vLmCoord', 'vec2')

		if stageI == 0:
			code.stmt('gl_FragColor = (%s) * vec4((%s), 1.0) * (%s)' % (texColor, vertexColor, lmColor))
		else:
			code.stmt('gl_FragColor = (%s) * vec4((%s), 1.0)' % (texColor, vertexColor))

		if alphaTest == 'GT0':
			code.stmt('if(gl_FragColor.a <= 0.) discard')
		elif alphaTest == 'LT128':
			code.stmt('if(gl_FragColor.a >= 0.5) discard')
		elif alphaTest == 'GE128':
			code.stmt('if(gl_FragColor.a < 0.9) discard')

		outStages.append(dict(
			Blend=blendMode, 
			Texture=texture, 
			AnimTex=animTex, 
			Clamp=clamp, 
			FragShader=str(code)
		))
		if alphaTest:
			break
	return outStages

def main(sdir):
	global am
	am = AssetManager(sdir)
	materials = {}
	for fn in am.findAll('shader'):
		with am.file(fn, 'r') as fp:
			sub = parseShader(fp.read())
			#for k in sub:
			#	assert k not in materials
			materials.update(sub)

	for name, stages in materials.items():
		materials[name] = [list(preprocess(stage)) for stage in stages]

	#pprint(materials)

	materials['plaintexture'] = [
		[], 
		[['map', 'models/mapobjects/teleporter/teleporter_edge.tga']]
	]

	for name, stages in materials.items():
		materials[name] = compile(stages)

	with file('materials.json', 'w') as fp:
		json.dump(materials, fp)

if __name__=='__main__':
	main(*sys.argv[1:])
