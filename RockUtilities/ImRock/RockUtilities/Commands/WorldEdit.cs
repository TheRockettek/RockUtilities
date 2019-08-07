using Chatting;
using Pipliz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Vector3Int = Pipliz.Vector3Int;

namespace RockUtils.WorldEdit
{
    public enum EAreaSelectionType : byte
    {
        Cuboid,
    }
    public enum Direction : byte
    {
        front,
        back,
        right,
        left,
        up,
        down
    }
    public enum PositionTypes : byte
    {
        xyz,
        radius,
        radius_widthdepth,
        radius_widthdepthheight
    }
    public class SchematicBlock
    {
        static SchematicBlock()
        {
            air = new SchematicBlock();
        }

        public static SchematicBlock air { get; private set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public ushort ID { get; set; }
        public string BlockID { get; set; }
    }
    public class Schematic
    {
        public enum Rotation
        {
            Front,
            Right,
            Back,
            Left
        }

        public string Name { get; set; }
        public int XMax { get; set; }
        public int YMax { get; set; }
        public int ZMax { get; set; }
        /// <summary>Contains all usual blocks</summary>
        public Dictionary<(int, int, int), SchematicBlock> Blocks { get; set; }
        /// <summary>Contains TileEntities such as hoppers and chests</summary>
        public Vector3Int playerPos { get; set; }
        public Vector3Int StartPos { get; set; }

        public Schematic()
        {

        }

        public Schematic(string name) : this()
        {
            Name = name;
        }

        public Schematic(string name, int x, int y, int z) : this(name)
        {
            XMax = x;
            YMax = y;
            ZMax = z;
        }

        public Schematic(string name, int x, int y, int z, Dictionary<(int, int, int), SchematicBlock> blocks, Vector3Int startPos) : this(name, x, y, z)
        {
            Blocks = blocks;
            StartPos = startPos;
        }

        public SchematicBlock GetBlock(int X, int Y, int Z)
        {
            SchematicBlock block;
            Blocks.TryGetValue((X, Y, Z), out block);

            if (block == default(SchematicBlock) || block == null)
            {
                block = SchematicBlock.air;
            }

            return block;
        }

        public SchematicBlock GetBlock(Vector3Int vec)
        {
            SchematicBlock block;
            Blocks.TryGetValue((vec.x, vec.y, vec.z), out block);

            if (block == default(SchematicBlock) || block == null)
            {
                block = SchematicBlock.air;
            }

            return block;
        }

        public void Rotate()
        {
            Dictionary<(int, int, int), SchematicBlock> newBlocks = new Dictionary<(int, int, int), SchematicBlock>();

            // 90 deg anticlockwise
            foreach (KeyValuePair<(int, int, int), SchematicBlock> block in Blocks)
            {
                int newX = block.Value.X;
                int newZ = ZMax - (block.Value.Z + 1);
                if (block.Value.BlockID.Contains("z+"))
                {
                    block.Value.BlockID = block.Value.BlockID.Replace("z+", "x-");
                }
                else if (block.Value.BlockID.Contains("z-"))
                {
                    block.Value.BlockID = block.Value.BlockID.Replace("z-", "x+");
                }
                else if (block.Value.BlockID.Contains("x+"))
                {
                    block.Value.BlockID = block.Value.BlockID.Replace("x+", "z+");
                }
                else if (block.Value.BlockID.Contains("x-"))
                {
                    block.Value.BlockID = block.Value.BlockID.Replace("x-", "z-");
                }
                block.Value.X = newX;
                block.Value.Z = newZ;
                newBlocks[(newX, block.Value.Y, newZ)] = block.Value;
            }

            Blocks = newBlocks;

            int tmpSize = XMax;
            XMax = ZMax;
            ZMax = tmpSize;
        }
    }
    public abstract class BasePattern
    {
        public List<ushort> patternBlocks;
        public BasePattern()
        {
        }
        public BasePattern(List<ushort> blocks)
        {
            patternBlocks = new List<ushort>();
            patternBlocks.AddRange(blocks);
        }
        public BasePattern(ushort block)
        {
            patternBlocks = new List<ushort>();
            patternBlocks.Add(block);
        }
        public BasePattern(string blocks)
        {
            patternBlocks = new List<ushort>();
            patternBlocks.AddRange(blocks.Split(',').Where(i => Helpers.TryMatchItemType(i, out _)).Select(i => Helpers.MatchItemType(i).ItemIndex));
        }
        public abstract ushort Apply(Vector3Int position);
    }
    public class RandomPattern : BasePattern
    {
        public RandomPattern(List<ushort> blocks) : base(blocks) { }
        public RandomPattern(ushort block) : base(block) { }
        public RandomPattern(string blocks) : base(blocks) { }

        public override ushort Apply(Vector3Int position)
        {
            System.Random rng = new System.Random(Guid.NewGuid().GetHashCode());
            return patternBlocks[rng.Next(patternBlocks.Count)];
        }
    }
    public class StaticPattern : BasePattern
    {
        public StaticPattern(ushort block) : base(block) { }
        public StaticPattern(string blocks) : base(blocks) { }

        public override ushort Apply(Vector3Int position)
        {
            return patternBlocks[0];
        }
    }
    public class AreaSelection
    {
        public Vector3Int posA = Vector3Int.maximum;
        public Vector3Int posB = Vector3Int.maximum;
        public EAreaSelectionType selectionType = EAreaSelectionType.Cuboid;
        public Vector3Int cornerA { get; internal set; }
        public Vector3Int cornerB { get; internal set; }
        public bool ArePositionsSet => posA != Vector3Int.maximum && posB != Vector3Int.maximum;
        public void SetCornerA(Vector3Int _posA, Players.Player player, bool informPlayer, WorldEditInteraction interaction)
        {
            posA = _posA;
            long posSize = Helpers.GetSize(posA, posB);

            if (ArePositionsSet && posSize > interaction.selectionLimit)
            {
                if (informPlayer)
                {
                    Chat.Send(player, $"<color=#3490fa>Selection size too large ({posSize} > {interaction.selectionLimit}), increase it with //limit [blocks]</color>");
                }
                return;
            }

            cornerA = Vector3Int.Min(posA, posB);
            cornerB = Vector3Int.Max(posA, posB);

            if (informPlayer)
            {
                if (ArePositionsSet && ArePositionsSet)
                {
                    Chat.Send(player, $"<color=#3490fa>First position set to ({_posA.x}, {_posA.y}, {_posA.z}) [{GetXSize()}x{GetYSize()}x{GetZSize()}] ({GetSize()})</color>");
                    if (posSize > interaction.selectionLimit * 30 && !interaction.fastMode)
                    {
                        Chat.Send(player, $"<color=#2490fa>This selection is large, concider using fastmode to speed jobs up on larger areas</color>");
                    }
                }
                else
                {
                    Chat.Send(player, $"<color=#3490fa>First position set to ({_posA.x}, {_posA.y}, {_posA.z})</color>");
                }
            }

            AreaJobTracker.SendData(player); // Display area
        }
        public void SetCornerB(Vector3Int _posB, Players.Player player, bool informPlayer, WorldEditInteraction interaction)
        {
            posB = _posB;
            long posSize = Helpers.GetSize(posA, posB);

            if (ArePositionsSet && posSize > interaction.selectionLimit)
            {
                if (informPlayer)
                {
                    Chat.Send(player, $"<color=#3490fa>Selection size too large ({posSize} > {interaction.selectionLimit}), increase it with //limit [blocks]</color>");
                }
                return;
            }
            cornerA = Vector3Int.Min(posA, posB);
            cornerB = Vector3Int.Max(posA, posB);

            if (informPlayer)
            {
                if (ArePositionsSet)
                {
                    Chat.Send(player, $"<color=#3490fa>Secondary position set to ({_posB.x}, {_posB.y}, {_posB.z}) [{GetXSize()}x{GetYSize()}x{GetZSize()}] ({GetSize()})</color>");
                    if (posSize > interaction.selectionLimit * 30 && !interaction.fastMode)
                    {
                        Chat.Send(player, $"<color=#2490fa>This selection is large, concider using fastmode to speed jobs up on larger areas</color>");
                    }
                }
                else
                {
                    Chat.Send(player, $"<color=#3490fa>Secondary position set to ({_posB.x}, {_posB.y}, {_posB.z})</color>");
                }
            }

            AreaJobTracker.SendData(player); // Display area
        }

        public int GetXSize() { return System.Math.Abs(posA.x - posB.x) + 1; }
        public int GetYSize() { return System.Math.Abs(posA.y - posB.y) + 1; }
        public int GetZSize() { return System.Math.Abs(posA.z - posB.z) + 1; }
        public int GetSize() { return GetXSize() * GetYSize() * GetZSize(); }
    }

