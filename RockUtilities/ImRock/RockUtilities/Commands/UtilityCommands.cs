using Chatting;
using Pipliz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace RockUtils.Commands
{
    public class ActiveTeleport
    {
        public NetworkID requester;
        public NetworkID recepient;
        public DateTime timeStart;

        public int expirationInSeconds = 30;
        public bool completed = false;
        public bool ishere;

        public bool expired => (MilliSecondsSinceStart > expirationInSeconds * 1000);
        public long MilliSecondsSinceStart => (DateTime.UtcNow.Ticks - timeStart.Ticks) / 10000;

        public ActiveTeleport(NetworkID _req, NetworkID _rec, bool _ishere)
        {
            ishere = _ishere;
            requester = _req;
            recepient = _rec;
            timeStart = DateTime.UtcNow;
        }
    }

    [ModLoader.ModManager]
    public class MiscManager
    {
        public static Dictionary<NetworkID, ActiveTeleport> activeteleports = new Dictionary<NetworkID, ActiveTeleport>();
    }

    [ChatCommandAutoLoader]
    public class MiscCommands : IChatCommand
    {
        public bool TryDoCommand(Players.Player player, string chat, List<string> args)
        {
            if (args.Count < 1)
            {
                return false;
            }

            ActiveTeleport currentTeleport;
            Players.Player subject;

            switch (args[0].ToLower())
            {
                case "/spawn":
                    {
                        UnityEngine.Vector3Int spawn = ServerManager.TerrainGenerator.GetDefaultSpawnLocation();
                        Chatting.Commands.Teleport.TeleportTo(player, spawn);
                        Chat.Send(player, "You have been sent to spawn");

                        return true;
                    }
                case "/colonyspawn":
                    {
                        if (player.ActiveColony == null)
                        {
                            Chat.Send(player, "You are not in an active colony");
                            return true;
                        }
                        UnityEngine.Vector3Int spawn = player.ActiveColony.GetClosestBanner(player.VoxelPosition).Position;
                        Chatting.Commands.Teleport.TeleportTo(player, spawn);
                        Chat.Send(player, "You have been sent to the nearest colony spawn");

                        return true;
                    }
                case "/players":
                    {
                        List<string> playerNames = Players.PlayerDatabase.Where(i => i.Value.ConnectionState == Players.EConnectionState.Connected).Select(i => i.Value.Name).ToList();
                        Chat.Send(player, $"Players ({playerNames.Count}): {string.Join(", ", playerNames)}");

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
                case "/tpa":
                    if (args.Count < 2)
                    {
                        Chat.Send(player, "Usage: /tpa {player}");
                        return true;
                    }

                    if (!CommandManager.TryMatchKnownPlayer(player, args[1], sendResponse: false, out subject))
                    {
                        Chat.Send(player, "Unknown player specified");
                        return true;
                    }

                    if (subject.Equals(player))
                    {
                        Chat.Send(player, "You cannot teleport to yourself");
                        return true;
                    }

                    if (MiscManager.activeteleports.ContainsKey(subject.ID) && MiscManager.activeteleports[subject.ID].requester == player.ID && !MiscManager.activeteleports[subject.ID].completed)
                    {
                        Chat.Send(player, "You already have a pending teleport to this person");
                        return true;
                    }

                    Chat.Send(player, $"Sending a teleport request to <color=cyan>{subject.Name}</color>");
                    Chat.Send(subject, $"<color=cyan>{player.Name}</color> would like to teleport to you. Do <color=cyan>/tpaccept</color> to accept");
                    currentTeleport = new ActiveTeleport(player.ID, subject.ID, false);
                    MiscManager.activeteleports[subject.ID] = currentTeleport;

                    Task.Run(async delegate
                    {
                        await Task.Delay(currentTeleport.expirationInSeconds * 1000);
                        if (currentTeleport.ChangeType<bool>() && !currentTeleport.completed)
                        {
                            Chat.Send(player, $"<color=#f5350f>Teleport request to <color=cyan>{subject.Name}</color> timed out</color>");
                        }
                        if (MiscManager.activeteleports.ContainsKey(subject.ID))
                        {
                            MiscManager.activeteleports.Remove(subject.ID);
                        }
                    });

                    return true;
                case "/tpahere":
                    if (args.Count < 2)
                    {
                        Chat.Send(player, "Usage: /tpahere {player}");
                        return true;
                    }

                    if (!CommandManager.TryMatchKnownPlayer(player, args[1], sendResponse: false, out subject))
                    {
                        Chat.Send(player, "Unknown player specified");
                        return true;
                    }

                    if (subject.Equals(player))
                    {
                        Chat.Send(player, "You cannot teleport to yourself");
                        return true;
                    }

                    if (MiscManager.activeteleports.ContainsKey(subject.ID) && MiscManager.activeteleports[subject.ID].requester == player.ID && !MiscManager.activeteleports[subject.ID].completed)
                    {
                        Chat.Send(player, "You already have a pending teleport to this person");
                        return true;
                    }

                    Chat.Send(player, $"Sending a teleport request to <color=cyan>{subject.Name}</color>");
                    Chat.Send(subject, $"<color=cyan>{player.Name}</color> would you to teleport to them. Do <color=cyan>/tpaccept</color> to accept");
                    currentTeleport = new ActiveTeleport(player.ID, subject.ID, true);
                    MiscManager.activeteleports[subject.ID] = currentTeleport;

                    Task.Run(async delegate
                    {
                        await Task.Delay(currentTeleport.expirationInSeconds * 1000);
                        if (!currentTeleport.completed)
                        {
                            Chat.Send(player, $"<color=#f5350f>Teleport request for <color=cyan>{subject.Name}</color> timed out</color>");
                        }
                        MiscManager.activeteleports.Remove(subject.ID);
                    });

                    return true;
                case "/tpaccept":
                    if (!MiscManager.activeteleports.ContainsKey(player.ID))
                    {
                        Chat.Send(player, "You do not have any active teleport requests");
                        return true;
                    }

                    currentTeleport = MiscManager.activeteleports[player.ID];
                    if (currentTeleport.expired || currentTeleport.completed)
                    {
                        Chat.Send(player, "You do not have any active teleport requests");
                        return true;
                    }

                    currentTeleport.completed = true;
                    MiscManager.activeteleports[player.ID] = currentTeleport;

                    Players.TryGetPlayer(currentTeleport.requester, out subject);
                    Chat.Send(player, $"You have accepted a teleport request from <color=cyan>{subject.Name}</color>");
                    Chat.Send(subject, $"<color=cyan>{player.Name}</color> accepted the teleport request");
                    if (currentTeleport.ishere)
                    {
                        Chatting.Commands.Teleport.TeleportTo(player, subject.Position);
                    }
                    else
                    {
                        Chatting.Commands.Teleport.TeleportTo(subject, player.Position);
                    }

                    return true;
                case "/tpdeny":
                    if (!MiscManager.activeteleports.ContainsKey(player.ID))
                    {
                        Chat.Send(player, "You do not have any active teleport requests");
                        return true;
                    }

                    currentTeleport = MiscManager.activeteleports[player.ID];
                    if (currentTeleport.expired || currentTeleport.completed)
                    {
                        Chat.Send(player, "You do not have any active teleport requests");
                        return true;
                    }

                    currentTeleport.completed = true;
                    MiscManager.activeteleports[player.ID] = currentTeleport;

                    Players.TryGetPlayer(currentTeleport.requester, out subject);
                    Chat.Send(player, $"You have denied a teleport request from <color=cyan>{subject.Name}</color>");
                    Chat.Send(subject, $"<color=cyan>{player.Name}</color> denied the teleport request");

                    return true;
            }

            return false;
        }
    }
}