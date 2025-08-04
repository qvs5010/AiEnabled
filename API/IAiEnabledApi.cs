using System;
using System.Collections.Generic;
using VRageMath;
using AiEnabled.Api.Data;

namespace AiEnabled.Api
{
    /// <summary>
    /// Main interface for the AiEnabled API
    /// Provides methods for spawning, controlling, and managing bots
    /// </summary>
    public interface IAiEnabledApi
    {
        #region Bot Spawning
        
        /// <summary>
        /// Spawns a bot with the specified parameters
        /// </summary>
        /// <param name="request">Bot spawn request containing all spawn parameters</param>
        /// <returns>Entity ID of the spawned bot, or 0 if spawn failed</returns>
        long SpawnBot(BotSpawnRequest request);

        /// <summary>
        /// Despawns (removes) a bot by its entity ID
        /// </summary>
        /// <param name="botEntityId">Entity ID of the bot to despawn</param>
        /// <returns>True if bot was successfully despawned, false otherwise</returns>
        bool DespawnBot(long botEntityId);

        #endregion

        #region Bot Control

        /// <summary>
        /// Sets a target for the bot to attack or interact with
        /// </summary>
        /// <param name="botEntityId">Entity ID of the bot</param>
        /// <param name="targetEntityId">Entity ID of the target</param>
        /// <returns>True if target was set successfully, false otherwise</returns>
        bool SetBotTarget(long botEntityId, long targetEntityId);

        /// <summary>
        /// Sets a destination for the bot to move to
        /// </summary>
        /// <param name="botEntityId">Entity ID of the bot</param>
        /// <param name="destination">World coordinates to move to</param>
        /// <returns>True if destination was set successfully, false otherwise</returns>
        bool SetBotDestination(long botEntityId, Vector3D destination);

        /// <summary>
        /// Changes the bot's behavior type
        /// </summary>
        /// <param name="botEntityId">Entity ID of the bot</param>
        /// <param name="behaviorType">New behavior type to set</param>
        /// <returns>True if behavior was changed successfully, false otherwise</returns>
        bool SetBotBehavior(long botEntityId, string behaviorType);

        /// <summary>
        /// Changes the bot's role
        /// </summary>
        /// <param name="botEntityId">Entity ID of the bot</param>
        /// <param name="role">New role to assign to the bot</param>
        /// <returns>True if role was changed successfully, false otherwise</returns>
        bool SetBotRole(long botEntityId, string role);

        /// <summary>
        /// Resets the bot's targeting and allows it to resume autonomous behavior
        /// </summary>
        /// <param name="botEntityId">Entity ID of the bot</param>
        /// <returns>True if targeting was reset successfully, false otherwise</returns>
        bool ResetBotTargeting(long botEntityId);

        #endregion

        #region Bot Information

        /// <summary>
        /// Gets detailed information about a specific bot
        /// </summary>
        /// <param name="botEntityId">Entity ID of the bot</param>
        /// <returns>BotInfo object with bot details, or null if bot not found</returns>
        BotInfo GetBotInfo(long botEntityId);

        /// <summary>
        /// Gets information about all active bots
        /// </summary>
        /// <returns>List of BotInfo objects for all active bots</returns>
        List<BotInfo> GetAllBots();

        /// <summary>
        /// Gets information about all bots belonging to a specific faction
        /// </summary>
        /// <param name="factionId">Faction ID to filter by</param>
        /// <returns>List of BotInfo objects for bots in the specified faction</returns>
        List<BotInfo> GetBotsByFaction(long factionId);

        /// <summary>
        /// Gets information about all bots with a specific role
        /// </summary>
        /// <param name="role">Role to filter by (REPAIR, COMBAT, etc.)</param>
        /// <returns>List of BotInfo objects for bots with the specified role</returns>
        List<BotInfo> GetBotsByRole(string role);

        /// <summary>
        /// Gets information about all bots owned by a specific player
        /// </summary>
        /// <param name="ownerId">Owner's identity ID</param>
        /// <returns>List of BotInfo objects for bots owned by the specified player</returns>
        List<BotInfo> GetBotsByOwner(long ownerId);

        #endregion

        #region Bot Ownership

        /// <summary>
        /// Changes the owner of a bot
        /// </summary>
        /// <param name="botEntityId">Entity ID of the bot</param>
        /// <param name="newOwnerId">Identity ID of the new owner</param>
        /// <returns>True if ownership was transferred successfully, false otherwise</returns>
        bool SetBotOwner(long botEntityId, long newOwnerId);

        /// <summary>
        /// Makes a bot follow a specific player
        /// </summary>
        /// <param name="botEntityId">Entity ID of the bot</param>
        /// <param name="playerId">Identity ID of the player to follow</param>
        /// <returns>True if follow mode was set successfully, false otherwise</returns>
        bool SetBotFollowPlayer(long botEntityId, long playerId);

        #endregion

        #region Utility Methods

        /// <summary>
        /// Checks if the API is ready to spawn bots
        /// </summary>
        /// <returns>True if bots can be spawned, false otherwise</returns>
        bool CanSpawn();

        /// <summary>
        /// Gets the current number of active bots
        /// </summary>
        /// <returns>Number of active bots</returns>
        int GetActiveBotCount();

        /// <summary>
        /// Executes a custom command on a bot (for extensibility)
        /// </summary>
        /// <param name="botEntityId">Entity ID of the bot</param>
        /// <param name="command">Command to execute</param>
        /// <param name="args">Command arguments</param>
        /// <returns>True if command was executed successfully, false otherwise</returns>
        bool ExecuteCommand(long botEntityId, string command, params object[] args);

        #endregion

        #region Events

        /// <summary>
        /// Event fired when a bot is successfully spawned
        /// </summary>
        event Action<long> BotSpawned;

        /// <summary>
        /// Event fired when a bot is despawned or dies
        /// </summary>
        event Action<long> BotDespawned;

        /// <summary>
        /// Event fired when a bot's state changes (behavior, target, etc.)
        /// </summary>
        event Action<long, string> BotStateChanged;

        #endregion
    }
}