using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.ModAPI;
using VRage;
using AiEnabled.Api.Data;

namespace AiEnabled.Api
{
    /// <summary>
    /// Client for external mods to communicate with AiEnabled API via ModAPI messaging
    /// Use this class in your external mods or Torch plugins to control AiEnabled bots
    /// </summary>
    public static class AiEnabledApiClient
    {
        private const long API_REQUEST_ID = 2337;
        private const long API_RESPONSE_ID = 2338;
        
        private static object _lastResponse = null;
        private static bool _responseReceived = false;
        private static readonly object _responseLock = new object();

        static AiEnabledApiClient()
        {
            // Register response handler
            MyAPIGateway.Utilities.RegisterMessageHandler(API_RESPONSE_ID, HandleApiResponse);
        }

        #region Bot Spawning

        /// <summary>
        /// Spawns a bot with the specified parameters
        /// </summary>
        /// <param name="request">Bot spawn request containing all spawn parameters</param>
        /// <returns>Entity ID of the spawned bot, or 0 if spawn failed</returns>
        public static long SpawnBot(BotSpawnRequest request)
        {
            if (request == null)
                return 0;

            SendRequest("SpawnBot", request);
            return WaitForResponse<long>();
        }

        /// <summary>
        /// Spawns a bot with simplified parameters
        /// </summary>
        /// <param name="position">World position to spawn the bot</param>
        /// <param name="role">Bot role (REPAIR, COMBAT, SCAVENGER, etc.)</param>
        /// <param name="displayName">Display name for the bot</param>
        /// <param name="ownerId">Owner's identity ID (optional)</param>
        /// <returns>Entity ID of the spawned bot, or 0 if spawn failed</returns>
        public static long SpawnBot(Vector3D position, string role, string displayName = null, long? ownerId = null)
        {
            var request = new BotSpawnRequest
            {
                Position = position,
                BotRole = role,
                DisplayName = displayName ?? $"Bot_{role}_{DateTime.Now.Ticks}",
                OwnerId = ownerId
            };

            return SpawnBot(request);
        }

        /// <summary>
        /// Despawns (removes) a bot by its entity ID
        /// </summary>
        /// <param name="botEntityId">Entity ID of the bot to despawn</param>
        /// <returns>True if bot was successfully despawned, false otherwise</returns>
        public static bool DespawnBot(long botEntityId)
        {
            SendRequest("DespawnBot", botEntityId);
            return WaitForResponse<bool>();
        }

        #endregion

        #region Bot Control

        /// <summary>
        /// Sets a target for the bot to attack or interact with
        /// </summary>
        /// <param name="botEntityId">Entity ID of the bot</param>
        /// <param name="targetEntityId">Entity ID of the target</param>
        /// <returns>True if target was set successfully, false otherwise</returns>
        public static bool SetBotTarget(long botEntityId, long targetEntityId)
        {
            SendRequest("SetBotTarget", botEntityId, targetEntityId);
            return WaitForResponse<bool>();
        }

        /// <summary>
        /// Sets a destination for the bot to move to
        /// </summary>
        /// <param name="botEntityId">Entity ID of the bot</param>
        /// <param name="destination">World coordinates to move to</param>
        /// <returns>True if destination was set successfully, false otherwise</returns>
        public static bool SetBotDestination(long botEntityId, Vector3D destination)
        {
            SendRequest("SetBotDestination", botEntityId, destination);
            return WaitForResponse<bool>();
        }

        /// <summary>
        /// Resets the bot's targeting and allows it to resume autonomous behavior
        /// </summary>
        /// <param name="botEntityId">Entity ID of the bot</param>
        /// <returns>True if targeting was reset successfully, false otherwise</returns>
        public static bool ResetBotTargeting(long botEntityId)
        {
            SendRequest("ResetBotTargeting", botEntityId);
            return WaitForResponse<bool>();
        }

        #endregion

        #region Bot Information

        /// <summary>
        /// Gets detailed information about a specific bot
        /// </summary>
        /// <param name="botEntityId">Entity ID of the bot</param>
        /// <returns>BotInfo object with bot details, or null if bot not found</returns>
        public static BotInfo GetBotInfo(long botEntityId)
        {
            SendRequest("GetBotInfo", botEntityId);
            return WaitForResponse<BotInfo>();
        }

