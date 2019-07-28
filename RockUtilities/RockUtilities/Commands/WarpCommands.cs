using Chatting;
using Pipliz;
using Pipliz.JSON;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RockUtils.Commands
{
    [ModLoader.ModManager]
    public class WarpManager
    {
        public static Dictionary<string, UnityEngine.Vector3Int> warps = new Dictionary<string, UnityEngine.Vector3Int>();

        // Load Warps when server starts
        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, "RockUtils.RockUtils.AfterWorldLoad")]
        [ModLoader.ModDocumentation("Loads users homes that are set")]
        private static void LoadHomes()
        {
            Log.Write("Loading warps...");
            string jsonFilePath = "./gamedata/savegames/" + ServerManager.WorldName + "/warps.json";

            if (!File.Exists(jsonFilePath))
            {
                Log.Write("Could not locate warps.json! Ignoring");
                return;
            }

            JSONNode warpsJSON = JSON.Deserialize(jsonFilePath);

            foreach (JSONNode v in warpsJSON.LoopArray())
            {
                UnityEngine.Vector3Int position = new UnityEngine.Vector3Int(v["x"].GetAs<int>(), v["y"].GetAs<int>(), v["z"].GetAs<int>());
                warps.Add(v["n"].GetAs<string>(), position);
            }

            Log.Write("Loaded warps.");
        }

        // Save Warps
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnAutoSaveWorld, "RockUtils.RockUtils.SaveOnAutoSave")]
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnQuit, "RockUtils.RockUtils.SaveOnQuit")]
        [ModLoader.ModDocumentation("Saves stored user warps set")]
        private static void SaveHomes()
        {
            Log.Write("Saving warps...");
            string jsonFilePath = "./gamedata/savegames/" + ServerManager.WorldName + "/warps.json";

            if (File.Exists(jsonFilePath))
                File.Delete(jsonFilePath);

            JSONNode warpsJSON = new JSONNode(NodeType.Array);

            foreach (var n in warps.Keys)
            {
                UnityEngine.Vector3Int v = warps[n];
                JSONNode warpEntryJSON = new JSONNode().SetAs("x", v.x).SetAs("y", v.y).SetAs("z", v.z).SetAs("n", n);
                warpsJSON.AddToArray(warpEntryJSON);
            }

            JSON.Serialize(jsonFilePath, warpsJSON);
            Log.Write("Saved warps.");
        }
    }

    [ChatCommandAutoLoader]
    public class WarpCommands : IChatCommand
    {
        public bool TryDoCommand(Players.Player player, string chat, List<string> args)
        {
            if (args.Count < 1)
            {
                return false;
            }

            switch (args[0].ToLower())
            {
                case "/warp":
                    {
                        string warpName;
                        if (args.Count > 1)
                        {
                            warpName = string.Join(" ", args.Skip(1));
                        }
                        else
                        {
                            if (WarpManager.warps.Count == 0)
                            {
                                Chat.Send(player, "There are no warps you can teleport to");
                            }
                            else
                            {
                                Chat.Send(player, "Usage: /warp {name}");
                            }
                            return true;
                        }

                        if (!WarpManager.warps.ContainsKey(warpName))
                        {
                            Chat.Send(player, $"No such warp with the name: <color=cyan>{warpName}</color>");
                            return true;
                        }

                        UnityEngine.Vector3Int warp = WarpManager.warps[warpName];
                        Chat.Send(player, $"Sending you to <color=cyan>{warpName}</color>...");
                        Chatting.Commands.Teleport.TeleportTo(player, warp);

                        return true;
                    }
                case "/warps":
                    {
                        Chat.Send(player, $"Warps ({WarpManager.warps.Keys.Count}): {string.Join(", ", WarpManager.warps.Keys)}");
                        return true;
                    }
                case "/addwarp":
                    {
                        if (PermissionsManager.CheckAndWarnPermission(player, "warp.add"))
                        {
                            if (!(args.Count > 1))
                            {
                                Chat.Send(player, "Usage: /addwarp {name}");
                                return true;
                            }
                            string warpName = string.Join(" ", args.Skip(1));

                            if (WarpManager.warps.ContainsKey(warpName))
                            {
                                Chat.Send(player, "A warp with this name already exists");
                                return true;
                            }

                            UnityEngine.Vector3Int intplayerPosition = new UnityEngine.Vector3Int(player.Position.x.ChangeType<int>(), player.Position.y.ChangeType<int>(), player.Position.z.ChangeType<int>());
                            WarpManager.warps.Add(warpName, intplayerPosition);

                            Chat.Send(player, $"Added warp <color=cyan>{warpName}</color> at position [{intplayerPosition.x}, {intplayerPosition.y}, {intplayerPosition.z}]");
                        }
                        return true;
                    }
                case "/delwarp":
                    {
                        if (PermissionsManager.CheckAndWarnPermission(player, "warp.remove"))
                        {
                            if (!(args.Count > 1))
                            {
                                Chat.Send(player, "Usage: /delwarp {name}");
                                return true;
                            }
                            string warpName = string.Join(" ", args.Skip(1));

                            if (!WarpManager.warps.ContainsKey(warpName))
                            {
                                Chat.Send(player, "No warp with this name exists");
                                return true;
                            }

                            WarpManager.warps.Remove(warpName);

                            Chat.Send(player, $"Removed warp <color=cyan>{warpName}</color>");
                        }
                        return true;
                    }
            }

            return false;
        }
    }
}