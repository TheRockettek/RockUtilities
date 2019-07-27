using Chatting;
using Pipliz;
using Pipliz.JSON;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

namespace RockUtils
{

    [ModLoader.ModManager]
    public class Events
    {
        // Create public dictionaries
        public static Dictionary<NetworkID, Dictionary<string, UnityEngine.Vector3Int>> homes = new Dictionary<NetworkID, Dictionary<string, UnityEngine.Vector3Int>>();

        // Load Homes when server starts
        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, "RockUtils.RockUtils.AfterWorldLoad")]
        [ModLoader.ModDocumentation("Loads users homes that are set")]
        private static void LoadHomes()
        {
            Log.Write("Loading homes...");
            string jsonFilePath = "./gamedata/savegames/" + ServerManager.WorldName + "/homes.json";

            if (!File.Exists(jsonFilePath))
            {
                Log.Write("Could not locate homes.json! Ignoring");
                return;
            }

            JSONNode homesJSON = JSON.Deserialize(jsonFilePath);

            foreach (JSONNode playerJSON in homesJSON.LoopArray())
            {
                NetworkID player = NetworkID.Parse(playerJSON.GetAs<string>("i"));
                Dictionary<string, UnityEngine.Vector3Int> playerHomesJSON = new Dictionary<string, UnityEngine.Vector3Int>();

                // Load homes
                foreach (var v in playerJSON.GetAs<JSONNode>("h").LoopArray())
                {
                    UnityEngine.Vector3Int position = new UnityEngine.Vector3Int(v["x"].GetAs<int>(), v["y"].GetAs<int>(), v["z"].GetAs<int>());
                    playerHomesJSON.Add(v.GetAs<string>("n"), position);
                }

                homes.Add(player, playerHomesJSON);
            }

            Log.Write("Loaded homes.");
        }

        // Save Homes
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnAutoSaveWorld, "RockUtils.RockUtils.SaveOnAutoSave")]
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnQuit, "RockUtils.RockUtils.SaveOnQuit")]
        [ModLoader.ModDocumentation("Saves stored user homes set")]
        private static void SaveHomes()
        {
            Log.Write("Saving homes...");
            string jsonFilePath = "./gamedata/savegames/" + ServerManager.WorldName + "/homes.json";

            if (File.Exists(jsonFilePath))
                File.Delete(jsonFilePath);

            JSONNode homesJSON = new JSONNode(NodeType.Array);

            foreach (var key in homes.Keys)
            {
                JSONNode playerJSON = new JSONNode();
                JSONNode playerHomesJSON = new JSONNode(NodeType.Array);

                foreach (var n in homes[key].Keys)
                {
                    UnityEngine.Vector3Int v = homes[key][n];
                    JSONNode homeEntryJSON = new JSONNode().SetAs("x", v.x).SetAs("y", v.y).SetAs("z", v.z).SetAs("n", n);
                    playerHomesJSON.AddToArray(homeEntryJSON);
                }

                playerJSON.SetAs("i", key.ToString());
                playerJSON.SetAs("h", playerHomesJSON);

                homesJSON.AddToArray(playerJSON);
            }

            JSON.Serialize(jsonFilePath, homesJSON);
            Log.Write("Saved homes.");
        }
    }

    [ChatCommandAutoLoader]
    public class Commands : IChatCommand
    {
        public bool TryDoCommand(Players.Player player, string chat, List<string> args)
        {
            if (args.Count < 1)
            {
                return false;
            }

            Dictionary<string, UnityEngine.Vector3Int> playerHomes;
            string homeName;

            switch (args[0].ToLower())
            {
                case "/players":
                    {
                        List<string> playerNames = Players.PlayerDatabase.Where(i => i.Value.ConnectionState == Players.EConnectionState.Connected).Select(i => i.Value.Name).ToList();
                        Chat.Send(player, $"Players ({playerNames.Count}): {string.Join(", ", playerNames)}");
                        return true;
                    }
                case "/spawn":
                    {
                        UnityEngine.Vector3Int spawn = ServerManager.TerrainGenerator.GetDefaultSpawnLocation();
                        Chatting.Commands.Teleport.TeleportTo(player, spawn);
                        Chat.Send(player, "You have been sent to spawn");
                        return true;
                    }
                case "/nearcolonies":
                case "/nearcolony":
                case "/nearbycolonies":
                case "/nearbycolony":
                    {
                        float maxDistance = 250;
                        Vector3 playerPosition = player.Position;
                        List<string> nearbyColonies = ServerManager.ColonyTracker.ColoniesByID.Where(i => i.Value.Banners.Any(k => Vector3.Distance(playerPosition, k.Position.Vector) < maxDistance)).Select(i => i.Value.Name).ToList();

                        if (nearbyColonies.Count == 0)
                        {
                            Chat.Send(player, "There are no nearby colonies");
                        }
                        else
                        {
                            Chat.Send(player, $"Nearby Colonies ({nearbyColonies.Count}): {string.Join(", ", nearbyColonies)}");
                        }
                        return true;
                    }
                case "/near":
                case "/nearby":
                    {
                        int maxDistance = 250;
                        Vector3 playerPosition = player.Position;
                        List<Players.Player> players = Players.PlayerDatabase.Where(i => i.Value.ConnectionState == Players.EConnectionState.Connected & i.Value.ID != player.ID).Select(i => i.Value).ToList();
                        List<string> nearbyPlayers = players.Where(i => Vector3.Distance(playerPosition, i.Position) < maxDistance).Select(i => i.Name).ToList();
                        if (nearbyPlayers.Count == 0)
                        {
                            Chat.Send(player, "There are no players near, except you of course");
                        }
                        else
                        {
                            Chat.Send(player, $"Nearby Players ({nearbyPlayers.Count}): {string.Join(", ", nearbyPlayers)}");
                        }
                        return true;
                    }
                case "/home":
                    {
                        playerHomes = RockUtils.Events.homes.GetValueOrDefault(key: player.ID, new Dictionary<string, UnityEngine.Vector3Int>());

                        if (args.Count > 1)
                        {
                            homeName = string.Join(" ", args.Skip(1));
                        }
                        else
                        {
                            switch (playerHomes.Count)
                            {
                                case 1:
                                    homeName = playerHomes.Keys.First();
                                    break;
                                case 0:
                                    Chat.Send(player, "You do not have any homes you can teleport to");
                                    return true;
                                default:
                                    Chat.Send(player, "Usage: /home {home}");
                                    return true;
                            }
                        }

                        if (!playerHomes.ContainsKey(homeName))
                        {
                            Chat.Send(player, $"No such home with the name: <color=cyan>{homeName}</color>");
                            return true;
                        }

                        UnityEngine.Vector3Int home = playerHomes[homeName];
                        Chat.Send(player, $"Sending you to <color=cyan>{homeName}</color>...");
                        Chatting.Commands.Teleport.TeleportTo(player, home);

                        return true;
                    }
                case "/homes":
                    playerHomes = RockUtils.Events.homes.GetValueOrDefault(key: player.ID, new Dictionary<string, UnityEngine.Vector3Int>());

                    Chat.Send(player, $"Homes ({playerHomes.Keys.Count}): {string.Join(", ", playerHomes.Keys)}");
                    return true;
                case "/removehome":
                case "/rmhome":
                case "/delhome":
                    playerHomes = RockUtils.Events.homes.GetValueOrDefault(key: player.ID, new Dictionary<string, UnityEngine.Vector3Int>());

                    if (args.Count > 1)
                    {
                        homeName = string.Join(" ", args.Skip(1));
                    }
                    else
                    {
                        switch (playerHomes.Count)
                        {
                            case 1:
                                homeName = playerHomes.Keys.First();
                                break;
                            case 0:
                                Chat.Send(player, "You do not have any homes you can delete");
                                return true;
                            default:
                                Chat.Send(player, "Usage: /delhome {home}");
                                return true;
                        }
                    }

                    if (!playerHomes.ContainsKey(homeName))
                    {
                        Chat.Send(player, $"No such home with the name: <color=cyan>{homeName}</color>");
                        return true;
                    }

                    playerHomes.Remove(homeName);
                    RockUtils.Events.homes[player.ID] = playerHomes;

                    Chat.Send(player, $"Removed home <color=cyan>{homeName}</color>");

                    return true;
                case "/sethome":
                    playerHomes = RockUtils.Events.homes.GetValueOrDefault(key: player.ID, new Dictionary<string, UnityEngine.Vector3Int>());
                    homeName = "home";

                    if (args.Count > 1)
                    {
                        homeName = string.Join(" ", args.Skip(1));
                    }

                    if (playerHomes.ContainsKey(homeName))
                    {
                        Chat.Send(player, "A home with this name already exists");
                        return true;
                    }

                    playerHomes = RockUtils.Events.homes.GetValueOrDefault(key: player.ID, new Dictionary<string, UnityEngine.Vector3Int>());
                    UnityEngine.Vector3Int intplayerPosition = new UnityEngine.Vector3Int(player.Position.x.ChangeType<int>(), player.Position.y.ChangeType<int>(), player.Position.z.ChangeType<int>());

                    playerHomes.Add(homeName, intplayerPosition);
                    RockUtils.Events.homes[player.ID] = playerHomes;

                    Chat.Send(player, $"Added home <color=cyan>{homeName}</color> at position [{intplayerPosition.x}, {intplayerPosition.y}, {intplayerPosition.z}]");

                    return true;
            }

            // TODO:
            // /warp to colonies
            // /colonies command
            // /colony owners

            return false;
        }
    }
}