TestFlight
==========

Project Status
--------------

[![Stories in Testing](https://badge.waffle.io/jwvanderbeck/TestFlight.png?label=testing&title=In Testing)](https://waffle.io/jwvanderbeck/TestFlight) [![Stories in In Progress](https://badge.waffle.io/jwvanderbeck/TestFlight.png?label=in%20progress&title=In%20Progress)](https://waffle.io/jwvanderbeck/TestFlight)


TestFlight is currently in Alpha development, but is available for play in KSP.

As of 30 January 2015, TestFlight is has separate releases for Stock and Realism Overhaul.  Please ensure you download the proper one.

Stock
-----
[![Build Status Master](https://travis-ci.org/KSP-RO/TestFlight.svg?branch=master)](https://travis-ci.org/jwvanderbeck/TestFlight)

Latest Release: [v0.4.5 Alpha Stock](https://github.com/KSP-RO/TestFlight/releases/tag/0.4.5-Stock)    
Forum Thread: [KSP Add Ons Development Thread](http://forum.kerbalspaceprogram.com/threads/88187)    
GitHub: https://github.com/jwvanderbeck/TestFlight    
Bug Reports & Feature Requests: https://github.com/jwvanderbeck/TestFlight/issues    
Waffle Status Board: https://waffle.io/jwvanderbeck/TestFlight

**Config Status**

* Engines
	* All liquid engines
	* All solid engines
	* All monopropellant engines
* Fuel Tanks
	* All liquid fuel tanks
	* All monopropellant fuel tanks
	* All xenon fuel tanks

Currently the configs for the stock branch apply to any stock or stock-alike parts and part packs that use the standard stock resources.  In generall if you are playing a "Stock" or "Stock-alike" game, you should find valid TestFlight configs for the parts listed above in **Config Status**.  However there are no special configs for specific parts.  This means that currently for example, every liquid engine has the same reliability.

I am looking for people willing to volunteer to help build better configs for stock.  If you are interested please let me know by posting in the forum thread.  Thank you!

Realism Overhaul
----------------
[![Build Status RealismOverhaul](https://travis-ci.org/KSP-RO/TestFlight.svg?branch=RealismOverhaul)](https://travis-ci.org/jwvanderbeck/TestFlight) 

Latest Release: [v0.4.5 Alpha RealismOverhaul](https://github.com/KSP-RO/TestFlight/releases/tag/0.4.5.1-RealismOverhaul)    
Forum Thread: [KSP Add Ons Development Thread](http://forum.kerbalspaceprogram.com/threads/88187)    
GitHub: https://github.com/KSP-RO/TestFlight/RealismOverhaul    
Bug Reports & Feature Requests: https://github.com/KSP-RO/TestFlight/issues    
Waffle Status Board: https://waffle.io/jwvanderbeck/TestFlight

**Config Status**

* Engines
	* WAC-Corporal, Aerobee-Hi, Aerobee-150
	* X-405
	* AJ-10-37,AJ-10-42

I am looking for people willing to volunteer to help with the configs for Realism Overhaul.  If you are interested please let me know by posting in the forum thread.  Thank you!


----------------------------------

#TestFlight
###A configurable, extensible, parts research and reliability system for Kerbal Space Program (KSP).

---

##Release Details
TestFlight is currently in development and not yet released.  Alpha test versions will be released soon.

##License
TestFlight is released on the Creative Commons 4.0 by-nc-sa License. 

http://creativecommons.org/licenses/by-nc-sa/4.0/

##What Is TestFlight?
TestFlight is a persistent, parts based, research and reliability system.  It gives you a reason to do test flights of your rockets.  Test flights let your engineers generate flight data, which can then be analyzed to further improve the reliability of your rocket parts.  Flight data generated on a part is persistent, and carries over to new rockets flown with those parts.

In a nutshell; Fly your parts more, generate flight data, get more reliable parts.  But TestFlight is much more!

##FlightData
The more you fly parts, the more flight data they record, which in turns improves reliability.  As stated, this is persistent.  That means that if you launch a "Super Rocket 1" with a Mainsail engine, that mainsail generates let's say 10,000 units of flight data.  Now you go back to the VAB and build "Super Rocket 2" which also uses the mainsail.  That instance of the mainsail starts with 10,000 units of flight data already from the earlier flight.  That means test flights make your rockets more relable!

##Reliability and Failures
TestFlight calculates reliability using the flight data recorded by a part, based on various criteria and mathemtical equations.  It will periodicly make failure checks against this data and if a part is determined to have failed, it will generate failure events.

##TestFlight is Configurable and Extensible
The real beauty of TestFlight is that it is extremely configurable by the end user, or by mod authors who want to integrate it into thier own mods.  TestFlight works on a series of pluggable PartModules that give full control over how reliability is calculated and what failures are possible - all on a per part basis if desired.  Further more, all of the various options can be configured by the user or mod authors, such as minimum and maximum reliability ratings, failure rates, repair costs, and more.

On top of all that, TestFlight comes with a public API that allows mod authors to further extend the system!  Mod authors can easily add additional failure types, or change how flight data is recorded, or how reliability is calculated.  The API for example could allow a mod auther to easily extend the system to penalize the reliability of individual parts as they get old.
