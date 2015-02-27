# -*- coding: utf8 -*-
import urllib, json, argparse, sys, os, codecs

class DefaultHelpParser(argparse.ArgumentParser):
    def error(self, message):
        sys.stderr.write('error: %s\n' % message)
        self.print_help()
        sys.exit(2)

HELP_DESC = "Auto creates change log for next version based on commits since last release"
parser = DefaultHelpParser(description=HELP_DESC)
parser.add_argument('version', metavar='version', type=str, nargs=1,
                   help='version number of *next* release version in format v1.2.3.4')

args = parser.parse_args()

if not args.version or len(args.version) < 1:
    print "ERROR: Next release version must be specified"
    sys.exit(2)

latestReleaseURL = urllib.urlopen("https://api.github.com/repos/jwvanderbeck/TestFlight/releases/latest")
latestRelease = json.load(latestReleaseURL)
sinceDate = latestRelease["created_at"]
commitsURL = urllib.urlopen("https://api.github.com/repos/jwvanderbeck/TestFlight/commits?since=" + sinceDate)
commits = json.load(commitsURL)

commits = commits[:-1]

print args.version[0]
print "----------\n"
for commit in commits:
	print "* " + commit["commit"]["message"]
