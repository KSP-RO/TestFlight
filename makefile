SHELL=/bin/bash
PROJ_NAME = $(shell basename `pwd`)
CONFIG_DIR = configs
VERSION = $(shell git describe --tags)
BRANCH := $(shell git rev-parse --abbrev-ref HEAD 2>&1)

ifdef TRAVIS_TAG
BUILD := $(shell echo $(TRAVIS_TAG) | cut -d - -f 2)
else
BUILD := $(BRANCH)
endif

ZIPFILE := $(PROJ_NAME)-$(TRAVIS_TAG).zip

all: configs

release: zip
	echo BUILD IS $(BUILD)

configs_master: configs_HEAD

configs_HEAD: configs_Stock configs_RealismOverhaul

configs_Stock: $(CONFIG_DIR)/Stock/%.cfg
	cp $(CONFIG_DIR)/Stock/*.cfg GameData/TestFlight

configs_RealismOverhaul: $(CONFIG_DIR)/RealismOverhaul/%.cfg
	cp $(CONFIG_DIR)/RealismOverhaul/*.cfg GameData/TestFlight

$(CONFIG_DIR)/Stock/%.cfg:
	cd $(CONFIG_DIR);python compileConfigs.py Stock

$(CONFIG_DIR)/RealismOverhaul/%.cfg:
	cd $(CONFIG_DIR);python compileConfigs.py RealismOverhaul

ifdef TRAVIS_TAG
meta:
	python makeMeta.py $(TRAVIS_TAG)
	cp TestFlight.version GameData/TestFlight/TestFlight.version
else
meta:
endif

zip: configs_$(BUILD) meta
	zip -r $(ZIPFILE) GameData

clean: clean_$(BUILD)
	echo Build is $(BUILD)
	-rm GameData/TestFlight/*.cfg
	-rm *.zip
	-rm GameData/TestFlight/*.version
	-rm *.version

clean_master: clean_HEAD

clean_HEAD: clean_Stock clean_RealismOverhaul

clean_Stock:
	-rm $(CONFIG_DIR)/Stock/*.cfg

clean_RealismOverhaul:
	-rm $(CONFIG_DIR)/RealismOverhaul/*.cfg

