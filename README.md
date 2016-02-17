# Tether
Server Density compatible Windows Agent. We currently have this agent running on over 110 machines, and is in active development.

## Requirements

- A Windows Machine
- .NET 4.0 minimum
- Server Density Account

## How to use

Grab a build, either build yourself or from the AppVeyor link at the bottom of this page (I've tested this only with Visual Studio 2015, but it should work with other versions, your mileage may vary), and it should produce you some files in the bin/Debug folder. 

Copy these to your server or servers, put into a directory, then edit settings: 

### Editing settings

Edit your settings.json, and you will need to change at least the following:

    "ServerDensityUrl": "https://accountname.serverdensity.io",
    "ServerDensityKey": "[Machine Key goes in here!]",

and put in your SD account name in there, and Server's Key.

### Provisioning / Installation Agent

There is now an installation agent available - [Tether.Installer](https://github.com/surgicalcoder/tether.installer) - instructions can be found on the project page.

### Configuration - Logging Level

By default, the logging level is a bit too low, it will log everything it will send, into the logs folder. While you may wish to keep this, its suggested that you drop this. You can do this by editing Tether.exe.config, by changing:

      <logger name="*" minLevel="Trace" appendTo="console" />
      <logger name="*" minLevel="Trace" appendTo="file" />
      <logger name="*" minLevel="Trace" appendTo="selectiveFile" />

to

      <logger name="*" minLevel="Error" appendTo="console" />
      <logger name="*" minLevel="Error" appendTo="file" />
      <logger name="*" minLevel="Error" appendTo="selectiveFile" />

> NOTE: There are 3 separate loggers, one  that outputs to console (ie. if you are not running in Windows Services Mode), one for a file that contains all the levels, and one that will produce you a Warning only file, a Debug only file etc.

### Installation

Once you are happy with your JSON, save it, then load up an Administrator's command prompt, and type in the following:

    Tether.exe install

That should spit out quite a few lines of gibberish, at the end you are looking for:

	The transacted install has completed.

That means it has happily registered it self as a Windows Service, and can be started by hand.

### To start

	net start ThreeOneThree.Tether

You can also run this as a command line, and not through Windows Services, simply by running ThreeOneThree.Tether.exe

### To stop:

	net stop ThreeOneThree.Tether

## Plugin Framework

By default, depending on how you built this, you will just get the basic SD compatible plugin, if you want some deeper system stats, build **Tether.CoreSlices**, create a **plugins** folder, and put the dll in there.

We have essentially the same interface as Server Density's windows agent. 

### Addtional Plugins

A seperate GitHub project - [Tether.Plugins](https://github.com/surgicalcoder/Tether.Plugins) has been set up for additional plugins.

### Self updating Plugins!

A new feature of Tether 0.0.8 is automatically checking for updates to plugins, every 5 mins, from a URL you specify in the configuration file like so:

      "PluginManifestLocation": "~/PluginManifest.json"
      
This will go and check that file location. There are 3 ways to specify a path - an absolute local path, a relative path, and a URL.

Every 5 minutes, Tether will check the plugins loaded, against the plugin manifest file - it will perform a regex match against the name (so you can have one manifest file targetting many machines!), then automatically download and extract the file, and restart itself.

## Version History

* [0.0.11] PerformanceCounterGroups can now actually point to Performance Counters, not just WMI counters that they (should) represent. The [Tether.Plugins](https://github.com/surgicalcoder/Tether.Plugins) project (Specifically the ASPNetRequests project) has a example of this working.
* [0.0.10] A couple more Divide by Zero errors (this release was never made public)
* [0.0.9] Fixed a couple of Divide by Zero errors, and auto-renaming of clashing plugins
* [0.0.8] Introduction of self updating plugin framework
* [0.0.8] Removal of some unused settings i.e. MongoDB for Windows.

## Build
Builds are being run, thanks to AppVeyor!

[![Build status](https://ci.appveyor.com/api/projects/status/0a6937115b1hwdtv?svg=true)](https://ci.appveyor.com/project/surgicalcoder/tether)
