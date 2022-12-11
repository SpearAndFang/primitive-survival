namespace PrimitiveSurvival.ModSystem
{
    using System.Linq;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Config;
    using Vintagestory.API.Client;
    using Vintagestory.API.Server;
    using Vintagestory.API.Util;
    //using System.Diagnostics;


    public class BlockFurrowedLand : Block
    {
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if (world.BlockAccessor.GetBlockEntity(selection.Position) is BEFurrowedLand be)
            {
                if (!be.OtherSlot.Empty)
                {
                    return new WorldInteraction[]
                    {
                        new WorldInteraction()
                        {
                            ActionLangCode = "primitivesurvival:blockhelp-furrowedland-removedebris",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = null
                        }
                    }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
                }
            }
            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEFurrowedLand be)
            {
                var result = be.OnInteract(byPlayer, blockSel);
                if (result)
                {
                    if (world.Api.Side == EnumAppSide.Server)
                    {
                        if (!be.OtherSlot.Empty)
                        {
                            (byPlayer as IServerPlayer)?.SendIngameError("debris-error", Lang.Get("primitivesurvival:ingameerror-debris"));
                        }
                    }
                }
                return result;

            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public BlockPos[] AreaAround(BlockPos pos)
        {
            return new BlockPos[]
            {  pos.WestCopy(), pos.SouthCopy(), pos.EastCopy(), pos.NorthCopy() };
        }

        public bool WaterAbove(BlockPos pos)
        {
            var upPos = pos.UpCopy();
            var blockChk = this.api.World.BlockAccessor.GetBlock(upPos, BlockLayersAccess.Default);
            if (blockChk.Code.Path.Contains("water-"))
            { return true; }
            return false;
        }

        public bool FullWaterBlock(Block block)
        {
            if (block.FirstCodePart() == "water" && block.LastCodePart() == "7")
            { return true; }
            return false;
        }



        public void FillPlacedBlock(IWorldAccessor world, BlockPos pos)
        {
            //calling this from OnBlockPlaced below AND when we remove a blockage on the BE side
            var waterBlock = world.GetBlock(new AssetLocation("game:water-still-7"));
            //if there's water directly above then fill the furrowed land
            if (this.WaterAbove(pos))
            {
                var thisWaterBlock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
                if (!thisWaterBlock.Code.Path.Contains("ice"))
                {
                    world.BlockAccessor.SetBlock(waterBlock.BlockId, pos, BlockLayersAccess.Fluid);
                    world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
                }
            }

            //check for full water blocks in cardinal directions and
            //trigger update if found
            foreach (var waterPos in this.AreaAround(pos))
            {
                var blockChk = this.api.World.BlockAccessor.GetBlock(waterPos, BlockLayersAccess.Fluid);
                if (this.FullWaterBlock(blockChk))
                {
                    world.BlockAccessor.SetBlock(waterBlock.BlockId, pos, BlockLayersAccess.Fluid);
                    world.BlockAccessor.TriggerNeighbourBlockUpdate(waterPos);
                }
            }
        }


        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, pos, byItemStack);
            world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
            this.FillPlacedBlock(world, pos);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);

            //MIGHT NEED THIS
            world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);

            var waterFound = false;
            var positions = this.AreaAround(pos);
            positions.Append(pos.Copy());
            foreach (var waterPos in positions)
            {
                var blockChk = this.api.World.BlockAccessor.GetBlock(waterPos, BlockLayersAccess.Fluid);
                if (this.FullWaterBlock(blockChk))
                { waterFound = true; }
            }
            if (!waterFound)
            { world.BlockAccessor.SetBlock(0, pos, BlockLayersAccess.Fluid); }
            else
            {
                //similar to how I handled lava instead
                var waterBlock = world.GetBlock(new AssetLocation("game:water-still-3"));
                world.BlockAccessor.SetBlock(waterBlock.Id, pos, BlockLayersAccess.Default);
            }
        }


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            if (pos == neibpos)
            {
                return;
            } //WTF They can be equal?

            var thisWaterBlock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
            var neibWaterBlock = world.BlockAccessor.GetBlock(neibpos, BlockLayersAccess.Fluid);
            //Debug.WriteLine(thisWater + "  " + neibWater);
            var thisBlock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            var neibBlock = world.BlockAccessor.GetBlock(neibpos, BlockLayersAccess.Default);

            //connect them by changing the current connections of the two
            //start with the target block
            if (world.BlockAccessor.GetBlockEntity(pos) is BEFurrowedLand beBlock)
            {
                bool updated;
                if (thisBlock.FirstCodePart() == neibBlock.FirstCodePart() || this.FullWaterBlock(neibWaterBlock)) //added a neighbor or water is neighbor
                {
                    updated = beBlock.AddConnection(pos, neibpos.FacingFrom(pos));
                    if (this.FullWaterBlock(neibWaterBlock))
                    {
                        if (!thisWaterBlock.Code.Path.Contains("ice"))
                        {
                            var waterBlock = world.GetBlock(new AssetLocation("game:water-still-7"));
                            world.BlockAccessor.SetBlock(waterBlock.BlockId, pos, BlockLayersAccess.Fluid);
                        }
                    }
                }
                else
                {
                    updated = beBlock.RemoveConnection(pos, neibpos.FacingFrom(pos));
                }
                if (updated)
                { beBlock.MarkDirty(true); }
            }

            //now do the new block
            if (world.BlockAccessor.GetBlockEntity(neibpos) is BEFurrowedLand bebBlock)
            {
                var updated = false;
                if (thisBlock.FirstCodePart() == neibBlock.FirstCodePart() || this.FullWaterBlock(thisWaterBlock)) //added a neighbor
                {
                    updated = bebBlock.AddConnection(neibpos, pos.FacingFrom(neibpos));
                    if (this.FullWaterBlock(thisWaterBlock))
                    {
                        if (!neibWaterBlock.Code.Path.Contains("ice"))
                        {
                            var waterBlock = world.GetBlock(new AssetLocation("game:water-still-7"));
                            world.BlockAccessor.SetBlock(waterBlock.BlockId, neibpos, BlockLayersAccess.Fluid);
                        }
                    }
                }
                else
                {
                    updated = bebBlock.RemoveConnection(neibpos, pos.FacingFrom(neibpos));
                }
                if (updated)
                { bebBlock.MarkDirty(true); }
            }
        }
    }
}
