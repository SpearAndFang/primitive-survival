namespace PrimitiveSurvival.ModSystem
{
    using System.Collections.Generic;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.Config;
    //using Vintagestory.GameContent;

    public class BlockStakeInWater : Block
    {

        private string GetOrientations(IWorldAccessor world, BlockPos pos)
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


        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            var block = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            if (block.Class == "blockstakeinwater" && block.Code.Path.Contains("open"))
            {
                var weirSidesPos = new BlockPos[] { pos.EastCopy(), pos.WestCopy(), pos.NorthCopy(), pos.SouthCopy() };
                Block testBlock;
                foreach (var neighbor in weirSidesPos) //scan around this neighbor for a weirtrap and break it
                {
                    testBlock = world.BlockAccessor.GetBlock(neighbor, BlockLayersAccess.Default);
                    if (testBlock.Code.Path.Contains("weirtrap"))
                    {
                        world.BlockAccessor.BreakBlock(neighbor, null);
                    }
                }
            }
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);

            //1.17.pre.5 do not replace with water
            //world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation("water-still-7")).BlockId, pos);
            world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default).OnNeighbourBlockChange(world, pos, pos);
        }

        // JUST MOVED OVER FROM THE NOW OBSOLETE blockstake class
        // as it's a requirement
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            bool placed;
            //bool inwater;
            var pos = blockSel.Position;
            var block = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            if (block.Class == "blockstakeinwater" || block.Code.Path.Contains("fishbasket-"))
            { return false; }
            //inwater = block.LiquidCode == "water";
            var blockSelBelow = blockSel.Clone();
            blockSelBelow.Position.Y -= 1;
            var blockBelow = world.BlockAccessor.GetBlock(blockSelBelow.Position, BlockLayersAccess.Default);
            if (blockBelow.Fertility <= 0)
            {
                failureCode = Lang.Get("softer-ground-needed");
                return false;
            }
            var orientations = this.GetOrientations(world, pos);
            block = world.BlockAccessor.GetBlock(this.CodeWithVariant("type", orientations));
            if (block == null)
            { block = this; }
            placed = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (placed)
            {
                /*
                 * They're all stakeinwater now, so...
                if (inwater)
                {
                    var newPath = block.Code.Path.Replace("stake", "stakeinwater");
                    block = this.api.World.GetBlock(block.CodeWithPath(newPath));
                    this.api.World.BlockAccessor.SetBlock(block.BlockId, pos);
                }
                */
                return true;
            }
            return false;
        }


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            var neibBlock = world.BlockAccessor.GetBlock(neibpos, BlockLayersAccess.Default);
            var block = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);

            if (block.Class == "blockstakeinwater" && block.Code.Path.Contains("open"))
            {
                if (neibBlock.Class == "blockstakeinwater")
                { return; }
                else
                {
                    var weirSidesPos = new BlockPos[] { pos.EastCopy(), pos.WestCopy(), pos.NorthCopy(), pos.SouthCopy() };
                    Block testBlock;
                    foreach (var neighbor in weirSidesPos) //scan around this neighbor for a weirtrap and break it
                    {
                        testBlock = world.BlockAccessor.GetBlock(neighbor, BlockLayersAccess.Default);
                        if (testBlock.Code.Path.Contains("weirtrap"))
                        {
                            //this one ok
                            world.BlockAccessor.BreakBlock(neighbor, null);
                        }
                    }
                }
            }

            var orientations = this.GetOrientations(world, pos);
            var newBlockCode = this.CodeWithVariant("type", orientations);
            if (!this.Code.Equals(newBlockCode))
            {
                block = world.BlockAccessor.GetBlock(newBlockCode);
                if (block == null)
                { return; }
                world.BlockAccessor.SetBlock(block.BlockId, pos);
                world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
            }
            else
            {
                //added for 1.17 - stakes not breaking when ground below broken
                base.OnNeighbourBlockChange(world, pos, neibpos);
            }
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


        static BlockStakeInWater()
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


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            Block waterBlock;
            BlockPos waterPos;
            Block testBlock;
            BlockPos[] weirSidesPos;
            BlockPos[] weirBasesPos;

            var pos = blockSel.Position;
            var block = world.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
            var path = block.Code.Path;
            var facing = byPlayer.CurrentBlockSelection.Face;
            if (facing.IsHorizontal && (path.EndsWith("-ew") || path.EndsWith("-ns")))
            {
                if (facing.ToString() == "north")
                {
                    path = path.Replace("-ew", "-we");
                    waterPos = pos.NorthCopy();
                    weirSidesPos = new BlockPos[] { waterPos.EastCopy(), waterPos.WestCopy() };
                    weirBasesPos = new BlockPos[] { waterPos.NorthCopy(), waterPos.NorthCopy().EastCopy(), waterPos.NorthCopy().WestCopy() };

                }
                else if (facing.ToString() == "west")
                {
                    path = path.Replace("-ns", "-sn");
                    waterPos = pos.WestCopy();
                    weirSidesPos = new BlockPos[] { waterPos.NorthCopy(), waterPos.SouthCopy() };
                    weirBasesPos = new BlockPos[] { waterPos.WestCopy(), waterPos.WestCopy().NorthCopy(), waterPos.WestCopy().SouthCopy() };
                }
                else if (facing.ToString() == "south")
                {
                    waterPos = pos.SouthCopy();
                    weirSidesPos = new BlockPos[] { waterPos.EastCopy(), waterPos.WestCopy() };
                    weirBasesPos = new BlockPos[] { waterPos.SouthCopy(), waterPos.SouthCopy().EastCopy(), waterPos.SouthCopy().WestCopy() };
                }
                else //east
                {
                    waterPos = pos.EastCopy();
                    weirSidesPos = new BlockPos[] { waterPos.NorthCopy(), waterPos.SouthCopy() };
                    weirBasesPos = new BlockPos[] { waterPos.EastCopy(), waterPos.EastCopy().NorthCopy(), waterPos.EastCopy().SouthCopy() };
                }
                waterBlock = world.BlockAccessor.GetBlock(waterPos, BlockLayersAccess.Default);
                var areaOK = true;

                foreach (var neighbor in weirSidesPos) // Examine sides 
                {
                    testBlock = world.BlockAccessor.GetBlock(neighbor, BlockLayersAccess.Default);
                    if (testBlock.Class != "blockstakeinwater")
                    { areaOK = false; }
                }

                foreach (var neighbor in weirBasesPos) // Examine bases
                {
                    testBlock = world.BlockAccessor.GetBlock(neighbor, BlockLayersAccess.Default);
                    if (testBlock.BlockId == 0 || (testBlock.Code.Path.Contains("water") && (testBlock.Class != "blockstakeinwater")))
                    { areaOK = false; }
                }

                if (waterBlock.Code.Path.Contains("water") && areaOK)
                {
                    path += "open";
                    block = world.GetBlock(block.CodeWithPath(path));
                    world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);

                    testBlock = world.BlockAccessor.GetBlock(waterPos, BlockLayersAccess.Default); //make sure it isn't already a weir trap!!!
                    if (!testBlock.Code.Path.Contains("weirtrap"))
                    {
                        testBlock = world.BlockAccessor.GetBlock(new AssetLocation("primitivesurvival:weirtrap-" + facing.ToString()));
                        world.BlockAccessor.SetBlock(testBlock.BlockId, waterPos);
                    }
                    return true;
                }
            }

            //THIS COULD BE PROBLEMATIC TEST
            //THE WEIR TRAP EXTENSIVELY
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
            //return true;
        }


        public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
        {
            //This override to prevent server crashing when resetting weir trap
        }
    }
}
