import sys, yaml
from pprint import pprint
from math import *

typemap = dict(
	uint8='byte', 
	uint16='ushort', 
	uint32='uint', 
	int8='sbyte', 
	int16='short', 
	int32='int', 
	vec2='Vec2', 
	vec3='Vec3', 
	vec4='Vec4', 
)

btypemap = dict(
	uint8='Byte', 
	uint16='UInt16', 
	uint32='UInt32', 
	int8='SByte', 
	int16='Int16', 
	int32='Int32', 
	float='Single', 
	double='Double', 
	vec2='Vec2', 
	vec3='Vec3', 
	vec4='Vec4', 
)

class Type(object):
	def __init__(self, struct, spec):
		self.struct = struct
		if '<' in spec:
			base, gen = spec.split('<', 1)
			gen, rest = gen.split('>', 1)
			self.gen = Type(self.struct, gen)
			spec = base + rest
		else:
			self.gen = None

		if '[' in spec:
			self.rank = spec.split('[', 1)[1].split(']', 1)[0]
			# XXX: Handle multidimensional arrays
		else:
			self.rank = None

		self.base = spec.split('<', 1)[0].split('[', 1)[0]

	def declare(self):
		if self.base == 'string':
			type = 'string'
		elif self.base == 'list':
			assert self.gen is not None
			type = 'List<%s>' % self.gen.declare()
		else:
			type = typemap[self.base] if self.base in typemap else self.base
			if self.base != 'bool' and self.gen is not None: # Bools shouldn't be generic in C#
				type += '<%s>' % self.gen.declare()

		if self.base not in ('list', ) and self.rank is not None:
			type += '[]'

		return type

	def pack(self, name, ws='', array=False):
		if not array and self.rank is not None:
			tvar = chr(ord('i') + len(ws) - 3)
			print '%sfor(var %s = 0; %s < %s; ++%s) {' % (ws, tvar, tvar, self.rank, tvar)
			self.pack('%s[%s]' % (name, tvar), ws=ws + '\t', array=True)
			print '%s}' % ws
			return

		if self.base in btypemap:
			print '%sbw.Write(%s);' % (ws, name)
		elif self.base == 'bool':
			print '%sbw.Write((%s) (%s ? 1 : 0));' % (ws, typemap[self.gen.base], name)
		elif self.base == 'string':
			print '%sbw.WaWrite(%s);' % (ws, name)
		elif self.base in allEnums:
			print '%sbw.Write((%s) %s);' % (ws, allEnums[self.base].cast, name)
		else:
			print '%s%s.Pack(bw);' % (ws, name)

	def unpack(self, name, ws='', array=False):
		if not array and self.base != 'string' and self.rank is not None:
			if self.base == 'list':
				print '%s%s = new List<%s>();' % (ws, name, self.gen.declare())
			else:
				print '%s%s = new %s[%s];' % (ws, name, typemap[self.base] if self.base in typemap else self.base, self.rank)
			tvar = chr(ord('i') + len(ws) - 3)
			print '%sfor(var %s = 0; %s < %s; ++%s) {' % (ws, tvar, tvar, self.rank, tvar)
			if self.base == 'list':
				self.gen.unpack(name, ws=ws + '\t', array='list')
			else:
				self.unpack('%s[%s]' % (name, tvar), ws=ws + '\t', array=self.base)
			print '%s}' % ws
			return

		if self.base in btypemap:
			val = 'br.Read%s()' % btypemap[self.base]
		elif self.base == 'bool':
			val = 'br.Read%s() != 0' % btypemap[self.gen.base]
		elif self.base == 'string':
			val = 'br.ReadWaString()'
		elif self.base in allEnums:
			val = '(%s) br.ReadUInt32()' % self.base
		else:
			val = 'new %s(br)' % self.base

		if array == 'list':
			print '%s%s.Add(%s);' % (ws, name, val)
		else:
			print '%s%s = %s;' % (ws, name, val)