    [ModLoader.ModManager]
    public class Events
    {
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, "RockUtils.RockUtils.WandAction")]
        public static void OnPlayerClicked(Players.Player player, Shared.PlayerClickedData click)
        {
            if (click.TypeSelected == BlockTypes.BuiltinBlocks.Indices.bronzeaxe && click.HitType == Shared.PlayerClickedData.EHitType.Block && PermissionsManager.HasPermission(player, "worldedit"))
            {
                Shared.PlayerClickedData.VoxelHit voxel;
                try
                {
                    voxel = click.GetVoxelHit();
                }
                catch (Exception e)
                {
                    Chat.Send(player, $"<color=red>Encountered exception whilst retrieving voxel: {e}</color>");
                    throw;
                }

                WorldEditInteraction.GetInteraction(player, true, out WorldEditInteraction currentInteraction);

                if (currentInteraction.active)
                {
                    if (click.ClickType == Shared.PlayerClickedData.EClickType.Left)
                    {
                        currentInteraction.area.SetCornerA(voxel.BlockHit, player, true, currentInteraction);
                    }
                    else
                    {
                        currentInteraction.area.SetCornerB(voxel.BlockHit, player, true, currentInteraction);
                    }
                }
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerDisconnected, "RockUtils.RockUtils.OnPlayerDisconnected")]
        public static void OnPlayerDisconnected(Players.Player player)
        {
            if (Helpers.interactions.ContainsKey(player.ID))
            {
                Helpers.interactions.Remove(player.ID);
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnTryChangeBlock, "RockUtils.RockUtils.StopWandGriefing")]
        public static void OnTryChangeBlockUser(ModLoader.OnTryChangeBlockData userData)
        {
            if (userData.RequestOrigin.Type == BlockChangeRequestOrigin.EType.Player &&
                userData.PlayerClickedData != null &&
                userData.PlayerClickedData.TypeSelected.Equals(BlockTypes.BuiltinBlocks.Indices.bronzeaxe) &&
                Helpers.interactions.TryGetValue(userData.RequestOrigin.AsPlayer.ID, out WorldEditInteraction currentInteraction))
            {
                AreaSelection area = currentInteraction.area;

                if (!currentInteraction.active)
                {
                    return;
                }

                if (!area.ArePositionsSet)
                {
                    return;
                }

                userData.InventoryItemResults.Clear();
                userData.CallbackState = ModLoader.OnTryChangeBlockData.ECallbackState.Cancelled;
                userData.CallbackConsumedResult = EServerChangeBlockResult.CancelledByCallback;
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnSendAreaHighlights, "RockUtils.RockUtils.ShowArea")]
        public static void OnSendAreaHighlights(Players.Player player, List<AreaJobTracker.AreaHighlight> list, List<ushort> showWhileHoldingTypes)
        {
            if (!player.Equals(null))
            {
                if (Helpers.interactions.TryGetValue(player.ID, out WorldEditInteraction currentInteraction))
                {
                    AreaSelection area = currentInteraction.area;

                    if (!currentInteraction.active)
                    {
                        return;
                    }

                    if (!area.ArePositionsSet)
                    {
                        return;
                    }

                    showWhileHoldingTypes.Add(BlockTypes.BuiltinBlocks.Indices.bronzeaxe);
                    list.Add(new AreaJobTracker.AreaHighlight(area.cornerA, area.cornerB, Shared.EAreaMeshType.ThreeDActive, Shared.EServerAreaType.ConstructionArea));
                }
            }
        }
    }

    [ModLoader.ModManager]
    public class WorldEditInteraction
    {
        public bool active = true;
        public bool fastMode = false; // If enabled, will ignore any block update limit
        public int blockUpdates = 40960; // Sets a limit on how many blocks can be modified per update
        public int selectionLimit = 250000; // total selection limit
        public int limit = 50000; // total block count limit
        public int blocksMaxPerUpdate()
        {
            if (fastMode)
            {
                return 10000000;
            }
            else
            {
                return blockUpdates;
            }
        }

        public AreaSelection area = new AreaSelection(); // stores selection
        public Stack<Schematic> jobhistory = new Stack<Schematic>();
        public Schematic clipboard = null;

        private static Stack<Vector3Int> chunkStack = new Stack<Vector3Int>(); // Stack of chunk ids to process
        private static Dictionary<Vector3Int, KeyValuePair<BlockChangeRequestOrigin, List<(Vector3Int, ushort)>>> chunkModifications = new Dictionary<Vector3Int, KeyValuePair<BlockChangeRequestOrigin, List<(Vector3Int, ushort)>>>();
        private static Queue<Vector3Int> failedChunks = new Queue<Vector3Int>(); // Failed chunks will be put back into queue to be reattempted

        private static long nextUpdate = 0;
        private static readonly long increment = 250; // ms until next chunk edit

        private static bool failFallback = true; // If a change block fails and is false, it will ignore it else will retry

        public static bool FailFallback { get => failFallback; set => failFallback = value; }
        public static Stack<Vector3Int> ChunkStack { get => chunkStack; set => chunkStack = value; }
        public static Dictionary<Vector3Int, KeyValuePair<BlockChangeRequestOrigin, List<(Vector3Int, ushort)>>> ChunkModifications { get => chunkModifications; set => chunkModifications = value; }
        public static Queue<Vector3Int> FailedChunks { get => failedChunks; set => failedChunks = value; }

        public static bool GetInteraction(Players.Player player, bool createEmpty, out WorldEditInteraction output)
        {
            return GetInteraction(player.ID, createEmpty, out output);
        }
        public static bool GetInteraction(NetworkID playerID, bool createEmpty, out WorldEditInteraction output)
        {
            if (!Helpers.interactions.TryGetValue(playerID, out WorldEditInteraction currentInteraction))
            {
                if (createEmpty)
                {
                    currentInteraction = new WorldEditInteraction();
                    Helpers.interactions.Add(playerID, currentInteraction);
                    output = currentInteraction;
                    return true;
                }
                output = null;
                return false;
            }
            output = currentInteraction;
            return true;
        }
        public void AddBlockJob(Vector3Int position, ushort blockType, Players.Player player = null)
        {
            Vector3Int chunk = position.ToChunk();
            if (!ChunkModifications.ContainsKey(chunk))
            {
                KeyValuePair<BlockChangeRequestOrigin, List<(Vector3Int, ushort)>> _chunkModifications = new KeyValuePair<BlockChangeRequestOrigin, List<(Vector3Int, ushort)>>(player, new List<(Vector3Int, ushort)>());
                ChunkModifications.Add(chunk, _chunkModifications);
            }

            ChunkModifications[chunk].Value.Add((position, blockType));

            if (!ChunkStack.Contains(chunk))
            {
                ChunkStack.Push(chunk);
            }
        }

        public void ConvertPosToJobs(List<Vector3Int> positions, BasePattern blockSelection, Players.Player player, int limit = 40960)
        {
            int totalSize = positions.Count;
            int blocksProcessed = 0;
            int blocksDone = 0;

            Task.Run(async delegate
            {
                foreach (var blockPosition in positions.Distinct().ToList())
                {
                    AddBlockJob(blockPosition, blockSelection.Apply(blockPosition), player);
                    blocksDone++;
                    blocksProcessed++;
                    if (blocksProcessed > limit)
                    {
                        Chat.Send(player, $"{Helpers.CreateProgress(blocksDone, totalSize, 15, "#3490fa", "white", '█')} <color=#3490fa>Creating jobs... {((decimal)blocksDone / totalSize).ToString("0.0%")} ({blocksDone} / {totalSize})</color>");
                        await Task.Delay(1000);
                        blocksProcessed = 0;
                    }
                }
            });
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnLateUpdate, "RockUtils.RockUtils.HandleBlockJobs")]
        public static void OnUpdate()
        {
            if (Pipliz.Time.MillisecondsSinceStart < nextUpdate)
            {
                return;
            }

            nextUpdate = Pipliz.Time.MillisecondsSinceStart + increment;

            int blocksPerUpdate = 4096;
            Vector3Int chunk;

            while (blocksPerUpdate > 0)
            {
                if (ChunkStack.Count > 0)
                {
                    chunk = ChunkStack.Pop();
                }
                else if (FailedChunks.Count > 0)
                {
                    chunk = FailedChunks.Dequeue();
                    ChunkQueue.QueuePlayerSurrounding(chunk);
                }
                else
                {
                    return;
                }

                if (!ChunkModifications.TryGetValue(chunk, out KeyValuePair<BlockChangeRequestOrigin, List<(Vector3Int, ushort)>> changes))
                {
                    return;
                }

                int count = changes.Value.ToList().Count;

                foreach (var _blockJob in changes.Value.ToList())
                {
                    if (ServerManager.TryChangeBlock(_blockJob.Item1, _blockJob.Item2, changes.Key) == EServerChangeBlockResult.ChunkNotReady && WorldEditInteraction.FailFallback)
                    {
                        Log.Write($"Failed to change block due to chunk not being ready, enqueing into failed chunk ballback");
                        FailedChunks.Enqueue(chunk);
                        return;
                    }
                    blocksPerUpdate -= 1;
                }

                if (count != ChunkModifications[chunk].Value.Count)
                {
                    Log.Write($"Adding chunk back to queue as queue changed from {count} to {ChunkModifications[chunk].Value.Count}");
                    FailedChunks.Enqueue(chunk);
                }
                else
                {
                    ChunkModifications.Remove(chunk);
                }
            }
        }
    }
    public class Helpers
    {
        public static Dictionary<NetworkID, WorldEditInteraction> interactions = new Dictionary<NetworkID, WorldEditInteraction>();
        public static Vector3Int Clamp(Vector3Int pos, Vector3Int beg, Vector3Int end)
        {
            Vector3Int posA = Vector3Int.Min(beg, end);
            Vector3Int posB = Vector3Int.Max(beg, end);

            return new Vector3Int(
                Mathf.Clamp(pos.x, posA.x, posB.x),
                Mathf.Clamp(pos.y, posA.y, posB.y),
                Mathf.Clamp(pos.z, posA.z, posB.z));
        }
        public static bool ConvertPosToSchematic(Vector3Int corner1, Vector3Int corner2, out Schematic schematic)
        {
            Vector3Int pos1 = Vector3Int.Min(corner1, corner2);
            Vector3Int pos2 = Vector3Int.Max(corner1, corner2);
            int x1 = pos1.x;
            int y1 = pos1.y;
            int z1 = pos1.z;
            int x2 = pos2.x;
            int y2 = pos2.y;
            int z2 = pos2.z;
            Dictionary<(int, int, int), SchematicBlock> blocks = new Dictionary<(int, int, int), SchematicBlock>();

            for (int x = x2; x >= x1; x--)
            {
                for (int y = y2; y >= y1; y--)
                {
                    for (int z = z2; z >= z1; z--)
                    {
                        if (World.TryGetTypeAt(new Vector3Int(x, y, z), out ItemTypes.ItemType blockout))
                        {
                            if (blockout.ItemIndex == BlockTypes.BuiltinBlocks.Indices.air)
                            {
                                continue;
                            }

                            SchematicBlock block = new SchematicBlock
                            {
                                X = x,
                                Y = y,
                                Z = z,
                                ID = blockout.ItemIndex,
                                BlockID = blockout.Name,
                            };
                            blocks[(x, y, z)] = block;
                        }
                    }
                }
            }

            schematic = new Schematic("_", x2, y2, z2, blocks, pos1);
            return true;
        }
        public static bool PositionParser(PositionTypes posType, List<string> args, out List<int> output)
        {
            switch (posType)
            {
                case PositionTypes.xyz:
                    {
                        if (args.Count >= 3)
                        {
                            List<int> numArgs = args.Where(i => int.TryParse(i, out int _)).Select(i => int.Parse(i)).ToList();
                            if (numArgs.Count >= 3)
                            {
                                output = numArgs.GetRange(0, 3);
                                return true;
                            }
                        }
                        break;
                    }
                case PositionTypes.radius:
                    if (args.Count >= 1)
                    {
                        List<int> numArgs = args.Where(i => int.TryParse(i, out int _)).Select(i => int.Parse(i)).ToList();
                        if (numArgs.Count >= 1)
                        {
                            output = numArgs.GetRange(0, 1);
                            return true;
                        }
                    }
                    break;
                case PositionTypes.radius_widthdepth:
                    if (args.Count >= 2)
                    {
                        List<int> numArgs = args.Where(i => int.TryParse(i, out int _)).Select(i => int.Parse(i)).Where(i => i > 0).ToList();
                        if (numArgs.Count >= 2)
                        {
                            output = numArgs.GetRange(0, 2);
                            return true;
                        }
                    }
                    if (args.Count >= 1)
                    {
                        List<int> numArgs = args.Where(i => int.TryParse(i, out int _)).Select(i => int.Parse(i)).Where(i => i > 0).ToList();
                        if (numArgs.Count >= 1)
                        {
                            output = numArgs.GetRange(0, 1);
                            return true;
                        }
                    }
                    break;
                case PositionTypes.radius_widthdepthheight:
                    if (args.Count >= 3)
                    {
                        List<int> numArgs = args.Where(i => int.TryParse(i, out int _)).Select(i => int.Parse(i)).Where(i => i > 0).ToList();
                        if (numArgs.Count >= 3)
                        {
                            output = numArgs.GetRange(0, 3);
                            return true;
                        }
                    }
                    if (args.Count >= 1)
                    {
                        List<int> numArgs = args.Where(i => int.TryParse(i, out int _)).Select(i => int.Parse(i)).Where(i => i > 0).ToList();
                        if (numArgs.Count >= 1)
                        {
                            output = numArgs.GetRange(0, 1);
                            return true;
                        }
                    }
                    break;
            }
            output = null;
            return false;
        }
        public static string CreateProgress(long value, long total, int segments, string colourFill, string colourBackground, char bartype)
        {
            int barsFilled = System.Math.Min((int)System.Math.Floor((double)(value * segments / total)), segments);
            return $"<color={colourFill}>{new string(bartype, barsFilled)}</color><color={colourBackground}>{new string(bartype, segments - barsFilled)}</color>";
        }
        public static int GetSize(Vector3Int posA, Vector3Int posB)
        {
            return (System.Math.Abs(posA.x - posB.x) + 1) * (System.Math.Abs(posA.y - posB.y) + 1) * (System.Math.Abs(posA.z - posB.z) + 1);
        }
        public static Vector3Int GetDirection(Vector3 playerForward, Direction direction)
        {
            Vector3 testVector;
            switch (direction)
            {
                default:
                case Direction.front:
                    testVector = playerForward;
                    break;
                case Direction.back:
                    testVector = -playerForward;
                    break;
                case Direction.right:
                    testVector = -Vector3.Cross(playerForward, Vector3.up);
                    break;
                case Direction.left:
                    testVector = Vector3.Cross(playerForward, Vector3.up);
                    break;
                case Direction.up:
                    return new Vector3Int(0, 1, 0);
                case Direction.down:
                    return new Vector3Int(0, -1, 0);
            }

            float testVectorMax = Pipliz.Math.MaxMagnitude(testVector);
            if (testVectorMax == testVector.x)
            {
                if (testVectorMax >= 0f)
                {
                    return new Vector3Int(1, 0, 0);
                }
                else
                {
                    return new Vector3Int(-1, 0, 0);
                }
            }
            else
            {
                if (testVectorMax >= 0)
                {
                    return new Vector3Int(0, 0, 1);
                }
                else
                {
                    return new Vector3Int(0, 0, -1);
                }
            }
        }

        public static bool TryMatchItemType(string str, out ItemTypes.ItemType type)
        {
            if (ushort.TryParse(str, out ushort idx) && ItemTypes.TryGetType(idx, out type))
            {
                return true;
            }
            if (ItemTypes.IndexLookup.TryGetIndex(str, out idx) && ItemTypes.TryGetType(idx, out type))
            {
                return true;
            }
            type = null;
            return false;
        }

        public static ItemTypes.ItemType MatchItemType(string str)
        {
            ItemTypes.ItemType type;
            if (ushort.TryParse(str, out ushort idx) && ItemTypes.TryGetType(idx, out type))
            {
                return type;
            }

            if (ItemTypes.IndexLookup.TryGetIndex(str, out idx) && ItemTypes.TryGetType(idx, out type))
            {
                return type;
            }

            return null;
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

            switch (args[0])
            {
                case "/toggleeditwand":
                case "//pos1":
                case "//pos2":
                case "//stack":
                case "//undo":
                case "//clearclipboard":
                case "//clearhistory":
                case "//fast":
                case "//limit":
                case "//wand":
                case "//desel":
                case "//chunk":
                case "//expand":
                case "//outset":
                case "//contract":
                case "//count":
                case "//distr":
                case "//hpos1":
                case "//hpos2":
                case "//set":
                case "//replace":
                case "//replacenear":
                case "//walls":
                case "//faces":
                case "//outline":
                case "//hollow":
                case "//cut":
                case "//cyl":
                case "//hcyl":
                case "//sphere":
                case "//hsphere":
                if (!PermissionsManager.CheckAndWarnPermission(player, "worldedit"))
                    {
                        return true;
                    }
                    break;
                default:
                    return false;
            }

            WorldEditInteraction.GetInteraction(player, true, out WorldEditInteraction worldEditInteraction);

            sVoxelPhysics.RayHit.VoxelHit hit;
            Dictionary<ushort, int> blockOccurences;
            Schematic previous;
            ItemTypes.ItemType itemType;
            BasePattern setBlockSelection;
            BasePattern replaceBlockSelection;
            SchematicBlock block;
            List<Vector3Int> blockChanges;
            List<int> positionOutput;
            List<string> positionSplit;

            Vector3Int vectorDirection;
            Vector3Int cornerA;
            Vector3Int cornerB;
            Vector3Int startposition;
            Vector3Int position;
            Vector3 center;
            Vector3 facing;

            Direction direction;

            int blocksDone;
            int blocksProcessed;
            int blocksFailed;
            int blockCount;
            int blockRange;
            int border;

            int distinctItems;
            int occurences;
            int totalSize;

            int width;
            int height;
            int depth;
            int length;

            bool toggled;
            ushort itemID;
            ushort blockID;

            switch (args[0].ToLower())
            {
                case "//debug":
                    BasePattern selection = new RandomPattern(args[1]);
                    blockChanges = Shapes.Shapes.MakePropSphere(new Vector3Int(player.Position), double.Parse(args[2]), bool.Parse(args[3]));
                    worldEditInteraction.ConvertPosToJobs(blockChanges, selection, player, worldEditInteraction.blocksMaxPerUpdate());

                    return true;
                case "//copy":
                    if (!worldEditInteraction.area.ArePositionsSet)
                    {
                        Chat.Send(player, "No positions are set");
                        return true;
                    }
                    if (!Helpers.ConvertPosToSchematic(worldEditInteraction.area.cornerA, worldEditInteraction.area.cornerB, out previous))
                    {
                        Chat.Send(player, "Failed to convert selection to schematic");
                        return true;
                    }

                    previous.playerPos = worldEditInteraction.area.cornerA - new Vector3Int(player.Position);
                    worldEditInteraction.clipboard = previous;
                    Chat.Send(player, "Saved selection to clipboard");

                    return true;
                case "//stack":
                    direction = Direction.front;
                    blockRange = 1;
                    switch (args.Count)
                    {
                        case 1:
                            Chat.Send(player, "Usage: //stack <count> [direction]");
                            return true;
                        case 2:
                            if (!int.TryParse(args[1], out blockRange))
                            {
                                Chat.Send(player, $"'{args[1]}' is not a valid number");
                                return true;
                            }
                            break;
                        case 3:
                            if (!int.TryParse(args[1], out blockRange))
                            {
                                Chat.Send(player, $"'{args[1]}' is not a valid number");
                                return true;
                            }

                            if (!Enum.TryParse(args[2], true, out direction))
                            {
                                Chat.Send(player, $"'{args[2]}' is not a valid direction. Expected front, backwards, left, right, up or down>");
                                return true;
                            }
                            break;
                    }
                    if (!worldEditInteraction.area.ArePositionsSet)
                    {
                        Chat.Send(player, "No positions are set");
                        return true;
                    }
                    if (!Helpers.ConvertPosToSchematic(worldEditInteraction.area.cornerA, worldEditInteraction.area.cornerB, out Schematic stack))
                    {
                        Chat.Send(player, "Failed to convert selection to schematic");
                        return true;
                    }

                    Vector3Int change = new Vector3Int(stack.XMax, stack.YMax, stack.ZMax) - stack.StartPos;
                    Vector3Int sdirection = Helpers.GetDirection(player.Forward, direction);
                    vectorDirection = UnityEngine.Vector3Int.Scale(change + 1, sdirection);

                    startposition = new Vector3Int(0, 0, 0);
                    Vector3Int begStack = worldEditInteraction.area.cornerB + vectorDirection;
                    Vector3Int endStack = worldEditInteraction.area.cornerA + vectorDirection * blockRange;

                    Log.Write("Creating area schematic");
                    if (Helpers.ConvertPosToSchematic(begStack, endStack, out previous))
                    {
                        worldEditInteraction.jobhistory.Push(previous);
                    }
                    Log.Write("Finished schematic");

                    Task.Run(async delegate
                    {
                        blocksDone = 0;
                        blocksProcessed = 0;

                        for (var i = 0; i < blockRange; i++)
                        {
                            startposition += vectorDirection;

                            int x1 = stack.StartPos.x;
                            int y1 = stack.StartPos.y;
                            int z1 = stack.StartPos.z;
                            int x2 = stack.XMax;
                            int y2 = stack.YMax;
                            int z2 = stack.ZMax;

                            for (int x = x2; x >= x1; x--)
                            {
                                for (int y = y2; y >= y1; y--)
                                {
                                    for (int z = z2; z >= z1; z--)
                                    {
                                        Vector3Int inipos = new Vector3Int(x, y, z);
                                        Vector3Int pos = new Vector3Int(x, y, z) + startposition;
                                        block = stack.GetBlock(inipos);

                                        if (World.TryGetTypeAt(pos, out ushort outputID) && outputID == block.ID)
                                        {
                                            continue;
                                        }

                                        worldEditInteraction.AddBlockJob(pos, block.ID, player);
                                        blocksDone++;
                                        blocksProcessed++;
                                        if (blocksProcessed > worldEditInteraction.blocksMaxPerUpdate())
                                        {
                                            Chat.Send(player, $"{Helpers.CreateProgress(blocksDone, stack.Blocks.Count * blockRange, 15, "#3490fa", "white", '█')} <color=#3490fa>Working... {((decimal)blocksDone / stack.Blocks.Count * blockRange).ToString("0.0%")} ({blocksDone} / {stack.Blocks.Count * blockRange})</color>");
                                            await Task.Delay(1000);
                                            blocksProcessed = 0;
                                        }
                                    }
                                }
                            }
                        }

                        Chat.Send(player, $"<color=#3490fa>{blocksDone} blocks have been changed</color>");
                    });

                    return true;
                case "//paste":
                    if (worldEditInteraction.clipboard == null)
                    {
                        Chat.Send(player, "Clipboard is empty");
                        return true;
                    }

                    previous = worldEditInteraction.clipboard;

                    blocksDone = 0;
                    blocksProcessed = 0;

                    Task.Run(async delegate
                    {
                        int x1 = previous.StartPos.x;
                        int y1 = previous.StartPos.y;
                        int z1 = previous.StartPos.z;
                        int x2 = previous.XMax;
                        int y2 = previous.YMax;
                        int z2 = previous.ZMax;

                        Vector3Int start = new Vector3Int(player.Forward.x, 0, player.Forward.z);
                        startposition = new Vector3Int(player.Position);
                        for (int x = x2; x >= x1; x--)
                        {
                            for (int y = y2; y >= y1; y--)
                            {
                                for (int z = z2; z >= z1; z--)
                                {
                                    block = previous.GetBlock(x, y, z);
                                    Vector3Int pos = (new Vector3Int(x - x1, y - y1, z - z1) + start) + previous.playerPos + startposition;

                                    worldEditInteraction.AddBlockJob(pos, block.ID, player);
                                    blocksDone++;
                                    blocksProcessed++;
                                    if (blocksProcessed > worldEditInteraction.blocksMaxPerUpdate())
                                    {
                                        Chat.Send(player, $"{Helpers.CreateProgress(blocksDone, previous.Blocks.Count, 15, "#3490fa", "white", '█')} <color=#3490fa>Working... {((decimal)blocksDone / previous.Blocks.Count).ToString("0.0%")} ({blocksDone} / {previous.Blocks.Count})</color>");
                                        await Task.Delay(1000);
                                        blocksProcessed = 0;
                                    }
                                }
                            }
                        }

                        Chat.Send(player, $"<color=#3490fa>{previous.Blocks.Count} blocks have been changed</color>");
                    });

                    return true;
                case "//undo":
                    int undocount;
                    if (!(args.Count > 1 && int.TryParse(args[1], out undocount)))
                    {
                        undocount = 1;
                    }
                    undocount = Mathf.Clamp(undocount, 1, worldEditInteraction.jobhistory.Count);

                    if (worldEditInteraction.jobhistory.Count == 0)
                    {
                        Chat.Send(player, $"No actions to undo");
                        return true;
                    }

                    Chat.Send(player, $"<color=#3490fa>Undoing {undocount} actions</color>");

                    Task.Run(async delegate
                    {
                        blocksDone = 0;
                        blocksProcessed = 0;
                        for (var i = 0; i < undocount; i++)
                        {
                            previous = worldEditInteraction.jobhistory.Pop();

                            int x1 = previous.StartPos.x;
                            int y1 = previous.StartPos.y;
                            int z1 = previous.StartPos.z;
                            int x2 = previous.XMax;
                            int y2 = previous.YMax;
                            int z2 = previous.ZMax;

                            Dictionary<(int, int, int), SchematicBlock> blocks = new Dictionary<(int, int, int), SchematicBlock>();

                            for (int x = x2; x >= x1; x--)
                            {
                                for (int y = y2; y >= y1; y--)
                                {
                                    for (int z = z2; z >= z1; z--)
                                    {
                                        block = previous.GetBlock(x, y, z);
                                        Vector3Int pos = new Vector3Int(x, y, z);

                                        if (World.TryGetTypeAt(pos, out ushort outputID) && outputID == block.ID)
                                        {
                                            continue;
                                        }

                                        worldEditInteraction.AddBlockJob(pos, block.ID, player);
                                        blocksDone++;
                                        blocksProcessed++;
                                        if (blocksProcessed > worldEditInteraction.blocksMaxPerUpdate())
                                        {
                                            Chat.Send(player, $"{Helpers.CreateProgress(blocksDone, previous.Blocks.Count, 15, "#3490fa", "white", '█')} <color=#3490fa>Working... {((decimal)blocksDone / previous.Blocks.Count).ToString("0.0%")} ({blocksDone} / {previous.Blocks.Count})</color>");
                                            await Task.Delay(1000);
                                            blocksProcessed = 0;
                                        }
                                    }
                                }
                            }
                        }
                    });

                    return true;
                case "//clearclipboard":
                    worldEditInteraction.clipboard = null;
                    Chat.Send(player, $"<color=#3490fa>Cleared clipboard</color>");

                    return true;
                case "//clearhistory":
                    worldEditInteraction.jobhistory.Clear();
                    Chat.Send(player, $"<color=#3490fa>Cleared history</color>");

                    return true;
                case "//fast":
                    if (!(args.Count > 1 && bool.TryParse(args[1], out toggled)))
                    {
                        toggled = !worldEditInteraction.fastMode;
                    }

                    worldEditInteraction.fastMode = toggled;

                    if (toggled)
                    {
                        Chat.Send(player, $"<color=red>Fastmode has been enabled, this may cause instability</color>");
                    }
                    else
                    {
                        Chat.Send(player, $"<color=#3490fa>Fastmode has been disabled</color>");
                    }

                    return true;
                case "//limit":
                    if (args.Count > 1)
                    {
                        if (int.TryParse(args[1], out blockCount))
                        {
                            if (blockCount > 0)
                            {
                                worldEditInteraction.selectionLimit = blockCount;
                                Chat.Send(player, $"<color=#3490fa>Changed selection limit to {worldEditInteraction.selectionLimit}</color>");
                            }
                            else
                            {
                                Chat.Send(player, $"Limits cannot be below 0");
                            }
                        }
                        else
                        {
                            Chat.Send(player, $"'{args[1]}' is not a valid number");
                        }
                    }
                    else
                    {
                        Chat.Send(player, $"<color=#3490fa>Your worldedit region is limited to {worldEditInteraction.selectionLimit} blocks</color>");
                    }

                    return true;
                case "//wand":
                    worldEditInteraction.active = true;
                    if (player.Inventory.TryAdd(BlockTypes.BuiltinBlocks.Indices.bronzeaxe))
                    {
                        Chat.Send(player, "<color=#3490fa>Left click: select position 1; Right click:: select position 2</color>");
                    }
                    else
                    {
                        Chat.Send(player, "Not enough space in inventory to add wand");
                    }

                    return true;
                case "/toggleeditwand":
                    if (!(args.Count >= 2 && bool.TryParse(args[1], out bool value)))
                    {
                        value = !worldEditInteraction.active;
                    }
                    worldEditInteraction.active = value;
                    Chat.Send(player, $"<color=#3490fa>WorldEdit wand has been {(value ? "enabled" : "disabled")}</color>");
                    AreaJobTracker.SendData(player);

                    return true;
                case "//desel":
                    worldEditInteraction.area.posA = Vector3Int.maximum;
                    worldEditInteraction.area.posB = Vector3Int.maximum;
                    Chat.Send(player, $"<color=#3490fa>Deselected selection</color>");
                    AreaJobTracker.SendData(player);

                    return true;
                case "//pos1":
                    worldEditInteraction.area.SetCornerA(player.VoxelPosition, player, true, worldEditInteraction);

                    return true;
                case "//pos2":
                    worldEditInteraction.area.SetCornerB(player.VoxelPosition, player, true, worldEditInteraction);

                    return true;
                case "//hpos1":
                    center = player.VoxelPosition.Vector + new Vector3(0, 1, 0);
                    facing = player.Forward;
                    if (sVoxelPhysics.RayHit.RayCastVoxel(center, facing, 100, sVoxelPhysics.RayHit.RayCastType.Voxels, out hit))
                    {
                        worldEditInteraction.area.SetCornerA(hit.VoxelPositionHit, player, true, worldEditInteraction);
                    }
                    else
                    {
                        Chat.Send(player, "Unable to locate selected block");
                    }

                    return true;
                case "//hpos2":
                    center = player.VoxelPosition.Vector + new Vector3(0, 1, 0);
                    facing = player.Forward;
                    if (sVoxelPhysics.RayHit.RayCastVoxel(center, facing, 100, sVoxelPhysics.RayHit.RayCastType.Voxels, out hit))
                    {
                        worldEditInteraction.area.SetCornerB(hit.VoxelPositionHit, player, true, worldEditInteraction);
                    }
                    else
                    {
                        Chat.Send(player, "Unable to locate selected block");
                    }

                    return true;
                case "//chunk":
                    Vector3Int chunkStart = player.VoxelPosition.ToChunk();
                    Vector3Int chunkEnd = chunkStart + new Vector3Int(15, 15, 15);
                    worldEditInteraction.area.cornerB = Vector3Int.maximum;
                    worldEditInteraction.area.SetCornerA(chunkStart, player, true, worldEditInteraction);
                    worldEditInteraction.area.SetCornerB(chunkEnd, player, true, worldEditInteraction);

                    return true;
                case "//expand":
                    direction = Direction.front;
                    blockRange = 1;

                    switch (args.Count)
                    {
                        case 1:
                            Chat.Send(player, "Usage: //expand <blocks> [direction]");
                            return true;
                        case 2:
                            if (!int.TryParse(args[1], out blockRange))
                            {
                                Chat.Send(player, $"'{args[1]}' is not a valid number");
                                return true;
                            }
                            break;
                        case 3:
                            if (!int.TryParse(args[1], out blockRange))
                            {
                                Chat.Send(player, $"'{args[1]}' is not a valid number");
                                return true;
                            }

                            if (!Enum.TryParse(args[2], true, out direction))
                            {
                                Chat.Send(player, $"'{args[2]}' is not a valid direction. Expected front, backwards, left, right, up or down>");
                                return true;
                            }
                            break;
                    }

                    cornerA = worldEditInteraction.area.cornerA;
                    cornerB = worldEditInteraction.area.cornerB;
                    vectorDirection = Helpers.GetDirection(player.Forward, direction) * blockRange;
                    if (vectorDirection.x == 1 || vectorDirection.y == 1 || vectorDirection.z == 1)
                    {
                        cornerA += vectorDirection;
                    }
                    else
                    {
                        cornerB += vectorDirection;
                    }

                    worldEditInteraction.area.SetCornerA(cornerA, player, false, worldEditInteraction);
                    worldEditInteraction.area.SetCornerB(cornerB, player, false, worldEditInteraction);

                    Chat.Send(player, $"<color=#3490fa>Area expanded by {blockRange} block(s) {direction} [{worldEditInteraction.area.GetXSize()}x{worldEditInteraction.area.GetYSize()}x{worldEditInteraction.area.GetZSize()}] ({worldEditInteraction.area.GetSize()})</color>");

                    return true;
                case "//outset":
                    if (args.Count < 2)
                    {
                        Chat.Send(player, "Usage: //outset <blocks>");
                        return true;
                    }
                    if (!int.TryParse(args[1], out blockRange))
                    {
                        Chat.Send(player, $"'{args[1]}' is not a valid number");
                        return true;
                    }

                    Vector3Int outset = new Vector3Int(blockRange, blockRange, blockRange);
                    cornerA = worldEditInteraction.area.cornerA;
                    cornerB = worldEditInteraction.area.cornerB;
                    cornerA -= outset;
                    cornerB += outset;

                    worldEditInteraction.area.SetCornerA(cornerA, player, false, worldEditInteraction);
                    worldEditInteraction.area.SetCornerB(cornerB, player, false, worldEditInteraction);

                    Chat.Send(player, $"<color=#3490fa>Outset by {blockRange} block(s) [{worldEditInteraction.area.GetXSize()}x{worldEditInteraction.area.GetYSize()}x{worldEditInteraction.area.GetZSize()}] ({worldEditInteraction.area.GetSize()})</color>");

                    return true;
                case "//contract":
                    direction = Direction.front;
                    blockRange = 1;

                    switch (args.Count)
                    {
                        case 1:
                            Chat.Send(player, "Usage: //contract <blocks> [direction]");
                            return true;
                        case 2:
                            if (!int.TryParse(args[1], out blockRange))
                            {
                                Chat.Send(player, $"'{args[1]}' is not a valid number");
                                return true;
                            }
                            break;
                        case 3:
                            if (!int.TryParse(args[1], out blockRange))
                            {
                                Chat.Send(player, $"'{args[1]}' is not a valid number");
                                return true;
                            }

                            if (!Enum.TryParse(args[2], true, out direction))
                            {
                                Chat.Send(player, $"'{args[2]}' is not a valid direction. Expected forwards, backwards, left, right, up or down");
                                return true;
                            }
                            break;
                    }

                    cornerA = worldEditInteraction.area.cornerA;
                    cornerB = worldEditInteraction.area.cornerB;
                    vectorDirection = Helpers.GetDirection(player.Forward, direction) * blockRange;
                    if (vectorDirection.x == 1 || vectorDirection.y == 1 || vectorDirection.z == 1)
                    {
                        cornerB -= vectorDirection;
                    }
                    else
                    {
                        cornerA -= vectorDirection;
                    }

                    worldEditInteraction.area.SetCornerA(cornerA, player, false, worldEditInteraction);
                    worldEditInteraction.area.SetCornerB(cornerB, player, false, worldEditInteraction);

                    Chat.Send(player, $"<color=#3490fa>Area contracted by {blockRange} block(s) {direction} [{worldEditInteraction.area.GetXSize()}x{worldEditInteraction.area.GetYSize()}x{worldEditInteraction.area.GetZSize()}] ({worldEditInteraction.area.GetSize()})</color>");

                    return true;
                case "//count":
                    if (!(args.Count > 1))
                    {
                        Chat.Send(player, "Usage: //count {block}");
                        return true;
                    }
                    if (!Helpers.TryMatchItemType(args[1], out itemType))
                    {
                        Chat.Send(player, $"'{args[1]}' is not a valid item");
                        return true;
                    }
                    if (!worldEditInteraction.area.ArePositionsSet)
                    {
                        Chat.Send(player, "No positions are set");
                        return true;
                    }

                    itemID = itemType.ItemIndex;
                    cornerA = worldEditInteraction.area.cornerA;
                    cornerB = worldEditInteraction.area.cornerB;
                    totalSize = Helpers.GetSize(cornerA, cornerB);

                    blocksProcessed = 0;
                    blocksDone = 0;
                    occurences = 0;

                    Task.Run(async delegate
                    {
                        for (int x = cornerB.x; x >= cornerA.x; x--)
                        {
                            for (int y = cornerB.y; y >= cornerA.y; y--)
                            {
                                for (int z = cornerB.z; z >= cornerA.z; z--)
                                {
                                    Vector3Int pos = new Vector3Int(x, y, z);
                                    if (World.TryGetTypeAt(pos, out blockID) && itemID == blockID)
                                    {
                                        occurences++;
                                    }
                                    blocksDone++;
                                    blocksProcessed++;
                                    if (blocksProcessed > worldEditInteraction.blocksMaxPerUpdate())
                                    {
                                        Chat.Send(player, $"{Helpers.CreateProgress(blocksDone, totalSize, 15, "#3490fa", "white", '█')} <color=#3490fa>Working... {((decimal)blocksDone / totalSize).ToString("0.0%")} ({blocksDone} / {totalSize})</color>");
                                        await Task.Delay(1000);
                                        blocksProcessed = 0;
                                    }
                                }
                            }
                        }

                        Chat.Send(player, $"<color=#3490fa>There are {occurences} occurences of {itemType.Name} in this selection</color>");
                    });

                    return true;
                case "//distr":
                    if (!worldEditInteraction.area.ArePositionsSet)
                    {
                        Chat.Send(player, "No positions are set");
                        return true;
                    }

                    blockOccurences = new Dictionary<ushort, int>();
                    cornerA = worldEditInteraction.area.cornerA;
                    cornerB = worldEditInteraction.area.cornerB;
                    totalSize = Helpers.GetSize(cornerA, cornerB);

                    blocksProcessed = 0;
                    blocksFailed = 0;
                    blocksDone = 0;

                    Task.Run(async delegate
                    {
                        for (int x = cornerB.x; x >= cornerA.x; x--)
                        {
                            for (int y = cornerB.y; y >= cornerA.y; y--)
                            {
                                for (int z = cornerB.z; z >= cornerA.z; z--)
                                {
                                    position = new Vector3Int(x, y, z);
                                    if (World.TryGetTypeAt(position, out itemID))
                                    {
                                        if (!blockOccurences.ContainsKey(itemID))
                                        {
                                            blockOccurences.Add(itemID, 0);
                                        }
                                        blockOccurences[itemID]++;
                                    }
                                    else
                                    {
                                        blocksFailed++;
                                    }
                                    blocksDone++;
                                    if (itemID == BlockTypes.BuiltinBlocks.Indices.air)
                                    {
                                        continue;
                                    }

                                    blocksProcessed++;
                                    if (blocksProcessed > worldEditInteraction.blocksMaxPerUpdate())
                                    {
                                        Chat.Send(player, $"{Helpers.CreateProgress(blocksDone, totalSize, 15, "#3490fa", "white", '█')} <color=#3490fa>Working... {((decimal)blocksDone / totalSize).ToString("0.0%")} ({blocksDone} / {totalSize})</color>");
                                        await Task.Delay(1000);
                                        blocksProcessed = 0;
                                    }
                                }
                            }
                        }

                        List<KeyValuePair<ushort, int>> sorted = blockOccurences.ToList();
                        sorted.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
                        foreach (KeyValuePair<ushort, int> blockPair in sorted)
                        {
                            ItemTypes.TryGetType(blockPair.Key, out itemType);
                            Chat.Send(player, $"{itemType.Name} ({blockPair.Value}) - {((decimal)blockPair.Value / (blocksDone - blocksFailed)).ToString("0.0%")}");
                        }
                        Chat.Send(player, $"<color=#3490fa>Summary of {blocksDone - blocksFailed} blocks, {blocksFailed} failed</color>");
                    });

                    return true;
                case "//set":
                    if (!worldEditInteraction.area.ArePositionsSet)
                    {
                        Chat.Send(player, "No positions are set");
                        return true;
                    }
                    if (args.Count < 2)
                    {
                        Chat.Send(player, "Usage: //set {block}");
                        return true;
                    }
                    setBlockSelection = new RandomPattern(args[1]);
                    if (setBlockSelection.patternBlocks.Count == 0)
                    {
                        Chat.Send(player, "No valid blocks specified");
                        return true;
                    }

                    blockChanges = new List<Vector3Int>();
                    cornerA = worldEditInteraction.area.cornerA;
                    cornerB = worldEditInteraction.area.cornerB;
                    totalSize = Helpers.GetSize(cornerA, cornerB);

                    blocksProcessed = 0;
                    blocksFailed = 0;
                    blocksDone = 0;

                    distinctItems = setBlockSelection.patternBlocks.Distinct().Count();

                    Log.Write("Creating area schematic");
                    if (Helpers.ConvertPosToSchematic(worldEditInteraction.area.cornerA, worldEditInteraction.area.cornerB, out previous))
                    {
                        worldEditInteraction.jobhistory.Push(previous);
                    }
                    Log.Write("Finished schematic");

                    Task.Run(async delegate
                    {
                        for (int x = cornerB.x; x >= cornerA.x; x--)
                        {
                            for (int y = cornerB.y; y >= cornerA.y; y--)
                            {
                                for (int z = cornerB.z; z >= cornerA.z; z--)
                                {
                                    position = new Vector3Int(x, y, z);
                                    if (distinctItems == 1 && World.TryGetTypeAt(position, out itemType) && setBlockSelection.patternBlocks.Contains(itemType.ItemIndex))
                                    {
                                        continue;
                                    }

                                    blockChanges.Add(position);
                                    blocksDone++;
                                    blocksProcessed++;
                                    if (blocksProcessed > worldEditInteraction.blocksMaxPerUpdate())
                                    {
                                        Chat.Send(player, $"{Helpers.CreateProgress(blocksDone, totalSize, 15, "#3490fa", "white", '█')} <color=#3490fa>Working... {((decimal)blocksDone / totalSize).ToString("0.0%")} ({blocksDone} / {totalSize})</color>");
                                        await Task.Delay(1000);
                                        blocksProcessed = 0;
                                    }
                                }
                            }
                        }

                        blockChanges = blockChanges.Distinct().ToList();
                        worldEditInteraction.ConvertPosToJobs(blockChanges, setBlockSelection, player, worldEditInteraction.blocksMaxPerUpdate());
                        Chat.Send(player, $"<color=#3490fa>{blockChanges.Count} blocks have been changed</color>");
                    });

                    return true;
                case "//replace":
                    if (!worldEditInteraction.area.ArePositionsSet)
                    {
                        Chat.Send(player, "No positions are set");
                        return true;
                    }
                    if (args.Count < 3)
                    {
                        Chat.Send(player, "Usage: //replace {from} {to}");
                        return true;
                    }
                    setBlockSelection = new RandomPattern(args[1]);
                    if (setBlockSelection.patternBlocks.Count == 0)
                    {
                        Chat.Send(player, "No valid from blocks specified");
                        return true;
                    }
                    replaceBlockSelection = new RandomPattern(args[2]);
                    if (setBlockSelection.patternBlocks.Count == 0)
                    {
                        Chat.Send(player, "No valid to blocks specified");
                        return true;
                    }

                    blockChanges = new List<Vector3Int>();
                    cornerA = worldEditInteraction.area.cornerA;
                    cornerB = worldEditInteraction.area.cornerB;
                    totalSize = Helpers.GetSize(cornerA, cornerB);

                    blocksProcessed = 0;
                    blocksFailed = 0;
                    blocksDone = 0;

                    Log.Write("Creating area schematic");
                    if (Helpers.ConvertPosToSchematic(worldEditInteraction.area.cornerA, worldEditInteraction.area.cornerB, out previous))
                    {
                        worldEditInteraction.jobhistory.Push(previous);
                    }
                    Log.Write("Finished schematic");

                    Task.Run(async delegate
                    {
                        for (int x = cornerB.x; x >= cornerA.x; x--)
                        {
                            for (int y = cornerB.y; y >= cornerA.y; y--)
                            {
                                for (int z = cornerB.z; z >= cornerA.z; z--)
                                {
                                    position = new Vector3Int(x, y, z);
                                    itemType = null;
                                    if (World.TryGetTypeAt(position, out itemType))
                                    {
                                        if (setBlockSelection.patternBlocks.Contains(itemType.ItemIndex))
                                        {
                                            blockChanges.Add(position);
                                        }
                                    }

                                    blocksDone++;
                                    blocksProcessed++;
                                    if (blocksProcessed > worldEditInteraction.blocksMaxPerUpdate())
                                    {
                                        Chat.Send(player, $"{Helpers.CreateProgress(blocksDone, totalSize, 15, "#3490fa", "white", '█')} <color=#3490fa>Working... {((decimal)blocksDone / totalSize).ToString("0.0%")} ({blocksDone} / {totalSize})</color>");
                                        await Task.Delay(1000);
                                        blocksProcessed = 0;
                                    }
                                }
                            }
                        }

                        blockChanges = blockChanges.Distinct().ToList();
                        worldEditInteraction.ConvertPosToJobs(blockChanges, replaceBlockSelection, player, worldEditInteraction.blocksMaxPerUpdate());
                        Chat.Send(player, $"<color=#3490fa>{blockChanges.Count} blocks have been changed</color>");
                    });

                    return true;
                case "//replacenear":
                    if (args.Count < 4)
                    {
                        Chat.Send(player, "Usage: //replacenear <radius> <from> <to>");
                        return true;
                    }

                    setBlockSelection = new RandomPattern(args[2]);
                    if (setBlockSelection.patternBlocks.Count == 0)
                    {
                        Chat.Send(player, "No valid from blocks specified");
                        return true;
                    }
                    replaceBlockSelection = new RandomPattern(args[3]);
                    if (replaceBlockSelection.patternBlocks.Count == 0)
                    {
                        Chat.Send(player, "No valid to blocks specified");
                        return true;
                    }
                    if (!int.TryParse(args[1], out length))
                    {
                        Chat.Send(player, $"{args[1]} is not a valid number");
                        return true;
                    }
                    width = length;

                    blockChanges = new List<Vector3Int>();
                    cornerB = worldEditInteraction.area.cornerB;

                    blocksProcessed = 0;
                    blocksFailed = 0;
                    blocksDone = 0;

                    Log.Write("Creating area schematic");
                    if (Helpers.ConvertPosToSchematic(player.VoxelPosition - new Vector3Int(width, width, width), player.VoxelPosition + new Vector3Int(width, width, width), out previous))
                    {
                        worldEditInteraction.jobhistory.Push(previous);
                    }
                    Log.Write("Finished schematic");

                    distinctItems = setBlockSelection.patternBlocks.Distinct().Count();
                    List<Vector3Int> blockOutput = Shapes.Shapes.MakeSphere(player.VoxelPosition, width, width, width, true);

                    Task.Run(async delegate
                    {
                        foreach (Vector3Int blockPosition in blockOutput)
                        {
                            if (World.TryGetTypeAt(blockPosition, out itemType))
                            {
                                if (setBlockSelection.patternBlocks.Contains(itemType.ItemIndex))
                                {
                                    blockChanges.Add(blockPosition);
                                }
                            }

                            blocksDone++;
                            blocksProcessed++;
                            if (blocksProcessed > worldEditInteraction.blocksMaxPerUpdate())
                            {
                                Chat.Send(player, $"{Helpers.CreateProgress(blocksDone, blockOutput.Count, 15, "#3490fa", "white", '█')} <color=#3490fa>Working... {((decimal)blocksDone / blockOutput.Count).ToString("0.0%")} ({blocksDone} / {blockOutput.Count})</color>");
                                await Task.Delay(1000);
                                blocksProcessed = 0;
                            }
                        }

                        blockChanges = blockChanges.Distinct().ToList();
                        worldEditInteraction.ConvertPosToJobs(blockChanges, replaceBlockSelection, player, worldEditInteraction.blocksMaxPerUpdate());
                        Chat.Send(player, $"<color=#3490fa>{blockOutput.Count} blocks have been changed</color>");
                    });

                    return true;
                case "//walls":
                    if (!worldEditInteraction.area.ArePositionsSet)
                    {
                        Chat.Send(player, "No positions are set");
                        return true;
                    }
                    if (args.Count < 2)
                    {
                        Chat.Send(player, "Usage: //walls {block}");
                        return true;
                    }
                    setBlockSelection = new RandomPattern(args[1]);
                    if (setBlockSelection.patternBlocks.Count == 0)
                    {
                        Chat.Send(player, ">No valid blocks specified");
                        return true;
                    }

                    blockChanges = new List<Vector3Int>();
                    cornerA = worldEditInteraction.area.cornerA;
                    cornerB = worldEditInteraction.area.cornerB;
                    totalSize = Helpers.GetSize(cornerA, cornerB);

                    blocksProcessed = 0;
                    blocksFailed = 0;
                    blocksDone = 0;

                    distinctItems = setBlockSelection.patternBlocks.Distinct().Count();

                    Log.Write("Creating area schematic");
                    if (Helpers.ConvertPosToSchematic(worldEditInteraction.area.cornerA, worldEditInteraction.area.cornerB, out previous))
                    {
                        worldEditInteraction.jobhistory.Push(previous);
                    }
                    Log.Write("Finished schematic");

                    blockChanges = Shapes.Shapes.MakeWalls(worldEditInteraction.area, 1);
                    worldEditInteraction.ConvertPosToJobs(blockChanges, setBlockSelection, player, worldEditInteraction.blocksMaxPerUpdate());
                    Chat.Send(player, $"<color=#3490fa>{blockChanges.Count} blocks have been changed</color>");

                    return true;
                case "//faces":
                case "//outline":
                    if (!worldEditInteraction.area.ArePositionsSet)
                    {
                        Chat.Send(player, "No positions are set");
                        return true;
                    }
                    if (args.Count < 2)
                    {
                        Chat.Send(player, "Usage: //faces {block}");
                        return true;
                    }
                    setBlockSelection = new RandomPattern(args[1]);
                    if (setBlockSelection.patternBlocks.Count == 0)
                    {
                        Chat.Send(player, "No valid blocks specified");
                        return true;
                    }

                    blockChanges = new List<Vector3Int>();
                    cornerA = worldEditInteraction.area.cornerA;
                    cornerB = worldEditInteraction.area.cornerB;
                    totalSize = Helpers.GetSize(cornerA, cornerB);

                    Log.Write("Creating area schematic");
                    if (Helpers.ConvertPosToSchematic(worldEditInteraction.area.cornerA, worldEditInteraction.area.cornerB, out previous))
                    {
                        worldEditInteraction.jobhistory.Push(previous);
                    }
                    Log.Write("Finished schematic");

                    blocksProcessed = 0;
                    blocksFailed = 0;
                    blocksDone = 0;

                    distinctItems = setBlockSelection.patternBlocks.Distinct().Count();

                    blockChanges = Shapes.Shapes.MakeOutline(worldEditInteraction.area, 0);
                    worldEditInteraction.ConvertPosToJobs(blockChanges, setBlockSelection, player, worldEditInteraction.blocksMaxPerUpdate());
                    Chat.Send(player, $"<color=#3490fa>{blockChanges.Count} blocks have been changed</color>");

                    return true;
                case "//hollow":
                    if (!worldEditInteraction.area.ArePositionsSet)
                    {
                        Chat.Send(player, "No positions are set");
                        return true;
                    }
                    if (args.Count < 2)
                    {
                        Chat.Send(player, "Usage: //hollow [thickness]");
                        return true;
                    }
                    if (int.TryParse(args[1], out border))
                    {
                        if (border < 1)
                        {
                            Chat.Send(player, "Thickness cannot be smaller than 1");
                            return true;
                        }
                    }
                    else
                    {
                        Chat.Send(player, $"Invalid thickness '{args[1]}'");
                        return true;
                    }
                    setBlockSelection = new StaticPattern(BlockTypes.BuiltinBlocks.Indices.air);

                    blockChanges = new List<Vector3Int>();
                    cornerA = worldEditInteraction.area.cornerA;
                    cornerB = worldEditInteraction.area.cornerB;
                    totalSize = Helpers.GetSize(cornerA, cornerB);

                    Log.Write("Creating area schematic");
                    if (Helpers.ConvertPosToSchematic(worldEditInteraction.area.cornerA, worldEditInteraction.area.cornerB, out previous))
                    {
                        worldEditInteraction.jobhistory.Push(previous);
                    }
                    Log.Write("Finished schematic");

                    blocksProcessed = 0;
                    blocksFailed = 0;
                    blocksDone = 0;

                    distinctItems = setBlockSelection.patternBlocks.Distinct().Count();

                    blockChanges = Shapes.Shapes.MakeCuboid(worldEditInteraction.area, border, true);
                    worldEditInteraction.ConvertPosToJobs(blockChanges, setBlockSelection, player, worldEditInteraction.blocksMaxPerUpdate());
                    Chat.Send(player, $"<color=#3490fa>{blockChanges.Count} blocks have been changed</color>");

                    return true;
                case "//cut":
                    if (!worldEditInteraction.area.ArePositionsSet)
                    {
                        Chat.Send(player, "No positions are set");
                        return true;
                    }
                    setBlockSelection = new StaticPattern(BlockTypes.BuiltinBlocks.Indices.air);

                    blockChanges = new List<Vector3Int>();
                    cornerA = worldEditInteraction.area.cornerA;
                    cornerB = worldEditInteraction.area.cornerB;
                    totalSize = Helpers.GetSize(cornerA, cornerB);

                    Log.Write("Creating area schematic");
                    if (Helpers.ConvertPosToSchematic(worldEditInteraction.area.cornerA, worldEditInteraction.area.cornerB, out previous))
                    {
                        worldEditInteraction.jobhistory.Push(previous);
                    }
                    Log.Write("Finished schematic");

                    blocksProcessed = 0;
                    blocksFailed = 0;
                    blocksDone = 0;

                    Task.Run(async delegate
                    {
                        for (int x = cornerB.x; x >= cornerA.x; x--)
                        {
                            for (int y = cornerB.y; y >= cornerA.y; y--)
                            {
                                for (int z = cornerB.z; z >= cornerA.z; z--)
                                {
                                    position = new Vector3Int(x, y, z);
                                    if (World.TryGetTypeAt(position, out itemType) && BlockTypes.BuiltinBlocks.Indices.air == itemType.ItemIndex)
                                    {
                                        continue;
                                    }

                                    blockChanges.Add(position);
                                    blocksDone++;
                                    blocksProcessed++;
                                    if (blocksProcessed > worldEditInteraction.blocksMaxPerUpdate())
                                    {
                                        Chat.Send(player, $"{Helpers.CreateProgress(blocksDone, totalSize, 15, "#3490fa", "white", '█')} <color=#3490fa>Working... {((decimal)blocksDone / totalSize).ToString("0.0%")} ({blocksDone} / {totalSize})</color>");
                                        await Task.Delay(1000);
                                        blocksProcessed = 0;
                                    }
                                }
                            }
                        }

                        blockChanges = blockChanges.Distinct().ToList();
                        worldEditInteraction.ConvertPosToJobs(blockChanges, setBlockSelection, player, worldEditInteraction.blocksMaxPerUpdate());
                        Chat.Send(player, $"<color=#3490fa>{blockChanges.Count} blocks have been changed</color>");
                    });

                    return true;
                case "//line": // Not currently working
                    if (!worldEditInteraction.area.ArePositionsSet)
                    {
                        Chat.Send(player, "No positions are set");
                        return true;
                    }
                    if (args.Count < 2)
                    {
                        Chat.Send(player, "Usage: //line {block}");
                        return true;
                    }
                    setBlockSelection = new RandomPattern(args[1]);
                    if (setBlockSelection.patternBlocks.Count == 0)
                    {
                        Chat.Send(player, "No valid blocks specified");
                        return true;
                    }

                    blockChanges = new List<Vector3Int>();
                    cornerA = worldEditInteraction.area.cornerA;
                    cornerB = worldEditInteraction.area.cornerB;
                    totalSize = Helpers.GetSize(cornerA, cornerB);

                    Log.Write("Creating area schematic");
                    if (Helpers.ConvertPosToSchematic(worldEditInteraction.area.cornerA, worldEditInteraction.area.cornerB, out previous))
                    {
                        worldEditInteraction.jobhistory.Push(previous);
                    }
                    Log.Write("Finished schematic");

                    blocksProcessed = 0;
                    blocksFailed = 0;
                    blocksDone = 0;

                    distinctItems = setBlockSelection.patternBlocks.Distinct().Count();

                    blockChanges = Shapes.Shapes.MakeLine(worldEditInteraction.area.cornerA, worldEditInteraction.area.cornerB);
                    worldEditInteraction.ConvertPosToJobs(blockChanges, setBlockSelection, player, worldEditInteraction.blocksMaxPerUpdate());
                    Chat.Send(player, $"<color=#3490fa>{blockChanges.Count} blocks have been changed</color>");

                    return true;
                case "//cyl":
                    if (args.Count < 3)
                    {
                        Chat.Send(player, "Usage: //cyl <pattern> <radius|x,z> [height]");
                        return true;
                    }

                    height = 1;
                    if (args.Count >= 4)
                    {
                        int.TryParse(args[3], out height);
                    }

                    setBlockSelection = new RandomPattern(args[1]);
                    if (setBlockSelection.patternBlocks.Count == 0)
                    {
                        Chat.Send(player, "No valid blocks specified");
                        return true;
                    }

                    positionSplit = args[2].Split(',').ToList();
                    if (positionSplit.Count == 1)
                    {
                        if (!int.TryParse(positionSplit[0], out length))
                        {
                            Chat.Send(player, $"{positionSplit[0]} is not a valid number");
                            return true;
                        }
                        width = length;
                        depth = length;
                    }
                    else
                    {
                        if (!Helpers.PositionParser(PositionTypes.radius_widthdepth, positionSplit, out positionOutput))
                        {
                            Chat.Send(player, $"{args[2]} is not a valid");
                            return true;
                        }
                        width = positionOutput[0];
                        depth = positionOutput[1];
                    }

                    blockChanges = new List<Vector3Int>();
                    cornerB = worldEditInteraction.area.cornerB;

                    Log.Write("Creating area schematic");
                    if (Helpers.ConvertPosToSchematic(player.VoxelPosition - new Vector3Int(width, height, depth), player.VoxelPosition + new Vector3Int(width, height, depth), out previous))
                    {
                        worldEditInteraction.jobhistory.Push(previous);
                    }
                    Log.Write("Finished schematic");

                    distinctItems = setBlockSelection.patternBlocks.Distinct().Count();

                    blockChanges = Shapes.Shapes.MakeCylinder(player.VoxelPosition, width, depth, height, true);
                    worldEditInteraction.ConvertPosToJobs(blockChanges, setBlockSelection, player, worldEditInteraction.blocksMaxPerUpdate());
                    Chat.Send(player, $"<color=#3490fa>{blockChanges.Count} blocks have been changed</color>");

                    return true;
                case "//hcyl":
                    if (args.Count < 3)
                    {
                        Chat.Send(player, "Usage: //hcyl <pattern> <radius|x,z> [height]");
                        return true;
                    }

                    height = 1;
                    if (args.Count >= 4)
                    {
                        int.TryParse(args[3], out height);
                    }

                    setBlockSelection = new RandomPattern(args[1]);
                    if (setBlockSelection.patternBlocks.Count == 0)
                    {
                        Chat.Send(player, "No valid blocks specified");
                        return true;
                    }

                    positionSplit = args[2].Split(',').ToList();
                    if (positionSplit.Count == 1)
                    {
                        if (!int.TryParse(positionSplit[0], out length))
                        {
                            Chat.Send(player, $"{positionSplit[0]} is not a valid number");
                            return true;
                        }
                        width = length;
                        depth = length;
                    }
                    else
                    {
                        if (!Helpers.PositionParser(PositionTypes.radius_widthdepth, positionSplit, out positionOutput))
                        {
                            Chat.Send(player, $"{args[2]} is not a valid");
                            return true;
                        }
                        width = positionOutput[0];
                        depth = positionOutput[1];
                    }

                    blockChanges = new List<Vector3Int>();
                    cornerB = worldEditInteraction.area.cornerB;

                    Log.Write("Creating area schematic");
                    if (Helpers.ConvertPosToSchematic(player.VoxelPosition - new Vector3Int(width, height, depth), player.VoxelPosition + new Vector3Int(width, height, depth), out previous))
                    {
                        worldEditInteraction.jobhistory.Push(previous);
                    }
                    Log.Write("Finished schematic");

                    distinctItems = setBlockSelection.patternBlocks.Distinct().Count();

                    blockChanges = Shapes.Shapes.MakeCylinder(player.VoxelPosition, width, depth, height, false);
                    worldEditInteraction.ConvertPosToJobs(blockChanges, setBlockSelection, player, worldEditInteraction.blocksMaxPerUpdate());
                    Chat.Send(player, $"<color=#3490fa>{blockChanges.Count} blocks have been changed</color>");

                    return true;
                case "//sphere":
                    if (args.Count < 3)
                    {
                        Chat.Send(player, "Usage: //sphere <pattern> <radius|x,y,z>");
                        return true;
                    }

                    setBlockSelection = new RandomPattern(args[1]);
                    if (setBlockSelection.patternBlocks.Count == 0)
                    {
                        Chat.Send(player, "No valid blocks specified");
                        return true;
                    }

                    positionSplit = args[2].Split(',').ToList();
                    if (positionSplit.Count == 1)
                    {
                        if (!int.TryParse(positionSplit[0], out length))
                        {
                            Chat.Send(player, $"{positionSplit[0]} is not a valid number");
                            return true;
                        }
                        width = length;
                        depth = length;
                        height = length;
                    }
                    else
                    {
                        if (!Helpers.PositionParser(PositionTypes.radius_widthdepthheight, positionSplit, out positionOutput))
                        {
                            Chat.Send(player, $"{args[2]} is not a valid");
                            return true;
                        }
                        if (positionOutput.Count == 3)
                        {
                            width = positionOutput[0];
                            depth = positionOutput[1];
                            height = positionOutput[2];
                        }
                        else
                        {
                            width = positionOutput[0];
                            depth = positionOutput[0];
                            height = positionOutput[0];

                        }
                    }

                    blockChanges = new List<Vector3Int>();
                    cornerB = worldEditInteraction.area.cornerB;

                    Log.Write("Creating area schematic");
                    if (Helpers.ConvertPosToSchematic(player.VoxelPosition - new Vector3Int(width, height, depth), player.VoxelPosition + new Vector3Int(width, height, depth), out previous))
                    {
                        worldEditInteraction.jobhistory.Push(previous);
                    }
                    Log.Write("Finished schematic");

                    distinctItems = setBlockSelection.patternBlocks.Distinct().Count();

                    blockChanges = Shapes.Shapes.MakeSphere(player.VoxelPosition, width, depth, height, true);
                    worldEditInteraction.ConvertPosToJobs(blockChanges, setBlockSelection, player, worldEditInteraction.blocksMaxPerUpdate());
                    Chat.Send(player, $"<color=#3490fa>{blockChanges.Count} blocks have been changed</color>");

                    return true;
                case "//hsphere":
                    if (args.Count < 3)
                    {
                        Chat.Send(player, "Usage: //sphere <pattern> <radius|x,y,z>");
                        return true;
                    }

                    setBlockSelection = new RandomPattern(args[1]);
                    if (setBlockSelection.patternBlocks.Count == 0)
                    {
                        Chat.Send(player, "No valid blocks specified");
                        return true;
                    }

                    positionSplit = args[2].Split(',').ToList();
                    if (positionSplit.Count == 1)
                    {
                        if (!int.TryParse(positionSplit[0], out length))
                        {
                            Chat.Send(player, $"{positionSplit[0]} is not a valid number");
                            return true;
                        }
                        width = length;
                        depth = length;
                        height = length;
                    }
                    else
                    {
                        if (!Helpers.PositionParser(PositionTypes.radius_widthdepthheight, positionSplit, out positionOutput))
                        {
                            Chat.Send(player, $"{args[2]} is not a valid x,y,z collection");
                            return true;
                        }
                        width = positionOutput[0];
                        depth = positionOutput[1];
                        height = positionOutput[2];
                    }

                    blockChanges = new List<Vector3Int>();
                    cornerB = worldEditInteraction.area.cornerB;

                    Log.Write("Creating area schematic");
                    if (Helpers.ConvertPosToSchematic(player.VoxelPosition - new Vector3Int(width, height, depth), player.VoxelPosition + new Vector3Int(width, height, depth), out previous))
                    {
                        worldEditInteraction.jobhistory.Push(previous);
                    }
                    Log.Write("Finished schematic");

                    distinctItems = setBlockSelection.patternBlocks.Distinct().Count();

                    blockChanges = Shapes.Shapes.MakeSphere(player.VoxelPosition, width, depth, height, false);
                    worldEditInteraction.ConvertPosToJobs(blockChanges, setBlockSelection, player, worldEditInteraction.blocksMaxPerUpdate());
                    Chat.Send(player, $"<color=#3490fa>{blockChanges.Count} blocks have been changed</color>");

                    return true;
            }

            return false;
        }
    }
}