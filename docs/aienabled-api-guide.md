# AiEnabled API Implementation Guide

## Overview
Instead of creating a separate Torch plugin, we'll branch AiEnabled and add API functionality directly to the mod. This approach gives you immediate access to all bot spawning and control mechanisms without the overhead of inter-mod communication.

## Step 1: Fork and Clone the Repository

1. Fork the AiEnabled repository from https://github.com/jturp/AiEnabled
2. Clone your fork locally:
```bash
git clone https://github.com/YOUR_USERNAME/AiEnabled.git
cd AiEnabled
```

## Step 2: Create the API Structure

Add a new folder `Api` in the project root with these files:

### Api/IAiEnabledApi.cs
```csharp
using Sandbox.ModAPI;
using VRageMath;
using System.Collections.Generic;

namespace AiEnabled.Api
{
    public interface IAiEnabledApi
    {
        // Bot spawning
        long SpawnBot(BotSpawnRequest request);
        bool DespawnBot(long botEntityId);
        
        // Bot control
        bool SetBotTarget(long botEntityId, long targetEntityId);
        bool SetBotDestination(long botEntityId, Vector3D destination);
        bool SetBotBehavior(long botEntityId, string behaviorType);
        bool SetBotRole(long botEntityId, string role);
        
        // Bot information
        BotInfo GetBotInfo(long botEntityId);
        List<BotInfo> GetAllBots();
        List<BotInfo> GetBotsByFaction(long factionId);
        
        // Events
        event Action<long> BotSpawned;
        event Action<long> BotDespawned;
        event Action<long, string> BotStateChanged;
    }
    
    public class BotSpawnRequest
    {
        public Vector3D Position { get; set; }
        public string BotRole { get; set; } // REPAIR, COMBAT, SCAVENGER, etc.
        public string CharacterSubtype { get; set; }
        public long? FactionId { get; set; }
        public string DisplayName { get; set; }
        public Color? Color { get; set; }
        public bool UseAirNodes { get; set; } = true;
        public bool UseSpaceNodes { get; set; } = true;
        public Dictionary<string, object> CustomData { get; set; }
    }
    
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
    }
}
```