class Struct(object):
	def __init__(self, name, ydef):
		self.name = name
		self.elems = []
		self.suppress = []
		for elem in ydef:
			(type, names), = elem.items()
			if '@' in type:
				type, cond = type.split('@', 2)
				assert cond.startswith('if<') and cond.endswith('>')
				cond = cond[3:-1]
			else:
				cond = None
			type = Type(self, type)
			for name in names.split(','):
				name = name.strip()
				if name.startswith('$'):
					name = name[1:]
					self.suppress.append(name)
				self.elems.append((name, type, cond))

	def __repr__(self):
		return 'Struct(%r, %r)' % (self.name, self.elems)

	def declare(self):
		print '\tpublic struct %s {' % self.name

		for name, type, cond in self.elems:
			stype = type.declare()
			if stype is None:
				continue
			print '\t\t%s%s %s;' % ('public ' if name[0].isupper() else '', stype, name)

		print
		print '\t\tpublic %s(byte[] data, int offset = 0) : this() {' % self.name
		print '\t\t\tUnpack(data, offset);'
		print '\t\t}'
		print '\t\tpublic %s(BinaryReader br) : this() {' % self.name
		print '\t\t\tUnpack(br);'
		print '\t\t}'

		print '\t\tpublic void Unpack(byte[] data, int offset = 0) {'
		print '\t\t\tusing(var ms = new MemoryStream(data, offset, data.Length - offset)) {'
		print '\t\t\t\tusing(var br = new BinaryReader(ms)) {'
		print '\t\t\t\t\tUnpack(br);'
		print '\t\t\t\t}'
		print '\t\t\t}'
		print '\t\t}'

		print '\t\tpublic void Unpack(BinaryReader br) {'
		for name, type, cond in self.elems:
			if cond is not None:
				print '\t\t\tif(%s) {' % cond
				type.unpack(name, '\t\t\t\t')
				print '\t\t\t}'
			else:
				type.unpack(name, '\t\t\t')
		print '\t\t}'

		print
		print '\t\tpublic byte[] Pack() {'
		print '\t\t\tusing(var ms = new MemoryStream()) {'
		print '\t\t\t\tusing(var bw = new BinaryWriter(ms)) {'
		print '\t\t\t\t\tPack(bw);'
		print '\t\t\t\t\treturn ms.ToArray();'
		print '\t\t\t\t}'
		print '\t\t\t}'
		print '\t\t}'

		print '\t\tpublic void Pack(BinaryWriter bw) {'
		for name, type, cond in self.elems:
			if cond is not None:
				print '\t\t\tif(%s) {' % cond
				type.pack(name, '\t\t\t\t')
				print '\t\t\t}'
			else:
				type.pack(name, '\t\t\t')
		print '\t\t}'

		print
		print '\t\tpublic override string ToString() => $"%s {{ %s }}";' % (self.name, ', '.join('%s={%s}' % (name, name) for name, type, cond in self.elems))

		print '\t}'


class Enum(object):
	def __init__(self, name, ydef):
		type = Type(self, name)
		self.base = (' : ' + type.gen.declare()) if type.gen and type.gen.declare() != 'uint' else ''
		self.mbase = btypemap[type.gen.base if type.gen else 'uint32']
		self.cast = type.gen.declare() if type.gen else 'uint'
		self.name = type.base

		self.elems = []
		for elem in ydef:
			if isinstance(elem, dict):
				(name, values), = elem.items()
				self.elems.append((name, [values] if isinstance(values, int) else [x.strip() for x in values.split(',')]))
			else:
				self.elems.append((elem, []))

	def declare(self):
		print '\tpublic enum %s%s {' % (self.name, self.base)
		for i, (name, values) in enumerate(self.elems):
			print '\t\t%s%s%s' % (name, ' = %s' % values[0] if len(values) else '', ', ' if i != len(self.elems) - 1 else '')
		print '\t}'

	def pack(self):
		pass

	def unpack(self):
		pass

sfile = yaml.load(file('structs.yaml'))

structs = {name : Struct(name, struct) for name, struct in sfile['structs'].items()} if 'structs' in sfile else {}
enums = {name : Enum(name, enum) for name, enum in sfile['enums'].items()} if 'enums' in sfile else {}
constants = {name : value for name, value in sfile['constants'].items()} if 'constants' in sfile else {}
allEnums = {enum.name : enum for name, enum in enums.items()}

