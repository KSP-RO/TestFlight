SHELL=/bin/bash
PROJ_NAME = $(shell basename `pwd`)
CONFIG_DIR = configs
VERSION = $(shell git describe --tags)

ifdef $(TRAVIS_TAG)
BUILD := $(shell echo $(TRAVIS_TAG) | cut -d - -f 2)
else
ifdef $(TRAVIS_BRANCH)
BUILD := $(TRAVIS_BRANCH)
else
BUILD := $(shell git rev-parse --abbrev-ref HEAD 2>&1)
endif
endif
ZIPFILE := $(PROJ_NAME)-$(VERSION).zip

all: configs

release: zip

configs: $(CONFIG_DIR)/%.cfg
	cp $(CONFIG_DIR)/$(BUILD)/*.cfg GameData/TestFlight

$(CONFIG_DIR)/%.cfg:
	cd $(CONFIG_DIR);python compileConfigs.py $(BUILD)

zip: configs
	zip -r $(ZIPFILE) GameData

clean:
	-rm GameData/TestFlight/*.cfg
	-rm $(CONFIG_DIR)/$(BUILD)/*.cfg
	-rm *.zip
