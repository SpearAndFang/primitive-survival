namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    //using System.Diagnostics;

    public class BlockPipe : Block
    {

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            /*
             blockSel - the empty block between the player and the pipe/support
                either the support or the empty block to the right of it
                or a pipe, or the empty block to the right of it

            target - the pipe/support
                either the support or the empty block to the right of it
                or a pipe, or the empty block to the right of it
            */

            var blockSelPos = blockSel.Position;
            var blockSelEmptyPos = blockSel.Position; //temporarily
            var targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
            var targetBlock = world.BlockAccessor.GetBlock(targetPos, BlockLayersAccess.Default);
            var facing = "north";

            if (!(targetBlock.FirstCodePart() == "support" || targetBlock.FirstCodePart() == "pipe"))
            { return false; } //not a valid target

            var dir = blockSel.Face.Code; //Debug.WriteLine("dir:" + dir);

            if (dir == "up" || dir == "down")
            { return false; } //cant place a pipe above or below a pipe//cant place a pipe above or below a pipe

            if (targetBlock.FirstCodePart() == "support")
            {
                var supportDir = targetBlock.LastCodePart();
                if ((dir == "north" || dir == "south") && (supportDir == "north" || supportDir == "south"))
                { return false; } //cant place a pipe on the side of a pipe
                if ((dir == "east" || dir == "west") && (supportDir == "east" || supportDir == "west"))
                { return false; } //cant place a pipe on the side of a pipe

                //determine the exact loction of the support be and adjust targetblock accordingly
                if (targetBlock.FirstCodePart(2) == "mainempty")
                {
                    blockSelEmptyPos = blockSel.Position;
                    if (supportDir == "west")
                    {
                        targetPos = targetPos.EastCopy();
                        blockSelPos = blockSelPos.EastCopy();
                        facing = "south";
                    }
                    else if (supportDir == "east")
                    {
                        targetPos = targetPos.WestCopy();
                        blockSelPos = blockSelPos.WestCopy();
                    }
                    else if (supportDir == "north")
                    {
                        targetPos = targetPos.SouthCopy();
                        blockSelPos = blockSelPos.SouthCopy();
                        facing = "west";
                    }
                    else //south
                    {
                        targetPos = targetPos.NorthCopy();
                        blockSelPos = blockSelPos.NorthCopy();
                        facing = "east";
                    }
                    targetBlock = world.BlockAccessor.GetBlock(targetPos, BlockLayersAccess.Default);
                }
                else //main
                {
                    if (supportDir == "west")
                    {
                        blockSelEmptyPos = blockSelPos.WestCopy();
                        facing = "south";
                    }
                    else if (supportDir == "east")
                    {
                        blockSelEmptyPos = blockSelPos.EastCopy();
                        facing = "north";
                    }
                    else if (supportDir == "north")
                    {
                        blockSelEmptyPos = blockSelPos.NorthCopy();
                        facing = "west";
                    }
                    else //south
                    {
                        blockSelEmptyPos = blockSelPos.SouthCopy();
                        facing = "east";
                    }
                }

                //make sure both destination blocks are air
                var placeBlock = world.BlockAccessor.GetBlock(blockSelPos, BlockLayersAccess.Default);
                var placeEmptyBlock = world.BlockAccessor.GetBlock(blockSelEmptyPos, BlockLayersAccess.Default);
                if (placeBlock.Code.Path != "air" || placeEmptyBlock.Code.Path != "air")
                { return false; } //Debug.WriteLine("one of the place blocks is not air");

                //Confirm matching wood type
                /*if (world.BlockAccessor.GetBlockEntity(targetPos) is BESupport be)
                {
                    if (!be.Inventory[0].Empty)
                    {
                        var tempStack = be.Inventory[0].Itemstack;
                        if (tempStack?.Block != null)
                        {
                            var tempBlock = world.GetBlock(tempStack.Block.Id);
                            if (tempBlock.FirstCodePart(1) != itemstack.Collectible.FirstCodePart(1))
                            { return false; } //Debug.WriteLine("not matching wood type");
                        }
                    }
                }*/
            }
            else //pipe
            {
                var pipeDir = targetBlock.LastCodePart();
                if ((dir == "north" || dir == "south") && (pipeDir == "east" || pipeDir == "west"))
                { return false; } //cant place a pipe on the side of a pipe
                if ((dir == "east" || dir == "west") && (pipeDir == "north" || pipeDir == "south"))
                { return false; } //cant place a pipe on the side of a pipe

                //determine the exact loction of the pipe and adjust targetblock accordingly
                if (targetBlock.FirstCodePart(3) == "empty")
                {
                    blockSelEmptyPos = blockSel.Position;
                    if (pipeDir == "west")
                    {
                        targetPos = targetPos.SouthCopy();
                        blockSelPos = blockSelPos.SouthCopy();
                    }
                    else if (pipeDir == "east")
                    {
                        targetPos = targetPos.NorthCopy();
                        blockSelPos = blockSelPos.NorthCopy();
                    }
                    else if (pipeDir == "north")
                    {
                        targetPos = targetPos.WestCopy();
                        blockSelPos = blockSelPos.WestCopy();
                    }
                    else //south
                    {
                        targetPos = targetPos.EastCopy();
                        blockSelPos = blockSelPos.EastCopy();
                    }
                    targetBlock = world.BlockAccessor.GetBlock(targetPos, BlockLayersAccess.Default);
                }
                else //main
                {
                    if (pipeDir == "west")
                    { blockSelEmptyPos = blockSelPos.NorthCopy(); }
                    else if (pipeDir == "east")
                    { blockSelEmptyPos = blockSelPos.SouthCopy(); }
                    else if (pipeDir == "north")
                    { blockSelEmptyPos = blockSelPos.EastCopy(); }
                    else //south
                    { blockSelEmptyPos = blockSelPos.WestCopy(); }
                }
                facing = pipeDir;

                //make sure both destination blocks are air
                var placeBlock = world.BlockAccessor.GetBlock(blockSelPos, BlockLayersAccess.Default);
                var placeEmptyBlock = world.BlockAccessor.GetBlock(blockSelEmptyPos, BlockLayersAccess.Default);
                //Debug.WriteLine("clear? " + placeBlock.Code.Path + ", " + placeEmptyBlock.Code.Path);
                if (placeBlock.Code.Path != "air" || placeEmptyBlock.Code.Path != "air")
                { return false; } //Debug.WriteLine("one of the place blocks is not air");

                //Confirm matching wood type
                //if (targetBlock.FirstCodePart(1) != itemstack.Collectible.FirstCodePart(1))
                //{ return false; } //Debug.WriteLine("not matching wood type");

                //Last check - needs to check for a nearby support
                if (!this.PipeHasSupport(world, facing, blockSelPos))
                { return false; }
            }

            // Time to lay some pipe
            var placeSel = new BlockSelection() { Position = blockSelPos, Face = BlockFacing.UP };
            bool placed;
            placed = base.TryPlaceBlock(world, byPlayer, itemstack, placeSel, ref failureCode);
            if (placed)
            {
                var pipePath = itemstack.Collectible.Code.Path;
                pipePath = "primitivesurvival:" + pipePath.Replace("north", facing);
                var pipeFinalPath = this.GetPipeType(world, pipePath, placeSel.Position);

                var newBlock = world.BlockAccessor.GetBlock(new AssetLocation(pipeFinalPath));
                if (newBlock != null)
                { world.BlockAccessor.SetBlock(newBlock.BlockId, placeSel.Position); }
                var tempBlockSel = new BlockSelection() { Position = blockSelEmptyPos, Face = BlockFacing.UP };
                var emptyPipePath = pipePath.Replace("none", "empty");
                newBlock = world.BlockAccessor.GetBlock(new AssetLocation(emptyPipePath));
                newBlock.DoPlaceBlock(world, byPlayer, tempBlockSel, itemstack);
                return placed;
            }
            return false;
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


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            var targetBlock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            if (targetBlock.FirstCodePart(3) != "empty")
            {
                var facing = targetBlock.LastCodePart();
                if (!this.PipeHasSupport(world, facing, pos))
                { this.BreakPipe(world, pos, null, 1); }

                targetBlock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
                if (targetBlock.FirstCodePart() == "pipe")
                {
                    var newPath = this.GetPipeType(world, "primitivesurvival:" + targetBlock.Code.Path, pos);
                    var newBlock = world.BlockAccessor.GetBlock(new AssetLocation(newPath));
                    if (newBlock != null)
                    { world.BlockAccessor.SetBlock(newBlock.BlockId, pos); }
                }
            }
            else
            { base.OnNeighbourBlockChange(world, pos, neibpos); }
        }


        private string GetPipeType(IWorldAccessor world, string blockPath, BlockPos pos)
        {
            var newPath = blockPath;
            BlockPos test1Pos;
            BlockPos test2Pos;

            var pipeNE = false;
            var pipeSW = false;
            var newBlock = world.BlockAccessor.GetBlock(new AssetLocation(blockPath));
            if (newBlock != null)
            {
                var facing = newBlock.LastCodePart();
                var type = newBlock.FirstCodePart(3);
                var nesw = "ne";
                if (type == "empty")
                { return newPath; }
                if (facing == "north" || facing == "south")
                {
                    test1Pos = pos.NorthCopy();
                    test2Pos = pos.SouthCopy();
                }
                else // east west
                {
                    test1Pos = pos.EastCopy();
                    test2Pos = pos.WestCopy();
                }

                var testBlock = world.BlockAccessor.GetBlock(test1Pos, BlockLayersAccess.Default);
                if ((testBlock.FirstCodePart() == "support" && facing != testBlock.LastCodePart()) || (testBlock.FirstCodePart() == "pipe" && facing == testBlock.LastCodePart()))
                {
                    pipeNE = true;
                    if (facing == "north" || facing == "east")
                    { nesw = "ne"; }
                    else
                    { nesw = "sw"; }
                }

                testBlock = world.BlockAccessor.GetBlock(test2Pos, BlockLayersAccess.Default);
                if ((testBlock.FirstCodePart() == "support" && facing != testBlock.LastCodePart()) || (testBlock.FirstCodePart() == "pipe" && facing == testBlock.LastCodePart()))
                {
                    pipeNE = true;
                    if (facing == "south" || facing == "west")
                    { nesw = "sw"; }
                    else
                    { nesw = "ne"; }
                }

                if (pipeNE && pipeSW)
                { newPath = newPath.Replace(type, "none"); }
                else if (pipeNE || pipeSW)
                {
                    newPath = newPath.Replace(type, "one" + nesw);
                }
                else
                { newPath = newPath.Replace(type, "two"); }
            }
            return newPath;
        }


        private bool PipeHasSupport(IWorldAccessor world, string facing, BlockPos blockSelPos)
        {
            //check blockSelPos to ensure that a pipe placed there (facing that direction) has connected pipe and a nearby support
            var dir1Ok = true;
            var dir2Ok = true;
            var support1Ok = false;
            var support2Ok = false;
            var test1Pos = blockSelPos;
            var test2Pos = blockSelPos;
            var maxLength = 3;

            if (facing == "north" || facing == "south")
            {
                for (var i = 0; i < maxLength; i++)
                {
                    if (dir1Ok)
                    {
                        test1Pos = test1Pos.NorthCopy();
                        var testBlock = world.BlockAccessor.GetBlock(test1Pos, BlockLayersAccess.Default);
                        if (!(testBlock.FirstCodePart() == "support" || testBlock.FirstCodePart() == "pipe"))
                        { dir1Ok = false; }
                        if (testBlock.FirstCodePart() == "support")
                        {
                            if (testBlock.LastCodePart() == "east" || testBlock.LastCodePart() == "west")
                            { support1Ok = true; }
                        }
                    }
                    if (dir2Ok)
                    {
                        test2Pos = test2Pos.SouthCopy();
                        var testBlock = world.BlockAccessor.GetBlock(test2Pos, BlockLayersAccess.Default);
                        if (!(testBlock.FirstCodePart() == "support" || testBlock.FirstCodePart() == "pipe"))
                        { dir2Ok = false; }
                        if (testBlock.FirstCodePart() == "support")
                        {
                            if (testBlock.LastCodePart() == "east" || testBlock.LastCodePart() == "west")
                            { support2Ok = true; }
                        }
                    }
                }
            }
            else //east west
            {
                for (var i = 0; i < maxLength; i++)
                {
                    if (dir1Ok)
                    {
                        test1Pos = test1Pos.EastCopy();
                        var testBlock = world.BlockAccessor.GetBlock(test1Pos, BlockLayersAccess.Default);
                        if (!(testBlock.FirstCodePart() == "support" || testBlock.FirstCodePart() == "pipe"))
                        { dir1Ok = false; }
                        if (testBlock.FirstCodePart() == "support")
                        {
                            if (testBlock.LastCodePart() == "north" || testBlock.LastCodePart() == "south")
                            { support1Ok = true; }
                        }
                    }
                    if (dir2Ok)
                    {
                        test2Pos = test2Pos.WestCopy();
                        var testBlock = world.BlockAccessor.GetBlock(test2Pos, BlockLayersAccess.Default);
                        if (!(testBlock.FirstCodePart() == "support" || testBlock.FirstCodePart() == "pipe"))
                        { dir2Ok = false; }
                        if (testBlock.FirstCodePart() == "support")
                        {
                            if (testBlock.LastCodePart() == "north" || testBlock.LastCodePart() == "south")
                            { support2Ok = true; }
                        }
                    }
                }
            }
            if (support1Ok == false && support2Ok == false)
            { return false; } //Debug.WriteLine("Can't place block here");
            return true;
        }

        private void BreakPipe(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            BlockPos neibPos;
            var testBlock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            if (testBlock != null)
            {
                var direction = testBlock.LastCodePart();
                if (testBlock.FirstCodePart(3) == "empty")
                {
                    world.BlockAccessor.SetBlock(0, pos);
                    if (direction == "south")
                    { neibPos = pos.EastCopy(); }
                    else if (direction == "north")
                    { neibPos = pos.WestCopy(); }
                    else if (direction == "east")
                    { neibPos = pos.NorthCopy(); }
                    else
                    { neibPos = pos.SouthCopy(); }
                    base.OnBlockBroken(world, neibPos, byPlayer, dropQuantityMultiplier);
                    this.NotifyNeighborsOfBlockChange(neibPos, world);
                }
                else
                {
                    base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
                    this.NotifyNeighborsOfBlockChange(pos, world);
                    if (direction == "north")
                    { neibPos = pos.EastCopy(); }
                    else if (direction == "south")
                    { neibPos = pos.WestCopy(); }
                    else if (direction == "west")
                    { neibPos = pos.NorthCopy(); }
                    else
                    { neibPos = pos.SouthCopy(); }
                    world.BlockAccessor.SetBlock(0, neibPos);
                }
            }
        }


        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        { this.BreakPipe(world, pos, byPlayer, 1); }
    }
}