with file('Protocol.cs', 'w') as fp:
	sys.stdout = fp
	print '''/*
*       o__ __o       o__ __o__/_   o          o    o__ __o__/_   o__ __o                o    ____o__ __o____   o__ __o__/_   o__ __o      
*      /v     v\     <|    v       <|\        <|>  <|    v       <|     v\              <|>    /   \   /   \   <|    v       <|     v\     
*     />       <\    < >           / \\o      / \  < >           / \     <\             / \         \o/        < >           / \     <\    
*   o/                |            \o/ v\     \o/   |            \o/     o/           o/   \o        |          |            \o/       \o  
*  <|       _\__o__   o__/_         |   <\     |    o__/_         |__  _<|           <|__ __|>      < >         o__/_         |         |> 
*   \\          |     |            / \    \o  / \   |             |       \          /       \       |          |            / \       //  
*     \         /    <o>           \o/     v\ \o/  <o>           <o>       \o      o/         \o     o         <o>           \o/      /    
*      o       o      |             |       <\ |    |             |         v\    /v           v\   <|          |             |      o     
*      <\__ __/>     / \  _\o__/_  / \        < \  / \  _\o__/_  / \         <\  />             <\  / \        / \  _\o__/_  / \  __/>     
*
* THIS FILE IS GENERATED BY structgen.py/structs.yaml
* DO NOT EDIT
*
*/'''
	print 'using System;'
	print 'using System.Collections.Generic;'
	print 'using System.IO;'
	print 'using System.Threading.Tasks;'

	if len(constants):
		print
		print 'using static WebArena.Constants;'
		print 'namespace WebArena {'
		print '\tinternal static class Constants {'
		for name, value in constants.items():
			print '\t\tpublic static int %s = %i;' % (name, value)
		print '\t}'
		print '}'

	print
	print 'namespace WebArena {'

	if len(enums):
		for i, (name, enum) in enumerate(enums.items()):
			enum.declare()
			if i != len(enums) - 1:
				print
	
	if len(enums) and len(structs):
		print

	for i, (name, struct) in enumerate(structs.items()):
		struct.declare()
		if i != len(structs) - 1:
			print

	if len(enums) or len(structs):
		print

	print '\tpublic abstract class ProtocolHandler {'
	print '\t\tprotected abstract void Send(byte[] data);'
	print '\t\tpublic void Handle(object msg) {'
	for i, (name, struct) in enumerate(structs.items()):
		print '\t\t\t%sif(msg is %s) Handle((%s) msg);' % ('else ' if i != 0 else '', name, name)
	print '\t\t}'
	for name in structs.keys():
		print '\t\tprotected virtual void Handle(%s msg) { throw new NotImplementedException(); }' % name
	print '\t\tprotected object ParseMessage(byte[] data) {'
	print '\t\t\tvar br = new BinaryReader(new MemoryStream(data));'
	print '\t\t\tswitch(br.ReadUInt32()) {'
	for i, (name, struct) in enumerate(structs.items()):
		print '\t\t\t\tcase %i:' % i
		print '\t\t\t\t\treturn new %s(br);' % name
	print '\t\t\t}'
	print '\t\t\treturn null;'
	print '\t\t}'
	for i, (name, struct) in enumerate(structs.items()):
		print '\t\t'
		print '\t\tprotected void SendMessage(%s value) {' % name
		print '\t\t\tusing(var ms = new MemoryStream()) {'
		print '\t\t\t\tusing(var bw = new BinaryWriter(ms)) {'
		print '\t\t\t\t\tbw.Write(%iU);' % i
		print '\t\t\t\t\tvalue.Pack(bw);'
		print '\t\t\t\t}'
		print '\t\t\t\tSend(ms.ToArray());'
		print '\t\t\t}'
		print '\t\t}'
	print '\t}'
	print '\t'
	print '\tpublic abstract class AsyncProtocolHandler {'
	print '\t\tprotected abstract Task Send(byte[] data);'
	print '\t\tpublic async Task Handle(object msg) {'
	for i, (name, struct) in enumerate(structs.items()):
		print '\t\t\t%sif(msg is %s) await Handle((%s) msg);' % ('else ' if i != 0 else '', name, name)
	print '\t\t}'
	for name in structs.keys():
		print '\t\tprotected virtual Task Handle(%s msg) { throw new NotImplementedException(); }' % name
	print '\t\tprotected object ParseMessage(byte[] data) {'
	print '\t\t\tvar br = new BinaryReader(new MemoryStream(data));'
	print '\t\t\tswitch(br.ReadUInt32()) {'
	for i, (name, struct) in enumerate(structs.items()):
		print '\t\t\t\tcase %i:' % i
		print '\t\t\t\t\treturn new %s(br);' % name
	print '\t\t\t}'
	print '\t\t\treturn null;'
	print '\t\t}'
	for i, (name, struct) in enumerate(structs.items()):
		print '\t\t'
		print '\t\tprotected async Task SendMessage(%s value) {' % name
		print '\t\t\tusing(var ms = new MemoryStream()) {'
		print '\t\t\t\tusing(var bw = new BinaryWriter(ms)) {'
		print '\t\t\t\t\tbw.Write(%iU);' % i
		print '\t\t\t\t\tvalue.Pack(bw);'
		print '\t\t\t\t}'
		print '\t\t\t\tawait Send(ms.ToArray());'
		print '\t\t\t}'
		print '\t\t}'
	print '\t}'

	print '}'
