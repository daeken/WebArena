import json, os, os.path

class AssetManager(object):
	def __init__(self, base):
		self.base = base

		if os.path.exists(base + '/filecache.json'):
			self.files = json.load(file(base + '/filecache.json', 'r'))
		else:
			self.files = {}
			for dp, dns, fns in os.walk(base, followlinks=True):
				for fn in fns:
					cn = dp + '/' + fn
					self.files[cn.lower()] = cn
			json.dump(self.files, file(base + '/filecache.json', 'w'))

	def findAll(self, ext):
		if ext[0] != '.':
			ext = '.' + ext
		return [x for x in self.files.keys() if x.endswith(ext)]

	def file(self, name, mode='rb'):
		return file(self.files[name], mode)

	def fileFuzzy(self, name, mode='rb'):
		if name in self.files:
			return file(self.files[name], mode)
		name = '/' + name
		for fn in self.files:
			if fn.endswith(name):
				return file(self.files[fn], mode)
		return None

	def exists(self, name, exact=False):
		name = name.lower()
		if exact:
			return name in self.files
		
		if name in self.files:
			return True
		name = '/' + name
		for fn in self.files:
			if fn.endswith(name):
				return True
		return False
