namespace PrimitiveSurvival.ModSystem
{
    //using System;
    using Vintagestory.API.Common;
    //using Vintagestory.API.MathTools;
    //using System.Diagnostics;

    public class ItemFuse : Item
    {

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefaultAction;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null || byEntity == null)
            { return; }
            var world = byEntity.World;
            if (world == null)
            { return; }
            var block = world.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
            var face = blockSel.Face.ToString();

            //this will need to change if I add more fuse types!
            var blockNew = world.GetBlock(new AssetLocation("primitivesurvival:bfuse-blackmatch-empty"));

            if (face == "down")
            {
                var blockSelBelow = blockSel.Clone();
                blockSelBelow.Position.Y -= 1;
                var blockAbove = world.BlockAccessor.GetBlock(blockSelBelow.Position, BlockLayersAccess.Default);
                if (blockAbove.BlockId == 0 || blockAbove.Code.Path.Contains("tallgrass-") || block.Code.Path.Contains("tallgrass-"))
                {
                    var blockAccessor = world.BlockAccessor;
                    if (block.Code.Path.Contains("tallgrass-"))
                    {
                        blockAccessor.SetBlock(blockNew.BlockId, blockSel.Position);
                        blockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
                    }
                    else
                    {
                        blockAccessor.SetBlock(blockNew.BlockId, blockSelBelow.Position);
                        blockAccessor.TriggerNeighbourBlockUpdate(blockSelBelow.Position);
                    }
                    slot.TakeOut(1);
                    slot.MarkDirty();
                }
            }
            else if (face == "up")
            {
                if (block.FirstCodePart() == "bfuse") //prevent fuse on top for now
                { return; }
                var blockSelAbove = blockSel.Clone();
                blockSelAbove.Position.Y += 1;
                var blockAbove = world.BlockAccessor.GetBlock(blockSelAbove.Position, BlockLayersAccess.Default);
                if (blockAbove.BlockId == 0 || blockAbove.Code.Path.Contains("tallgrass-") || block.Code.Path.Contains("tallgrass-"))
                {
                    var blockAccessor = world.BlockAccessor;
                    var posFinal = blockSelAbove.Position;
                    var neib = blockSelAbove.Clone();
                    if (block.Code.Path.Contains("tallgrass-"))
                    {
                        posFinal = blockSel.Position;
                        neib = blockSel.Clone();
                    }
                    blockAccessor.SetBlock(blockNew.BlockId, posFinal);
                    blockAccessor.TriggerNeighbourBlockUpdate(posFinal);

                    //update fuse next to firework
                    neib.Position.Z += 1;
                    var testBlock = world.BlockAccessor.GetBlock(neib.Position, BlockLayersAccess.Default);
                    blockAccessor.TriggerNeighbourBlockUpdate(neib.Position);

                    slot.TakeOut(1);
                    slot.MarkDirty();
                }
            }
            else if (block.Code.Path.Contains("tallgrass-")) //side of grass
            {
                var blockAccessor = world.BlockAccessor;
                blockAccessor.SetBlock(blockNew.BlockId, blockSel.Position);
                blockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);

                //update fuse next to firework
                var neib = blockSel.Clone();
                neib.Position.Z += 1;
                var testBlock = world.BlockAccessor.GetBlock(neib.Position, BlockLayersAccess.Default);
                blockAccessor.TriggerNeighbourBlockUpdate(neib.Position);

                slot.TakeOut(1);
                slot.MarkDirty();
            }
        }
    }
}
