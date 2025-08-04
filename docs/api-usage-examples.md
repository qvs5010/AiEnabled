# AiEnabled API Usage Examples

This document provides comprehensive examples of how to use the AiEnabled API in different scenarios.

## Table of Contents

1. [Local API Usage (Direct Access)](#local-api-usage-direct-access)
2. [Remote API Usage (External Mods)](#remote-api-usage-external-mods)
3. [Torch Plugin Usage](#torch-plugin-usage)
4. [Event Subscription Examples](#event-subscription-examples)
5. [Error Handling Patterns](#error-handling-patterns)
6. [Advanced Usage Scenarios](#advanced-usage-scenarios)

## Local API Usage (Direct Access)

Use this approach when your code is running within the AiEnabled mod itself or when you have direct access to the AiSession instance.

### Basic Bot Spawning

```csharp
using AiEnabled.Api;
using AiEnabled.Api.Data;
using VRageMath;

// Get the API instance
var api = AiEnabledApiImplementation.Instance;

// Create a spawn request
var spawnRequest = new BotSpawnRequest
{
    Position = new Vector3D(0, 0, 0),
    BotRole = "REPAIR",
    DisplayName = "RepairBot-001",
    CharacterSubtype = "Drone_Bot",
    OwnerId = MyAPIGateway.Session.Player?.IdentityId,
    UseAirNodes = true,
    UseSpaceNodes = true
};

// Spawn the bot
long botEntityId = api.SpawnBot(spawnRequest);
if (botEntityId != 0)
{
    MyAPIGateway.Utilities.ShowMessage("Success", $"Spawned bot with ID: {botEntityId}");
}
else
{
    MyAPIGateway.Utilities.ShowMessage("Error", "Failed to spawn bot");
}
```

### Bot Control and Management

```csharp
// Get bot information
var botInfo = api.GetBotInfo(botEntityId);
if (botInfo != null)
{
    MyAPIGateway.Utilities.ShowMessage("Bot Info", botInfo.ToString());
}

// Set bot destination
Vector3D destination = new Vector3D(100, 0, 100);
bool success = api.SetBotDestination(botEntityId, destination);

// Get all bots
var allBots = api.GetAllBots();
MyAPIGateway.Utilities.ShowMessage("Bot Count", $"Total active bots: {allBots.Count}");

// Get bots by role
var repairBots = api.GetBotsByRole("REPAIR");
MyAPIGateway.Utilities.ShowMessage("Repair Bots", $"Active repair bots: {repairBots.Count}");

// Despawn bot when done
api.DespawnBot(botEntityId);
```

## Remote API Usage (External Mods)

Use this approach when calling the AiEnabled API from external mods or plugins.

### Basic Setup in External Mod

```csharp
using AiEnabled.Api;
using AiEnabled.Api.Data;
using VRageMath;

public class MyExternalMod : MySessionComponentBase
{
    public override void LoadData()
    {
        // The AiEnabledApiClient automatically registers message handlers
        // No additional setup required
    }

    public override void UnloadData()
    {
        // Clean up message handlers
        AiEnabledApiClient.Cleanup();
    }

    private void SpawnMyBot()
    {
        // Check if AiEnabled is ready
        if (!AiEnabledApiClient.CanSpawn())
        {
            MyAPIGateway.Utilities.ShowMessage("Error", "AiEnabled is not ready to spawn bots");
            return;
        }

        // Simple bot spawning
        long botId = AiEnabledApiClient.SpawnBot(
            position: MyAPIGateway.Session.Player.GetPosition() + Vector3D.Forward * 10,
            role: "COMBAT",
            displayName: "Guardian Bot",
            ownerId: MyAPIGateway.Session.Player?.IdentityId
        );

        if (botId != 0)
        {
            MyAPIGateway.Utilities.ShowMessage("Success", $"Spawned guardian bot: {botId}");
            
            // Set the bot to follow the player
            var playerPosition = MyAPIGateway.Session.Player.GetPosition();
            AiEnabledApiClient.SetBotDestination(botId, playerPosition);
        }
    }
}
```

### Advanced Remote Usage

```csharp
private void AdvancedBotManagement()
{
    // Create detailed spawn request
    var spawnRequest = new BotSpawnRequest
    {
        Position = MyAPIGateway.Session.Player.GetPosition() + Vector3D.Up * 5,
        BotRole = "SCAVENGER",
        DisplayName = "Resource Collector",
        CharacterSubtype = "RoboDog",
        OwnerId = MyAPIGateway.Session.Player?.IdentityId,
        Color = Color.Blue,
        UseAirNodes = true,
        UseSpaceNodes = false,
        CustomData = new Dictionary<string, object>
        {
            { "Priority", "Resources" },
            { "SearchRadius", 1000.0 }
        }
    };

    long botId = AiEnabledApiClient.SpawnBot(spawnRequest);
    
    if (botId != 0)
    {
        // Get detailed bot information
        var botInfo = AiEnabledApiClient.GetBotInfo(botId);
        if (botInfo != null)
        {
            MyAPIGateway.Utilities.ShowMessage("Bot Status", 
                $"Bot: {botInfo.DisplayName}, Health: {botInfo.Health:F1}%, State: {botInfo.CurrentState}");
        }

        // Monitor all bots
        var allBots = AiEnabledApiClient.GetAllBots();
        foreach (var bot in allBots)
        {
            if (!bot.IsAlive)
            {
                MyAPIGateway.Utilities.ShowMessage("Alert", $"Bot {bot.DisplayName} is dead!");
            }
        }
    }
}
```

## Torch Plugin Usage

Example of using the AiEnabled API in a Torch plugin:

```csharp
using AiEnabled.Api;
using AiEnabled.Api.Data;
using Torch.API.Plugins;
using VRageMath;

[Plugin("MyTorchPlugin", "1.0.0")]
public class MyTorchPlugin : TorchPluginBase
{
    public override void Init()
    {
        // Plugin initialization
    }

    [Command("spawnguard", "Spawns a guard bot at the specified location")]
    public void SpawnGuardBot(Vector3D position, string playerName = null)
    {
        try
        {
            // Find player if specified
            long? ownerId = null;
            if (!string.IsNullOrEmpty(playerName))
            {
                var players = new HashSet<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);
                var player = players.FirstOrDefault(p => p.DisplayName.Equals(playerName, StringComparison.OrdinalIgnoreCase));
                ownerId = player?.IdentityId;
            }

            // Spawn guard bot
            var spawnRequest = new BotSpawnRequest
            {
                Position = position,
                BotRole = "COMBAT",
                DisplayName = $"Guard-{DateTime.Now:HHmmss}",
                CharacterSubtype = "Police_Bot",
                OwnerId = ownerId,
                Color = Color.Red
            };

            long botId = AiEnabledApiClient.SpawnBot(spawnRequest);
            
            if (botId != 0)
            {
                Context.Respond($"Successfully spawned guard bot with ID: {botId}");
            }
            else
            {
                Context.Respond("Failed to spawn guard bot. Check if AiEnabled is loaded and ready.");
            }
        }
        catch (Exception ex)
        {
            Context.Respond($"Error spawning guard bot: {ex.Message}");
        }
    }

    [Command("listbots", "Lists all active bots")]
    public void ListBots()
    {
        try
        {
            var bots = AiEnabledApiClient.GetAllBots();
            if (bots.Count == 0)
            {
                Context.Respond("No active bots found.");
                return;
            }

            Context.Respond($"Active Bots ({bots.Count}):");
            foreach (var bot in bots)
            {
                Context.Respond($"  {bot.EntityId}: {bot.DisplayName} ({bot.Role}) - {(bot.IsAlive ? "Alive" : "Dead")}");
            }
        }
        catch (Exception ex)
        {
            Context.Respond($"Error listing bots: {ex.Message}");
        }
    }
}
```

## Event Subscription Examples

Subscribe to bot lifecycle events for monitoring and automation:

```csharp
public class BotMonitor : MySessionComponentBase
{
    public override void LoadData()
    {
        // Subscribe to bot events
        var api = AiEnabledApiImplementation.Instance;
        api.BotSpawned += OnBotSpawned;
        api.BotDespawned += OnBotDespawned;
        api.BotStateChanged += OnBotStateChanged;
    }

    public override void UnloadData()
    {
        // Unsubscribe from events
        var api = AiEnabledApiImplementation.Instance;
        if (api != null)
        {
            api.BotSpawned -= OnBotSpawned;
            api.BotDespawned -= OnBotDespawned;
            api.BotStateChanged -= OnBotStateChanged;
        }
    }

    private void OnBotSpawned(long botEntityId)
    {
        var botInfo = AiEnabledApiImplementation.Instance.GetBotInfo(botEntityId);
        if (botInfo != null)
        {
            MyAPIGateway.Utilities.ShowMessage("Bot Monitor", 
                $"New bot spawned: {botInfo.DisplayName} ({botInfo.Role})");
            
            // Log to file or database
            LogBotEvent("SPAWNED", botInfo);
        }
    }

    private void OnBotDespawned(long botEntityId)
    {
        MyAPIGateway.Utilities.ShowMessage("Bot Monitor", 
            $"Bot despawned: {botEntityId}");
        
        // Clean up any tracking data
        CleanupBotData(botEntityId);
    }

    private void OnBotStateChanged(long botEntityId, string newState)
    {
        MyAPIGateway.Utilities.ShowMessage("Bot Monitor", 
            $"Bot {botEntityId} state changed: {newState}");
        
        // React to specific state changes
        if (newState.StartsWith("TargetSet:"))
        {
            // Bot has acquired a new target
            HandleBotTargetAcquired(botEntityId, newState);
        }
    }

    private void LogBotEvent(string eventType, BotInfo botInfo)
    {
        // Implementation for logging bot events
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {eventType}: {botInfo.DisplayName} ({botInfo.EntityId})";
        // Write to log file or send to monitoring system
    }

    private void CleanupBotData(long botEntityId)
    {
        // Clean up any tracking data for the despawned bot
    }

    private void HandleBotTargetAcquired(long botEntityId, string stateInfo)
    {
        // Extract target ID from state info and handle accordingly
        if (stateInfo.StartsWith("TargetSet:") && long.TryParse(stateInfo.Substring(10), out long targetId))
        {
            // Bot has targeted something - maybe alert other systems
            MyAPIGateway.Utilities.ShowMessage("Combat Alert", 
                $"Bot {botEntityId} is engaging target {targetId}");
        }
    }
}
```

## Error Handling Patterns

Proper error handling when using the API:

```csharp
public class SafeBotManager
{
    public bool SafeSpawnBot(Vector3D position, string role, string name, long? ownerId = null)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(role))
            {
                MyAPIGateway.Utilities.ShowMessage("Error", "Bot role cannot be empty");
                return false;
            }

            if (position == Vector3D.Zero)
            {
                MyAPIGateway.Utilities.ShowMessage("Error", "Invalid spawn position");
                return false;
            }

            // Check if API is available
            if (!AiEnabledApiClient.CanSpawn())
            {
                MyAPIGateway.Utilities.ShowMessage("Error", "AiEnabled API is not ready");
                return false;
            }

            // Create and validate spawn request
            var request = new BotSpawnRequest
            {
                Position = position,
                BotRole = role,
                DisplayName = name,
                OwnerId = ownerId
            };

            if (!request.IsValid(out string errorMessage))
            {
                MyAPIGateway.Utilities.ShowMessage("Error", $"Invalid spawn request: {errorMessage}");
                return false;
            }

            // Attempt to spawn
            long botId = AiEnabledApiClient.SpawnBot(request);
            if (botId == 0)
            {
                MyAPIGateway.Utilities.ShowMessage("Error", "Failed to spawn bot - check server logs");
                return false;
            }

            // Verify bot was created
            var botInfo = AiEnabledApiClient.GetBotInfo(botId);
            if (botInfo == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Warning", "Bot spawned but info not available");
                return true; // Still consider it a success
            }

            MyAPIGateway.Utilities.ShowMessage("Success", 
                $"Successfully spawned {botInfo.DisplayName} (ID: {botId})");
            return true;
        }
        catch (Exception ex)
        {
            MyAPIGateway.Utilities.ShowMessage("Exception", $"Error spawning bot: {ex.Message}");
            // Log full exception details
            MyAPIGateway.Utilities.ShowMessage("Debug", ex.StackTrace);
            return false;
        }
    }

    public void SafeBotCleanup()
    {
        try
        {
            var allBots = AiEnabledApiClient.GetAllBots();
            int removedCount = 0;

            foreach (var bot in allBots)
            {
                if (!bot.IsAlive || bot.Health <= 0)
                {
                    if (AiEnabledApiClient.DespawnBot(bot.EntityId))
                    {
                        removedCount++;
                    }
                }
            }

            if (removedCount > 0)
            {
                MyAPIGateway.Utilities.ShowMessage("Cleanup", $"Removed {removedCount} dead bots");
            }
        }
        catch (Exception ex)
        {
            MyAPIGateway.Utilities.ShowMessage("Error", $"Bot cleanup failed: {ex.Message}");
        }
    }
}
```

## Advanced Usage Scenarios

### Bot Squad Management

```csharp
public class BotSquad
{
    private List<long> _squadMembers = new List<long>();
    private Vector3D _rallyPoint;
    private string _squadName;

    public BotSquad(string name, Vector3D rallyPoint)
    {
        _squadName = name;
        _rallyPoint = rallyPoint;
    }

    public bool AddBot(string role, Vector3D spawnPosition, long? ownerId = null)
    {
        var request = new BotSpawnRequest
        {
            Position = spawnPosition,
            BotRole = role,
            DisplayName = $"{_squadName}-{role}-{_squadMembers.Count + 1}",
            OwnerId = ownerId,
            CustomData = new Dictionary<string, object>
            {
                { "Squad", _squadName },
                { "RallyPoint", _rallyPoint }
            }
        };

        long botId = AiEnabledApiClient.SpawnBot(request);
        if (botId != 0)
        {
            _squadMembers.Add(botId);
            return true;
        }
        return false;
    }

    public void MoveSquadTo(Vector3D destination)
    {
        _rallyPoint = destination;
        foreach (var botId in _squadMembers.ToList())
        {
            if (!AiEnabledApiClient.SetBotDestination(botId, destination))
            {
                // Bot might be dead, remove from squad
                _squadMembers.Remove(botId);
            }
        }
    }

    public void DisbandSquad()
    {
        foreach (var botId in _squadMembers)
        {
            AiEnabledApiClient.DespawnBot(botId);
        }
        _squadMembers.Clear();
    }

    public List<BotInfo> GetSquadStatus()
    {
        var status = new List<BotInfo>();
        foreach (var botId in _squadMembers.ToList())
        {
            var info = AiEnabledApiClient.GetBotInfo(botId);
            if (info != null)
            {
                status.Add(info);
            }
            else
            {
                // Bot no longer exists, remove from squad
                _squadMembers.Remove(botId);
            }
        }
        return status;
    }
}
```

### Automated Base Defense

```csharp
public class BaseDefenseSystem
{
    private Vector3D _baseCenter;
    private double _defenseRadius;
    private List<long> _guardBots = new List<long>();
    private int _maxGuards;

    public BaseDefenseSystem(Vector3D baseCenter, double radius, int maxGuards = 4)
    {
        _baseCenter = baseCenter;
        _defenseRadius = radius;
        _maxGuards = maxGuards;
    }

    public void UpdateDefenses()
    {
        // Remove dead guards
        _guardBots.RemoveAll(botId => 
        {
            var info = AiEnabledApiClient.GetBotInfo(botId);
            return info == null || !info.IsAlive;
        });

        // Spawn new guards if needed
        while (_guardBots.Count < _maxGuards && AiEnabledApiClient.CanSpawn())
        {
            SpawnGuardBot();
        }

        // Position guards around the base
        PositionGuards();
    }

    private void SpawnGuardBot()
    {
        var spawnPosition = _baseCenter + Vector3D.CreateFromAzimuthAndElevation(
            MyUtils.GetRandomDouble(0, Math.PI * 2), 0) * _defenseRadius * 0.8;

        var request = new BotSpawnRequest
        {
            Position = spawnPosition,
            BotRole = "COMBAT",
            DisplayName = $"BaseGuard-{_guardBots.Count + 1}",
            CharacterSubtype = "Police_Bot",
            Color = Color.Orange
        };

        long botId = AiEnabledApiClient.SpawnBot(request);
        if (botId != 0)
        {
            _guardBots.Add(botId);
        }
    }

    private void PositionGuards()
    {
        for (int i = 0; i < _guardBots.Count; i++)
        {
            var angle = (Math.PI * 2 * i) / _guardBots.Count;
            var position = _baseCenter + Vector3D.CreateFromAzimuthAndElevation(angle, 0) * _defenseRadius;
            
            AiEnabledApiClient.SetBotDestination(_guardBots[i], position);
        }
    }

    public void RespondToThreat(long threatEntityId)
    {
        // Direct all guards to engage the threat
        foreach (var botId in _guardBots)
        {
            AiEnabledApiClient.SetBotTarget(botId, threatEntityId);
        }
    }

    public void StandDown()
    {
        // Reset all guards to patrol mode
        foreach (var botId in _guardBots)
        {
            AiEnabledApiClient.ResetBotTargeting(botId);
        }
    }
}
```

## Best Practices

1. **Always check `CanSpawn()`** before attempting to spawn bots
2. **Validate input parameters** before making API calls
3. **Handle exceptions gracefully** and provide meaningful error messages
4. **Clean up resources** by unsubscribing from events and calling cleanup methods
5. **Use async methods** when possible to avoid blocking the game thread
6. **Monitor bot health** and clean up dead bots to prevent resource leaks
7. **Respect server limits** on bot counts and spawn rates
8. **Test thoroughly** in both single-player and multiplayer environments

## Troubleshooting

### Common Issues

1. **Bot not spawning**: Check if AiEnabled is loaded and `CanSpawn()` returns true
2. **API not responding**: Verify message handler registration and mod load order
3. **Bots disappearing**: Check for faction conflicts or server cleanup scripts
4. **Performance issues**: Limit bot counts and use efficient update patterns

### Debug Information

```csharp
public void DiagnosticInfo()
{
    MyAPIGateway.Utilities.ShowMessage("API Status", $"Can Spawn: {AiEnabledApiClient.CanSpawn()}");
    MyAPIGateway.Utilities.ShowMessage("Bot Count", $"Active Bots: {AiEnabledApiClient.GetActiveBotCount()}");
    
    var allBots = AiEnabledApiClient.GetAllBots();
    MyAPIGateway.Utilities.ShowMessage("Bot Details", $"Total: {allBots.Count}");
    
    foreach (var bot in allBots.Take(5)) // Show first 5 bots
    {
        MyAPIGateway.Utilities.ShowMessage("Bot", 
            $"{bot.DisplayName}: {bot.Role}, Health: {bot.Health:F1}%, State: {bot.CurrentState}");
    }
}