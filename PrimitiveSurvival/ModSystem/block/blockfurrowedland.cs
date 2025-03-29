namespace PrimitiveSurvival.ModSystem
{
    using System.Linq;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Config;
    using Vintagestory.API.Client;
    using Vintagestory.API.Server;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.Util;
    using System.Text;
    using PrimitiveSurvival.ModConfig;
    using System.Diagnostics;

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
            if (block.FirstCodePart().Contains("water") && block.LastCodePart() == "7")
            { return true; }
            return false;
        }



        public void FillPlacedBlock(IWorldAccessor world, BlockPos pos)
        {
            //calling this from OnBlockPlaced below AND when we remove a blockage on the BE side
            //if there's water directly above then fill the furrowed land
            if (this.WaterAbove(pos))
            {
                var thisWaterBlock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
                if (!thisWaterBlock.Code.Path.Contains("ice"))
                {
                    var assetCode = "game:" + thisWaterBlock.FirstCodePart() + "-still-7";
                    var waterBlock = world.GetBlock(new AssetLocation(assetCode));

                    if (waterBlock != null)
                    {
                        world.BlockAccessor.SetBlock(waterBlock.BlockId, pos, BlockLayersAccess.Fluid);
                    }
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
                    var assetCode = "game:" + blockChk.FirstCodePart() + "-still-7";
                    var waterBlock = world.GetBlock(new AssetLocation(assetCode));

                    if (waterBlock != null)
                    {
                        world.BlockAccessor.SetBlock(waterBlock.BlockId, pos, BlockLayersAccess.Fluid);
                    }
                    world.BlockAccessor.TriggerNeighbourBlockUpdate(waterPos);
                }
            }
        }


        //if the entity is standing on the land when furrowing, push them up a bit
        public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
        {
            if (isImpact && facing.Axis == EnumAxis.Y)
            {
                if ((entity.Pos.Motion.Y > -0.03) && (entity.Pos.Motion.Y < 0))
                {
                    entity.Pos.Motion.Y *= -2;
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

            var blockFound = "water";
            foreach (var waterPos in positions)
            {
                var blockChk = this.api.World.BlockAccessor.GetBlock(waterPos, BlockLayersAccess.Fluid);
                if (this.FullWaterBlock(blockChk))
                {
                    waterFound = true;
                    blockFound = blockChk.FirstCodePart();
                }
            }
            if (!waterFound)
            { world.BlockAccessor.SetBlock(0, pos, BlockLayersAccess.Fluid); }
            else
            {
                //arbitrarily use the last water block found
                string assetCode = "game:" + blockFound + "-still-3";
                var waterBlock = world.GetBlock(new AssetLocation(assetCode));
                if (waterBlock != null)
                {
                    world.BlockAccessor.SetBlock(waterBlock.Id, pos, BlockLayersAccess.Default);
                }
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
                            var assetCode = "game:" + thisWaterBlock.FirstCodePart() + "-still-7";
                            var waterBlock = world.GetBlock(new AssetLocation(assetCode));

                            if (waterBlock != null)
                            {
                                world.BlockAccessor.SetBlock(waterBlock.BlockId, pos, BlockLayersAccess.Fluid);
                            }
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
                            var assetCode = "game:" + thisWaterBlock.FirstCodePart() + "-still-7";
                            var waterBlock = world.GetBlock(new AssetLocation(assetCode));

                            if (waterBlock != null)
                            {
                                world.BlockAccessor.SetBlock(waterBlock.BlockId, pos, BlockLayersAccess.Fluid);
                            }
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

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            var dsc = new StringBuilder();
            //dsc.AppendLine(base.GetPlacedBlockInfo(world, pos, forPlayer));
            if (ModConfig.Loaded.ShowModNameInHud)
            {
                dsc.AppendLine("\n<font color=\"#D8EAA3\"><i>" + Lang.GetMatching("game:tabname-primitive") + "</i></font>").AppendLine();
            }
            return dsc.AppendLine().ToString();
        }
    }
}
