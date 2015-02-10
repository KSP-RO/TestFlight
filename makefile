SHELL=/bin/bash
PROJ_NAME = $(shell basename `pwd`)
CONFIG_DIR = configs
VERSION = $(shell git describe --tags)
BRANCH := $(shell git rev-parse --abbrev-ref HEAD 2>&1)

ZIPFILE := $(PROJ_NAME)-$(TRAVIS_TAG).zip

all: clean configs meta
	cp -r GameData/TestFlight/ ~/Dropbox/KSP/TestFlight/

release: zip

configs: $(CONFIG_DIR)/RealismOverhaul/%.cfg
	cp $(CONFIG_DIR)/RealismOverhaul/*.cfg GameData/TestFlight

$(CONFIG_DIR)/RealismOverhaul/%.cfg:
	cd $(CONFIG_DIR);python compileConfigs.py RealismOverhaul

ifdef TRAVIS_TAG
meta:
	python makeMeta.py $(TRAVIS_TAG)
	cp TestFlight.version GameData/TestFlight/TestFlight.version
else
meta:
endif

zip: configs meta
	zip -r $(ZIPFILE) GameData

clean:
	-rm $(CONFIG_DIR)/RealismOverhaul/*.cfg
	-rm GameData/TestFlight/*.cfg
	-rm *.zip
	-rm GameData/TestFlight/*.version
	-rm GameData/TestFlight/*.ckan
	-rm *.version
	-rm *.ckan
