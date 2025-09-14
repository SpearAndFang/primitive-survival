namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Diagnostics;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;

    public class BlockLimbTrotLineLure : Block
    {
        private static readonly Random Rnd = new Random(); 

        public MeshData GenMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, bool alive) //, ITesselatorAPI tesselator = null)
        {
            Shape shape; // = null;
            var tesselator = capi.Tesselator;
            shape = capi.Assets.TryGet(shapePath + ".json").ToObject<Shape>();
            float x = this.Shape.rotateX;
            float y = this.Shape.rotateY;
            float z = this.Shape.rotateZ;

            if (shapePath.Contains("/saltwater/"))
            {
                x = 0f;
                y = 180 + this.Shape.rotateY;
                z = 0f;
                if (y == 270f) { x = 60f; }
                else { z = -60f; }
            }
            tesselator.TesselateShape(shapePath, shape, out var mesh, texture, new Vec3f(x, y, z));

            var offY = -0.83f;
            var offL = 0.47f;

            if (shapePath.Contains("/saltwater/"))
            {

                if (shapePath.Contains("coelacanth") || shapePath.Contains("grouper") || shapePath.Contains("mahi-mahi"))
                { 
                    offY = -1.1f;
                    offL = 0.56f;
                }
                if (shapePath.Contains("barracuda"))
                {
                    offY = -1f;
                    offL = 0.6f;
                }
                if (shapePath.Contains("sturgeon"))
                {
                    offY = -1f;
                    offL = 0.66f;
                }
                if (shapePath.Contains("haddock") || shapePath.Contains("pollock") || shapePath.Contains("gurnard"))
                {
                    offY = -0.88f;
                    offL = 0.50f;
                }
                if (shapePath.Contains("herring") || shapePath.Contains("mackerel"))
                {
                    offY = -0.7f;
                    offL = 0.45f;
                }
                if (shapePath.Contains("perch"))
                {
                    offY = -0.55f;
                    offL = 0.375f;
                }
                if (shapePath.Contains("amberjack") || shapePath.Contains("snapper"))
                {
                    offY = -0.88f;
                    offL = 0.5f;
                }

                if (y == 270f) 
                { 
                    mesh.Translate(-0.06f, offY, offL); 
                }
                else if (y == 450f) 
                { 
                    mesh.Translate(0.06f, offY, -offL); 
                }
                else if (y == 180f) 
                { 
                    mesh.Translate(-offL, offY, -0.06f); 
                }
                else if (y == 360f) 
                { 
                    mesh.Translate(offL, offY, 0.06f); 
                }

                
            }

            if (shapePath.Contains("catfish"))
            { mesh.Scale(new Vec3f(0.5f, 0, 0.5f), 0.7f, 0.7f, 0.7f); }
            if (shapePath.Contains("lure"))
            {
                var rando = Rnd.Next(10);
                if (rando == 0)
                { mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, 0, 5 * GameMath.DEG2RAD); }
                else if (rando == 1)
                { mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, 0, 355 * GameMath.DEG2RAD); }
            }

            if (alive) //let's animate these fishes
            {
                var flength = 0.7;
                if (shapePath.Contains("catfish"))
                { flength = 0.8; }
                else if (shapePath.Contains("bluegill"))
                { flength = 0.25; }
                if (shapePath.Contains("/saltwater/"))
                {
                    flength = offY * -1 - 0.2f;
                }
                    // 1.16
                    //var fishWave = VertexFlags.LeavesWindWaveBitMask | VertexFlags.WeakWaveBitMask;
                    var fishWave = EnumWindBitModeMask.ExtraWeakWind | VertexFlags.LiquidExposedToSkyBitMask; // LiquidWaterModeBitMask; 1.20

                for (var vertexNum = 0; vertexNum < mesh.GetVerticesCount(); vertexNum++)
                {
                    //tail only
                    if (mesh.xyz[(3 * vertexNum) + 1] < -0.2 - flength)
                    { mesh.Flags[vertexNum] |= fishWave; }
                    else
                    { mesh.Flags[vertexNum] |= 6144; }
                }
            }
            else
            {
                for (var vertexNum = 0; vertexNum < mesh.GetVerticesCount(); vertexNum++)
                { mesh.Flags[vertexNum] |= 6144; }
            }

            if ((this.Shape.rotateY == 0 || this.Shape.rotateY == 90) && !shapePath.Contains("-end") && !shapePath.Contains("-middle"))
            { mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, 180 * GameMath.DEG2RAD, 0); }



            return mesh;
        }


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            var block = world.BlockAccessor.GetBlock(neibpos, BlockLayersAccess.Default);
            if (block.BlockId <= 0) //block removed
            {
                block = world.BlockAccessor.GetBlock(neibpos.NorthCopy(), BlockLayersAccess.Default);
                if (block.FirstCodePart() == "limbtrotlinelure" && block.LastCodePart() != "east" && block.LastCodePart() != "west")
                { world.BlockAccessor.BreakBlock(neibpos.NorthCopy(), null); }
                block = world.BlockAccessor.GetBlock(neibpos.SouthCopy(), BlockLayersAccess.Default);
                if (block.FirstCodePart() == "limbtrotlinelure" && block.LastCodePart() != "east" && block.LastCodePart() != "west")
                { world.BlockAccessor.BreakBlock(neibpos.SouthCopy(), null); }
                block = world.BlockAccessor.GetBlock(neibpos.EastCopy(), BlockLayersAccess.Default);
                if (block.FirstCodePart() == "limbtrotlinelure" && block.LastCodePart() != "north" && block.LastCodePart() != "south")
                { world.BlockAccessor.BreakBlock(neibpos.EastCopy(), null); }
                block = world.BlockAccessor.GetBlock(neibpos.WestCopy(), BlockLayersAccess.Default);
                if (block.FirstCodePart() == "limbtrotlinelure" && block.LastCodePart() != "north" && block.LastCodePart() != "south")
                { world.BlockAccessor.BreakBlock(neibpos.WestCopy(), null); }
            }
        }


        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (world.BlockAccessor.GetBlockEntity(pos) is BELimbTrotLineLure bedc)
            { bedc.OnBreak(); } // (byPlayer, pos); } //empty the inventory onto the ground
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }


        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            blockSel = blockSel.Clone();
            var block = this.api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
            //var placed = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            block = this.api.World.GetBlock(block.CodeWithPath(block.Code.Path));
            this.api.World.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
            return false;
        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BELimbTrotLineLure bedc)
            //{ return bedc.OnInteract(world, byPlayer, blockSel); }
            { return bedc.OnInteract(byPlayer, blockSel); }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
