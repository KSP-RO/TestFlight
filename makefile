PROJ_NAME = $(shell basename `pwd`)
CONFIG_DIR = configs
VERSION = $(shell git describe --tags)
BRANCH=$(shell git rev-parse --abbrev-ref HEAD 2>&1)

ZIPFILE := $(PROJ_NAME)-$(VERSION).zip

all: configs

release: zip

configs: $(CONFIG_DIR)/%.cfg
	cp $(CONFIG_DIR)/$(BRANCH)/*.cfg GameData/TestFlight

$(CONFIG_DIR)/%.cfg:
	cd $(CONFIG_DIR);python compileConfigs.py $(BRANCH)

zip: configs
	zip -r $(ZIPFILE) GameData

clean:
	-rm GameData/TestFlight/*.cfg
	-rm $(CONFIG_DIR)/$(BRANCH)/*.cfg
	-rm *.zip
