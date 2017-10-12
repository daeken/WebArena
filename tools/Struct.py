import copy, struct, sys

class AttrDict(dict):
	def __init__(self, *args, **kwargs):
		super(AttrDict, self).__init__(*args, **kwargs)
		self.__dict__ = self

class Ignorable:
	_ignore = False
	@property
	def ignore(self):
		out = copy.deepcopy(self)
		for i, x in enumerate(out):
			try:
				out[i] = x.ignore
			except:
				pass
		out._ignore = True
		return out

class IgnorableTuple(tuple, Ignorable):
	pass

class IgnorableList(list, Ignorable):
	pass

class NameSurrogate(object):
	def __getattr__(self, name):
		return name

nameSurrogate = NameSurrogate()

ABSOLUTE = 0
STRUCT_RELATIVE = 1
RELATIVE = 2
class struct_seek(object):
	def __init__(self, offset, whence=ABSOLUTE):
		self.offset, self.whence = offset, whence
		frame = BaseStruct.__framestack__[-1]
		names = [name for name in frame.f_code.co_varnames[frame.f_code.co_argcount:] if name in frame.f_locals]
		s = BaseStruct.__structstack__[-1]
		s.trigger_after(names[-1] if len(names) else None, self.seek)

	def __enter__(self):
		pass

	def seek(self, s):
		self.lastpos = s.tell()

		if isinstance(self.offset, str) or isinstance(self.offset, unicode):
			offset = getattr(s, self.offset)
		else:
			offset = self.offset

		if self.whence == ABSOLUTE:
			s.seek(offset)
		elif self.whence == STRUCT_RELATIVE:
			s.seek(s.__startpos__ + offset)
		elif self.whence == RELATIVE:
			s.seek(self.lastpos + offset)

	def unseek(self, s):
		s.seek(self.lastpos)

	def __exit__(self, type, value, tb):
		frame = BaseStruct.__framestack__[-1]
		names = [name for name in frame.f_code.co_varnames[frame.f_code.co_argcount:] if name in frame.f_locals]
		s = BaseStruct.__structstack__[-1]
		s.trigger_after(names[-1] if len(names) else None, self.unseek)

class StructType(IgnorableTuple):
	def __getitem__(self, value):
		if isinstance(value, tuple):
			return IgnorableTuple(('array', (self, value)))
		else:
			return IgnorableList([self] * value)

	def __call__(self, value, endian='<'):
		if isinstance(value, str):
			return struct.unpack(endian + tuple.__getitem__(self, 0), value[:tuple.__getitem__(self, 1)])[0]
		else:
			return struct.pack(endian + tuple.__getitem__(self, 0), value)

sbyte = schar = int8 = StructType(('b', 1))
byte = char = uint8 = StructType(('B', 1))

short = int16 = StructType(('h', 2))
ushort = uint16 = StructType(('H', 2))

sint = int32 = StructType(('l', 4))
uint = uint32 = StructType(('L', 4))

int64 = StructType(('q', 8))
uint64 = StructType(('Q', 8))

float = StructType(('f', 4))
vec2 = float[2]
vec3 = float[3]
vec4 = float[4]

def string(len, offset=0, encoding=None, nullTerm=True, stripNulls=True, value=''):
	return StructType(('string', (len, offset, encoding, nullTerm, stripNulls, value)))

class ArrayType(object):
	def __init__(self, type, obj, attr):
		self.type = type
		self.obj = obj
		self.attr = attr

	def count(self):
		return getattr(self.obj, self.attr)

class ExprArrayType(ArrayType):
	def __init__(self, type, obj, cb):
		self.type = type
		self.obj = obj
		self.cb = cb

	def count(self):
		return self.cb(self.obj)

class ConstantArrayType(ArrayType):
	def __init__(self, type, size):
		self.type = type
		self.size = size

	def count(self):
		return self.size

class StructException(Exception):
	pass

