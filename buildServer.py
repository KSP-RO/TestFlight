import sys, os, httplib, urllib, argparse, json

# This script runs after a successful build and uses the build server API to add
# a new build and the files that go with it
# This only adds database entries though, the files still need to be FTPd

def parseChanges(s):
	s = s.strip()
	if len(s) == 0:
		return s
	if s[0] == "*":
		s = "\n" + s
	elif s.startswith("commit") or s.startswith("Date"):
		return s + "\n"
	elif s.startswith("Author"):
		return s[0:s.find("<")] + "\n"
	else:
		s = " " + s
	return s


def authenticate(buildserver, endpoint, username, password):
	# Authenticate with the API to get a token
	params = urllib.urlencode({'name': username, 'password': password})
	headers = {"Content-type": "application/x-www-form-urlencoded","Accept": "text/json"}
	conn = httplib.HTTPConnection(buildserver)
	conn.request("POST", endpoint + "/authenticate", params, headers)
	response = conn.getresponse()
	data = json.loads(response.read())
	# print data
	# print data['token']
	apitoken = data['token']
	conn.close()
	return apitoken

def submitBuild(projectID, buildName, changes, token):
	changes = map(parseChanges, changes)
	changelog = "".join(changes)

	params = urllib.urlencode({
		'buildName': buildName, 
		'buildChangelog': changelog,
		'projectID' : projectID
		})
	headers = {"Content-type": "application/x-www-form-urlencoded","Accept": "text/json", "x-access-token": token}
	conn = httplib.HTTPConnection(buildserver)
	conn.request("POST", endpoint + "/builds", params, headers)
	response = conn.getresponse()
	data = json.loads(response.read())
	# print data
	# print data['id']
	buildID = data['id']
	conn.close()
	return buildID

def submitFile(buildID, filename, filepath, token):
	params = urllib.urlencode({
		'releaseFilename': filename, 
		'releaseFilepath': filepath,
		'buildID' : buildID
		})
	headers = {"Content-type": "application/x-www-form-urlencoded","Accept": "text/json", "x-access-token": token}
	conn = httplib.HTTPConnection(buildserver)
	conn.request("POST", endpoint + "/releases", params, headers)
	response = conn.getresponse()
	data = json.loads(response.read())
	# print data
	# print data['id']
	releaseID = data['id']
	conn.close()
	return releaseID


class DefaultHelpParser(argparse.ArgumentParser):
    def error(self, message):
        sys.stderr.write('error: %s\n' % message)
        self.print_help()
        sys.exit(2)

HELP_DESC = """
Submits new builds and files to a build server API

The script will try to get any omitted values from environment variables.  All string values ARE case sensitive
"""
parser = DefaultHelpParser(description=HELP_DESC)
parser.add_argument('action', metavar='action', type=str, nargs=1,
                   choices=['build', 'file', 'all'], help='Action to perform, either submitting a build, or a file, or all which will submit the build and then all specified files')
parser.add_argument('--project-name', metavar='projectName', type=str, required=True,
                   help='Name of the project as given on the build server.  Must match filepaths.  Uses $PROJECT_NAME if not specified')
parser.add_argument('--build-name', metavar='buildName', type=str, required=True,
                   help='Name of this build  By convention, should be [branch]_[build_number]. Uses [$TRAVIS_BRANCH]_[$TRAVIS_BUILD_NUMBER] if not specified')
parser.add_argument('--project-id', metavar='projectID', type=int,
                   help='(BUILD)Database ID of the project on the build server.')
parser.add_argument('--changelog', metavar='changelog', type=str,
                   help='(BUILD)path to file that contains change log for the build.')
parser.add_argument('--build-id', metavar='buildID', type=int,
                   help='(FILE)Database ID of the build this file belongs to.  Uses $BUILD_ID if not specified.')
parser.add_argument('--files', metavar='filename', type=str, nargs='*',
                   help='(FILE)Filename of the file to be associated with the build.  This is *just* the filename, not the path.')

args = parser.parse_args()

# Setup initial variables by grabbing from environment
# API Server URL
# builds.johnvanderbeck.com
buildserver = os.getenv('API_SERVER')
endpoint = os.getenv('API_ENDPOINT')

# The username and password are stored in environment variables so
# as to remain hidden from code
# y5fE2TsjLVjmk1SzuLKZPdDNiChlq
username = os.getenv('API_USERNAME')
password = os.getenv('API_PASSWORD')

print args
if not args.project_name:
	args.project_name = os.getenv('PROJECT_NAME')
if not args.build_name:
	args.build_name = os.getenv('TRAVIS_BRANCH') + '_' + os.getenv('TRAVIS_BUILD_NUMBER')
if not args.build_id:
	args.build_id = os.getenv('BUILD_ID')

projectName = args.project_name
buildName = args.build_name
changelogFile = args.changelog
buildID = args.build_id

token = authenticate(buildserver, endpoint, username, password)

if 'build' in args.action or 'all' in args.action:
	with open(changelogFile, 'r') as f:
		changes = f.readlines();
	buildID = submitBuild(args.project_id, buildName, changes, token)

if 'file' in args.action or 'all' in args.action:
	for filename in args.files:
		filepath = 'http://builds.johnvanderbeck.com/builds/' + projectName + '/build_' + buildName + '/' + filename
		submitFile(buildID, filename, filepath, token)

print buildID