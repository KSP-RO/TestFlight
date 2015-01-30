TestFlight
==========

Project Status
--------------

[![Stories in Ready](https://badge.waffle.io/jwvanderbeck/TestFlight.png?label=ready&title=Ready)](https://waffle.io/jwvanderbeck/TestFlight) [![Stories in In Progress](https://badge.waffle.io/jwvanderbeck/TestFlight.png?label=ready&title=In%20Progress)](https://waffle.io/jwvanderbeck/TestFlight)


TestFlight is currently in Alpha development, but is available for play in KSP.

As of 30 January 2015, TestFlight is not maintained in two separate versions and branches in GitHub.  Stock and Realism Overhaul.

Stock
-----
[![Build Status Master](https://travis-ci.org/jwvanderbeck/TestFlight.svg?branch=master)](https://travis-ci.org/jwvanderbeck/TestFlight) 
Latest Release: https://github.com/jwvanderbeck/TestFlight/releases/tag/v0.4.0
Forum Thread: http://forum.kerbalspaceprogram.com/threads/88187-0-90-TestFlight-0-4-0-28JAN15-A-configurable-extensible-parts-research-reliability-system
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

Currently the configs for the stock branch apply to any stock or stock-alike parts and part packs that use the standard stock resources.  In generall if you are playing a "Stock" or "Stock-alike" game, you should valid TestFlight configs for the parts listed above in **Config Status**.  However there are no special configs for specific parts.  This means that currently for example, every liquid engine has the same reliability.

I am looking for people willing to volunteer to help build better configs for stock.  If you are interested please let me know by posting in the forum thread.  Thank you!

Realism Overhaul
----------------
[![Build Status RealismOverhaul](https://travis-ci.org/jwvanderbeck/TestFlight.svg?branch=RealismOverhaul)](https://travis-ci.org/jwvanderbeck/TestFlight) 
Latest Release: None
Forum Thread: http://forum.kerbalspaceprogram.com/threads/88187-0-90-TestFlight-0-4-0-28JAN15-A-configurable-extensible-parts-research-reliability-system
GitHub: https://github.com/jwvanderbeck/TestFlight/RealismOverhaul
Bug Reports & Feature Requests: https://github.com/jwvanderbeck/TestFlight/issues
Waffle Status Board: https://waffle.io/jwvanderbeck/TestFlight

**Config Status**

* Engines
	* WAC Corporal

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
The more you fly parts, the more flight data they record, which in turns improves reliability.  As stated, this is persistent.  That means that if you launch a "Super Rocket 1" with a Mainsail engine, that mainsail generates let's say 10,000 units of flight data.  Now you go back to the VAB and build "Super Rocket 2" which also uses the mainsail.  That instance o fthe mainsail starts with 10,000 units of flight data already from the earlier flight.  That means test flights make your rockets more relable!

##Reliability and Failures
TestFlight calculates reliability using the flight data recorded by a part, based on various criteria and mathemtical equations.  It will periodicly make failure checks against this data and if a part is determined to have failed, it will generate failure events.

##TestFlight is Configurable and Extensible
The real beauty of TestFlight is that it is extremely configurable by the end user, or by mod authors who want to integrate it into thier own mods.  TestFlight works on a series of pluggable PartModules that give full control over how reliability is calculated and what failures are possible - all on a per part basis if desired.  Further more, all of the various options can be configured by the user or mod authors, such as minimum and maximum reliability ratings, failure rates, repair costs, and more.

On top of all that, TestFlight comes with a public API that allows mod authors to further extend the system!  Mod authors can easily add additional failure types, or change how flight data is recorded, or how reliability is calculated.  The API for example could allow a mod auther to easily extend the system to penalize the reliability of individual parts as they get old.

---


##Module Structure for Mod Authors (and others technicaly curious)
####TestFlight

Core of the system.  All other modules plug into and interact with this one
	
####FlightDataRecorder
	
The FlightDataRecorder module is responsible for recording two types of information:
  * FlightData - Which is an abstract float value that conceptualizes the recording of flight data that is analyzed to improve reliability.  FlightData from "instance" of a part, carries over to newer instances of that same part.  In other words, the more you fly a certain part, the more flight data it accumulates.
  * FlightTime - Which is a record of how many seconds of flight time the part has.  Flight time is specific to the part instance and does not carry over to other instances.
These two pieces of information are stored in various contexts.  One overall recording for "deep space", and then individual recording for "space" and "atmosphere" for each body of influence in the game.

The behaviour of the FlightDataRecorder module influences how that data is recorded, and thus directly influences the behaviour of other parts of the system.  This allows modders to expand the system to generate and record data in a different manner than included in the base TestFlight mod.  By default the TestFlight mod contains three types of FlightDataRecorder modules.  The base module, FlightDataRecorder, records data at all times that the part is "active".  Active is defined by the game itself and is pretty much all the time between launch and the part being destroyed.  FlightDataRecorder_Engine, defines a module that records only when a rocket engine is actively in use and generating thrust.  Lastly, FlightDataRecorder_Online, is designed for things such as unmanned command pods that require electrical energy to function.  This module only records data when that part is receiving electricity and is "online".

####TestFlight_Reliability

The TestFlight_Reliability modules are responsible for determine the overall relaibility of the part, based on  whatever criteria it uses.  There can be more than one type of TestFlight_Reliability module on a aprt, and they are cumulative.  Therefore some modules may add reliablity, while others may take it away.  The base module, TestFlight_Reliability, determines reliability based on recorded flight data.  Varous forumulae may be configured, but essentially the more flightdata recorded, the more reliable the part is.  The default TestFlight mod contains the base reliability module just mentioned, as well as additional modules such as TestFlight_Reliability_GPenalty, TestFlight_Reliability_TemperaturePenalty, and TestFlight_Reliability_PressurePenalty, which all give a negative reliablity, in other words a penalty, for parts that are operating at the extremes of thier specifications such as under high G loads, high pressure, or high temperatures.

####TestFlight_Failure

The last type of module is the TestFlight_Failure modules which define specific failures for the part.  This module based approach to failures makes the system incredibly powerul, and extensible, as you can easily create new failure types and plug them into the system.  The internal code of the specific failure module defines what happens if that failure occurs, but they all have common settings that plug them into the system such as severity -- minor, standard, major, the chances of that failure occuring, what type of faliure it is -- mechanical or software, and what is required to attempt repair of the failure, if it is at all possible.
