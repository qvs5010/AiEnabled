using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;
using AiEnabled.Api.Data;
using AiEnabled.Bots;
using AiEnabled.Bots.Roles;
using AiEnabled.Bots.Roles.Helpers;
using AiEnabled.Bots.Behaviors;
using AiEnabled.Utilities;

namespace AiEnabled.Api
{
    /// <summary>
    /// Main implementation of the AiEnabled API
    /// Provides comprehensive bot management functionality
    /// </summary>
    public class AiEnabledApiImplementation : IAiEnabledApi
    {
        private static AiEnabledApiImplementation _instance;
        public static AiEnabledApiImplementation Instance => _instance ??= new AiEnabledApiImplementation();

        #region Events

        public event Action<long> BotSpawned;
        public event Action<long> BotDespawned;
        public event Action<long, string> BotStateChanged;

        #endregion

        #region Bot Spawning

        public long SpawnBot(BotSpawnRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    AiSession.Instance?.Logger?.Log("SpawnBot: Request is null", MessageType.ERROR);
                    return 0;
                }

                if (!request.IsValid(out string errorMessage))
                {
                    AiSession.Instance?.Logger?.Log($"SpawnBot: Invalid request - {errorMessage}", MessageType.ERROR);
                    return 0;
                }

                // Check if we can spawn
                if (!CanSpawn())
                {
                    AiSession.Instance?.Logger?.Log("SpawnBot: Cannot spawn - system not ready or at capacity", MessageType.WARNING);
                    return 0;
                }

                // Only allow spawning on server
                if (!MyAPIGateway.Multiplayer.IsServer)
                {
                    AiSession.Instance?.Logger?.Log("SpawnBot: Bot spawning is only allowed on server", MessageType.WARNING);
                    return 0;
                }

                // Get default subtype if not specified
                string subtype = request.CharacterSubtype ?? GetDefaultSubtypeForRole(request.BotRole);

                // Create position and orientation
                var positionAndOrientation = new MyPositionAndOrientation(
                    request.Position,
                    Vector3.Forward,
                    Vector3.Up
                );

                // Determine if this is a friendly bot
                bool isFriendly = IsFriendlyRole(request.BotRole);
                long? ownerId = isFriendly ? request.OwnerId : null;

                IMyCharacter botCharacter = null;

                if (isFriendly)
                {
                    // Spawn friendly bot using existing system
                    botCharacter = BotFactory.SpawnHelper(
                        subtype,
                        request.DisplayName,
                        positionAndOrientation,
                        null, // grid
                        request.BotRole,
                        ownerId,
                        request.Color,
                        request.FactionId
                    );
                }
                else
                {
                    // Spawn enemy/neutral bot using existing system
                    botCharacter = BotFactory.SpawnNPC(
                        subtype,
                        request.DisplayName,
                        positionAndOrientation,
                        null, // grid
                        request.BotRole,
                        request.FactionId,
                        request.Color
                    );
                }

