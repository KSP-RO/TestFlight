import os, argparse, sys, json, glob

class DefaultHelpParser(argparse.ArgumentParser):
    def error(self, message):
        sys.stderr.write('error: %s\n' % message)
        self.print_help()
        sys.exit(2)

HELP_DESC = "Creates neccesary metadata files"
parser = DefaultHelpParser(description=HELP_DESC)
parser.add_argument('tag', metavar='tag', type=str, nargs=1,
                   help='tag of release (e.g. 0.4.6.0-RealismOverhaul')

args = parser.parse_args()

if not args.tag or len(args.tag) < 1:
    print "ERROR: git tag must be specified and must be in the format major.minor.patch.build-configuration.\ne.g. 0.4.6.0-RealismOverhaul"
    sys.exit(2)

version = args.tag[0].split("-")[0]
configuration = args.tag[0].split("-")[1]
if configuration == "Stock":
	opposingConfiguration = "RealismOverhaul"
	avcID = 118
elif configuration == "RealismOverhaul":
	opposingConfiguration = "Stock"
	avcID = 117
major = version.split(".")[0]
minor = version.split(".")[1]
patch = version.split(".")[2]
build = version.split(".")[3]
# create AVC .version file
avc = {
	"NAME" : "TestFlight-{}".format(configuration),
	"URL" : "http://ksp-avc.cybutek.net/version.php?id={}".format(avcID),
	"DOWNLOAD" : "https://github.com/jwvanderbeck/TestFlight/releases/download/v{}/TestFlight-{}.zip".format(args.tag[0],args.tag[0]),
	"CHANGE_LOG_URL" : "https://github.com/jwvanderbeck/TestFlight/releases/tag/v{}".format(args.tag[0]),
	"VERSION" :
	{
		"MAJOR" : major,
		"MINOR" : minor,
		"PATCH" : patch,
		"BUILD" : build
	},
	"KSP_VERSION" :
	{
		"MAJOR" : 0,
		"MINOR" : 90,
		"PATCH" : 0
	}
}
with open("TestFlight.version", "w") as f:
	f.write(json.dumps(avc))

# create CKAN'T file
ckant = {
	"spec_version" : 1,
	"name" : "TestFlight-{}".format(configuration),
	"abstract" : "Persistent part research & reliability system gives you a reason to test fly your space hardware.  Fly parts to gain data, the more data the more reliable the parts.",
	"identifier" : "TestFlight-{}".format("configuration"),
	"download" : "https://github.com/jwvanderbeck/TestFlight/releases/download/v{}/TestFlight-{}.zip".format(args.tag[0],args.tag[0]),
	"license" : "CC-BY-NC-SA-4.0",
	"version" : version,
	"release_status" : "testing",
	"ksp_version" : "0.90",
	"resources" : {
		"homepage" : "http://forum.kerbalspaceprogram.com/threads/88187",
		"repository" : "https://github.com/jwvanderbeck/TestFlight",
		"bugtracker" : "https://github.com/jwvanderbeck/TestFlight/issues",
	},
	"install" : [
        {
            "file"       : "GameData/TestFlight",
            "install_to" : "GameData/TestFlight"
        }
    ],
	"depends" : [
        { "name" : "ModuleManager", "min_version" : "2.3.8" }
    ],
    "conflicts" : [
    	{ "name" : "TestFlight-{}".format(opposingConfiguration)}
    ]
}
with open("TestFlight-{}-{}.ckan".format(configuration, version), "w") as f:
	f.write(json.dumps(ckant))
