# Tether
Server Density compatible Windows Agent. This agent has been running on over 200 machines, and is in active development.

## Requirements

- A Windows Machine
- .NET 4.7.2 minimum
- Server Density Account

# Installation

## Automated installation

There is a separate project - [Tether.Installer](https://github.com/surgicalcoder/tether.installer) - that automates the installation and provisioning of a machine through the Server Density API. While there are more instructions on the project, you can use the following powershell script to automatically download and install Tether, using the latest builds:

      $source = "https://ci.appveyor.com/api/projects/surgicalcoder/tether-installer/artifacts/SDInstaller.exe" 
      $destination = "c:\SDInstaller.exe"
      $WebClient = New-Object System.Net.WebClient
      $WebClient.DownloadFile( $source, $destination )
      Start-Process -FilePath $destination -ArgumentList '((APIKEY))','https://ci.appveyor.com/api/projects/surgicalcoder/tether/artifacts/Tether%2Fbin%2FBuild.zip','C:\Tether' -NoNewWindow -Wait
      Remove-Item $destination

Replace ((APIKEY)) with an API key you have retrieved from Server Density - [You can find more information about this here](https://developer.serverdensity.com/reference#getting-a-token-via-the-ui). You may also want to update the other parameters (for example, installation location which in this example is C:\Tether).

This will automatically download Tether, the Tether installer, and install it to the path specified.

## Manual Installation Instructions

Grab a build, either build yourself or from the AppVeyor link at the bottom of this page (Visual Studio 2017 and the .NET framework 4.7.2 SDK are required), and it should produce you some files in the bin/Debug folder. 

Copy these to your server or servers, put into a directory, then edit settings: 

### Editing settings

Edit your settings.json, and you will need to change at least the following:

    "ServerDensityUrl": "https://{account}.agent.serverdensity.io/intake/?agent_key={agentkey}",
    "ServerDensityKey": "{agentkey}",

and put in your SD account name in there, and Agent Key.

For example:

      "ServerDensityUrl": "https://example.agent.serverdensity.io/intake/?agent_key=1234b36fadcacb07d0c7c1111767afee",
      "ServerDensityKey": "1234b36fadcacb07d0c7c1111767afee",



### Configuration - Logging Level

By default, Tether is set to log only Errors, which is defined in `Tether.exe.config`:

      <logger name="*" minLevel="Error" appendTo="console" />
      <logger name="*" minLevel="Error" appendTo="file" />
      <logger name="*" minLevel="Error" appendTo="selectiveFile" />

It's possible to change the logging level by updating `Tether.exe.config` with the desired logging levels, which are (in order of verbosity, ascending):

* Fatal
* Error
* Warn
* Info
* Debug
* Trace

For example, if you wanted to log everything, set the logging levels to Trace, such as:

      <logger name="*" minLevel="Trace" appendTo="console" />
      <logger name="*" minLevel="Trace" appendTo="file" />
      <logger name="*" minLevel="Trace" appendTo="selectiveFile" />

> NOTE: There are 3 separate loggers, one  that outputs to console (ie. if you are not running in Windows Services Mode), one for a file that contains all the levels, and one that will produce you a Warning only file, a Debug only file etc.

If you wish to add new loggers, new targets etc - the logging framework used is [NLog](http://nlog-project.org/) - you can find more information on [NLog's documentation](https://github.com/nlog/nlog/wiki/Targets)

### Installation

Once you are happy with your JSON, save it, then load up an Administrator's command prompt, and type in the following:

    Tether.exe install

That should spit out quite a few lines, at the end you are looking for:

	The transacted install has completed.

That means it has happily registered it self as a Windows Service, and can be started by hand.

### To start

	net start Tether

You can also run this as a command line, and not through Windows Services, simply by running Tether.exe

### To stop:

	net stop Tether

# Plugin Framework

By default, depending on how you built this, you will just get the basic SD compatible plugin, if you want some deeper system stats, build **Tether.CoreSlices**, create a **plugins** folder, and put the dll in there.

We have essentially the same interface as Server Density's windows agent. 

### Additional Plugins

A separate GitHub project - [Tether.Plugins](https://github.com/surgicalcoder/Tether.Plugins) has been set up for additional plugins.

### Self updating Plugins! [0.0.8+]

A new feature of Tether 0.0.8 is automatically checking for updates to plugins, every 5 mins, from a URL you specify in the configuration file like so:

      "PluginManifestLocation": "~/PluginManifest.json"
      
This will go and check that file location. There are 3 ways to specify a path - an absolute local path, a relative path, and a URL.

Every 5 minutes, Tether will check the plugins loaded, against the plugin manifest file - it will perform a regex match against the name (so you can have one manifest file targeting many machines!), then automatically download and extract the file, and restart itself.

### Plugin configuration data [0.0.13+]

Plugins can now make use of Configuration Data - by implementing the `IRequireConfigurationData` interface - the `LoadConfigurationData(dynamic data)` method will be called before the check is called, so you can load all the configuration data you need.

Configuration data files live in the `plugins` directory, and are named `(Full Class Name).json` i.e. `Tether.SamplePlugin.ASPNet.json` and will be loaded automatically on start. 

## Version History

* [2.0.x] A number of changes were introduced at one point through Version 2, without documentation:
  * Long running checks are now here - `ILongRunningMetricProvider` -  the plugin provides a duration for it's metrics to be cached. Note - this data does not persist between application runs, so if you stop Tether and restart it, the cache will be cleared. An example of this can be found in the [Windows Updates Plugin](https://github.com/surgicalcoder/Tether.Plugins/blob/master/Tether.WindowsUpdates/WindowsUpdateCheck.cs)

  * A configuration option - `PluginMemoryLimit` (in bytes) - denotes how much memory the Plugins process can consume before being marked as plugins need to be reloaded - some custom plugins leak memory and resources, so this ensures that if the limit is reached, Plugins are restarted. Default value is 200mb.

* [2.0.0] **BREAKING CHANGES!** Version 2 contains the following breaking changes:
    * Plugins are now loaded in their own AppDomain. This will allow future work to unload an AppDomain and reload plugins, to dynamically load new versions of plugins, without restarting the entire process. This will also allow for unloading/reloading AppDomains due to memory-leaky plugins.
    * Support for resending of old telemetry data to SD, in case of transient connectivity issues. 
    * Tether now requires Admin rights to run.
* [1.0.34] Issue found by Steve Hurley @ Server Density with base IIS check not being run.
* [1.0.33] Issue with loading dependencies from DLL's, should be resolved now.
* [1.0.27] Changed convention for build numbers! Also fixed two issues - one where non plugin dll's were registered as plugins, and where some downloaded plugins were not updating.
* [0.0.13] Plugins can now require configuration data to work!
* [0.0.12] Fixed memory leak issue with Performance Counter work.
* [0.0.11] PerformanceCounterGroups can now actually point to Performance Counters, not just WMI counters that they (should) represent. The [Tether.Plugins](https://github.com/surgicalcoder/Tether.Plugins) project (Specifically the ASPNetRequests project) has a example of this working.
* [0.0.10] A couple more Divide by Zero errors (this release was never made public)
* [0.0.9] Fixed a couple of Divide by Zero errors, and auto-renaming of clashing plugins
* [0.0.8] Introduction of self updating plugin framework
* [0.0.8] Removal of some unused settings i.e. MongoDB for Windows.

## Roadmap
* Pull config manifest settings from DNS TXT records (for large scale deployments)

## Build
Builds are being run, thanks to AppVeyor!

[![Build status](https://ci.appveyor.com/api/projects/status/0a6937115b1hwdtv?svg=true)](https://ci.appveyor.com/project/surgicalcoder/tether)