                if (botCharacter != null)
                {
                    // Apply custom data if provided
                    if (request.CustomData != null)
                    {
                        ApplyCustomData(botCharacter.EntityId, request.CustomData);
                    }

                    // Configure pathfinding options
                    var bot = AiSession.Instance.GetBotById(botCharacter.EntityId);
                    if (bot != null)
                    {
                        ConfigureBotPathfinding(bot, request);
                    }

                    // Fire event
                    BotSpawned?.Invoke(botCharacter.EntityId);

                    AiSession.Instance?.Logger?.Log($"SpawnBot: Successfully spawned {request.BotRole} bot '{request.DisplayName}' with ID {botCharacter.EntityId}", MessageType.INFO);
                    return botCharacter.EntityId;
                }
                else
                {
                    AiSession.Instance?.Logger?.Log($"SpawnBot: Failed to spawn {request.BotRole} bot '{request.DisplayName}'", MessageType.ERROR);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"SpawnBot: Exception occurred - {ex.Message}\n{ex.StackTrace}", MessageType.ERROR);
                return 0;
            }
        }

        public bool DespawnBot(long botEntityId)
        {
            try
            {
                var bot = AiSession.Instance?.GetBotById(botEntityId);
                if (bot == null)
                {
                    AiSession.Instance?.Logger?.Log($"DespawnBot: Bot with ID {botEntityId} not found", MessageType.WARNING);
                    return false;
                }

                // Fire event before despawning
                BotDespawned?.Invoke(botEntityId);

                // Close the bot
                bot.Close();

                AiSession.Instance?.Logger?.Log($"DespawnBot: Successfully despawned bot with ID {botEntityId}", MessageType.INFO);
                return true;
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"DespawnBot: Exception occurred - {ex.Message}", MessageType.ERROR);
                return false;
            }
        }

        #endregion

        #region Bot Control

        public bool SetBotTarget(long botEntityId, long targetEntityId)
        {
            try
            {
                var bot = AiSession.Instance?.GetBotById(botEntityId);
                if (bot == null)
                {
                    AiSession.Instance?.Logger?.Log($"SetBotTarget: Bot with ID {botEntityId} not found", MessageType.WARNING);
                    return false;
                }

                var target = MyEntities.GetEntityById(targetEntityId);
                if (target == null)
                {
                    AiSession.Instance?.Logger?.Log($"SetBotTarget: Target entity with ID {targetEntityId} not found", MessageType.WARNING);
                    return false;
                }

                bot.SetTarget(target);
                BotStateChanged?.Invoke(botEntityId, $"TargetSet:{targetEntityId}");

                AiSession.Instance?.Logger?.Log($"SetBotTarget: Set target {targetEntityId} for bot {botEntityId}", MessageType.INFO);
                return true;
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"SetBotTarget: Exception occurred - {ex.Message}", MessageType.ERROR);
                return false;
            }
        }

        public bool SetBotDestination(long botEntityId, Vector3D destination)
        {
            try
            {
                var bot = AiSession.Instance?.GetBotById(botEntityId);
                if (bot == null)
                {
                    AiSession.Instance?.Logger?.Log($"SetBotDestination: Bot with ID {botEntityId} not found", MessageType.WARNING);
                    return false;
                }

                bot.MoveTo(destination);
                BotStateChanged?.Invoke(botEntityId, $"DestinationSet:{destination}");

                AiSession.Instance?.Logger?.Log($"SetBotDestination: Set destination {destination} for bot {botEntityId}", MessageType.INFO);
                return true;
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"SetBotDestination: Exception occurred - {ex.Message}", MessageType.ERROR);
                return false;
            }
        }

        public bool SetBotBehavior(long botEntityId, string behaviorType)
        {
            try
            {
                var bot = AiSession.Instance?.GetBotById(botEntityId);
                if (bot == null)
                {
                    AiSession.Instance?.Logger?.Log($"SetBotBehavior: Bot with ID {botEntityId} not found", MessageType.WARNING);
                    return false;
                }

                var behavior = CreateBehavior(behaviorType, bot);
                if (behavior != null)
                {
                    bot.SetBehavior(behavior);
                    BotStateChanged?.Invoke(botEntityId, $"BehaviorChanged:{behaviorType}");

                    AiSession.Instance?.Logger?.Log($"SetBotBehavior: Set behavior {behaviorType} for bot {botEntityId}", MessageType.INFO);
                    return true;
                }
                else
                {
                    AiSession.Instance?.Logger?.Log($"SetBotBehavior: Invalid behavior type '{behaviorType}'", MessageType.WARNING);
                    return false;
                }
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"SetBotBehavior: Exception occurred - {ex.Message}", MessageType.ERROR);
                return false;
            }
        }

        public bool SetBotRole(long botEntityId, string role)
        {
            try
            {
                var bot = AiSession.Instance?.GetBotById(botEntityId);
                if (bot == null || !IsValidRole(role))
                {
                    AiSession.Instance?.Logger?.Log($"SetBotRole: Bot {botEntityId} not found or invalid role '{role}'", MessageType.WARNING);
                    return false;
                }

                bot.ChangeRole(role);
                BotStateChanged?.Invoke(botEntityId, $"RoleChanged:{role}");

                AiSession.Instance?.Logger?.Log($"SetBotRole: Changed role to {role} for bot {botEntityId}", MessageType.INFO);
                return true;
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"SetBotRole: Exception occurred - {ex.Message}", MessageType.ERROR);
                return false;
            }
        }

        public bool ResetBotTargeting(long botEntityId)
        {
            try
            {
                var bot = AiSession.Instance?.GetBotById(botEntityId);
                if (bot == null)
                {
                    AiSession.Instance?.Logger?.Log($"ResetBotTargeting: Bot with ID {botEntityId} not found", MessageType.WARNING);
                    return false;
                }

                BotFactory.ResetBotTargeting(bot);
                BotStateChanged?.Invoke(botEntityId, "TargetingReset");

                AiSession.Instance?.Logger?.Log($"ResetBotTargeting: Reset targeting for bot {botEntityId}", MessageType.INFO);
                return true;
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"ResetBotTargeting: Exception occurred - {ex.Message}", MessageType.ERROR);
                return false;
            }
        }

        #endregion

        #region Bot Information

        public BotInfo GetBotInfo(long botEntityId)
        {
            try
            {
                var bot = AiSession.Instance?.GetBotById(botEntityId);
                if (bot == null)
                {
                    return null;
                }

                return CreateBotInfo(bot);
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"GetBotInfo: Exception occurred - {ex.Message}", MessageType.ERROR);
                return null;
            }
        }

        public List<BotInfo> GetAllBots()
        {
            try
            {
                var result = new List<BotInfo>();
                var allBots = AiSession.Instance?.GetAllBots();
                
                if (allBots != null)
                {
                    foreach (var bot in allBots)
                    {
                        var botInfo = CreateBotInfo(bot);
                        if (botInfo != null)
                        {
                            result.Add(botInfo);
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"GetAllBots: Exception occurred - {ex.Message}", MessageType.ERROR);
                return new List<BotInfo>();
            }
        }

        public List<BotInfo> GetBotsByFaction(long factionId)
        {
            try
            {
                var result = new List<BotInfo>();
                var allBots = AiSession.Instance?.GetAllBots();
                
                if (allBots != null)
                {
                    foreach (var bot in allBots)
                    {
                        if (bot.Character?.GetFactionId() == factionId)
                        {
                            var botInfo = CreateBotInfo(bot);
                            if (botInfo != null)
                            {
                                result.Add(botInfo);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"GetBotsByFaction: Exception occurred - {ex.Message}", MessageType.ERROR);
                return new List<BotInfo>();
            }
        }

        public List<BotInfo> GetBotsByRole(string role)
        {
            try
            {
                var result = new List<BotInfo>();
                var allBots = AiSession.Instance?.GetAllBots();
                
                if (allBots != null)
                {
                    foreach (var bot in allBots)
                    {
                        if (bot.Role.ToString().Equals(role, StringComparison.OrdinalIgnoreCase))
                        {
                            var botInfo = CreateBotInfo(bot);
                            if (botInfo != null)
                            {
                                result.Add(botInfo);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"GetBotsByRole: Exception occurred - {ex.Message}", MessageType.ERROR);
                return new List<BotInfo>();
            }
        }

        public List<BotInfo> GetBotsByOwner(long ownerId)
        {
            try
            {
                var result = new List<BotInfo>();
                var allBots = AiSession.Instance?.GetAllBots();
                
                if (allBots != null)
                {
                    foreach (var bot in allBots)
                    {
                        if (bot.Owner?.IdentityId == ownerId)
                        {
                            var botInfo = CreateBotInfo(bot);
                            if (botInfo != null)
                            {
                                result.Add(botInfo);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"GetBotsByOwner: Exception occurred - {ex.Message}", MessageType.ERROR);
                return new List<BotInfo>();
            }
        }

        #endregion

        #region Bot Ownership

        public bool SetBotOwner(long botEntityId, long newOwnerId)
        {
            try
            {
                var bot = AiSession.Instance?.GetBotById(botEntityId);
                if (bot == null)
                {
                    AiSession.Instance?.Logger?.Log($"SetBotOwner: Bot with ID {botEntityId} not found", MessageType.WARNING);
                    return false;
                }

                var player = AiSession.Instance?.Players?.GetValueOrDefault(newOwnerId, null);
                bot.Owner = player;
                BotStateChanged?.Invoke(botEntityId, $"OwnerChanged:{newOwnerId}");

                AiSession.Instance?.Logger?.Log($"SetBotOwner: Changed owner to {newOwnerId} for bot {botEntityId}", MessageType.INFO);
                return true;
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"SetBotOwner: Exception occurred - {ex.Message}", MessageType.ERROR);
                return false;
            }
        }

        public bool SetBotFollowPlayer(long botEntityId, long playerId)
        {
            try
            {
                var bot = AiSession.Instance?.GetBotById(botEntityId);
                if (bot == null)
                {
                    AiSession.Instance?.Logger?.Log($"SetBotFollowPlayer: Bot with ID {botEntityId} not found", MessageType.WARNING);
                    return false;
                }

                var player = AiSession.Instance?.Players?.GetValueOrDefault(playerId, null);
                if (player == null)
                {
                    AiSession.Instance?.Logger?.Log($"SetBotFollowPlayer: Player with ID {playerId} not found", MessageType.WARNING);
                    return false;
                }

                bot.Owner = player;
                bot.FollowMode = true;
                BotStateChanged?.Invoke(botEntityId, $"FollowingPlayer:{playerId}");

                AiSession.Instance?.Logger?.Log($"SetBotFollowPlayer: Bot {botEntityId} now following player {playerId}", MessageType.INFO);
                return true;
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"SetBotFollowPlayer: Exception occurred - {ex.Message}", MessageType.ERROR);
                return false;
            }
        }

        #endregion

        #region Utility Methods

        public bool CanSpawn()
        {
            return AiSession.Instance?.CanSpawn ?? false;
        }

        public int GetActiveBotCount()
        {
            return AiSession.Instance?.BotNumber ?? 0;
        }

        public bool ExecuteCommand(long botEntityId, string command, params object[] args)
        {
            try
            {
                var bot = AiSession.Instance?.GetBotById(botEntityId);
                if (bot == null)
                {
                    AiSession.Instance?.Logger?.Log($"ExecuteCommand: Bot with ID {botEntityId} not found", MessageType.WARNING);
                    return false;
                }

                // Handle basic commands
                switch (command?.ToUpper())
                {
                    case "SPEAK":
                        if (args.Length > 0 && args[0] is string phrase)
                        {
                            bot.Behavior?.Speak(phrase);
                            return true;
                        }
                        break;

                    case "PERFORM":
                        if (args.Length > 0 && args[0] is string action)
                        {
                            bot.Behavior?.Perform(action);
                            return true;
                        }
                        break;

                    case "STOP":
                        ResetBotTargeting(botEntityId);
                        return true;

                    default:
                        AiSession.Instance?.Logger?.Log($"ExecuteCommand: Unknown command '{command}'", MessageType.WARNING);
                        return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"ExecuteCommand: Exception occurred - {ex.Message}", MessageType.ERROR);
                return false;
            }
        }

        #endregion

        #region Helper Methods

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
                case "GHOST": return "Ghost_Bot";
                case "BRUISER": return "Boss_Bot";
                case "CREATURE": return "Wolf";
                case "NOMAD": return "Default_Astronaut";
                case "ENFORCER": return "Police_Bot";
                default: return "Default_Astronaut";
            }
        }

        private bool IsFriendlyRole(string role)
        {
            var friendlyRoles = new[] { "REPAIR", "COMBAT", "SCAVENGER", "CREW" };
            return friendlyRoles.Contains(role.ToUpper());
        }

        private bool IsValidRole(string role)
        {
            var validRoles = new[] { "REPAIR", "COMBAT", "SCAVENGER", "CREW", "SOLDIER", "ZOMBIE", "GRINDER", "GHOST", "BRUISER", "CREATURE", "NOMAD", "ENFORCER" };
            return validRoles.Contains(role.ToUpper());
        }

        private void ApplyCustomData(long botEntityId, Dictionary<string, object> customData)
        {
            var bot = AiSession.Instance?.GetBotById(botEntityId);
            if (bot != null)
            {
                foreach (var kvp in customData)
                {
                    bot.SetCustomData(kvp.Key, kvp.Value);
                }
            }
        }

        private void ConfigureBotPathfinding(BotBase bot, BotSpawnRequest request)
        {
            // Configure pathfinding based on request settings
            // This would need to be implemented based on the bot's pathfinding system
        }

        private BotBehavior CreateBehavior(string behaviorType, BotBase bot)
        {
            switch (behaviorType?.ToUpper())
            {
                case "FRIENDLY":
                    return new FriendlyBehavior(bot);
                case "ENEMY":
                    return new EnemyBehavior(bot);
                case "NEUTRAL":
                    return new NeutralBehavior(bot);
                case "SCAVENGER":
                    return new ScavengerBehavior(bot);
                case "WORKER":
                    return new WorkerBehavior(bot);
                case "CREW":
                    return new CrewBehavior(bot);
                case "ZOMBIE":
                    return new ZombieBehavior(bot);
                case "CREATURE":
                    return new CreatureBehavior(bot);
                default:
                    return null;
            }
        }

        private BotInfo CreateBotInfo(BotBase bot)
        {
            if (bot?.Character == null)
                return null;

            return new BotInfo
            {
                EntityId = bot.Character.EntityId,
                DisplayName = bot.Character.DisplayName,
                Role = bot.Role.ToString(),
                CurrentBehavior = bot.CurrentBehavior?.GetType().Name ?? "None",
                Position = bot.Character.GetPosition(),
                FactionId = bot.Character.GetFactionId(),
                IsAlive = !bot.Character.IsDead,
                Health = bot.Character.StatComp?.Health?.Value ?? 0,
                OwnerId = bot.Owner?.IdentityId,
                TargetEntityId = bot.Target?.Entity?.EntityId,
                OverrideDestination = bot.Target?.Override,
                IsFollowing = bot.FollowMode,
                IsPatrolling = bot.PatrolMode,
                CharacterSubtype = bot.Character.Definition?.Id.SubtypeName,
                CurrentState = bot.BotState?.CurrentState.ToString() ?? "Unknown"
            };
        }

        #endregion
    }
}