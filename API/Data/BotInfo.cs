using VRageMath;

namespace AiEnabled.Api.Data
{
    /// <summary>
    /// Information about a bot managed by AiEnabled
    /// </summary>
    public class BotInfo
    {
        /// <summary>
        /// Entity ID of the bot character
        /// </summary>
        public long EntityId { get; set; }

        /// <summary>
        /// Display name of the bot
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Current role of the bot (REPAIR, COMBAT, etc.)
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Current behavior type of the bot
        /// </summary>
        public string CurrentBehavior { get; set; }

        /// <summary>
        /// Current world position of the bot
        /// </summary>
        public Vector3D Position { get; set; }

        /// <summary>
        /// Faction ID the bot belongs to (if any)
        /// </summary>
        public long? FactionId { get; set; }

        /// <summary>
        /// Whether the bot is alive
        /// </summary>
        public bool IsAlive { get; set; }

        /// <summary>
        /// Current health of the bot (0-100)
        /// </summary>
        public float Health { get; set; }

        /// <summary>
        /// Owner's identity ID (for friendly bots)
        /// </summary>
        public long? OwnerId { get; set; }

        /// <summary>
        /// Current target entity ID (if any)
        /// </summary>
        public long? TargetEntityId { get; set; }

        /// <summary>
        /// Current override destination (if any)
        /// </summary>
        public Vector3D? OverrideDestination { get; set; }

        /// <summary>
        /// Whether the bot is currently following a player
        /// </summary>
        public bool IsFollowing { get; set; }

        /// <summary>
        /// Whether the bot is in patrol mode
        /// </summary>
        public bool IsPatrolling { get; set; }

        /// <summary>
        /// Character subtype used for this bot
        /// </summary>
        public string CharacterSubtype { get; set; }

        /// <summary>
        /// Bot's current state (Idle, Moving, Fighting, etc.)
        /// </summary>
        public string CurrentState { get; set; }

        /// <summary>
        /// Creates a string representation of the bot info
        /// </summary>
        /// <returns>Formatted bot information</returns>
        public override string ToString()
        {
            return $"Bot[{EntityId}]: {DisplayName} ({Role}) - {(IsAlive ? "Alive" : "Dead")} - Health: {Health:F1}% - State: {CurrentState ?? "Unknown"}";
        }
    }
}