# SimHub Property Server

## About

This is a plugin for [SimHub](https://www.simhubdash.com/). It allows access to SimHub properties via a tcp connection.

Clients can subscribe for property changes. They will then receive updates each time, when the value of a subscribed property changes. 

One use case is the project [StreamDeckSimHubPlugin](https://github.com/pre-martin/StreamDeckSimHubPlugin), which allows updating the state of Stream Deck keys via SimHub properties.

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
<subscribe dcp.gd.EngineIgnitionOn
>Property dcp.gd.EngineIgnitionOn integer (null)
<subscribe dcp.gd.IsPitlimiterOrPitLane
>Property dcp.gd.IsPitlimiterOrPitLane boolean (null)

>Property dcp.gd.EngineIgnitionOn integer 0
>Property dcp.gd.IsPitlimiterOrPitLane boolean True
>Property dcp.gd.EngineIgnitionOn integer 1
>Property dcp.gd.EngineIgnitionOn integer 0
<disconnect
```

In this example, the client subscribes to the ignition property and to "IsPitLimiterOrPitLane". Initially, as no game is running, both properties have a value of `null`, transmitted as `(null)`. When a game is launched, the ignition property changes to `0`, the "pitlane" property to "True", and the client receives both changes. Afterwards the ignition is toggled on and off in the game, before the client decides to `disconnect`.

Property names follow the convention of SimHub, e.g. `[DataCorePlugin.GameRunning]` is called `dcp.GameRunning` in this plugin, or `[DataCorePlugin.GameData.ABSLevel]` is called `dcp.gd.ABSLevel` in this plugin.


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
  acc.graphics.ABS integer
  acc.graphics.CarCount integer
  acc.graphics.CompletedLaps integer
  acc.graphics.CurrentSectorIndex integer
  acc.graphics.currentTyreSet integer
  acc.graphics.directionLightsLeft integer
  acc.graphics.directionLightsRight integer
  acc.graphics.DriverStintTimeLeft integer
  acc.graphics.DriverStintTotalTimeLeft integer
  acc.graphics.EngineMap integer
  acc.graphics.FlashingLights integer
  acc.graphics.gapAhead integer
  acc.graphics.gapBehind integer
  acc.graphics.GetHashCode integer
  acc.graphics.globalChequered integer
  acc.graphics.globalGreen integer
  acc.graphics.globalRed integer
  acc.graphics.globalWhite integer
  acc.graphics.globalYellow integer
  acc.graphics.globalYellow1 integer
  acc.graphics.globalYellow2 integer
  acc.graphics.globalYellow3 integer
  acc.graphics.iBestTime integer
  acc.graphics.iCurrentTime integer
  acc.graphics.IdealLineOn integer
  acc.graphics.iDeltaLapTime integer
  acc.graphics.iEstimatedLapTime integer
  acc.graphics.iLastTime integer
  acc.graphics.isDeltaPositive integer
  acc.graphics.IsInPit integer
  acc.graphics.IsInPitLane integer
  acc.graphics.iSplit integer
  acc.graphics.IsSetupMenuVisible integer
  acc.graphics.isValidLap integer
  acc.graphics.LastSectorTime integer
  acc.graphics.LightsStage integer
  acc.graphics.MainDisplayIndex integer
  acc.graphics.MandatoryPitDone integer
  acc.graphics.mfdTyreSet integer
  acc.graphics.missingMandatoryPits integer
  acc.graphics.NumberOfLaps integer
  acc.graphics.PacketId integer
  acc.graphics.PlayerCarID integer
  acc.graphics.Position integer
  acc.graphics.RainLights integer
  acc.graphics.RainTyres integer
  acc.graphics.SecondaryDisplayIndex integer
  acc.graphics.SessionIndex integer
  acc.graphics.strategyTyreSet integer
  acc.graphics.TC integer
  acc.graphics.TCCut integer
  acc.graphics.WiperLV integer
  acc.physics.absinAction integer
  acc.physics.AutoShifterOn integer
  acc.physics.DrsAvailable integer
  acc.physics.DrsEnabled integer
  acc.physics.EngineBrake integer
  acc.physics.ErsHeatCharging integer
  acc.physics.ErsisCharging integer
  acc.physics.ErsPowerLevel integer
  acc.physics.ErsRecoveryLevel integer
  acc.physics.frontBrakeCompound integer
  acc.physics.Gear integer
  acc.physics.GetHashCode integer
  acc.physics.ignitionOn integer
  acc.physics.IsAIControlled integer
  acc.physics.isEngineRunning integer
  acc.physics.NumberOfTyresOut integer
  acc.physics.P2PActivation integer
  acc.physics.P2PStatus integer
  acc.physics.PacketId integer
  acc.physics.PitLimiterOn integer
  acc.physics.rearBrakeCompound integer
  acc.physics.Rpms integer
  acc.physics.starterEngineOn integer
  acc.physics.tcinAction integer
  dcp.GameInMenu boolean
  dcp.GamePaused boolean
  dcp.GameReplay boolean
  dcp.GameRunning boolean
  dcp.gd.ABSActive integer
  dcp.gd.ABSLevel integer
  dcp.gd.BestLapOpponentPosition integer
  dcp.gd.CarSettings_FuelAlertActive integer
  dcp.gd.CarSettings_FuelAlertEnabled integer
  dcp.gd.CarSettings_MaxGears integer
  dcp.gd.CarSettings_RPMRedLinePerGearOverride integer
  dcp.gd.CompletedLaps integer
  dcp.gd.CurrentLap integer
  dcp.gd.CurrentSectorIndex integer
  dcp.gd.DRSAvailable integer
  dcp.gd.DRSEnabled integer
  dcp.gd.EngineIgnitionOn integer
  dcp.gd.EngineMap integer
  dcp.gd.EngineStarted integer
  dcp.gd.Flag_Black integer
  dcp.gd.Flag_Blue integer
  dcp.gd.Flag_Checkered integer
  dcp.gd.Flag_Green integer
  dcp.gd.Flag_Orange integer
  dcp.gd.Flag_White integer
  dcp.gd.Flag_Yellow integer
  dcp.gd.IsInPit integer
  dcp.gd.IsInPitLane integer
  dcp.gd.IsLapValid boolean
  dcp.gd.IsPitlimiterOrPitLane boolean
  dcp.gd.MapAllowed boolean
  dcp.gd.OpponentsCount integer
  dcp.gd.PitLimiterOn integer
  dcp.gd.PlayerClassOpponentsCount integer
  dcp.gd.Position integer
  dcp.gd.RemainingLaps integer
  dcp.gd.Spectating boolean
  dcp.gd.SpotterCarLeft integer
  dcp.gd.SpotterCarRight integer
  dcp.gd.TCActive integer
  dcp.gd.TCLevel integer
  dcp.gd.TotalLaps integer
  dcp.gd.TurnIndicatorLeft integer
  dcp.gd.TurnIndicatorRight integer
  dcp.Spectating boolean
Available commands:
  subscribe propertyName
  unsubscribe propertyName
  disconnect
```


## Building of the plugin

1. Copy required DLLs from SimHub to the local directory `SimHub`
   - SimHub.Plugins.dll
   - GameReaderCommon.dll
   - log4net.dll
   - Newtonsoft.Json.dll
2. Restore NuGet packages:  
   `msbuild -t:restore -p:Platform="Any CPU" -p:RestorePackagesConfig=true`
3. Build the project:  
   `msbuild -p:Platform="Any CPU" -p:Configuration=Release`
