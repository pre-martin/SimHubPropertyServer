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
<subscribe GameData.StatusDataBase.EngineIgnitionOn
>Property GameData.StatusDataBase.EngineIgnitionOn integer (null)
>Property GameData.StatusDataBase.EngineIgnitionOn integer 0
>Property GameData.StatusDataBase.EngineIgnitionOn integer 1
>Property GameData.StatusDataBase.EngineIgnitionOn integer 0
<disconnect
```

In this example, the client subscribed to the ignition property. Initially, as no game is running, the property has a value of `null`, transmitted as `(null)`. When a game is launched, the property changes to `0` and the client receives this change. Afterwards the ignition was toggled on and off in the game, before the client decided to `disconnect`.


## Limitations

At the moment, there are two limitations in effect:

1. The plugin will send data only at a rate of 10 Hz.
2. Only properties of type `bool` and `int` are supported.

Limitation (1) was chosen because the plugin is not meant for real time communication. If real time is a requirement, then the UDP forwarding of SimHub should be used instead.

Limitation (2) could be changed, if there are requirements to read other properties. It's just a matter of implementing other data types and maybe something like a "min delta" concept, so that not every tiny change in the decimal places of a double value will trigger a network transfer. 