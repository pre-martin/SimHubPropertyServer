# SimHub Property Server

## About

This is a plugin for [SimHub](https://www.simhubdash.com/). It allows access to SimHub properties via a tcp connection.

Clients can subscribe for property changes. They will then receive updates each time, when the value of a subscribed property changes. 


## Installation

Simply copy the file `PropertyServer.dll` into the root directory of your SimHub installation. After having launched SimHub, the plugin has to be activated under "_Settings - Plugins_" (usually SimHub will autodetect the plugin and ask at startup, if the plugin shall be activated).

Optionally, the checkbox "_Show in left menu_" can be activated. This will show an entry named "_Property Server_" in the left menu bar. This entry allows to adjust the settings of the plugin.


## Usage

Open a telnet connection to `localhost` on port `18082` (or whatever port has been set in the settings). The property will respond with its name:

```
$ telnet localhost 18082
SimHub Property Server
```

Now simply send `help` in order to receive a list of subscribable properties and a list of supported commands.

The following shows an example of the communication. The characters `<` and `>` are not part of the communication, they are just used in this example to illustrate what has been sent by the server (`>`) and what has been sent by the client (`<`):

```
$ telnet localhost 18082
>SimHub Property Server
<subscribe gd.sdb.EngineIgnitionOn
>Property gd.sdb.EngineIgnitionOn integer (null)
<subscribe gd.sdb.IsPitlimiterOrPitLane
>Property gd.sdb.IsPitlimiterOrPitLane boolean (null)

>Property gd.sdb.EngineIgnitionOn integer 0
>Property gd.sdb.IsPitlimiterOrPitLane boolean True
>Property gd.sdb.EngineIgnitionOn integer 1
>Property gd.sdb.EngineIgnitionOn integer 0
<disconnect
```

In this example, the client subscribes to the ignition property and to "IsPitLimiterOrPitLane". Initially, as no game is running, both properties have a value of `null`, transmitted as `(null)`. When a game is launched, the ignition property changes to `0`, the "pitlane" property to "True", and the client receives both changes. Afterwards the ignition is toggled on and off in the game, before the client decides to `disconnect`.


## Limitations

At the moment, there are two limitations in effect:

1. The plugin will send data only at a rate of 10 Hz.
2. Only properties of type `bool` and `int` are supported.

Limitation (1) was chosen because the plugin is not meant for real time communication. If real time is a requirement, then the UDP forwarding of SimHub should be used instead.

Limitation (2) could be changed, if there are requirements to read other properties. It's just a matter of implementing other data types and maybe something like a "min delta" concept, so that not every tiny change in the decimal places of a double value will trigger a network transfer.


## "Help"

This is the current output of the command `help`:

```
Available properties:
  gd.GameInMenu
  gd.GamePaused
  gd.GameReplay
  gd.GameRunning
  gd.sdb.ABSActive
  gd.sdb.ABSLevel
  gd.sdb.BestLapOpponentPosition
  gd.sdb.CarSettings_FuelAlertActive
  gd.sdb.CarSettings_FuelAlertEnabled
  gd.sdb.CarSettings_MaxGears
  gd.sdb.CarSettings_RPMRedLinePerGearOverride
  gd.sdb.CompletedLaps
  gd.sdb.CurrentLap
  gd.sdb.CurrentSectorIndex
  gd.sdb.DRSAvailable
  gd.sdb.DRSEnabled
  gd.sdb.EngineIgnitionOn
  gd.sdb.EngineMap
  gd.sdb.EngineStarted
  gd.sdb.Flag_Black
  gd.sdb.Flag_Blue
  gd.sdb.Flag_Checkered
  gd.sdb.Flag_Green
  gd.sdb.Flag_Orange
  gd.sdb.Flag_White
  gd.sdb.Flag_Yellow
  gd.sdb.IsInPit
  gd.sdb.IsInPitLane
  gd.sdb.IsLapValid
  gd.sdb.MapAllowed
  gd.sdb.OpponentsCount
  gd.sdb.PitLimiterOn
  gd.sdb.PlayerClassOpponentsCount
  gd.sdb.Position
  gd.sdb.RemainingLaps
  gd.sdb.Spectating
  gd.sdb.SpotterCarLeft
  gd.sdb.SpotterCarRight
  gd.sdb.TCActive
  gd.sdb.TCLevel
  gd.sdb.TotalLaps
  gd.sdb.TurnIndicatorLeft
  gd.sdb.TurnIndicatorRight
  gd.Spectating
Available commands:
  subscribe propertyName
  unsubscribe propertyName
  disconnect
```


## Building of the plugin

```
msbuild /p:Platform="Any CPU" /p:Configuration=Release
```
