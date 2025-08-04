using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using AiEnabled.Api.Data;
using AiEnabled.Bots;
using AiEnabled.Utilities;

namespace AiEnabled.Api
{
    /// <summary>
    /// Compatibility layer that extends the new AiEnabled API to support legacy LocalBotAPI method signatures
    /// This ensures backward compatibility with existing mods that use the old API
    /// </summary>
    public static class AiEnabledApiCompatibility
    {
        private static readonly AiEnabledApiImplementation _api = AiEnabledApiImplementation.Instance;

        #region Legacy SpawnBot Methods

        /// <summary>
        /// Legacy SpawnBot method for backward compatibility
        /// </summary>
        public static IMyCharacter SpawnBot(string subType, string displayName, MyPositionAndOrientation positionAndOrientation, MyCubeGrid grid = null, string role = null, long? owner = null, Color? color = null)
        {
            try
            {
                var request = new BotSpawnRequest
                {
                    Position = positionAndOrientation.Position,
                    BotRole = role ?? GetDefaultRoleForSubtype(subType),
                    CharacterSubtype = subType,
                    DisplayName = displayName,
                    OwnerId = owner,
                    Color = color
                };

                long botId = _api.SpawnBot(request);
                if (botId != 0)
                {
                    var bot = AiSession.Instance?.GetBotById(botId);
                    return bot?.Character;
                }
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"Legacy SpawnBot failed: {ex.Message}", MessageType.ERROR);
            }

            return null;
        }

        /// <summary>
        /// Legacy SpawnBot with custom data for backward compatibility
        /// </summary>
        public static IMyCharacter SpawnBot(MyPositionAndOrientation positionAndOrientation, byte[] spawnData, MyCubeGrid grid = null, long? owner = null)
        {
            try
            {
                // For now, create a basic request - in a full implementation you'd deserialize the spawnData
                var request = new BotSpawnRequest
                {
                    Position = positionAndOrientation.Position,
                    BotRole = "REPAIR", // Default role
                    DisplayName = "LegacyBot",
                    OwnerId = owner
                };

                long botId = _api.SpawnBot(request);
                if (botId != 0)
                {
                    var bot = AiSession.Instance?.GetBotById(botId);
                    return bot?.Character;
                }
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"Legacy SpawnBot (custom) failed: {ex.Message}", MessageType.ERROR);
            }

