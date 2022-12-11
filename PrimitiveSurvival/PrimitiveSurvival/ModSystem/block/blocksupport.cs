namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Config;
    //using System.Diagnostics;

    public class BlockSupport : Block
    {
        //private static readonly Random Rnd = new Random();

        public MeshData GenMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, int slot, bool alive, ITesselatorAPI tesselator = null)
        {
            Shape shape = null;
            tesselator = capi.Tesselator;
            shape = capi.Assets.TryGet(shapePath + ".json").ToObject<Shape>();
            tesselator.TesselateShape(shapePath, shape, out var mesh, texture, new Vec3f(0, 0, 0));
            mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, this.Shape.rotateY * GameMath.DEG2RAD, 0); //orient based on direction last
            return mesh;
        }


        private void NotifyNeighborsOfBlockChange(BlockPos pos, IWorldAccessor world)
        {
            foreach (var facing in BlockFacing.ALLFACES)
            {
                var npos = pos.AddCopy(facing);
                var neib = world.BlockAccessor.GetBlock(npos, BlockLayersAccess.Default);
                neib.OnNeighbourBlockChange(world, npos, pos);
            }
        }


        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            BlockPos[] neibPos;
            var testBlock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);

            world.BlockAccessor.SetBlock(0, pos.DownCopy());
            var direction = testBlock.LastCodePart();
            //Debug.WriteLine(direction);
            if (testBlock.Code.Path.Contains("mainempty"))
            {
                world.BlockAccessor.SetBlock(0, pos);
                if (direction == "north")
                { neibPos = new BlockPos[] { pos.SouthCopy(), pos.SouthCopy().DownCopy() }; }
                else if (direction == "south")
                { neibPos = new BlockPos[] { pos.NorthCopy(), pos.NorthCopy().DownCopy() }; }
                else if (direction == "east")
                { neibPos = new BlockPos[] { pos.WestCopy(), pos.WestCopy().DownCopy() }; }
                else
                { neibPos = new BlockPos[] { pos.EastCopy(), pos.EastCopy().DownCopy() }; }
                foreach (var neib in neibPos)
                {
                    if (world.BlockAccessor.GetBlockEntity(neib) is BESupport be)
                    {
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
                    be.OnBreak(); //empty the inventory onto the ground
                    base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
                    //this.api.World.BlockAccessor.SetBlock(0, pos);
                }
                if (direction == "south")
                { neibPos = new BlockPos[] { pos.SouthCopy(), pos.SouthCopy().DownCopy() }; }
                else if (direction == "north")
                { neibPos = new BlockPos[] { pos.NorthCopy(), pos.NorthCopy().DownCopy() }; }
                else if (direction == "west")
                { neibPos = new BlockPos[] { pos.WestCopy(), pos.WestCopy().DownCopy() }; }
                else
                { neibPos = new BlockPos[] { pos.EastCopy(), pos.EastCopy().DownCopy() }; }
                foreach (var neib in neibPos)
                {
                    world.BlockAccessor.SetBlock(0, neib);
                }
            }
        }


        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            //prevent placing on wall
            if (blockSel.Face.IsHorizontal)
            { return false; }

            var block = world.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
            var blockBelowPos = blockSel.Position.Copy();
            blockBelowPos.Y -= 1;
            var blockBelow = world.BlockAccessor.GetBlock(blockBelowPos, BlockLayersAccess.Default);

            if (!blockBelow.Code.Path.Contains("crop") && !(blockBelow.Fertility > 0) && (!blockBelow.Code.Path.Contains("farmland")))
            {
                failureCode = Lang.Get("you need more suitable ground to place this support");
                return false;
            }

            var material = itemstack.Collectible.FirstCodePart(1);

            var targetPos = blockSel.Position;
            if (blockBelow.Code.Path.Contains("crop"))
            { targetPos.Y -= 1; }

            var neibPos = new BlockPos[] { targetPos.UpCopy(), targetPos.UpCopy().UpCopy(),
                targetPos.UpCopy().NorthCopy(),targetPos.UpCopy().UpCopy().NorthCopy()};

            var playerFacing = SuggestedHVOrientation(byPlayer, blockSel)[0].ToString();
            if (playerFacing == "north")
            {
                blockBelow = world.BlockAccessor.GetBlock(targetPos.NorthCopy().DownCopy(), BlockLayersAccess.Default);
                if (!blockBelow.Code.Path.Contains("crop") && !(blockBelow.Fertility > 0) && (!blockBelow.Code.Path.Contains("farmland")))
                {
                    failureCode = Lang.Get("you need more suitable ground to place this support");
                    return false;
                }
            }

            else if (playerFacing == "east")
            {
                blockBelow = world.BlockAccessor.GetBlock(targetPos.EastCopy().DownCopy(), BlockLayersAccess.Default);
                if (!blockBelow.Code.Path.Contains("crop") && !(blockBelow.Fertility > 0) && (!blockBelow.Code.Path.Contains("farmland")))
                {
                    failureCode = Lang.Get("you need more suitable ground to place this support");
                    return false;
                }
                neibPos = new BlockPos[] { targetPos.UpCopy(), targetPos.UpCopy().UpCopy(),
                targetPos.UpCopy().EastCopy(),targetPos.UpCopy().UpCopy().EastCopy()};
            }
            else if (playerFacing == "south")
            {
                blockBelow = world.BlockAccessor.GetBlock(targetPos.SouthCopy().DownCopy(), BlockLayersAccess.Default);
                if (!blockBelow.Code.Path.Contains("crop") && !(blockBelow.Fertility > 0) && (!blockBelow.Code.Path.Contains("farmland")))
                {
                    failureCode = Lang.Get("you need more suitable ground to place this support");
                    return false;
                }
                neibPos = new BlockPos[] { targetPos.UpCopy(), targetPos.UpCopy().UpCopy(),
                targetPos.UpCopy().SouthCopy(),targetPos.UpCopy().UpCopy().SouthCopy()};
            }
            else if (playerFacing == "west")
            {
                blockBelow = world.BlockAccessor.GetBlock(targetPos.WestCopy().DownCopy(), BlockLayersAccess.Default);
                if (!blockBelow.Code.Path.Contains("crop") && !(blockBelow.Fertility > 0) && (!blockBelow.Code.Path.Contains("farmland")))
                {
                    failureCode = Lang.Get("you need more suitable ground to place this support");
                    return false;
                }
                neibPos = new BlockPos[] { targetPos.UpCopy(), targetPos.UpCopy().UpCopy(),
                targetPos.UpCopy().WestCopy(),targetPos.UpCopy().UpCopy().WestCopy()};
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
                var asset1 = "primitivesurvival:support-" + material + "-main";
                if (count == 0)
                { asset1 += "below"; }
                else if (count == 2)
                { asset1 += "belowempty"; }
                else if (count == 3)
                { asset1 += "empty"; }
                asset1 += "-" + playerFacing;
                block = this.api.World.GetBlock(new AssetLocation(asset1));
                if (block != null)
                {
                    this.api.World.BlockAccessor.SetBlock(block.BlockId, neib);
                }
                count++;
            }
            return true;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BESupport be)
            {
                //Debug.WriteLine("OnInteract");
                return be.OnInteract(byPlayer, blockSel);
            }
            var testBlock = this.api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
            var direction = testBlock.LastCodePart();
            var neib = blockSel.Position.EastCopy();
            if (direction == "north")
            { neib = blockSel.Position.SouthCopy(); }
            else if (direction == "south")
            { neib = blockSel.Position.NorthCopy(); }
            else if (direction == "east")
            { neib = blockSel.Position.WestCopy(); }
            if (world.BlockAccessor.GetBlockEntity(neib) is BESupport be2)
            {
                //Debug.WriteLine("OnInteract Neighbor");
                return be2.OnInteract(byPlayer, blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
