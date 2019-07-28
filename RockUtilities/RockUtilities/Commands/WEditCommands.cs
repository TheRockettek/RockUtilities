using Chatting;
using Pipliz;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace RockUtils.Commands
{

    [ModLoader.ModManager]
    public class WEditManager
    {
        public static Dictionary<NetworkID, Dictionary<UnityEngine.Vector3Int, UnityEngine.Vector3Int>> selections = new Dictionary<NetworkID, Dictionary<UnityEngine.Vector3Int, UnityEngine.Vector3Int>>();

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, "RockUtils.RockUtils.OnPlayerClicked")]
        private static void HandleTools(Players.Player player, Shared.PlayerClickedData click)
        {
            if (click.TypeSelected == BlockTypes.BuiltinBlocks.Indices.bronzeaxe && click.HitType == Shared.PlayerClickedData.EHitType.Block)
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

                //if (click.ClickType == Shared.PlayerClickedData.EClickType.Left)
                UnityEngine.Vector3Int blockPosition = voxel.BlockHit;
                Chat.Send(player, $"Position: [{blockPosition.x}, {blockPosition.y}, {blockPosition.z}] Hit: {click.HitType} ClickType: {click.ClickType} Selected: {click.TypeSelected}");
            }
        }
    }
}