### Api/AiEnabledApiImplementation.cs
```csharp
using AiEnabled.Bots;
using AiEnabled.Bots.Roles;
using AiEnabled.Bots.Behaviors;
using Sandbox.ModAPI;
using VRageMath;
using System.Collections.Generic;
using System.Linq;

namespace AiEnabled.Api
{
    public class AiEnabledApiImplementation : IAiEnabledApi
    {
        private static AiEnabledApiImplementation _instance;
        public static AiEnabledApiImplementation Instance => _instance ??= new AiEnabledApiImplementation();
        
        // Events
        public event Action<long> BotSpawned;
        public event Action<long> BotDespawned;
        public event Action<long, string> BotStateChanged;
        
        public long SpawnBot(BotSpawnRequest request)
        {
            try
            {
                // Get the appropriate factory based on role
                BotFactory factory = GetFactoryForRole(request.BotRole);
                
                // Create spawn data
                var spawnData = new BotSpawnData
                {
                    SpawnPosition = request.Position,
                    CharacterSubtype = request.CharacterSubtype ?? GetDefaultSubtypeForRole(request.BotRole),
                    DisplayName = request.DisplayName,
                    BotRole = request.BotRole,
                    FactionId = request.FactionId,
                    Color = request.Color,
                    UseAirNodes = request.UseAirNodes,
                    UseSpaceNodes = request.UseSpaceNodes
                };
                
                // Spawn the bot
                var bot = factory.SpawnBot(spawnData);
                
                if (bot != null)
                {
                    // Store custom data if provided
                    if (request.CustomData != null)
                    {
                        foreach (var kvp in request.CustomData)
                        {
                            bot.SetCustomData(kvp.Key, kvp.Value);
                        }
                    }
                    
                    BotSpawned?.Invoke(bot.Character.EntityId);
                    return bot.Character.EntityId;
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"AiEnabled API: Failed to spawn bot - {ex.Message}");
            }
            
            return 0;
        }
        
        public bool DespawnBot(long botEntityId)
        {
            var bot = AiSession.Instance.GetBotById(botEntityId);
            if (bot != null)
            {
                bot.Close();
                BotDespawned?.Invoke(botEntityId);
                return true;
            }
            return false;
        }
        
        public bool SetBotTarget(long botEntityId, long targetEntityId)
        {
            var bot = AiSession.Instance.GetBotById(botEntityId);
            if (bot != null)
            {
                var target = MyEntities.GetEntityById(targetEntityId);
                if (target != null)
                {
                    bot.SetTarget(target);
                    return true;
                }
            }
            return false;
        }
        
        public bool SetBotDestination(long botEntityId, Vector3D destination)
        {
            var bot = AiSession.Instance.GetBotById(botEntityId);
            if (bot != null)
            {
                bot.MoveTo(destination);
                return true;
            }
            return false;
        }
        
        public bool SetBotBehavior(long botEntityId, string behaviorType)
        {
            var bot = AiSession.Instance.GetBotById(botEntityId);
            if (bot != null)
            {
                var behavior = CreateBehavior(behaviorType);
                if (behavior != null)
                {
                    bot.SetBehavior(behavior);
                    BotStateChanged?.Invoke(botEntityId, behaviorType);
                    return true;
                }
            }
            return false;
        }
        
        public bool SetBotRole(long botEntityId, string role)
        {
            var bot = AiSession.Instance.GetBotById(botEntityId);
            if (bot != null && IsValidRole(role))
            {
                bot.ChangeRole(role);
                return true;
            }
            return false;
        }
        
        public BotInfo GetBotInfo(long botEntityId)
        {
            var bot = AiSession.Instance.GetBotById(botEntityId);
            if (bot != null)
            {
                return new BotInfo
                {
                    EntityId = bot.Character.EntityId,
                    DisplayName = bot.Character.DisplayName,
                    Role = bot.Role.ToString(),
                    CurrentBehavior = bot.CurrentBehavior?.GetType().Name ?? "None",
                    Position = bot.Character.GetPosition(),
                    FactionId = bot.Character.GetFactionId(),
                    IsAlive = !bot.Character.IsDead,
                    Health = bot.Character.StatComp?.Health?.Value ?? 0
                };
            }
            return null;
        }
        
        public List<BotInfo> GetAllBots()
        {
            return AiSession.Instance.GetAllBots()
                .Select(bot => GetBotInfo(bot.Character.EntityId))
                .Where(info => info != null)
                .ToList();
        }
        
        public List<BotInfo> GetBotsByFaction(long factionId)
        {
            return AiSession.Instance.GetAllBots()
                .Where(bot => bot.Character.GetFactionId() == factionId)
                .Select(bot => GetBotInfo(bot.Character.EntityId))
                .Where(info => info != null)
                .ToList();
        }
        
        // Helper methods
        private BotFactory GetFactoryForRole(string role)
        {
            // This would need to be implemented based on AiEnabled's structure
            // You might need to modify how factories work in AiEnabled
            return AiSession.Instance.BotFactory;
        }
        
        private string GetDefaultSubtypeForRole(string role)
        {
            switch (role.ToUpper())
            {
                case "REPAIR": return "Drone_Bot";
                case "COMBAT": return "Target_Dummy";
                case "SCAVENGER": return "RoboDog";
                case "CREW": return "Default_Astronaut";
                case "SOLDIER": return "Police_Bot";
                case "ZOMBIE": return "Space_Zombie";
                case "GRINDER": return "Space_Skeleton";
                default: return "Default_Astronaut";
            }
        }
        
        private bool IsValidRole(string role)
        {
            var validRoles = new[] { "REPAIR", "COMBAT", "SCAVENGER", "CREW", "SOLDIER", "ZOMBIE", "GRINDER", "GHOST", "BRUISER", "CREATURE", "NOMAD", "ENFORCER" };
            return validRoles.Contains(role.ToUpper());
        }
        
        private IBotBehavior CreateBehavior(string behaviorType)
        {
            // Implementation would depend on AiEnabled's behavior system
            return null;
        }
    }
}
```