        /// <summary>
        /// Gets information about all active bots
        /// </summary>
        /// <returns>List of BotInfo objects for all active bots</returns>
        public static List<BotInfo> GetAllBots()
        {
            SendRequest("GetAllBots");
            return WaitForResponse<List<BotInfo>>() ?? new List<BotInfo>();
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Checks if the API is ready to spawn bots
        /// </summary>
        /// <returns>True if bots can be spawned, false otherwise</returns>
        public static bool CanSpawn()
        {
            SendRequest("CanSpawn");
            return WaitForResponse<bool>();
        }

        /// <summary>
        /// Gets the current number of active bots
        /// </summary>
        /// <returns>Number of active bots</returns>
        public static int GetActiveBotCount()
        {
            SendRequest("GetActiveBotCount");
            return WaitForResponse<int>();
        }

        #endregion

        #region Async Methods (for better performance)

        /// <summary>
        /// Spawns a bot asynchronously without waiting for response
        /// </summary>
        /// <param name="request">Bot spawn request</param>
        /// <param name="callback">Callback to invoke when spawn completes</param>
        public static void SpawnBotAsync(BotSpawnRequest request, Action<long> callback = null)
        {
            if (request == null)
            {
                callback?.Invoke(0);
                return;
            }

            SendRequestAsync("SpawnBot", callback, request);
        }

        /// <summary>
        /// Gets all bots asynchronously without blocking
        /// </summary>
        /// <param name="callback">Callback to invoke with bot list</param>
        public static void GetAllBotsAsync(Action<List<BotInfo>> callback)
        {
            SendRequestAsync("GetAllBots", callback);
        }

        #endregion

        #region Private Helper Methods

        private static void SendRequest(string method, params object[] args)
        {
            lock (_responseLock)
            {
                _responseReceived = false;
                _lastResponse = null;
            }

            var request = MyTuple.Create(method, args);
            MyAPIGateway.Utilities.SendModMessage(API_REQUEST_ID, request);
        }

        private static void SendRequestAsync<T>(string method, Action<T> callback, params object[] args)
        {
            var request = MyTuple.Create(method, args);
            MyAPIGateway.Utilities.SendModMessage(API_REQUEST_ID, request);

            // For async, we would need to store the callback and handle it in the response handler
            // This is a simplified version - in a real implementation you'd want to track multiple pending requests
            if (callback != null)
            {
                MyAPIGateway.Parallel.Start(() =>
                {
                    var response = WaitForResponse<T>();
                    MyAPIGateway.Utilities.InvokeOnGameThread(() => callback(response));
                });
            }
        }

        private static T WaitForResponse<T>()
        {
            const int maxWaitMs = 5000; // 5 second timeout
            const int checkIntervalMs = 10;
            int waitedMs = 0;

            while (waitedMs < maxWaitMs)
            {
                lock (_responseLock)
                {
                    if (_responseReceived)
                    {
                        if (_lastResponse is T response)
                        {
                            return response;
                        }
                        else if (_lastResponse == null)
                        {
                            return default(T);
                        }
                        else
                        {
                            // Try to convert the response
                            try
                            {
                                return (T)_lastResponse;
                            }
                            catch
                            {
                                return default(T);
                            }
                        }
                    }
                }

                System.Threading.Thread.Sleep(checkIntervalMs);
                waitedMs += checkIntervalMs;
            }

            // Timeout
            return default(T);
        }

        private static void HandleApiResponse(object obj)
        {
            lock (_responseLock)
            {
                _lastResponse = obj;
                _responseReceived = true;
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Call this when your mod unloads to clean up the message handler
        /// </summary>
        public static void Cleanup()
        {
            try
            {
                MyAPIGateway.Utilities.UnregisterMessageHandler(API_RESPONSE_ID, HandleApiResponse);
            }
            catch (Exception ex)
            {
                // Log error if possible, but don't throw during cleanup
                MyAPIGateway.Utilities.ShowMessage("AiEnabledApiClient", $"Error during cleanup: {ex.Message}");
            }
        }

        #endregion
    }
}