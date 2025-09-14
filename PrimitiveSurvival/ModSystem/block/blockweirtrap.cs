namespace PrimitiveSurvival.ModSystem
{
    using System.Diagnostics;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;

    public class BlockWeirTrap : Block
    {

        public MeshData GenMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, int slot, bool alive) //, ITesselatorAPI tesselator = null)
        {
            var tesselator = capi.Tesselator;
            var shape = capi.Assets.TryGet(shapePath + ".json").ToObject<Shape>();

            float x = 0f;
            float y = 0f;
            float z = 0f;

            if (shapePath.Contains("/saltwater/"))
            {
                x = 0f;
                y = 180;
                z = -60f;
            }

            var offY = -0.83f;
            if (shapePath.Contains("/saltwater/"))
            {
                if (shapePath.Contains("coelacanth") || shapePath.Contains("grouper") || shapePath.Contains("mahi-mahi"))
                { offY = -1.1f; }
                if (shapePath.Contains("barracuda"))
                { offY = -1f; }
                if (shapePath.Contains("sturgeon"))
                { offY = -1f; }
                if (shapePath.Contains("haddock") || shapePath.Contains("pollock") || shapePath.Contains("gurnard"))
                { offY = -0.88f; }
                if (shapePath.Contains("herring") || shapePath.Contains("mackerel"))
                { offY = -0.7f; }
                if (shapePath.Contains("perch"))
                { offY = -0.55f; }
                if (shapePath.Contains("amberjack") || shapePath.Contains("snapper"))
                { offY = -0.88f; }
            }


            tesselator.TesselateShape(shapePath, shape, out var mesh, texture, new Vec3f(x, y, z));
            if (slot == 0)
            {
                if (shapePath.Contains("seashell"))
                { mesh.Translate(-0.4f, 0.1f, -0.2f); }
                else if (shapePath.Contains("temporal") || shapePath.Contains("statue") || shapePath.Contains("necronomicon"))
                {
                    mesh.Translate(-0.4f, 0.1f, 0f);
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 20 * GameMath.DEG2RAD, 0);
                }
                else if (shapePath.Contains("/saltwater/"))
                {
                    /////////////////////////////////////
                    mesh.Translate(-0.1f, 1.4f+offY, -0.6f);
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 70 * GameMath.DEG2RAD, 100 * GameMath.DEG2RAD, 230 * GameMath.DEG2RAD);
                    /////////////////////////////////////
                }
                else
                {
                    mesh.Translate(0.5f, 1.4f, -0.6f);
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 70 * GameMath.DEG2RAD, 100 * GameMath.DEG2RAD, 230 * GameMath.DEG2RAD);
                    if (shapePath.Contains("catfish"))
                    {
                        mesh.Scale(new Vec3f(0.5f, 0, 0.5f), 0.85f, 0.85f, 0.85f);
                        mesh.Translate(-0.2f, 0f, -0.3f);
                    }
                }
            }
            else if (slot == 1)
            {
                if (shapePath.Contains("seashell"))
                { mesh.Translate(0.6f, 0.1f, 0f); }
                else if (shapePath.Contains("temporal") || shapePath.Contains("statue") || shapePath.Contains("necronomicon"))
                {
                    mesh.Translate(0.6f, 0.1f, -0.1f);
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 340 * GameMath.DEG2RAD, 0);
                }
                else if (shapePath.Contains("/saltwater/"))
                {
                    /////////////////////////////////////
                    mesh.Translate(-0.1f, 1.3f+offY, 0.7f);
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 70 * GameMath.DEG2RAD, 80 * GameMath.DEG2RAD, 230 * GameMath.DEG2RAD);
                    /////////////////////////////////////
                }
                else
                {
                    mesh.Translate(0.5f, 1.4f, 0.7f);
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 70 * GameMath.DEG2RAD, 80 * GameMath.DEG2RAD, 230 * GameMath.DEG2RAD);

                    if (shapePath.Contains("catfish"))
                    {
                        mesh.Scale(new Vec3f(0.5f, 0, 0.5f), 0.85f, 0.85f, 0.85f);
                        mesh.Translate(0.2f, 0f, -0.2f);
                    }
                }
            }

            if (alive) //let's animate these fishes
            {
                var flength = 0.45;
                if (shapePath.Contains("/saltwater/"))
                {
                    flength = offY * -1 - 0.4f;
                    //Debug.WriteLine(flength);
                }
                else if (shapePath.Contains("catfish") || shapePath.Contains("salmon"))
                { flength = 0.55; }
                else if (shapePath.Contains("perch") || shapePath.Contains("bass"))
                { flength = 0.35; }
                else if (shapePath.Contains("bluegill"))
                { flength = 0; } //make the bluegill really wiggly

                var fheight = 0.45;
                if (shapePath.Contains("salmon"))
                { fheight = 0.5; }
                else if (shapePath.Contains("pike"))
                { fheight = 0.39; }
                else if (shapePath.Contains("bluegill"))
                { fheight = 0; }

                // 1.16
                //var fishWave = VertexFlags.LeavesWindWaveBitMask | VertexFlags.WeakWaveBitMask;
                var fishWave = EnumWindBitModeMask.ExtraWeakWind | VertexFlags.LiquidExposedToSkyBitMask; // LiquidWaterModeBitMask; 1.20

                for (var vertexNum = 0; vertexNum < mesh.GetVerticesCount(); vertexNum++)
                {
                    //tail first, top fins second
                    if ((mesh.xyz[(3 * vertexNum) + 2] > flength) || (mesh.xyz[(3 * vertexNum) + 1] > fheight))
                    { mesh.Flags[vertexNum] |= fishWave; }
                    else
                    { mesh.Flags[vertexNum] |= 6144; }
                }
            }
            mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, this.Shape.rotateY * GameMath.DEG2RAD, 0); //orient based on direction last
            return mesh;
        }


        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);

            var tempBlock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
            if (tempBlock.Code.Path.Contains("saltwater"))
            {
                world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation("saltwater-still-7")).BlockId, pos);
            }
            else
            {
                world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation("water-still-7")).BlockId, pos);
            }


            world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default).OnNeighbourBlockChange(world, pos, pos);
        }


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            var neibBlock = world.BlockAccessor.GetBlock(neibpos, BlockLayersAccess.Default);
            if (neibBlock != null)
            {
                if (neibBlock.BlockId == 0 || neibBlock.Code.Path.StartsWith("water") || neibBlock.Code.Path.StartsWith("saltwater"))
                {
                    var weirSidesPos = new BlockPos[] { pos.EastCopy(), pos.WestCopy(), pos.NorthCopy(), pos.SouthCopy() };
                    Block testBlock;
                    foreach (var neighbor in weirSidesPos) // check for open in any of the stakes on the four sides
                    {
                        testBlock = world.BlockAccessor.GetBlock(neighbor, BlockLayersAccess.Default);
                        if (testBlock.Code.Path.Contains("open") && testBlock.Code.Path.Contains("stake-")) //1.18 was stakeinwater
                        {
                            world.BlockAccessor.BreakBlock(pos, null);
                            var newPath = testBlock.Code.Path.Replace("open", "").Replace("we", "ew").Replace("sn", "ns");
                            testBlock = world.GetBlock(testBlock.CodeWithPath(newPath));
                            if (testBlock != null)
                            {
                                world.BlockAccessor.SetBlock(testBlock.BlockId, neighbor);
                            }
                        }
                    }
                }
            }
        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEWeirTrap bedc)
            { return bedc.OnInteract(byPlayer, blockSel); }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
