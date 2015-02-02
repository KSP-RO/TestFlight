# This utility script compiles TestFlight configurations into final proper ModuleManager config files

import os, argparse, sys, json, glob

ALL_BODIES = [
	"sun",
	"moho",
	"eve",
	"gilly",
	"kerbin",
	"min",
	"minmus",
	"duna",
	"ike",
	"dres"
	"jool",
	"laythe",
	"vall",
	"tylo",
	"bop",
	"pol",
	"eeloo"
]

ALL_SITUATIONS = [
	"atmosphere",
	"space"
]

ALL_SCOPES = [
	"deep_space"
]

class DefaultHelpParser(argparse.ArgumentParser):
    def error(self, message):
        sys.stderr.write('error: %s\n' % message)
        self.print_help()
        sys.exit(2)

def loadJSON(jsonFilename):
    with open(jsonFilename, "r") as jsonFile:
        rawJson = json.load(jsonFile)
        return rawJson

def compileFailureDef(d):
	definition = rawJson["FailureConfigs"][d]
	node = ""
	for key in definition:
		node += "\n\t\t{} = {}".format(key, definition[key])
	return node

def compileRepairDef(d):
	definition = rawJson["RepairConfigs"][d]
	node = "\n\t\tREPAIR\n\t\t{"
	for key in definition:
		node += "\n\t\t\t{} = {}".format(key, definition[key])
	node += "\n\t\t}"
	return node

def compileCurveDef(d):
	definition = rawJson["ReliabilityDefinitions"][d]
	# build reliability curves for each scope unless its overridden
	configNodes = ""
	for scope in ALL_SCOPES:
		configNodes += "\n\t\tRELIABILITY_BODY\n\t\t{"
		configNodes += "\n\t\t\tscope = " + scope
		configNodes += "\n\t\t\treliabilityCurve"
		configNodes += "\n\t\t\t{"
		curve = definition["default"]
		if scope in definition:
			curve = definition[scope]
		for key in curve:
			configNodes += "\n\t\t\t\t" + key
		configNodes += "\n\t\t\t}"
		configNodes += "\n\t\t}"
	return configNodes

def compile(pattern, config):
	configDef = rawJson["TestFlightConfigs"][config]
	node = "\n" + pattern + "\n{"
	if not "TestFlightCore" in configDef:
		node += "\n\tMODULE\n\t{\n\t\tname = TestFlightCore\n\t}\n"

	for moduleName in configDef:
		moduleConfig = configDef[moduleName]
		node += "\n\tMODULE\n\t{\n\t\tname = " + moduleName
		for key in moduleConfig:
			if key == "CURVE_DEF":
				node += compileCurveDef(moduleConfig[key])
			elif key == "REPAIR_DEF":
				node += compileRepairDef(moduleConfig[key])
			elif key == "FAILURE_DEF":
				node += compileFailureDef(moduleConfig[key])
			else:
				if isinstance(moduleConfig[key], dict):
					node += "\n\t\t" + key
					node += "\n\t\t{"
					for subKey in moduleConfig[key]["keys"]:
						node += "\n\t\t\t" + subKey
					node += "\n\t\t}"
				else:
					node += "\n\t\t{} = {}".format(key, moduleConfig[key])
		node += "\n\t}"
	node += "\n}"
	return node


HELP_DESC = "Compiles a given TestFlight config.json to standard ModuleManager.cfg"
parser = DefaultHelpParser(description=HELP_DESC)
parser.add_argument('directory', metavar='directory', type=str, nargs=1,
                   help='directory containing .json configs to compile')

args = parser.parse_args()

# print args

if not args.directory or len(args.directory) < 1:
    print "ERROR: No configuration directory specified.  Configuration directory must be specified."
    sys.exit(2)

configs = glob.glob(args.directory[0] + "/*.json")

for jsonFile in configs:
	print "PROCESSING " + jsonFile
	rawJson = loadJSON(jsonFile)

	# Init scopes
	for body in ALL_BODIES:
		for situation in ALL_SITUATIONS:
			ALL_SCOPES.append(body + "_" + situation)

	# Process each defined part config
	finalConfig = ""
	for partConfig in rawJson["PartConfigs"].values():
		configs = partConfig["configs"]
		patterns = partConfig["patterns"]
		for pattern in patterns:
			for config in configs:
				finalConfig += compile(pattern, config)

	baseName = os.path.splitext(jsonFile)[0]
	with open(baseName + ".cfg", "w") as cfgFile:
		cfgFile.write(finalConfig)