            return null;
        }

        #endregion

        #region Legacy Bot Control Methods

        /// <summary>
        /// Legacy SetBotTarget method - supports object parameter for backward compatibility
        /// </summary>
        public static bool SetBotTarget(long botEntityId, object target)
        {
            try
            {
                if (target is IMyEntity entity)
                {
                    return _api.SetBotTarget(botEntityId, entity.EntityId);
                }
                else if (target is long targetId)
                {
                    return _api.SetBotTarget(botEntityId, targetId);
                }
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"Legacy SetBotTarget failed: {ex.Message}", MessageType.ERROR);
            }

            return false;
        }

        /// <summary>
        /// Legacy GetBotOverride method
        /// </summary>
        public static Vector3D? GetBotOverride(long botEntityId)
        {
            try
            {
                var botInfo = _api.GetBotInfo(botEntityId);
                return botInfo?.OverrideDestination;
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"Legacy GetBotOverride failed: {ex.Message}", MessageType.ERROR);
                return null;
            }
        }

        /// <summary>
        /// Legacy SetBotOverride method
        /// </summary>
        public static bool SetBotOverride(long botEntityId, Vector3D goTo)
        {
            return _api.SetBotDestination(botEntityId, goTo);
        }

        /// <summary>
        /// Legacy ResetBotTargeting method
        /// </summary>
        public static bool ResetBotTargeting(long botEntityId)
        {
            return _api.ResetBotTargeting(botEntityId);
        }

        /// <summary>
        /// Legacy CloseBot method (same as DespawnBot)
        /// </summary>
        public static bool CloseBot(long botEntityId)
        {
            return _api.DespawnBot(botEntityId);
        }

        #endregion

        #region Legacy Information Methods

        /// <summary>
        /// Legacy IsBot method
        /// </summary>
        public static bool IsBot(long id)
        {
            try
            {
                var botInfo = _api.GetBotInfo(id);
                return botInfo != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Legacy GetBots method - fills provided dictionary
        /// </summary>
        public static void GetBots(Dictionary<long, IMyCharacter> botDict, bool includeFriendly = true, bool includeEnemy = true, bool includeNeutral = true)
        {
            try
            {
                if (botDict == null)
                    return;

                botDict.Clear();
                var allBots = _api.GetAllBots();

                foreach (var botInfo in allBots)
                {
                    bool include = false;
                    
                    // Determine if we should include this bot based on its role
                    var role = botInfo.Role?.ToUpper();
                    if (includeFriendly && IsFriendlyRole(role))
                        include = true;
                    else if (includeEnemy && IsEnemyRole(role))
                        include = true;
                    else if (includeNeutral && IsNeutralRole(role))
                        include = true;

                    if (include)
                    {
                        var bot = AiSession.Instance?.GetBotById(botInfo.EntityId);
                        if (bot?.Character != null)
                        {
                            botDict[botInfo.EntityId] = bot.Character;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"Legacy GetBots failed: {ex.Message}", MessageType.ERROR);
            }
        }

        /// <summary>
        /// Legacy CanSpawn method
        /// </summary>
        public static bool CanSpawn()
        {
            return _api.CanSpawn();
        }

        /// <summary>
        /// Legacy GetBotOwnerId method
        /// </summary>
        public static long GetBotOwnerId(long botEntityId)
        {
            try
            {
                var botInfo = _api.GetBotInfo(botEntityId);
                return botInfo?.OwnerId ?? 0L;
            }
            catch
            {
                return 0L;
            }
        }

        #endregion

        #region Legacy Action Methods

        /// <summary>
        /// Legacy Speak method
        /// </summary>
        public static void Speak(long botEntityId, string phrase = null)
        {
            _api.ExecuteCommand(botEntityId, "SPEAK", phrase);
        }

        /// <summary>
        /// Legacy Perform method
        /// </summary>
        public static void Perform(long botEntityId, string action = null)
        {
            _api.ExecuteCommand(botEntityId, "PERFORM", action);
        }

        /// <summary>
        /// Legacy AssignToPlayer method
        /// </summary>
        public static bool AssignToPlayer(long botEntityId, long playerIdentityId)
        {
            return _api.SetBotOwner(botEntityId, playerIdentityId);
        }

        /// <summary>
        /// Legacy FollowPlayer method
        /// </summary>
        public static bool FollowPlayer(long botEntityId, long playerIdentityId)
        {
            return _api.SetBotFollowPlayer(botEntityId, playerIdentityId);
        }

        #endregion

        #region Legacy Role and Subtype Methods

        /// <summary>
        /// Legacy GetFriendlyBotRoles method
        /// </summary>
        public static string[] GetFriendlyBotRoles()
        {
            return new[] { "REPAIR", "COMBAT", "SCAVENGER", "CREW" };
        }

        /// <summary>
        /// Legacy GetNPCBotRoles method
        /// </summary>
        public static string[] GetNPCBotRoles()
        {
            return new[] { "SOLDIER", "ZOMBIE", "GRINDER", "GHOST", "BRUISER", "CREATURE" };
        }

        /// <summary>
        /// Legacy GetNeutralBotRoles method
        /// </summary>
        public static string[] GetNeutralBotRoles()
        {
            return new[] { "NOMAD", "ENFORCER" };
        }

        /// <summary>
        /// Legacy GetBotSubtypes method
        /// </summary>
        public static string[] GetBotSubtypes()
        {
            return AiSession.Instance?.RobotSubtypes?.ToArray() ?? new string[0];
        }

        #endregion

        #region Helper Methods

        private static string GetDefaultRoleForSubtype(string subtype)
        {
            switch (subtype)
            {
                case "Drone_Bot": return "REPAIR";
                case "RoboDog": return "SCAVENGER";
                case "Default_Astronaut":
                case "Default_Astronaut_Female": return "CREW";
                case "Police_Bot": return "SOLDIER";
                case "Space_Zombie": return "ZOMBIE";
                case "Space_Skeleton": return "GRINDER";
                case "Ghost_Bot": return "GHOST";
                case "Boss_Bot": return "BRUISER";
                case "Wolf": return "CREATURE";
                default: return "COMBAT";
            }
        }

        private static bool IsFriendlyRole(string role)
        {
            var friendlyRoles = new[] { "REPAIR", "COMBAT", "SCAVENGER", "CREW" };
            return friendlyRoles.Contains(role);
        }

        private static bool IsEnemyRole(string role)
        {
            var enemyRoles = new[] { "SOLDIER", "ZOMBIE", "GRINDER", "GHOST", "BRUISER", "CREATURE" };
            return enemyRoles.Contains(role);
        }

        private static bool IsNeutralRole(string role)
        {
            var neutralRoles = new[] { "NOMAD", "ENFORCER" };
            return neutralRoles.Contains(role);
        }

        #endregion

        #region Extended Compatibility Methods

        /// <summary>
        /// Legacy SetBotPatrol method with world coordinates
        /// </summary>
        public static bool SetBotPatrol(long botEntityId, List<Vector3D> waypoints)
        {
            try
            {
                if (waypoints?.Count > 0)
                {
                    // For now, just set destination to first waypoint
                    // Full patrol implementation would require extending the API
                    return _api.SetBotDestination(botEntityId, waypoints[0]);
                }
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"Legacy SetBotPatrol failed: {ex.Message}", MessageType.ERROR);
            }
            return false;
        }

        /// <summary>
        /// Legacy SetBotPatrol method with local coordinates
        /// </summary>
        public static bool SetBotPatrol(long botEntityId, List<Vector3I> waypoints)
        {
            try
            {
                if (waypoints?.Count > 0)
                {
                    // Convert local to world coordinates (simplified)
                    var worldPos = new Vector3D(waypoints[0].X, waypoints[0].Y, waypoints[0].Z);
                    return _api.SetBotDestination(botEntityId, worldPos);
                }
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"Legacy SetBotPatrol (local) failed: {ex.Message}", MessageType.ERROR);
            }
            return false;
        }

        /// <summary>
        /// Legacy SwitchBotRole method
        /// </summary>
        public static bool SwitchBotRole(long botEntityId, string newRole, List<string> toolSubtypes = null)
        {
            return _api.SetBotRole(botEntityId, newRole);
        }

        /// <summary>
        /// Legacy relationship checking method
        /// </summary>
        public static MyRelationsBetweenPlayerAndBlock GetRelationshipBetween(long botEntityId, long otherIdentityId)
        {
            try
            {
                var bot = AiSession.Instance?.GetBotById(botEntityId);
                if (bot?.Character != null)
                {
                    // Use existing relationship logic from LocalBotAPI
                    if (otherIdentityId == bot.Owner?.IdentityId)
                        return MyRelationsBetweenPlayerAndBlock.Friends;

                    var botOwnerId = bot.BotIdentityId;
                    var shareMode = bot.Owner != null ? MyOwnershipShareModeEnum.Faction : MyOwnershipShareModeEnum.None;
                    return MyIDModule.GetRelationPlayerBlock(botOwnerId, otherIdentityId, shareMode);
                }
            }
            catch (Exception ex)
            {
                AiSession.Instance?.Logger?.Log($"Legacy GetRelationshipBetween failed: {ex.Message}", MessageType.ERROR);
            }

            return MyRelationsBetweenPlayerAndBlock.NoOwnership;
        }

        #endregion
    }
}