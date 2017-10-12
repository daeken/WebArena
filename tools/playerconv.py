import json, re, sys
from glob import glob
from pprint import pprint
from md3conv import convert, Header

def loadSkin(fn):
	data = file(fn, 'r').read()
	return dict(line.strip().split(',', 1) for line in data.split('\n') if ',' in line and not line.strip().endswith(','))

def loadSkinClass(dir, name):
	return {
		'head' : loadSkin('%s/head_%s.skin' % (dir, name)), 
		'upper' : loadSkin('%s/upper_%s.skin' % (dir, name)), 
		'lower' : loadSkin('%s/lower_%s.skin' % (dir, name))
	}

def loadAllSkins(dir):
	names = [fn.rsplit('/', 1)[-1][5:-5] for fn in glob(dir + '/head_*.skin')]
	return {name: loadSkinClass(dir, name) for name in names}

def processFile(fn):
	fp = file(fn, 'rb')
	return convert(fp)

def linestrip(line):
	return re.sub(r'[ \t\r]+', ' ', line.split('//', 1)[0]).strip()

animationDefs = [
	(3, 'death1'), 
	(3, 'dead1'), 
	(3, 'death2'), 
	(3, 'dead2'), 
	(3, 'death3'), 
	(3, 'dead3'), 

	(1, 'gesture'), 
	(1, 'attack'), 
	(1, 'attack2'), 
	(1, 'drop'), 
	(1, 'raise'), 
	(1, 'stand'), 
	(1, 'stand2'), 

	(2, 'walkcr'), 
	(2, 'walk'), 
	(2, 'run'), 
	(2, 'back'), 
	(2, 'swim'), 
	(2, 'jump'), 
	(2, 'land'), 
	(2, 'jumpb'), 
	(2, 'landb'), 
	(2, 'idle'), 
	(2, 'idlecr'), 
	(2, 'turn'), 
]

def loadAnimations(dir):
	lines = map(linestrip, file(dir + '/animation.cfg').read().split('\n'))
	lines = [line.split(' ') for line in lines if line != '']
	assert lines[0][0] == 'sex'

	sex = lines[0][1]
	animations = {}
	offset = 0
	for i, line in enumerate(lines[1:]):
		line = map(int, line)
		adef = animationDefs[i]

		if adef[0] == 2 and adef[1] == 'walkcr':
			offset = line[0] - animations['gesture'][1]
		animations[adef[1]] = (adef[0], line[0] - offset, line[1], line[2], line[3])

	return sex, animations

def main(dir, ofn=None):
	skins = loadAllSkins(dir)
	sex, animations = loadAnimations(dir)

	output = dict(
		sex=sex, 
		animations=animations, 
		lower=processFile(dir + '/lower.md3'), 
		upper=processFile(dir + '/upper.md3'), 
		head=processFile(dir + '/head.md3'), 
		skins=skins
	)
	if ofn is None:
		pprint(output)
	else:
		json.dump(output, file(ofn, 'wb'))

if __name__=='__main__':
	main(*sys.argv[1:])
