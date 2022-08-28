using System;
using System.Timers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using RestSharp;
using Newtonsoft.Json;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
namespace invision_whitelist
{
    public class Main : BaseScript
    {
        string communityURL;
        string apiKey;
        List<string> groupIds = new List<string>();
        HashSet<string> whitelistedHexes { get; set; }
        public Main()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            EventHandlers["onResourceStart"] += new Action<string>(handleResourceStart); 
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(handleConnection);
            RegisterCommand("updatewhitelist", new Action<int>((source) =>
            {
                if(source == 0) {
                    Debug.WriteLine("[Invision Whitelist] Updating whitelist...");
                }
                updateWhitelistedHexes();
            }), true);

            loadConfig();
            if(communityURL is null || apiKey is null || groupIds.Count == 0)
            {
                Debug.WriteLine("^1No config variables set! Whitelist won't operate.^0");
                throw new Exception("NO_CONFIG");
            }

            Timer updateTimer = new Timer(TimeSpan.FromHours(4).TotalMilliseconds);
            updateTimer.AutoReset = true;
            updateTimer.Elapsed += onTimerTimeout;
            updateTimer.Start();
            
        }

        private void loadConfig()
        {
            string rawFileContent = LoadResourceFile(GetCurrentResourceName(), "config.json");
            if (rawFileContent is null) return;
            WhitelistConfig config = JsonConvert.DeserializeObject<WhitelistConfig>(rawFileContent);
            if (config.allowedGroupIds is null || config.apiToken is null || config.apiBaseURL is null) return;
            communityURL = config.apiBaseURL;
            apiKey = config.apiToken;
            foreach (string groupId in config.allowedGroupIds)
            {
                groupIds.Add(groupId);
            }
            Debug.WriteLine("^4Successfully loaded config.^0");
            return;
        }
        
