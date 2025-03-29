namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Linq;
    using System.Text;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Client.Tesselation;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.Util;
    using Vintagestory.API.Config;
    using PrimitiveSurvival.ModConfig;
    //using System.Diagnostics;


    public class BlockFloatingDock : Block, IDrawYAdjustable //1.20
    {

        static Random rand = new Random();
        float adjHeight;


        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }


        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            bool placed;

            BlockFacing[] horVer = Block.SuggestedHVOrientation(byPlayer, blockSel);
            string code = "ns";
            if (horVer[0].Index == 1 || horVer[0].Index == 3) code = "we";

            if (this.IsWater(world.BlockAccessor, blockSel.Position.UpCopy()))
            {
                blockSel = blockSel.Clone();
                blockSel.Position = blockSel.Position.Up();
                placed = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
                if (placed)
                {
                    var block = this.api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
                    var newPath = block.Code.Path;
                    newPath = newPath.Replace("ns", code);
                    block = this.api.World.GetBlock(block.CodeWithPath(newPath));
                    this.api.World.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
                }
                return placed;
            }
            
            if (blockSel.Face.IsHorizontal) //prevent placing dock on wall
            { return false; }

            var testBlock = this.api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
            if (testBlock.Code.Path.Contains("water")) // prevent placing dock in partial water
            {
                if (!IsWater(this.api.World.BlockAccessor, blockSel.Position))
                { return false; }
            }

            var testBlock2 = this.api.World.BlockAccessor.GetBlock(blockSel.Position.DownCopy(), BlockLayersAccess.Default);
            if (testBlock2.Code.FirstCodePart() == "floatingdock" || testBlock2.Code.FirstCodePart() == "raft") //prevent placing dock on dock or raft
            { return false; }

            placed = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (placed)
            {
                var block = this.api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
                var newPath = block.Code.Path;
                newPath = newPath.Replace("ns", code);
                block = this.api.World.GetBlock(block.CodeWithPath(newPath));
                this.api.World.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
            }
            return placed;
        }


        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, pos, byItemStack);
            world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
            
        }


        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
        }


        private bool IsWater(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var waterblock = blockAccessor.GetBlock(pos.DownCopy(), BlockLayersAccess.Fluid);
            var upblock = blockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
            var lastPart = waterblock.LastCodePart();
            var height = lastPart.ToInt();
            if (height <= 4)
            { return false; }

            return waterblock.IsLiquid() && waterblock.LiquidCode.Contains("water") && upblock.Id == 0; //&& waterblock.LiquidLevel == 7
        }


        public float AdjustYPosition(BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
        {
            var nblock = chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[TileSideEnum.Down]];
            var boxes = nblock.CollisionBoxes;
            var boxHeight = 0f;
            if (boxes != null && boxes.Length > 0)
            { boxHeight = boxes.Max(c => c.Y2); }

            adjHeight = boxHeight < 0.8f ? -0.19f : 0f; //0.3 as per below
            return adjHeight;
        }

        
        public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
        {
            base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
            if (entity is EntityPlayer)
            {
                var player = (entity as EntityAgent);
                if (player is EntityPlayer)
                {
                    var msX = Math.Abs(player.Pos.Motion.X);
                    var msY = Math.Abs(player.Pos.Motion.Y);
                    var msZ = Math.Abs(player.Pos.Motion.Z);
                    var msMax = Math.Max(msX, Math.Max(msY, msZ));

                    var be = world.BlockAccessor.GetBlockEntity(pos) as BEFloatingDock;

                    if (be != null && msMax >= 0.03 && player.Pos.Motion.Y <= 0)
                    { be.PlayerIsWalking(true); }
                }
            }
            //if (entity.FeetInLiquid && !(entity as EntityAgent).IsEyesSubmerged() && entity is IPlayer) //allow players to pass through bottom of floating dock
            //{ entity.Pos.Y = entity.Pos.Y + 1; }
        }




        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefault;
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            if (pos == neibpos)
            {
                return;
            } //WTF They can be equal?

            var thisWaterBlock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
            var neibWaterBlock = world.BlockAccessor.GetBlock(neibpos, BlockLayersAccess.Fluid);
            var thisBlock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            var neibBlock = world.BlockAccessor.GetBlock(neibpos, BlockLayersAccess.Default);

            //connect them by changing the current connections of the two - start with the target block
            if (world.BlockAccessor.GetBlockEntity(pos) is BEFloatingDock beBlock)
            {
                bool updated;
                if (thisBlock.FirstCodePart() == neibBlock.FirstCodePart()) //added a neighbor
                {
                    updated = beBlock.AddConnection(pos, neibpos.FacingFrom(pos));
                }
                else
                {
                    updated = beBlock.RemoveConnection(pos, neibpos.FacingFrom(pos));
                }
                if (updated)
                { beBlock.MarkDirty(true); }
            }
                        
            if (world.BlockAccessor.GetBlockEntity(neibpos) is BEFloatingDock bebBlock) //now do the new block
            {
                var updated = false;
                if (thisBlock.FirstCodePart() == neibBlock.FirstCodePart()) //added a neighbor
                {
                    updated = bebBlock.AddConnection(neibpos, pos.FacingFrom(neibpos));
                }
                else
                {
                    updated = bebBlock.RemoveConnection(neibpos, pos.FacingFrom(neibpos));
                }
                if (updated)
                { bebBlock.MarkDirty(true); }
            }
        }



        //*******************************************************************************************
        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            var dsc = new StringBuilder();
            var ba = world.BlockAccessor;
            var testBlock = ba.GetBlock(pos, BlockLayersAccess.Default);

            /* debug
            if (ba.GetBlockEntity(pos) is BEFloatingDock be)
            {
                string connections = be.GetConnections();
                dsc.AppendLine(Lang.GetMatching("Connections: " + connections));
            }
            */
            dsc.AppendLine(Lang.GetMatching("primitivesurvival:blockdesc-floatingdock-*"));

            if (ModConfig.Loaded.ShowModNameInHud)
            {
                dsc.AppendLine("\n<font color=\"#D8EAA3\"><i>" + Lang.GetMatching("game:tabname-primitive") + "</i></font>").AppendLine();
            }
            return dsc.ToString();

        }
    }
}
