using Chatting;
using Pipliz;
using Pipliz.JSON;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RockUtils.Commands
{
    [ModLoader.ModManager]
    public class HomeManager
    {
        public static Dictionary<NetworkID, Dictionary<string, UnityEngine.Vector3Int>> homes = new Dictionary<NetworkID, Dictionary<string, UnityEngine.Vector3Int>>();

        // Load Homes when server starts
        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, "RockUtils.RockUtils.LoadHomes")]
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
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnAutoSaveWorld, "RockUtils.RockUtils.SaveHomes")]
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnQuit, "RockUtils.RockUtils.SaveHomes")]
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
    public class HomeCommands : IChatCommand
    {
        public bool TryDoCommand(Players.Player player, string chat, List<string> args)
        {
            if (args.Count < 1)
            {
                return false;
            }

            Dictionary<string, UnityEngine.Vector3Int> playerHomes;
            string homeName;

            string command = args[0].ToLower().Remove(0, 1);
            if (command.StartsWith("rockutils:"))
            {
                command = command.Remove(0, 10);
            }
            switch (command)
            {
                case "home":
                    {
                        playerHomes = HomeManager.homes.GetValueOrDefault(key: player.ID, new Dictionary<string, UnityEngine.Vector3Int>());

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
                                    Chat.Send(player, "Usage: /home {name}");
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
                case "homes":
                    playerHomes = HomeManager.homes.GetValueOrDefault(key: player.ID, new Dictionary<string, UnityEngine.Vector3Int>());
                    Chat.Send(player, $"Homes ({playerHomes.Keys.Count}): {string.Join(", ", playerHomes.Keys)}");

                    return true;
                case "sethome":
                case "addhome":
                    playerHomes = HomeManager.homes.GetValueOrDefault(key: player.ID, new Dictionary<string, UnityEngine.Vector3Int>());
                    homeName = "home";

                    if (args.Count > 1)
                    {
                        homeName = string.Join(" ", args.Skip(1));
                    }

                    if (playerHomes.ContainsKey(homeName))
                    {
                        if (args[0].ToLower().Equals("/sethome"))
                        {
                            playerHomes.Remove(homeName);
                        }
                        else
                        {
                            Chat.Send(player, "A home with this name already exists");
                            return true;
                        }
                    }

                    playerHomes = HomeManager.homes.GetValueOrDefault(key: player.ID, new Dictionary<string, UnityEngine.Vector3Int>());
                    UnityEngine.Vector3Int intplayerPosition = new UnityEngine.Vector3Int(player.Position.x.ChangeType<int>(), player.Position.y.ChangeType<int>(), player.Position.z.ChangeType<int>());

                    playerHomes.Add(homeName, intplayerPosition);
                    HomeManager.homes[player.ID] = playerHomes;

                    Chat.Send(player, $"Added home <color=cyan>{homeName}</color> at position [{intplayerPosition.x}, {intplayerPosition.y}, {intplayerPosition.z}]");

                    return true;
                case "delhome":
                    playerHomes = HomeManager.homes.GetValueOrDefault(key: player.ID, new Dictionary<string, UnityEngine.Vector3Int>());

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
                                Chat.Send(player, "Usage: /delhome {name}");
                                return true;
                        }
                    }

                    if (!playerHomes.ContainsKey(homeName))
                    {
                        Chat.Send(player, $"No such home with the name: <color=cyan>{homeName}</color>");
                        return true;
                    }

                    playerHomes.Remove(homeName);
                    HomeManager.homes[player.ID] = playerHomes;

                    Chat.Send(player, $"Removed home <color=cyan>{homeName}</color>");

                    return true;
            }

            return false;
        }
    }
}