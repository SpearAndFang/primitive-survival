namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Common;

    public class BlockAlcove : Block
    {

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


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (!playerSlot.Empty)
            {
                var playerStack = playerSlot.Itemstack;
                if (playerStack.Block != null)
                {
                    //1.16
                    if (playerStack.Block.Code.Path.Contains("torch-") && !playerStack.Block.Code.Path.Contains("extinct"))
                    {
                        Block blockToPlace = this;
                        var newPath = blockToPlace.Code.Path;
                        if (newPath.Contains("-unlit"))
                        {
                            newPath = newPath.Replace("-unlit", "-lit");
                            blockToPlace = this.api.World.GetBlock(blockToPlace.CodeWithPath(newPath));
                            world.BlockAccessor.SetBlock(blockToPlace.BlockId, blockSel.Position);
                            return true;
                        }
                    }
                }
                else if (playerStack.Item != null)
                {
                    if (playerStack.Item.Code.Path.Contains("candle"))
                    {
                        Block blockToPlace = this;
                        var newPath = blockToPlace.Code.Path;
                        if (newPath.Contains("-unlit"))
                        {
                            newPath = newPath.Replace("-unlit", "-lit");
                            blockToPlace = this.api.World.GetBlock(blockToPlace.CodeWithPath(newPath));
                            world.BlockAccessor.SetBlock(blockToPlace.BlockId, blockSel.Position);
                            return true;
                        }
                    }
                }
            }
            else
            {
                Block blockToPlace = this;
                var newPath = blockToPlace.Code.Path;
                if (newPath.Contains("-lit"))
                {
                    newPath = newPath.Replace("-lit", "-unlit");
                    blockToPlace = this.api.World.GetBlock(blockToPlace.CodeWithPath(newPath));
                    world.BlockAccessor.SetBlock(blockToPlace.BlockId, blockSel.Position);
                    return true;
                }
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