## Step 3: Modify AiSession.cs

Add API initialization and helper methods to the main session component:

```csharp
// Add to AiSession class
private Dictionary<long, BotBase> _botRegistry = new Dictionary<long, BotBase>();

public void RegisterBot(BotBase bot)
{
    if (bot?.Character != null)
    {
        _botRegistry[bot.Character.EntityId] = bot;
    }
}

public void UnregisterBot(BotBase bot)
{
    if (bot?.Character != null)
    {
        _botRegistry.Remove(bot.Character.EntityId);
    }
}

public BotBase GetBotById(long entityId)
{
    return _botRegistry.TryGetValue(entityId, out var bot) ? bot : null;
}

public List<BotBase> GetAllBots()
{
    return _botRegistry.Values.ToList();
}

// In LoadData method, add:
MyAPIGateway.Utilities.RegisterMessageHandler(2337, HandleApiRequest);

// Add handler method:
private void HandleApiRequest(object obj)
{
    if (obj is MyTuple<string, object[]> request)
    {
        var (method, args) = request;
        var api = AiEnabledApiImplementation.Instance;
        
        // Handle API calls
        switch (method)
        {
            case "SpawnBot":
                if (args[0] is BotSpawnRequest spawnRequest)
                {
                    var result = api.SpawnBot(spawnRequest);
                    MyAPIGateway.Utilities.SendModMessage(2338, result);
                }
                break;
            // Add other cases...
        }
    }
}
```

## Step 4: Create External API Client

For other mods/plugins to use:

### AiEnabledApiClient.cs
```csharp
using Sandbox.ModAPI;
using System;

namespace AiEnabled.Api.Client
{
    public static class AiEnabledApiClient
    {
        private const long API_REQUEST_ID = 2337;
        private const long API_RESPONSE_ID = 2338;
        
        public static long SpawnBot(BotSpawnRequest request)
        {
            MyAPIGateway.Utilities.SendModMessage(API_REQUEST_ID, 
                MyTuple.Create("SpawnBot", new object[] { request }));
            
            // In a real implementation, you'd need to handle async responses
            return 0;
        }
        
        // Add other client methods...
    }
}
```

## Step 5: Usage Example

```csharp
// From your Torch plugin or another mod
var spawnRequest = new BotSpawnRequest
{
    Position = new Vector3D(0, 0, 0),
    BotRole = "COMBAT",
    CharacterSubtype = "Police_Bot",
    DisplayName = "Guardian Bot",
    FactionId = MySession.Static.Factions.TryGetPlayerFaction(playerId)?.FactionId
};

var botId = AiEnabledApiClient.SpawnBot(spawnRequest);

// Control the bot
AiEnabledApiClient.SetBotTarget(botId, enemyEntityId);
AiEnabledApiClient.SetBotDestination(botId, patrolPoint);
```

## Next Steps

1. Study the existing AiEnabled code structure to understand how bots are created and managed
2. Implement the API methods by hooking into existing bot systems
3. Add proper error handling and validation
4. Create documentation for API users
5. Submit a pull request to the original repository or maintain your own fork

## Benefits of This Approach

- Direct access to all bot systems
- No need for complex inter-mod communication
- Can expose any functionality you need
- Better performance than external plugin approach
- Easier to maintain and debug

## Alternative: Minimal Modification Approach

If you want to minimize changes to the original mod, you could:
1. Add just the API interface and implementation
2. Register it with ModAPI for other mods to access
3. Keep all existing functionality intact
4. This way, your changes could potentially be merged upstream