        private async void updateWhitelistedHexes()
        {
            HashSet<ApiUser> apiUsers = new HashSet<ApiUser>();
            List<string> steamHexes = new List<string>();
            for (int i = 0; i < groupIds.Count; i++)
            {
                // Makes a request to the Invision API for the first 500 users. We also use this to get the total number of users in the group and the ammount of pages.
                RestClient restClient = new RestClient(communityURL);
                RestRequest restRequest = (RestRequest)new RestRequest($"core/members?group={groupIds[i]}&perPage=500&key={apiKey}", Method.GET, DataFormat.Json).AddHeader("User-Agent", "WorldwideRP/1.0");
                RestResponse restResponse = (RestResponse)await restClient.ExecuteAsync(restRequest);

                if(restResponse.StatusCode == (HttpStatusCode)200)
                {
                    // Convert the response to a usable object so we can read from it
                    var formattedResponse = JsonConvert.DeserializeObject<ApiResponse>(restResponse.Content);
                    foreach (var apiUser in formattedResponse.results)
                    { 
                        // Add the user to the list of users to be whitelisted
                        apiUsers.Add(apiUser);
                    }
                    for (int z = 2; z <= formattedResponse.totalPages; z ++)
                    {
                        // Nested request to get the rest of the users in the group
                        RestRequest InternalRestRequest = (RestRequest)new RestRequest($"core/members?group={groupIds[i]}&perPage=500&page={z}&key={apiKey}", Method.GET, DataFormat.Json).AddHeader("User-Agent", "WorldwideRP/1.0");
                        RestResponse InternalRestResponse = (RestResponse)await restClient.ExecuteAsync(InternalRestRequest);

                        if(InternalRestResponse.StatusCode == (HttpStatusCode)200)
                        {
                            var InternalFormattedResponse = JsonConvert.DeserializeObject<ApiResponse>(InternalRestResponse.Content);
                            foreach( var apiUser in InternalFormattedResponse.results)
                            {
                                // Add the rest of the users to the list of users to be whitelisted
                                apiUsers.Add(apiUser);
                            }
                        } else
                        {
                            // If the request fails, log the error and continue on
                            Debug.WriteLine($"^1Recieved a non 200 response code when trying to query the IPS API for some whitelisted users! ^7Status: {InternalRestResponse.StatusCode} \n Response Content: {InternalRestResponse.Content}^0");
                            Debug.WriteLine(InternalRestResponse.ErrorMessage);
                        }
                    }
                    Debug.WriteLine($"^4Retrieved {apiUsers.Count} users so far from the API.");
                } else
                {
                    Debug.WriteLine($"^1Recieved a non 200 response code when trying to query the IPS API for some whitelisted users! ^7Status: {restResponse.StatusCode} \n Response Content: {restResponse.Content}^0");
                    Debug.WriteLine(restResponse.ErrorMessage);
                }
                // Iterate through every api user and add their steam hex to the list of steam hexes
                foreach(var apiUser in apiUsers)
                {
                    if (checkApiUserGroup(apiUser))
                    {
                        foreach (var customField in apiUser.customFields)
                        {
                            if (customField.Value.name == "Server Information") {
                                foreach (var field in customField.Value.fields)
                                {
                                    if (field.Value.name.ToLower().StartsWith("steam hex"))
                                    {
                                        string steamHex = field.Value.value;
                                        if(!(steamHex is null))
                                        {
                                            steamHexes.Add(field.Value.value.ToLower().Replace("steam:",""));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
            // Set the whitelisted users
            whitelistedHexes = new HashSet<string>(steamHexes);
            Debug.WriteLine($"^2Updated whitelisted users! There are {steamHexes.Count} whitelisted users. ^0");
        }

        /**
         * Checks if the user is in the allowed group
         */
        private bool checkApiUserGroup(ApiUser apiUser)
        {
            if (groupIds.Contains(apiUser.primaryGroup.id.ToString())) return true;
            foreach(ApiGroup ApiGroup in apiUser.secondaryGroups)
            {
                if(groupIds.Contains(ApiGroup.id.ToString())) return true;
            }
            return false;
        }
        private void handleResourceStart(string resName)
        {
            if (resName == GetCurrentResourceName()) { Task.Factory.StartNew(() => updateWhitelistedHexes()); } else return;
        }
        private async void handleConnection([FromSource] Player player, string plrName, dynamic _kickReason, dynamic deferrals)
        {
            // defer the user
            deferrals.defer();
            await Delay(0);
            deferrals.update("Hold on. We're making sure you're allowed here.");
            Debug.WriteLine($"^4Connecting user: ^7{plrName}^3 (Steam: {player.Identifiers["steam"] ?? "N/A"}, Discord: {player.Identifiers["discord"] ?? "N/A"}, IP: {player.Identifiers["ip"] ?? "N/A"})^0");
            //make sure they have a discord id & steam hex
            if(player.Identifiers["steam"] is null || player.Identifiers["discord"] is null)
            {
                deferrals.done($"We failed to find your {((player.Identifiers["steam"] is null && player.Identifiers["discord"] is null) ? "Steam Hex and Discord Id" : player.Identifiers["steam"] is null ? "Steam Hex" : "Discord Id")}. Make sure that application is open.");
                Debug.WriteLine($"^7{plrName} rejected. ^3They didn't have either Steam or Discord open.^0");
                return;
            }
            // make sure they're in the whitelist
            if (whitelistedHexes.Contains(player.Identifiers["steam"].ToLower().Replace("steam:", "")))
            {
                deferrals.done();
                Debug.WriteLine($"^7{plrName} authenticated. ^2Allowing connection.^0");
            } else
            {
                deferrals.done("You're not whitelisted!");
                Debug.WriteLine($"^7{plrName} didn't authenticate. ^1Terminating connection.^0");
                return;

            }
        }
        private void onTimerTimeout(object _source, ElapsedEventArgs _e)
        {
            Task.Factory.StartNew(() => updateWhitelistedHexes());
        }
    }
}
