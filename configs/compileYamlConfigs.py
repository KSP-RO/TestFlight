import os, argparse, sys, json, glob, pprint
import yaml

class DefaultHelpParser(argparse.ArgumentParser):
    def error(self, message):
        sys.stderr.write('error: %s\n' % message)
        self.print_help()
        sys.exit(2)

def load(filename):
    with open(filename, "r") as f:
        data = yaml.load(f)
        return data

def compileConfigNode(name, node, depth = 2):
	nodeString = ""
	nodeString += '\t' * depth
	nodeString += name + '\n'
	nodeString += '\t' * depth
	nodeString += "{\n"

	for key,value in node.iteritems():
		if isinstance(value,dict):
			nodeString += compileConfigNode(key, value, depth + 1)
		elif isinstance(value,list):
			nodeString += '\t' * (depth + 1)
			nodeString += key + '\n'
			nodeString += '\t' * (depth + 1)
			nodeString += "{\n"
			for item in value:
				nodeString += '\t' * (depth + 2)
				nodeString += "key = {}\n".format(item)
			nodeString += '\t' * (depth + 1)
			nodeString += "}\n"
		else:
			nodeString += '\t' * (depth + 1)
			nodeString += "{} = {}\n".format(key, value)

	nodeString += '\t' * depth
	nodeString += "}\n"
	return nodeString

def compileFloatCurve(name, node, depth = 2):
	nodeString = ""
	nodeString += '\t' * (depth)
	nodeString += name + '\n'
	nodeString += '\t' * (depth)
	nodeString += "{\n"
	for item in node:
		nodeString += '\t' * (depth + 1)
		nodeString += "key = {}\n".format(item)
	nodeString += '\t' * (depth)
	nodeString += "}\n"
	return nodeString

def compile(pattern, partConfig, data):
	cachedModule = {}
	moduleString = pattern + "\n{\n" 
	for module in partConfig:
		if not "name" in module:
			cachedModule = module
			continue
		if cachedModule != None and len(cachedModule) > 1:
			module.update(cachedModule)
			cachedModule = {}
		moduleString += "\n\tMODULE\n\t{\n"
		for key,value in module.iteritems():
			if isinstance(value,list):
				if isinstance(value[0],dict):
					for node in value:
						if isinstance(node,dict):
							moduleString += compileConfigNode(key, node)
				else:
					moduleString += compileFloatCurve(key, value, 2)
			else:
				moduleString += "\t\t{} = {}\n".format(key,value)
		moduleString += "\t}"
	moduleString += "\n}\n"
	return moduleString


HELP_DESC = "Compiles a given TestFlight Config YAML to standard ModuleManager.cfg"
parser = DefaultHelpParser(description=HELP_DESC)
parser.add_argument('directory', metavar='directory', type=str, nargs=1,
                   help='directory containing .yaml configs to compile')

args = parser.parse_args()

# print args

if not args.directory or len(args.directory) < 1:
    print "ERROR: No configuration directory specified.  Configuration directory must be specified."
    sys.exit(2)

configs = glob.glob(args.directory[0] + "/*.yaml")

for yamlFile in configs:
	print "PROCESSING " + yamlFile
	finalConfig = ""
	data = load(yamlFile)

	pp = pprint.PrettyPrinter(indent=2,width=120)
	# pp.pprint(data)

	parts = data["Parts"]
	for part in parts:
		print "\tProcessing part: ", part["part"]
		patterns = part["patterns"]
		configs = part["configs"]
		for pattern in patterns:
			for config in configs:
				finalConfig += compile(pattern, config, data)

	baseName = os.path.splitext(yamlFile)[0]
	print "WRITING " + baseName + ".cfg"
	with open(baseName + ".cfg", "w") as cfgFile:
		cfgFile.write(finalConfig)

