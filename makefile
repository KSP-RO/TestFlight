SHELL=/bin/bash
PROJ_NAME = $(shell basename `pwd`)
CONFIG_DIR = configs
VERSION = $(shell git describe --tags)
ifdef $(TRAVIS_BRANCH)
BRANCH := $(TRAVIS_BRANCH)
else
BRANCH := $(shell git rev-parse --abbrev-ref HEAD 2>&1)
endif

ifdef $(TRAVIS_TAG)
BUILD := $(shell echo $(TRAVIS_TAG) | cut -d - -f 2)
else
BUILD := $(BRANCH)
endif

ZIPFILE := $(PROJ_NAME)-$(TRAVIS_TAG).zip

all: configs

release: zip

configs: $(CONFIG_DIR)/%.cfg
	cp $(CONFIG_DIR)/$(BUILD)/*.cfg GameData/TestFlight

$(CONFIG_DIR)/%.cfg:
	cd $(CONFIG_DIR);python compileConfigs.py $(BUILD)

zip: configs
	zip -r $(ZIPFILE) GameData

clean:
	echo $(TRAVIS_TAG)
	echo $(TRAVIS_BRANCH)
	echo $(BRANCH)
	echo $(BUILD)
	-rm GameData/TestFlight/*.cfg
	-rm $(CONFIG_DIR)/$(BUILD)/*.cfg
	-rm *.zip
