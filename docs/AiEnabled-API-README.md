# AiEnabled API Documentation

A comprehensive API for spawning, controlling, and managing bots in the AiEnabled Space Engineers mod.

## Overview

The AiEnabled API provides both **local** and **remote** access patterns:

- **Local API**: Direct access when running within the AiEnabled mod
- **Remote API**: Message-based communication for external mods and Torch plugins

## Features

✅ **Bot Spawning**: Create bots with detailed configuration options  
✅ **Bot Control**: Set targets, destinations, and behaviors  
✅ **Bot Information**: Query bot status, health, and properties  
✅ **Event System**: Subscribe to bot lifecycle events  
✅ **Error Handling**: Comprehensive validation and error reporting  
✅ **Multiplayer Support**: Full server/client synchronization  
✅ **Extensibility**: Custom commands and data support  

## Quick Start

### For External Mods (Remote API)

```csharp
using AiEnabled.Api;
using AiEnabled.Api.Data;

// Check if AiEnabled is ready
if (AiEnabledApiClient.CanSpawn())
{
    // Spawn a repair bot
    long botId = AiEnabledApiClient.SpawnBot(
        position: MyAPIGateway.Session.Player.GetPosition() + Vector3D.Forward * 10,
        role: "REPAIR",
        displayName: "RepairBot-001",
        ownerId: MyAPIGateway.Session.Player?.IdentityId
    );
    
    if (botId != 0)
    {
        MyAPIGateway.Utilities.ShowMessage("Success", $"Spawned bot: {botId}");
    }
}

// Don't forget cleanup
public override void UnloadData()
{
    AiEnabledApiClient.Cleanup();
}
```

### For Internal Use (Local API)

```csharp
using AiEnabled.Api;
using AiEnabled.Api.Data;

var api = AiEnabledApiImplementation.Instance;

var spawnRequest = new BotSpawnRequest
{
    Position = new Vector3D(0, 0, 0),
    BotRole = "COMBAT",
    DisplayName = "Guardian",
    CharacterSubtype = "Police_Bot",
    OwnerId = playerId
};

long botId = api.SpawnBot(spawnRequest);
```

## API Reference

### Core Methods

#### Bot Spawning
- `SpawnBot(BotSpawnRequest)` - Spawn a bot with detailed configuration
- `DespawnBot(long)` - Remove a bot from the world

#### Bot Control
- `SetBotTarget(long, long)` - Set a target entity for the bot
- `SetBotDestination(long, Vector3D)` - Move bot to specific coordinates
- `SetBotBehavior(long, string)` - Change bot behavior type
- `SetBotRole(long, string)` - Change bot role
- `ResetBotTargeting(long)` - Reset bot to autonomous behavior

#### Bot Information
- `GetBotInfo(long)` - Get detailed information about a specific bot
- `GetAllBots()` - Get information about all active bots
- `GetBotsByFaction(long)` - Get bots belonging to a faction
- `GetBotsByRole(string)` - Get bots with a specific role
- `GetBotsByOwner(long)` - Get bots owned by a player

#### Utility Methods
- `CanSpawn()` - Check if the system is ready to spawn bots
- `GetActiveBotCount()` - Get the current number of active bots
- `ExecuteCommand(long, string, params object[])` - Execute custom commands

### Data Classes

#### BotSpawnRequest
```csharp
public class BotSpawnRequest
{
    public Vector3D Position { get; set; }
    public string BotRole { get; set; }
    public string CharacterSubtype { get; set; }
    public long? FactionId { get; set; }
    public string DisplayName { get; set; }
    public Color? Color { get; set; }
    public bool UseAirNodes { get; set; } = true;
    public bool UseSpaceNodes { get; set; } = true;
    public Dictionary<string, object> CustomData { get; set; }
    public long? OwnerId { get; set; }
}
```

#### BotInfo
```csharp
public class BotInfo
{
    public long EntityId { get; set; }
    public string DisplayName { get; set; }
    public string Role { get; set; }
    public string CurrentBehavior { get; set; }
    public Vector3D Position { get; set; }
    public long? FactionId { get; set; }
    public bool IsAlive { get; set; }
    public float Health { get; set; }
    public long? OwnerId { get; set; }
    public long? TargetEntityId { get; set; }
    public Vector3D? OverrideDestination { get; set; }
    public bool IsFollowing { get; set; }
    public bool IsPatrolling { get; set; }
    public string CharacterSubtype { get; set; }
    public string CurrentState { get; set; }
}
```

### Events

Subscribe to bot lifecycle events:

```csharp
var api = AiEnabledApiImplementation.Instance;
api.BotSpawned += (botId) => MyAPIGateway.Utilities.ShowMessage("Event", $"Bot spawned: {botId}");
api.BotDespawned += (botId) => MyAPIGateway.Utilities.ShowMessage("Event", $"Bot despawned: {botId}");
api.BotStateChanged += (botId, state) => MyAPIGateway.Utilities.ShowMessage("Event", $"Bot {botId} state: {state}");
```

## Bot Roles

### Friendly Bots
- **REPAIR** - Repairs damaged blocks and structures
- **COMBAT** - Engages hostile targets
- **SCAVENGER** - Collects resources and items
- **CREW** - General purpose crew member

