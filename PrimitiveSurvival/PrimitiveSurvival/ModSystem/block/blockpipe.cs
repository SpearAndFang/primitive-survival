namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Common;
    //using System.Diagnostics;

    public class BlockPipe : Block
    {
        //actually places an invisible support, then puts the pipe in slot 0
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            failureCode = "__ignore__";
            return false;
        }
    }
}
