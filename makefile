PROJ_NAME = $(shell basename `pwd`)
CONFIG_DIR = configs
VERSION = $(shell git describe --tags)
ZIPFILE := $(PROJ_NAME)-$(VERSION).zip

all: configs

release: zip

configs: $(CONFIG_DIR)/%.cfg
	cp $(CONFIG_DIR)/*.cfg GameData/TestFlight

$(CONFIG_DIR)/%.cfg:
	cd $(CONFIG_DIR);python compileConfigs.py

zip: configs
	zip -r $(ZIPFILE) GameData

clean:
	-rm GameData/TestFlight/*.cfg
	-rm $(CONFIG_DIR)/*.cfg
	-rm *.zip
