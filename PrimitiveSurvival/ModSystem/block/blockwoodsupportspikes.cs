namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Common.Entities;

    public class BlockWoodSupportSpikes : Block
    {

        public MeshData GenMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, int slot) //, ITesselatorAPI tesselator = null)
        {
            var tesselator = capi.Tesselator;
            var shape = capi.Assets.TryGet(shapePath + ".json").ToObject<Shape>();
            tesselator.TesselateShape(shapePath, shape, out var mesh, texture, new Vec3f(0, 0, 0));
            if (slot == -1) //spikes
            { mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), this.Shape.rotateX * GameMath.DEG2RAD, this.Shape.rotateY * GameMath.DEG2RAD, this.Shape.rotateZ * GameMath.DEG2RAD); }
            return mesh;
        }


        public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
        {
            if (entity.Code.Path.StartsWith("butterfly")) //no effect for butterflies
            { return; }

            base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
            if (world.Side == EnumAppSide.Server) // && isImpact)// && facing.Axis == EnumAxis.Y)
            { world.BlockAccessor.BreakBlock(pos, null); }
        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEWoodSupportSpikes be)
            {
                var result = be.OnInteract(byPlayer); //, blockSel);
                return result;
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }


        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            if (this.api.World.BlockAccessor.GetBlockEntity(pos) is BEWoodSupportSpikes be)
            { return be.GetBlockName(world, pos); }
            return base.GetPlacedBlockName(world, pos);
        }


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            var block = world.BlockAccessor.GetBlock(neibpos, BlockLayersAccess.Default);
            if (block.BlockId <= 0) //block removed
            {
                block = world.BlockAccessor.GetBlock(neibpos.NorthCopy(), BlockLayersAccess.Default);
                if (block.FirstCodePart() == "woodsupportspikes")
                { world.BlockAccessor.BreakBlock(neibpos.NorthCopy(), null); }
                block = world.BlockAccessor.GetBlock(neibpos.SouthCopy(), BlockLayersAccess.Default);
                if (block.FirstCodePart() == "woodsupportspikes")
                { world.BlockAccessor.BreakBlock(neibpos.SouthCopy(), null); }
                block = world.BlockAccessor.GetBlock(neibpos.EastCopy(), BlockLayersAccess.Default);
                if (block.FirstCodePart() == "woodsupportspikes")
                { world.BlockAccessor.BreakBlock(neibpos.EastCopy(), null); }
                block = world.BlockAccessor.GetBlock(neibpos.WestCopy(), BlockLayersAccess.Default);
                if (block.FirstCodePart() == "woodsupportspikes")
                { world.BlockAccessor.BreakBlock(neibpos.WestCopy(), null); }
            }
            else //block added
            {
                block = world.BlockAccessor.GetBlock(neibpos.DownCopy(), BlockLayersAccess.Default);
                if (block.FirstCodePart() == "woodsupportspikes")
                {
                    world.BlockAccessor.BreakBlock(neibpos.DownCopy(), null);
                    world.BlockAccessor.BreakBlock(neibpos, null);
                }
            }
        }
    }
}
