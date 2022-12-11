namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;

    public class BlockTemporalBase : Block
    {
        public MeshData GenMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, int index, string type, string dir) //, ITesselatorAPI tesselator = null)
        {
            var tesselator = capi.Tesselator;
            var shape = capi.Assets.TryGet(shapePath + ".json").ToObject<Shape>();
            tesselator.TesselateShape(shapePath, shape, out var mesh, texture, new Vec3f(0f, 0, 0f));
            if (shapePath.Contains("cube"))
            { mesh.Translate(0f, 0.1f, 0f); }
            else if (shapePath.Contains("lectern"))
            {
                mesh.Translate(0f, 0.15f, 0f); //up
            }
            else if (shapePath.Contains("necronomicon"))
            {
                mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), -22.5f * GameMath.DEG2RAD, 180 * GameMath.DEG2RAD, 0); //tilt
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.56f, 0.56f, 0.56f); //shrink
                mesh.Translate(0f, 0.7f, 0f);
            }
            else if (shapePath.Contains("statue"))
            {
                mesh.Translate(0f, 0.6f, 0f);
                mesh.Rotate(new Vec3f(0.5f, 1, 0.5f), 0, 90 * GameMath.DEG2RAD, 0); //fix for wrong direction in shape file
            }
            else if (shapePath.Contains("gear-"))
            {
                mesh.Translate(0.15f, 1.3f, 0.06f); //center above
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 22.5f * GameMath.DEG2RAD, 0); //twist
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.48f, 0.48f, 0.48f); //shrink
                mesh.Rotate(new Vec3f(0f, 0.1f, 0f), 0, 0, 270 * GameMath.DEG2RAD); //flip on side

                if (shapePath.Contains("rusty"))
                { mesh.Translate(0, 0.76f, 0); } //up
                else //temporal
                { mesh.Translate(0, 0.85f, 0); } //up
                if (type == "lectern")
                {
                    //mesh.Translate(0f, 0.21f, 0.1f); //in and up
                    mesh.Translate(-0.1f, 0.21f, 0); //
                }

                if (index == 2) //north
                { mesh.Rotate(new Vec3f(0.5f, 1, 0.5f), 0, 270 * GameMath.DEG2RAD, 0); }
                else if (index == 3) //east
                { mesh.Rotate(new Vec3f(0.5f, 1, 0.5f), 0, 180 * GameMath.DEG2RAD, 0); }
                else if (index == 4)  //south
                { mesh.Rotate(new Vec3f(0.5f, 1, 0.5f), 0, 90 * GameMath.DEG2RAD, 0); }
                else if (index == 5)  //west
                { mesh.Rotate(new Vec3f(0.5f, 1, 0.5f), 0, 0 * GameMath.DEG2RAD, 0); }
            }

            if (dir == "north") //direction
            { mesh.Rotate(new Vec3f(0.5f, 1, 0.5f), 0, 180 * GameMath.DEG2RAD, 0); }
            else if (dir == "south")
            { mesh.Rotate(new Vec3f(0.5f, 1, 0.5f), 0, 0 * GameMath.DEG2RAD, 0); }
            else if (dir == "east")
            { mesh.Rotate(new Vec3f(0.5f, 1, 0.5f), 0, 90 * GameMath.DEG2RAD, 0); }
            else if (dir == "west")
            { mesh.Rotate(new Vec3f(0.5f, 1, 0.5f), 0, 270 * GameMath.DEG2RAD, 0); }

            return mesh;
        }


        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            var facing = SuggestedHVOrientation(byPlayer, blockSel)[0].ToString();
            bool placed;
            placed = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (placed)
            {
                var block = this.api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
                var newPath = block.Code.Path;
                newPath = newPath.Replace("north", facing);
                block = this.api.World.GetBlock(block.CodeWithPath(newPath));
                this.api.World.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
            }
            return placed;
        }


        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (world.BlockAccessor.GetBlockEntity(pos) is BETemporalBase be)
            { be.OnBreak(); } // byPlayer, pos); } //empty the inventory onto the ground
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BETemporalBase be)
            { return be.OnInteract(byPlayer); } //, blockSel); }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
