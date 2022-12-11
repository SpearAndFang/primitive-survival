namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;

    public class BlockFishBasket : Block
    {

        public MeshData GenMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, int slot, bool alive, ITesselatorAPI tesselator = null)
        {
            Shape shape = null;
            tesselator = capi.Tesselator;
            shape = capi.Assets.TryGet(shapePath + ".json").ToObject<Shape>();
            tesselator.TesselateShape(shapePath, shape, out var mesh, texture, new Vec3f(0, 0, 0));
            if (slot == 0) //bait
            { mesh.Translate(-0.03f, 0.3f, 0.15f); }
            else if (slot == 1) //fish, rot, seashell
            {
                if (shapePath.Contains("seashell") || shapePath.Contains("gear"))
                {
                    mesh.Translate(0.3f, -0.1f, -0.2f);
                    mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0 * GameMath.DEG2RAD, 0 * GameMath.DEG2RAD, 60 * GameMath.DEG2RAD);
                }
                else
                {
                    mesh.Translate(-0.15f, 0.4f, -0.05f);
                    mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 80 * GameMath.DEG2RAD, -100 * GameMath.DEG2RAD, 10 * GameMath.DEG2RAD);
                }
            }
            else if (slot == 2) //fish, rot, seashell 
            {
                if (shapePath.Contains("seashell") || shapePath.Contains("gear"))
                {
                    mesh.Translate(-0.3f, -0.1f, -0.15f);
                    mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0 * GameMath.DEG2RAD, 0 * GameMath.DEG2RAD, -60 * GameMath.DEG2RAD);
                }
                else
                {
                    mesh.Translate(-0.2f, 0.45f, 0.05f);
                    mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 70 * GameMath.DEG2RAD, -80 * GameMath.DEG2RAD, 0 * GameMath.DEG2RAD);
                }
            }
            if (shapePath.Contains("catfish"))
            { mesh.Scale(new Vec3f(0.5f, 0, 0.5f), 0.75f, 0.75f, 0.75f); }

            if (alive) //let's animate these fishes
            {
                var flength = 0.75;
                if (shapePath.Contains("salmon"))
                { flength = 0.85; }
                else if (shapePath.Contains("catfish"))
                { flength = 0.9; }
                else if (shapePath.Contains("bass"))
                { flength = 0.7; }
                else if (shapePath.Contains("perch"))
                { flength = 0.6; }
                else if (shapePath.Contains("bluegill"))
                { flength = 0; } //make the bluegill really wiggly

                var fheight = 0.37;
                if (shapePath.Contains("salmon"))
                { fheight = 0.43; }
                else if (shapePath.Contains("bass"))
                { fheight = 0.43; }
                else if (shapePath.Contains("arctic"))
                { fheight = 0.36; }
                else if (shapePath.Contains("perch"))
                { fheight = 0.39; }
                else if (shapePath.Contains("catfish"))
                { fheight = 0.29; }

                // 1.16
                //var fishWave = VertexFlags.LeavesWindWaveBitMask | VertexFlags.WeakWaveBitMask;
                var fishWave = EnumWindBitModeMask.ExtraWeakWind | VertexFlags.LiquidWaterModeBitMask;
                for (var vertexNum = 0; vertexNum < mesh.GetVerticesCount(); vertexNum++)
                {
                    //tail first, top fins second
                    if ((mesh.xyz[(3 * vertexNum) + 2] < 0.6 - flength + ((slot - 1) * .05)) || (mesh.xyz[(3 * vertexNum) + 1] > fheight + ((slot - 1) * .05)))
                    { mesh.Flags[vertexNum] |= fishWave; }
                    else
                    { mesh.Flags[vertexNum] |= 6144; }
                }
            }
            mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, this.Shape.rotateY * GameMath.DEG2RAD, 0); //orient based on direction last
            return mesh;
        }


        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            var block = world.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);

            if (!this.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            { return false; }

            //1.17.pre.5 refactor - always place fishbasketinwater variant
            Block blockToPlace = this;
            //var inWater = block.IsLiquid() && block.LiquidLevel == 7 && block.LiquidCode.Contains("water");
            if (blockToPlace != null)
            {
                var facing = SuggestedHVOrientation(byPlayer, blockSel)[0].ToString();
                var newPath = blockToPlace.Code.Path;
                newPath = newPath.Replace("north", facing);
                //if (inWater)
                //{
                    if (!newPath.Contains("fishbasketinwater"))
                    { newPath = newPath.Replace("fishbasket", "fishbasketinwater"); }
                    blockToPlace = this.api.World.GetBlock(blockToPlace.CodeWithPath(newPath));
                    world.BlockAccessor.SetBlock(blockToPlace.BlockId, blockSel.Position);
                //}
                //else
                /*
                {
                    blockToPlace = this.api.World.GetBlock(blockToPlace.CodeWithPath(newPath));
                    world.BlockAccessor.SetBlock(blockToPlace.BlockId, blockSel.Position);
                }*/
                return true;
            }
            return false;
        }


        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            Block blockToBreak = this;
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);

            if (blockToBreak.FirstCodePart() == "fishbasketinwater")
            {
                //1.17.pre.5 don't replace with water
                //world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation("water-still-7")).BlockId, pos);
                world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default).OnNeighbourBlockChange(world, pos, pos);
            }
        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEFishBasket bedc)
            { return bedc.OnInteract(byPlayer, blockSel); }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
