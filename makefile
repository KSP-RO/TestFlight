SHELL=/bin/bash
PROJ_NAME = $(shell basename `pwd`)
CONFIG_DIR = configs
VERSION = $(shell git describe --tags)
BRANCH := $(shell git rev-parse --abbrev-ref HEAD 2>&1)

ZIP_CORE := TestFlightCore
ZIP_STOCK := TestFlightStock

$(info Travis Branch: $(TRAVIS_BRANCH))
$(info Travis Tag: $(TRAVIS_TAG))
ifdef TRAVIS_BRANCH
    ifeq "$(TRAVIS_BRANCH)" "master"
	    ZIP_CORE := $(ZIP_CORE)-release_$(TRAVIS_BUILD_NUMBER)
	    ZIP_STOCK := $(ZIP_STOCK)-release_$(TRAVIS_BUILD_NUMBER)
    else ifeq "$(TRAVIS_BRANCH)" "dev"
	    ZIP_CORE := $(ZIP_CORE)-prerelease_$(TRAVIS_BUILD_NUMBER)
	    ZIP_STOCK := $(ZIP_STOCK)-prerelease_$(TRAVIS_BUILD_NUMBER)
    else
	    ZIP_CORE := $(ZIP_CORE)-dev_$(TRAVIS_BUILD_NUMBER)
	    ZIP_STOCK := $(ZIP_STOCK)-dev_$(TRAVIS_BUILD_NUMBER)
    endif
endif

ifdef TRAVIS_TAG
ZIP_CORE := $(ZIP_CORE)-$(TRAVIS_TAG)
ZIP_STOCK := $(ZIP_STOCK)-$(TRAVIS_TAG)
else
endif

ZIP_CORE := $(ZIP_CORE).zip
ZIP_STOCK := $(ZIP_STOCK).zip

$(info $(ZIP_CORE))
$(info $(ZIP_STOCK))
all: clean meta configs
	cp -r GameData/TestFlight/ ~/Dropbox/KSP/TestFlight/

install: clean meta
	-rm ~/Developer/KSP/1.0/TestFlightDEV/Dev/GameData/TestFlight/Config/*.cfg
	cp -r GameData/TestFlight/ ~/Dropbox/KSP/TestFlight/

install11: clean meta
	-rm ~/Developer/KSP/1.1/TestFlightDEV/Dev/GameData/TestFlight/Config/*.cfg
	cp -r GameData/TestFlight/ ~/Dropbox/KSP11/TestFlight/

release: zip

local: clean configs plugins

ifdef TRAVIS_TAG
plugins:
else
plugins:
	cp bin/Release/TestFlight.dll GameData/TestFlight/Plugins/TestFlight.dll
	cp TestFlightCore/TestFlightCore/bin/Release/TestFlightCore.dll GameData/TestFlight/Plugins/TestFlightCore.dll
	cp TestFlightAPI/TestFlightAPI/bin/Release/TestFlightAPI.dll GameData/TestFlight/Plugins/TestFlightAPI.dll
	cp TestFlightContracts/bin/Release/TestFlightContracts.dll GameData/TestFlight/Plugins/TestFlightContracts.dll
endif

configs: $(CONFIG_DIR)/Stock/%.cfg
	cp $(CONFIG_DIR)/Stock/*.cfg GameData/TestFlight/Config
	zip $(ZIP_STOCK) -r GameData/TestFlight/Config -x ignore.txt

$(CONFIG_DIR)/Stock/%.cfg:
	cd $(CONFIG_DIR);python compileYamlConfigs.py Stock

ifdef TRAVIS_TAG
meta:
	python makeMeta.py $(TRAVIS_TAG)
	cp TestFlight.version GameData/TestFlight/TestFlight.version
	cp TestFlight.version GameData/TestFlight/Config/TestFlight.version
else
meta:
endif

zip: meta configs
	zip $(ZIP_CORE) GameData GameData/TestFlight/* GameData/TestFlight/Plugins/* GameData/TestFlight/Resources/* GameData/TestFlight/Resources/Textures/* -x ignore.txt

clean:
	-rm $(CONFIG_DIR)/Stock/*.cfg
	-rm GameData/TestFlight/Config/*.cfg
	-rm *.zip
	-rm GameData/TestFlight/*.version
	-rm GameData/TestFlight/Config/*.version
	-rm GameData/TestFlight/*.ckan
	-rm *.version
	-rm *.ckan

ifdef TRAVIS_TAG
deploy:
else
ifeq ($(TRAVIS_SECURE_ENV_VARS),true)
deploy:
	@curl --ftp-create-dirs -T ${ZIP_CORE} -u ${FTP_USER}:${FTP_PASSWD} ftp://stantonspacebarn.com/webapps/buildtracker/builds/TestFlight/build_$(TRAVIS_BRANCH)_$(TRAVIS_BUILD_NUMBER)/$(ZIP_CORE)
	@curl --ftp-create-dirs -T ${ZIP_STOCK} -u ${FTP_USER}:${FTP_PASSWD} ftp://stantonspacebarn.com/webapps/buildtracker/builds/TestFlight/build_$(TRAVIS_BRANCH)_$(TRAVIS_BUILD_NUMBER)/$(ZIP_STOCK)
	python buildServer.py all --project-id 0 --project-name TestFlight --build-name $(TRAVIS_BRANCH)_$(TRAVIS_BUILD_NUMBER) --changelog changes.md --files $(ZIP_CORE) $(ZIP_STOCK) $(ZIP_RO)
else
deploy:
	echo No secure environment available. Skipping deploy.
endif
endif
