SHELL=/bin/bash
PROJ_NAME = $(shell basename `pwd`)
CONFIG_DIR = configs
VERSION = $(shell git describe --tags)
BRANCH := $(shell git rev-parse --abbrev-ref HEAD 2>&1)
BINS = https://ksp-ro.s3-us-west-2.amazonaws.com/TestFlight_bin_KSP-1.10.zip

ZIP_CORE := TestFlightCore

$(info Travis Branch: $(TRAVIS_BRANCH))
$(info Travis Tag: $(TRAVIS_TAG))
ifdef TRAVIS_BRANCH
    ifeq "$(TRAVIS_BRANCH)" "master"
	    ZIP_CORE := $(ZIP_CORE)-release_$(TRAVIS_BUILD_NUMBER)
    else ifeq "$(TRAVIS_BRANCH)" "dev"
	    ZIP_CORE := $(ZIP_CORE)-prerelease_$(TRAVIS_BUILD_NUMBER)
    else
	    ZIP_CORE := $(ZIP_CORE)-dev_$(TRAVIS_BUILD_NUMBER)
    endif
endif

ifdef TRAVIS_TAG
ZIP_CORE := $(ZIP_CORE)-$(TRAVIS_TAG)
endif

ZIP_CORE := $(ZIP_CORE).zip

$(info $(ZIP_CORE))
all: clean meta

ifdef TRAVIS_TAG
release: zip
else
release:
endif

ifdef TRAVIS_TAG
meta:
	python makeMeta.py $(TRAVIS_TAG)
	cp TestFlight.version GameData/TestFlight/TestFlight.version
else
meta:
endif

zip: meta
	zip $(ZIP_CORE) GameData GameData/TestFlight/* GameData/TestFlight/Plugins/* GameData/TestFlight/Resources/* GameData/TestFlight/Resources/Textures/* -x ignore.txt

clean:
	-rm *.zip
	-rm GameData/TestFlight/*.version
	-rm GameData/TestFlight/*.ckan
	-rm *.version
	-rm *.ckan

getBins:
	curl $(BINS) --output bins.zip
	unzip -P $(ZIP_PW) bins.zip