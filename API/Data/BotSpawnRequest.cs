using System.Collections.Generic;
using VRageMath;

namespace AiEnabled.Api.Data
{
    /// <summary>
    /// Request data for spawning a bot through the AiEnabled API
    /// </summary>
    public class BotSpawnRequest
    {
        /// <summary>
        /// World position where the bot should be spawned
        /// </summary>
        public Vector3D Position { get; set; }

        /// <summary>
        /// Bot role (REPAIR, COMBAT, SCAVENGER, CREW, SOLDIER, ZOMBIE, GRINDER, GHOST, BRUISER, CREATURE, NOMAD, ENFORCER)
        /// </summary>
        public string BotRole { get; set; }

        /// <summary>
        /// Character subtype to use for the bot (e.g., "Drone_Bot", "Police_Bot", etc.)
        /// If null, will use default for the role
        /// </summary>
        public string CharacterSubtype { get; set; }

        /// <summary>
        /// Faction ID for the bot (optional)
        /// </summary>
        public long? FactionId { get; set; }

        /// <summary>
        /// Display name for the bot
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Color for the bot (optional)
        /// </summary>
        public Color? Color { get; set; }

        /// <summary>
        /// Whether the bot can use air nodes for pathfinding
        /// </summary>
        public bool UseAirNodes { get; set; } = true;

        /// <summary>
        /// Whether the bot can use space nodes for pathfinding
        /// </summary>
        public bool UseSpaceNodes { get; set; } = true;

        /// <summary>
        /// Custom data dictionary for additional bot configuration
        /// </summary>
        public Dictionary<string, object> CustomData { get; set; }

        /// <summary>
        /// Owner's identity ID (for friendly bots)
        /// </summary>
        public long? OwnerId { get; set; }

        /// <summary>
        /// Validates the spawn request data
        /// </summary>
        /// <param name="errorMessage">Error message if validation fails</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid(out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(BotRole))
            {
                errorMessage = "BotRole cannot be null or empty";
                return false;
            }

            var validRoles = new[] { "REPAIR", "COMBAT", "SCAVENGER", "CREW", "SOLDIER", "ZOMBIE", "GRINDER", "GHOST", "BRUISER", "CREATURE", "NOMAD", "ENFORCER" };
            if (!System.Array.Exists(validRoles, role => role.Equals(BotRole, System.StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = $"Invalid BotRole '{BotRole}'. Valid roles: {string.Join(", ", validRoles)}";
                return false;
            }

            if (Position == Vector3D.Zero)
            {
                errorMessage = "Position cannot be zero vector";
                return false;
            }

            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                DisplayName = $"Bot_{BotRole}_{System.DateTime.Now.Ticks}";
            }

            return true;
        }
    }
}