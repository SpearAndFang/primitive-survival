namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Config;
    //using System.Diagnostics;

    public class BlockSupport : Block
    {


        private void NotifyNeighborsOfBlockChange(BlockPos pos, IWorldAccessor world)
        {
            foreach (var facing in BlockFacing.ALLFACES)
            {
                var npos = pos.AddCopy(facing);
                var neib = world.BlockAccessor.GetBlock(npos, BlockLayersAccess.Default);
                neib.OnNeighbourBlockChange(world, npos, pos);
            }
        }


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            if (pos == neibpos)
            {
                return;
            } //WTF They can be equal?

            var thisBlock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            var neibBlock = world.BlockAccessor.GetBlock(neibpos, BlockLayersAccess.Default);
            var sameOrientation = thisBlock.LastCodePart() == neibBlock.LastCodePart();

            var thisPipe = false;
            var beBlock = world.BlockAccessor.GetBlockEntity(pos) as BESupport;
            if (beBlock != null)
            {
                if (!beBlock.Inventory[0].Empty)
                { thisPipe = true; }
            }

            var neibPipe = false;
            var bebBlock = world.BlockAccessor.GetBlockEntity(neibpos) as BESupport;
            if (bebBlock != null)
            {
                if (!bebBlock.Inventory[0].Empty)
                { neibPipe = true; }
            }

            //connect them by changing the current connections of the two - start with the target block
            bool updated;
            if (beBlock != null)
            {
                if (thisPipe && neibPipe && sameOrientation)
                { updated = beBlock.AddConnection(pos, neibpos.FacingFrom(pos)); }
                else
                { updated = beBlock.RemoveConnection(pos, neibpos.FacingFrom(pos)); }
                if (updated)
                { beBlock.MarkDirty(true); }
            }

            if (bebBlock != null)
            {
                if (thisPipe && neibPipe && sameOrientation)
                { updated = bebBlock.AddConnection(neibpos, pos.FacingFrom(neibpos)); }
                else
                { updated = bebBlock.RemoveConnection(neibpos, pos.FacingFrom(neibpos)); }
                if (updated)
                { bebBlock.MarkDirty(true); }
            }
        }


        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            BlockPos[] neibPos;
            var testBlock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            var direction = testBlock.LastCodePart();

            if (testBlock.Code.Path.Contains("empty"))
            {
                world.BlockAccessor.SetBlock(0, pos);
                if (direction == "ns")
                { neibPos = new BlockPos[] { pos.SouthCopy() }; }
                else // ew
                { neibPos = new BlockPos[] { pos.WestCopy() }; }

                foreach (var neib in neibPos)
                {
                    if (world.BlockAccessor.GetBlockEntity(neib) is BESupport be)
                    {
                        testBlock = world.BlockAccessor.GetBlock(neib, BlockLayersAccess.Default);
                        if (testBlock.FirstCodePart(2) == "none")
                        { dropQuantityMultiplier = 0f; } //pipe only
                        be.OnBreak(); //empty the inventory onto the ground
                        base.OnBlockBroken(world, neib, byPlayer, dropQuantityMultiplier);
                        this.NotifyNeighborsOfBlockChange(neib, world);
                    }
                    this.api.World.BlockAccessor.SetBlock(0, neib);
                }
            }
            else
            {
                if (world.BlockAccessor.GetBlockEntity(pos) is BESupport be)
                {
                    testBlock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
                    if (testBlock.FirstCodePart(2) == "none")
                    { dropQuantityMultiplier = 0f; } //pipe only
                    be.OnBreak(); //empty the inventory onto the ground
                    base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
                }
                if (direction == "ns")
                { neibPos = new BlockPos[] { pos.NorthCopy() }; }
                else // ew
                { neibPos = new BlockPos[] { pos.EastCopy() }; }
                foreach (var neib in neibPos)
                { world.BlockAccessor.SetBlock(0, neib); }
            }
        }


        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            // sigh. let's not micromanage this. It will just come back to bite me later
            var blockBelowPos = blockSel.Position.Copy();
            blockBelowPos.Y -= 1;
            var blockBelow = world.BlockAccessor.GetBlock(blockBelowPos, BlockLayersAccess.Default);

            if (!blockBelow.Code.Path.Contains("crop") && !(blockBelow.Fertility > 0) && (!blockBelow.Code.Path.Contains("farmland")))
            {
                failureCode = Lang.Get("you need more suitable ground to place this support");
                return false;
            }

            var material = itemstack.Collectible.FirstCodePart(1);
            var finalFacing = "";
            var targetPos = blockSel.Position;
            if (blockBelow.Code.Path.Contains("crop"))
            { targetPos.Y -= 1; }
            BlockPos[] neibPos = null;

            var playerFacing = SuggestedHVOrientation(byPlayer, blockSel)[0].ToString();
            if (playerFacing == "north")
            {
                blockBelow = world.BlockAccessor.GetBlock(targetPos.NorthCopy().DownCopy(), BlockLayersAccess.Default);
                neibPos = new BlockPos[] { targetPos.UpCopy(3), targetPos.UpCopy(3).NorthCopy() };
                finalFacing = "ns";

            }
            else if (playerFacing == "east")
            {
                blockBelow = world.BlockAccessor.GetBlock(targetPos.EastCopy().DownCopy(), BlockLayersAccess.Default);
                neibPos = new BlockPos[] { targetPos.UpCopy(2), targetPos.UpCopy(2).EastCopy() };
                finalFacing = "ew";
            }
            else if (playerFacing == "south")
            {
                blockBelow = world.BlockAccessor.GetBlock(targetPos.SouthCopy().DownCopy(), BlockLayersAccess.Default);
                neibPos = new BlockPos[] { targetPos.UpCopy(3).SouthCopy(), targetPos.UpCopy(3) };
                finalFacing = "ns";
            }
            else if (playerFacing == "west")
            {
                blockBelow = world.BlockAccessor.GetBlock(targetPos.WestCopy().DownCopy(), BlockLayersAccess.Default);
                neibPos = new BlockPos[] { targetPos.UpCopy(2).WestCopy(),
                targetPos.UpCopy(2)};
                finalFacing = "ew";
            }

            if (!blockBelow.Code.Path.Contains("crop") && !(blockBelow.Fertility > 0) && (!blockBelow.Code.Path.Contains("farmland")))
            {
                failureCode = Lang.Get("you need more suitable ground to place this support");
                return false;
            }

            foreach (var neib in neibPos)
            {
                var testBlock = this.api.World.BlockAccessor.GetBlock(neib, BlockLayersAccess.Default);
                if (testBlock.BlockId != 0)
                { return false; }
            }

            var count = 0;
            foreach (var neib in neibPos)
            {
                var testBlock = this.api.World.BlockAccessor.GetBlock(neib, BlockLayersAccess.Default);
                var asset1 = "primitivesurvival:support-" + material + "-";
                if (count == 0)
                { asset1 += "main"; }
                else
                { asset1 += "empty"; }
                asset1 += "-" + finalFacing;
                var block = this.api.World.GetBlock(new AssetLocation(asset1));
                if (block != null)
                { this.api.World.BlockAccessor.SetBlock(block.BlockId, neib); }
                count++;
            }
            return true;
        }


        //ok this is weird and not a good solution
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
            if (world.BlockAccessor.GetBlockEntity(blockPos) is BESupport be)
            {
                be.BreakIfUnsupported(blockPos);
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var testBlock = this.api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
            var type = testBlock.FirstCodePart(2);
            if (type == "main")
            {
                if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BESupport be)
                {
                    var result = be.OnInteract(byPlayer, blockSel.Position);
                    return result;
                }
            }
            else //empty
            {
                var direction = testBlock.LastCodePart();
                var neib = blockSel.Position.WestCopy();
                if (direction == "ns")
                { neib = blockSel.Position.SouthCopy(); }
                if (world.BlockAccessor.GetBlockEntity(neib) is BESupport be)
                {
                    var result = be.OnInteract(byPlayer, neib);
                    return result;
                }
            }
            return false;
        }


        //genmesh for supports
        public MeshData GenMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, string supportDir)
        {
            Shape shape = null;
            var tesselator = capi.Tesselator;
            var shapeAsset = capi.Assets.TryGet(shapePath + ".json");
            if (shapeAsset != null)
            {
                shape = shapeAsset.ToObject<Shape>();
                tesselator.TesselateShape(shapePath, shape, out var mesh, texture, new Vec3f(0, 0, 0));
                mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, this.Shape.rotateY * GameMath.DEG2RAD, 0); //orient based on direction last
                if (supportDir == "ew")
                { mesh.Translate(0f, 0.35f, 0f); }
                else
                { mesh.Translate(0f, -0.5f, 0f); }
                return mesh;
            }
            return null;
        }

    }
}
