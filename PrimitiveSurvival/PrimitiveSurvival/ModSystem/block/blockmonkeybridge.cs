namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Common.Entities;
    //using System.Diagnostics;

    public class BlockMonkeyBridge : Block
    {

        public void BreakAbove(IWorldAccessor world, BlockPos neibpos)
        {
            var block = world.BlockAccessor.GetBlock(neibpos.UpCopy(), BlockLayersAccess.Default);
            if (block.FirstCodePart() == "monkeybridge" && block.FirstCodePart(1) == "null")
            { world.BlockAccessor.SetBlock(0, neibpos.UpCopy()); } //remove the null block with no drop 
        }


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            var block = world.BlockAccessor.GetBlock(neibpos, BlockLayersAccess.Default);
            var thisblock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            float dropQty;

            if (block.BlockId <= 0) //block removed
            {
                if (thisblock.Code.Path.Contains("monkeybridge-null"))
                {
                    if (pos.Y == neibpos.Y)
                    {
                        var belowpos = neibpos.DownCopy();
                        var belowblock = world.BlockAccessor.GetBlock(belowpos, BlockLayersAccess.Default);
                        if (belowblock.Code.Path.Contains("monkeybridge-middle"))
                        { return; }
                    }
                }
                if (pos.Y != neibpos.Y)
                {
                    if (thisblock.Code.Path.Contains("monkeybridge-end") == false)
                    { return; }
                }

                block = world.BlockAccessor.GetBlock(neibpos.NorthCopy(), BlockLayersAccess.Default);
                if (block.FirstCodePart() == "monkeybridge" && block.LastCodePart() != "east" && block.LastCodePart() != "west")
                {
                    if (block.FirstCodePart(1) != "null")
                    { dropQty = 1f; }
                    else
                    { dropQty = 0f; }
                    world.BlockAccessor.BreakBlock(neibpos.NorthCopy(), null, dropQty);
                    this.BreakAbove(world, neibpos.NorthCopy());
                }

                block = world.BlockAccessor.GetBlock(neibpos.SouthCopy(), BlockLayersAccess.Default);
                if (block.FirstCodePart() == "monkeybridge" && block.LastCodePart() != "east" && block.LastCodePart() != "west")
                {
                    if (block.FirstCodePart(1) != "null")
                    { dropQty = 1f; }
                    else
                    { dropQty = 0f; }
                    world.BlockAccessor.BreakBlock(neibpos.SouthCopy(), null, dropQty);
                    this.BreakAbove(world, neibpos.SouthCopy());
                }

                block = world.BlockAccessor.GetBlock(neibpos.EastCopy(), BlockLayersAccess.Default);
                if (block.FirstCodePart() == "monkeybridge" && block.LastCodePart() != "north" && block.LastCodePart() != "south")
                {
                    if (block.FirstCodePart(1) != "null")
                    { dropQty = 1f; }
                    else
                    { dropQty = 0f; }
                    world.BlockAccessor.BreakBlock(neibpos.EastCopy(), null, dropQty);
                    this.BreakAbove(world, neibpos.EastCopy());
                }

                block = world.BlockAccessor.GetBlock(neibpos.WestCopy(), BlockLayersAccess.Default);
                if (block.FirstCodePart() == "monkeybridge" && block.LastCodePart() != "north" && block.LastCodePart() != "south")
                {
                    if (block.FirstCodePart(1) != "null")
                    { dropQty = 1f; }
                    else
                    { dropQty = 0f; }
                    world.BlockAccessor.BreakBlock(neibpos.WestCopy(), null, dropQty);
                    this.BreakAbove(world, neibpos.WestCopy());

                }

                block = world.BlockAccessor.GetBlock(neibpos.DownCopy(), BlockLayersAccess.Default);
                if (block.FirstCodePart() == "monkeybridge" && thisblock.FirstCodePart() == "monkeybridge")
                {
                    if (block.FirstCodePart(1) != "null")
                    { dropQty = 1f; }
                    else
                    { dropQty = 0f; }
                    world.BlockAccessor.BreakBlock(neibpos.DownCopy(), null, dropQty);
                    this.BreakAbove(world, neibpos.DownCopy());
                }
            }
        }


        public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
        {
            //need to override this, otherwise sometimes the bridge just vanishes when you jump on it (and reappears when you reload the game)
            //other times, it breaks when you jump on it

            //to-do: based on collision speed, I could have it break when you jump on it...
            //Debug.WriteLine("speed " + collideSpeed.ToString());
            //base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
        }
    }
}
