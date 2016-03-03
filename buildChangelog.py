# -*- coding: utf8 -*-
import urllib, json, argparse, sys, os, codecs

class DefaultHelpParser(argparse.ArgumentParser):
    def error(self, message):
        sys.stderr.write('error: %s\n' % message)
        self.print_help()
        sys.exit(2)

HELP_DESC = "Auto creates change log for next version based on commits since last release or a specified tagged release"
parser = DefaultHelpParser(description=HELP_DESC)
parser.add_argument('version', metavar='version', type=str, nargs=1,
                   help='version number of *next* release version in format v1.2.3.4')
parser.add_argument('--tag', metavar='tag', type=str,
                   help='Tagged release to use for building the changelog in the format 1.2.3.4')
parser.add_argument('--branch', metavar='branch', type=str,
                   help='Branch to look at for commits')

args = parser.parse_args()

if not args.version or len(args.version) < 1:
    print "ERROR: Next release version must be specified"
    sys.exit(2)

if not args.branch:
	args.branch = "master"

if (args.tag):
	releaseURL = urllib.urlopen("https://api.github.com/repos/KSP-RO/TestFlight/releases/tags/" + args.tag)
else:
	releaseURL = urllib.urlopen("https://api.github.com/repos/KSP-RO/TestFlight/releases/latest")
latestRelease = json.load(releaseURL)
print latestRelease
sinceDate = latestRelease["created_at"]
print "Getting since date: " + sinceDate
print "Using branch: " + args.branch
commitsURL = urllib.urlopen("https://api.github.com/repos/KSP-RO/TestFlight/commits?since=" + sinceDate + "&sha=" + args.branch)
print "https://api.github.com/repos/KSP-RO/TestFlight/commits?since=" + sinceDate + "?sha=" + args.branch
commits = json.load(commitsURL)
print commits

commits = commits[:-1]

print args.version[0]
print "----------\n"
for commit in commits:
	if not commit["commit"]["message"].startswith("-"):
		formattedMessage = commit["commit"]["message"].replace("\n\n", "\n").replace("\n", "").replace("NEW:", "\n* **NEW**:").replace("FIX:", "\n* **FIX**:")
		print formattedMessage