class BaseStruct(object):
	__slots__ = ('_ignore', '__attrs__', '__baked__', '__defs__', '__endian__', '__format_func__', '__fp__', '__frame__', '__next__', '__odefs__', '__pos__', '__sizes__', '__startpos__', '__triggers__', '__values__')
	
	LE = '<'
	BE = '>'
	__endian__ = '<'
	__format_func__ = None
	__framestack__ = []
	__structstack__ = []

	def __init__(self, unpack=None, **kwargs):
		BaseStruct.__structstack__.append(self)
		self.__baked__ = False
		self.__defs__ = []
		self.__odefs__ = {}
		self.__sizes__ = []
		self.__attrs__ = []
		self.__values__ = {}
		self.__next__ = True
		self._ignore = False

		self.__triggers__ = {}
		
		if self.__format_func__ is not None:
			self.__frame__ = None
			sys.settrace(self.__trace__)
			if self.__format_func__.func_code.co_argcount == 1:
				self.__format_func__.im_func(nameSurrogate)
			else:
				self.__format_func__.im_func()
			sys.settrace(None)
			BaseStruct.__framestack__.pop()
			varnames = self.__format_func__.func_code.co_varnames[self.__format_func__.func_code.co_argcount:]
			for name in varnames:
				value = self.__frame__.f_locals[name]
				self.__odefs__[name] = value
				self.__setattr__(name, value)

		self.__baked__ = True
		
		if unpack != None:
			if isinstance(unpack, tuple):
				self.unpack(*unpack)
			else:
				self.unpack(unpack)
		
		if len(kwargs):
			for name in kwargs:
				self.__values__[name] = kwargs[name]

		BaseStruct.__structstack__.pop()
	
	def __trace__(self, frame, event, arg):
		if self.__frame__ is None:
			self.__frame__ = frame
			BaseStruct.__framestack__.append(frame)

	def __setattr__(self, name, value):
		if name in self.__slots__:
			return object.__setattr__(self, name, value)
		if self.__baked__ == False:
			if not isinstance(value, list):
				value = [value]
				attrname = name
			else:
				attrname = '*' + name
			
			self.__values__[name] = None
			
			for sub in value:
				if isinstance(sub, BaseStruct):
					sub = sub.__class__
				try:
					if issubclass(sub, BaseStruct):
						sub = ('struct', sub)
				except TypeError:
					pass
				type_, size = tuple(sub)
				if type_ == 'string':
					self.__defs__.append(string)
					self.__sizes__.append(size)
					self.__attrs__.append(attrname)
					self.__next__ = True
					
					if attrname[0] != '*':
						self.__values__[name] = size[3]
					elif self.__values__[name] == None:
						self.__values__[name] = [size[3] for val in value]
				elif type_ == 'struct':
					self.__defs__.append(BaseStruct)
					self.__sizes__.append(size)
					self.__attrs__.append(attrname)
					self.__next__ = True
					
					if attrname[0] != '*':
						self.__values__[name] = size()
					elif self.__values__[name] == None:
						self.__values__[name] = [size() for val in value]
				elif type_ == 'array':
					stype, size = size
					if isinstance(size, tuple):
						if hasattr(size, '__fieldname__'):
							obj, attr = self, size.__fieldname__
						else:
							obj, attr = size
						self.__defs__.append(ArrayType(stype, obj, attr))
					elif isinstance(size, str):
						self.__defs__.append(ArrayType(stype, self, size))
					elif callable(size):
						self.__defs__.append(ExprArrayType(stype, self, size))
					else:
						self.__defs__.append(ConstantArrayType(stype, size))
					self.__sizes__.append(None)
					self.__attrs__.append(attrname)
					self.__values__[attrname] = []
				else:
					if self.__next__:
						self.__defs__.append('')
						self.__sizes__.append(0)
						self.__attrs__.append([])
						self.__next__ = False
					
					self.__defs__[-1] += type_
					self.__sizes__[-1] += size
					self.__attrs__[-1].append(attrname)
					
					if attrname[0] != '*':
						self.__values__[name] = 0
					elif self.__values__[name] == None:
						self.__values__[name] = [0 for val in value]
		else:
			try:
				self.__values__[name] = value
			except KeyError:
				raise AttributeError(name)

	def trigger_after(self, name, cb):
		if name not in self.__triggers__:
			self.__triggers__[name] = []
		self.__triggers__[name].append(cb)
	
	def __getattr__(self, name):
		try:
			return self.__values__[name]
		except KeyError:
			raise AttributeError(name)
	
	def __len__(self):
		ret = 0
		arraypos, arrayname = None, None
		
		for i in range(len(self.__defs__)):
			sdef, size, attrs = self.__defs__[i], self.__sizes__[i], self.__attrs__[i]
			
			if sdef == string:
				size, offset, encoding, nullTerm, stripNulls, value = size
				if isinstance(size, str):
					size = self.__values__[size] + offset
			elif sdef == BaseStruct:
				if attrs[0] == '*':
					if arrayname != attrs:
						arrayname = attrs
						arraypos = 0
					size = len(self.__values__[attrs[1:]][arraypos])
				size = len(self.__values__[attrs])
			elif isinstance(sdef, ArrayType):
				stype = tuple(sdef.type)
				size = sum(stype[1] if stype[0] != 'struct' else len(x) for x in self.__values__[attrs])
			
			ret += size
		
		return ret

	def __repr__(self):
		return self.__str__(multiline=False)

	def __str__(self, multiline=True, level=1):
		kv = []
		for attrs in self.__attrs__:
			if isinstance(attrs, list):
				for attr in attrs:
					if attr[0] == '*':
						if len(kv) == 0 or kv[-1][0] != attr[1:]:
							kv.append((attr[1:], self.__values__[attr[1:]]))
					else:
						kv.append((attr, self.__values__[attr]))
			else:
				kv.append((attrs, self.__values__[attrs]))

		if not multiline or len(kv) < 2:
			return '%s(%s)' % (self.__class__.__name__, ', '.join('%s=%r' % (k, v) for k, v in kv))
		else:
			return '%s(\n%s\n)' % (self.__class__.__name__, '\n'.join('%s%s=%s%s' % ('\t' * level, k, v.__str__(multiline=True, level=level+1) if isinstance(v, BaseStruct) else `v`, ', ' if i != len(kv) - 1 else ' ') for i, (k, v) in enumerate(kv)))

	def unpack(self, data, pos=0):
		self.__pos__ = pos
		is_file = isinstance(data, file)
		if is_file:
			self.__fp__ = data
		else:
			self.__fp__ = None
		self.__startpos__ = self.tell()
		for name in self.__values__:
			if not isinstance(self.__values__[name], BaseStruct):
				self.__values__[name] = None
			elif self.__values__[name].__class__ == list and len(self.__values__[name]) != 0:
				if not isinstance(self.__values__[name][0], BaseStruct):
					self.__values__[name] = None
		
		arraypos, arrayname = None, None
		
		for i in range(len(self.__defs__)):
			sdef, size, attrs = self.__defs__[i], self.__sizes__[i], self.__attrs__[i]

			if sdef == string:
				size, offset, encoding, nullTerm, stripNulls, value = size
				if isinstance(size, str):
					size = self.__values__[size] + offset
				
				if is_file:
					temp = data.read(size)
				else:
					temp = data[self.__pos__:self.__pos__+size]
				if len(temp) != size:
					raise StructException('Expected %i byte string, got %i' % (size, len(temp)))
				
				if encoding != None:
					temp = temp.decode(encoding)
				
				if nullTerm:
					temp = temp.split('\0', 1)[0]
				elif stripNulls:
					temp = temp.rstrip('\0')

				if attrs[0] == '*':
					name = attrs[1:]
					if self.__values__[name] == None:
						self.__values__[name] = []
					self.__values__[name].append(temp)
				else:
					self.__values__[attrs] = temp
				self.__pos__ += size
			elif sdef == BaseStruct:
				if attrs[0] == '*':
					if arrayname != attrs:
						arrayname = attrs
						arraypos = 0
					name = attrs[1:]
					self.__values__[attrs][arraypos].unpack(data, self.__pos__)
					self.__pos__ += len(self.__values__[attrs][arraypos])
					arraypos += 1
				else:
					self.__values__[attrs].unpack(data, self.__pos__)
					self.__pos__ += len(self.__values__[attrs])
			elif isinstance(sdef, ArrayType):
				stype = tuple(sdef.type)
				count = sdef.count()
				arr = self.__values__[attrs] = []
				for i in xrange(count):
					if stype[0] == 'struct':
						arr.append(stype[1]().unpack(data, self.__pos__))
						self.__pos__ += len(arr[-1])
					elif is_file:
						arr.append(struct.unpack(self.__endian__+stype[0], data.read(stype[1]))[0])
					else:
						arr.append(struct.unpack(self.__endian__+stype[0], data[self.__pos__:self.__pos__+stype[1]])[0])
			else:
				if is_file:
					values = struct.unpack(self.__endian__+sdef, data.read(size))
				else:
					values = struct.unpack(self.__endian__+sdef, data[self.__pos__:self.__pos__+size])
				self.__pos__ += size
				j = 0
				for name in attrs:
					if name[0] == '*':
						name = name[1:]
						if self.__values__[name] == None:
							self.__values__[name] = []
						self.__values__[name].append(values[j])
					else:
						self.__values__[name] = values[j]
					j += 1

			last = attrs[-1] if isinstance(attrs, list) else attrs
			if last in self.__triggers__:
				for cb in self.__triggers__[last]:
					cb(self)
		
		return self

	def seek(self, pos):
		if self.__fp__ is None:
			self.__pos__ = pos
		else:
			self.__fp__.seek(pos)

	def tell(self):
		if self.__fp__ is None:
			return self.__pos__
		else:
			return self.__fp__.tell()
	
	def pack(self):
		print 'Such deprecated.  Many unmaintained.'
		arraypos, arrayname = None, None
		
		ret = ''
		for i in range(len(self.__defs__)):
			sdef, size, attrs = self.__defs__[i], self.__sizes__[i], self.__attrs__[i]
			
			if sdef == string:
				size, offset, encoding, nullTerm, stripNulls, value = size
				if isinstance(size, str):
					size = self.__values__[size]+offset
				
				if attrs[0] == '*':
					if arrayname != attrs:
						arraypos = 0
						arrayname = attrs
					temp = self.__values__[attrs[1:]][arraypos]
					arraypos += 1
				else:
					temp = self.__values__[attrs]
				
				if encoding != None:
					temp = temp.encode(encoding)
				
				temp = temp[:size]
				ret += temp + ('\0' * (size - len(temp)))
			elif sdef == BaseStruct:
				if attrs[0] == '*':
					if arrayname != attrs:
						arraypos = 0
						arrayname = attrs
					ret += self.__values__[attrs[1:]][arraypos].pack()
					arraypos += 1
				else:
					ret += self.__values__[attrs].pack()
			else:
				values = []
				for name in attrs:
					if name[0] == '*':
						if arrayname != name:
							arraypos = 0
							arrayname = name
						values.append(self.__values__[name[1:]][arraypos])
						arraypos += 1
					else:
						values.append(self.__values__[name])
				
				ret += struct.pack(self.__endian__+sdef, *values)
		return ret
	
	def __getitem__(self, value):
		return IgnorableTuple(('array', (('struct', self.__class__), value)))

	def __call__(self, func):
		return type(func.__name__, (BaseStruct, ), dict(__format_func__=func))

	def toDict(self, recursive=True, attrs=True):
		def conv(obj):
			if isinstance(obj, list):
				return map(conv, obj)
			elif isinstance(obj, BaseStruct):
				return obj.toDict(attrs=attrs)
			else:
				return obj
		df = AttrDict if attrs else dict
		if recursive:
			return df({ k:conv(v) for k, v in self.__values__.items() if not self.__odefs__[k]._ignore})
		else:
			return df({ k:v for k, v in self.__values__.items() if not self.__odefs__[k]._ignore})

Struct = BaseStruct()

__all__ = 'string sbyte schar int8 byte char uint8 short int16 ushort uint16 sint int32 uint uint32 int64 uint64 float vec2 vec3 vec4 Struct StructException STRUCT_RELATIVE RELATIVE ABSOLUTE struct_seek'.split(' ')
