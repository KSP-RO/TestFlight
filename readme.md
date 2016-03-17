TestFlight
==========
[![Build Status Master](https://travis-ci.org/KSP-RO/TestFlight.svg?branch=master)](https://travis-ci.org/KSP-RO/TestFlight)
[![Build Status dev](https://travis-ci.org/KSP-RO/TestFlight.svg?branch=dev)](https://travis-ci.org/KSP-RO/TestFlight)

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
