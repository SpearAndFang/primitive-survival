namespace PrimitiveSurvival.ModSystem
{
    using System.Collections.Generic;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Config;

    //1.17.pre.5 OBSOLETE - USE BLOCKSTAKEINWATER 
    public class BlockStake : Block
    {

        public string GetOrientations(IWorldAccessor world, BlockPos pos)
        {
            var orientations =
                this.GetFenceCode(world, pos, BlockFacing.NORTH) +
                this.GetFenceCode(world, pos, BlockFacing.EAST) +
                this.GetFenceCode(world, pos, BlockFacing.SOUTH) +
                this.GetFenceCode(world, pos, BlockFacing.WEST);
            if (orientations.Length == 0)
            { orientations = "empty"; }
            return orientations;
        }


        private string GetFenceCode(IWorldAccessor world, BlockPos pos, BlockFacing facing)
        {
            if (this.ShouldConnectAt(world, pos, facing))
            { return "" + facing.Code[0]; }
            return "";
        }


        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            bool placed;
            bool inwater;
            var pos = blockSel.Position;
            var block = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            if (block.Code.Path.Contains("stakeinwater-") || block.Code.Path.Contains("fishbasket-"))
            { return false; }
            inwater = block.LiquidCode == "water";
            var blockSelBelow = blockSel.Clone();
            blockSelBelow.Position.Y -= 1;
            var blockBelow = world.BlockAccessor.GetBlock(blockSelBelow.Position, BlockLayersAccess.Default);
            if (blockBelow.Fertility <= 0)
            {
                failureCode = Lang.Get("primitivesurvival:blockdesc-firework-suitable-ground-needed");
                return false;
            }
            var orientations = this.GetOrientations(world, pos);
            block = world.BlockAccessor.GetBlock(this.CodeWithVariant("type", orientations));
            if (block == null)
            { block = this; }
            placed = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (placed)
            {
                if (inwater)
                {
                    var newPath = block.Code.Path.Replace("stake", "stakeinwater");
                    block = this.api.World.GetBlock(block.CodeWithPath(newPath));
                    this.api.World.BlockAccessor.SetBlock(block.BlockId, pos);
                }
                return true;
            }
            return false;
        }


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            var orientations = this.GetOrientations(world, pos);
            var newBlockCode = this.CodeWithVariant("type", orientations);
            if (!this.Code.Equals(newBlockCode))
            {
                var block = world.BlockAccessor.GetBlock(newBlockCode);
                if (block == null)
                { return; }
                world.BlockAccessor.SetBlock(block.BlockId, pos);
                world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
            }
            base.OnNeighbourBlockChange(world, pos, neibpos);
        }


        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            return new BlockDropItemStack[] { new BlockDropItemStack(handbookStack) };
        }

        public bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
        {
            var block = world.BlockAccessor.GetBlock(ownPos.AddCopy(side), BlockLayersAccess.Default);
            return block.FirstCodePart() == this.FirstCodePart() || block.SideSolid[side.Opposite.Index];
        }


        private static readonly string[] OneDir = new string[] { "n", "e", "s", "w" };
        private static readonly string[] TwoDir = new string[] { "ns", "ew" };
        private static readonly string[] AngledDir = new string[] { "ne", "es", "sw", "nw" };
        private static readonly string[] ThreeDir = new string[] { "nes", "new", "nsw", "esw" };
        private static readonly string[] GateLeft = new string[] { "egw", "ngs" };
        private static readonly string[] GateRight = new string[] { "gew", "gns" };
        private static readonly Dictionary<string, KeyValuePair<string[], int>> AngleGroups = new Dictionary<string, KeyValuePair<string[], int>>();


        static BlockStake()
        {
            AngleGroups["n"] = new KeyValuePair<string[], int>(OneDir, 0);
            AngleGroups["e"] = new KeyValuePair<string[], int>(OneDir, 1);
            AngleGroups["s"] = new KeyValuePair<string[], int>(OneDir, 2);
            AngleGroups["w"] = new KeyValuePair<string[], int>(OneDir, 3);

            AngleGroups["ns"] = new KeyValuePair<string[], int>(TwoDir, 0);
            AngleGroups["ew"] = new KeyValuePair<string[], int>(TwoDir, 1);

            AngleGroups["ne"] = new KeyValuePair<string[], int>(AngledDir, 0);
            AngleGroups["nw"] = new KeyValuePair<string[], int>(AngledDir, 1);
            AngleGroups["es"] = new KeyValuePair<string[], int>(AngledDir, 2);
            AngleGroups["sw"] = new KeyValuePair<string[], int>(AngledDir, 3);

            AngleGroups["nes"] = new KeyValuePair<string[], int>(ThreeDir, 0);
            AngleGroups["new"] = new KeyValuePair<string[], int>(ThreeDir, 1);
            AngleGroups["nsw"] = new KeyValuePair<string[], int>(ThreeDir, 2);
            AngleGroups["esw"] = new KeyValuePair<string[], int>(ThreeDir, 3);


            AngleGroups["egw"] = new KeyValuePair<string[], int>(GateLeft, 0);
            AngleGroups["ngs"] = new KeyValuePair<string[], int>(GateLeft, 1);

            AngleGroups["gew"] = new KeyValuePair<string[], int>(GateRight, 0);
            AngleGroups["gns"] = new KeyValuePair<string[], int>(GateRight, 1);
        }


        public override AssetLocation GetRotatedBlockCode(int angle)
        {
            var type = this.Variant["type"];
            if (type == "empty" || type == "nesw")
            { return this.Code; }
            var angleIndex = angle / 90;
            var val = AngleGroups[type];
            var newFacing = val.Key[(angleIndex + val.Value) % val.Key.Length];
            return this.CodeWithVariant("type", newFacing);
        }
    }
}
