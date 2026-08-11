# ULTRALogger

Server-side event logger for StarCore Space Engineers servers.

## Log output

ULTRALogger writes timestamped files to Space Engineers local storage for the mod, not the Keen log:

```text
ULTRALogger_yyyyMMdd_HHmmss.log
```

The logger is enabled by default and writes server events for grids, blocks, ownership/static changes, player joins/leaves/control changes, damage, chat commands, CoreSystems projectile damage events when that API is present, and external mod API messages.

## Admin commands

Commands require admin, owner, or space master promote level.

```text
/ultralogger status
/ultralogger on
/ultralogger off
/ultralogger flush
/ultralogger saveinterval <seconds>
/ultralogger scaninterval <seconds>
/ultralogger maxqueue <lines>
/ultralogger damage on|off
/ultralogger blocks on|off
/ultralogger grids on|off
/ultralogger players on|off
/ultralogger external on|off
```

`/ulog` is accepted as a short alias.

Settings are persisted in `ULTRALogger.cfg` in local storage.

## External mod API

Other mods can write events into the same server log with this channel:

```csharp
const long UltraLoggerChannel = 1129001129;
MyAPIGateway.Utilities.SendModMessage(UltraLoggerChannel, "WeaponCore|Fire|grid=123 weapon=Railgun shot=456");
```

Mods that prefer delegate endpoints can request them:

```csharp
MyAPIGateway.Utilities.RegisterMessageHandler(UltraLoggerChannel, HandleUltraLoggerApi);
MyAPIGateway.Utilities.SendModMessage(UltraLoggerChannel, "ApiEndpointRequest");
```

The response is a `Dictionary<string, Delegate>` containing:

```text
Log(source, category, message, entityId)
LogSimple(message)
IsEnabled()
```
