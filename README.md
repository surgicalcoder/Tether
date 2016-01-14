# Tether
Server Density compatible Windows Agent. We currently have this agent running on over 70 machines, and is in active development.

## Requirements

- A Windows Machine
- .NET 4.5 minimum
- Server Density Account

## How to use

Grab a build, either build yourself (I've tested this only with Visual Studio 2015, but it should work with other versions, your mileage may vary), and it should produce you some files in the bin/Debug folder. Copy these to your server or servers, and put in a directory. 

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

## Additional Plugins

By default, depending on how you built this, you will just get the basic SD compatible plugin, if you want some deeper system stats, build **Tether.CoreSlices**, create a **plugin** folder, and put the dll in there.

We have essentially the same interface as Server Density's windows agent. 

## Build
Builds are being run, thanks to AppVeyor!

[![Build status](https://ci.appveyor.com/api/projects/status/0a6937115b1hwdtv?svg=true)](https://ci.appveyor.com/project/surgicalcoder/tether)