### Enemy Bots
- **SOLDIER** - Military combat bot
- **ZOMBIE** - Undead hostile bot
- **GRINDER** - Destructive grinding bot
- **GHOST** - Stealth combat bot
- **BRUISER** - Heavy assault bot
- **CREATURE** - Animal-like hostile bot

### Neutral Bots
- **NOMAD** - Wandering neutral bot
- **ENFORCER** - Law enforcement bot

## Character Subtypes

Default subtypes for each role:
- REPAIR → "Drone_Bot"
- COMBAT → "Target_Dummy"
- SCAVENGER → "RoboDog"
- CREW → "Default_Astronaut"
- SOLDIER → "Police_Bot"
- ZOMBIE → "Space_Zombie"
- GRINDER → "Space_Skeleton"
- GHOST → "Ghost_Bot"
- BRUISER → "Boss_Bot"
- CREATURE → "Wolf"

## Message Handler IDs

For remote API communication:
- **Request ID**: 2337 (AI-ENABLED-API-REQUEST)
- **Response ID**: 2338 (AI-ENABLED-API-RESPONSE)

## Error Handling

The API includes comprehensive error handling:

```csharp
// Always validate spawn requests
var request = new BotSpawnRequest { /* ... */ };
if (!request.IsValid(out string errorMessage))
{
    MyAPIGateway.Utilities.ShowMessage("Error", errorMessage);
    return;
}

// Check system readiness
if (!AiEnabledApiClient.CanSpawn())
{
    MyAPIGateway.Utilities.ShowMessage("Error", "AiEnabled is not ready");
    return;
}

// Handle spawn failures
long botId = AiEnabledApiClient.SpawnBot(request);
if (botId == 0)
{
    MyAPIGateway.Utilities.ShowMessage("Error", "Failed to spawn bot");
}
```

## Multiplayer Considerations

- Bot spawning only works on the server
- Client requests are automatically forwarded to the server
- Bot state is synchronized across all clients
- Events are fired on all clients when bots spawn/despawn

## Performance Tips

1. **Batch Operations**: Group multiple API calls when possible
2. **Async Methods**: Use async variants for non-blocking operations
3. **Event Cleanup**: Always unsubscribe from events to prevent memory leaks
4. **Bot Limits**: Respect server bot count limits
5. **Validation**: Validate inputs before making API calls

## Integration Examples

### Torch Plugin Integration

```csharp
[Plugin("MyPlugin", "1.0.0")]
public class MyTorchPlugin : TorchPluginBase
{
    [Command("spawnbot", "Spawns a bot")]
    public void SpawnBot(string role = "REPAIR")
    {
        if (!AiEnabledApiClient.CanSpawn())
        {
            Context.Respond("AiEnabled is not ready");
            return;
        }

        var position = new Vector3D(0, 0, 0); // Get from context
        long botId = AiEnabledApiClient.SpawnBot(position, role);
        
        Context.Respond(botId != 0 ? $"Spawned bot: {botId}" : "Failed to spawn bot");
    }
}
```

### Mod Integration

```csharp
public class MyMod : MySessionComponentBase
{
    public override void LoadData()
    {
        // API is automatically available
    }

    public override void UnloadData()
    {
        AiEnabledApiClient.Cleanup();
    }

    private void SpawnDefenseBots()
    {
        var basePosition = MyAPIGateway.Session.Player.GetPosition();
        
        for (int i = 0; i < 4; i++)
        {
            var angle = (Math.PI * 2 * i) / 4;
            var position = basePosition + Vector3D.CreateFromAzimuthAndElevation(angle, 0) * 50;
            
            AiEnabledApiClient.SpawnBot(position, "COMBAT", $"Guard-{i+1}");
        }
    }
}
```

## Troubleshooting

### Common Issues

**Q: Bots not spawning**  
A: Check if AiEnabled mod is loaded and `CanSpawn()` returns true. Verify bot count limits.

**Q: API not responding**  
A: Ensure proper message handler registration and mod load order. Check for exceptions in logs.

**Q: Bots disappearing**  
A: Check for faction conflicts, server cleanup scripts, or bot health issues.

**Q: Performance problems**  
A: Limit bot counts, use efficient update patterns, and avoid excessive API calls.

### Debug Information

```csharp
public void ShowDebugInfo()
{
    MyAPIGateway.Utilities.ShowMessage("API Debug", $"Can Spawn: {AiEnabledApiClient.CanSpawn()}");
    MyAPIGateway.Utilities.ShowMessage("API Debug", $"Bot Count: {AiEnabledApiClient.GetActiveBotCount()}");
    
    var bots = AiEnabledApiClient.GetAllBots();
    foreach (var bot in bots.Take(3))
    {
        MyAPIGateway.Utilities.ShowMessage("Bot", bot.ToString());
    }
}
```

## Version History

### v1.0.0 (Current)
- Initial API implementation
- Full bot spawning and control functionality
- Event system implementation
- Remote API client
- Comprehensive documentation

## Support

For issues, questions, or contributions:
1. Check the [Usage Examples](api-usage-examples.md) documentation
2. Review the troubleshooting section above
3. Check server logs for detailed error messages
4. Test in single-player first to isolate multiplayer issues

## License

This API is part of the AiEnabled mod and follows the same licensing terms.