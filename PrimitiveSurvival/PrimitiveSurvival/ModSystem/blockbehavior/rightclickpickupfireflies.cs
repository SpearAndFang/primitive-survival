namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    //using System.Diagnostics;


    public class RightClickPickupFireflies : BlockBehavior
    {
        public RightClickPickupFireflies(Block block) : base(block)
        {
        }


        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            var stacks = new ItemStack[] { this.block.OnPickBlock(world, blockSel.Position) };
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            { return false; }

            if (!byPlayer.Entity.Controls.Sneak && byPlayer.InventoryManager.ActiveHotbarSlot.Empty)
            {
                if (world.Side == EnumAppSide.Server)
                {
                    var thisBlock = world.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
                    var newBlock = "primitivesurvival:" + thisBlock.Code.Path.Replace("angled", "straight");
                    //Debug.WriteLine(newBlock);
                    var newStack = new ItemStack(world.GetBlock(new AssetLocation(newBlock)), 1);
                    if (byPlayer.InventoryManager.TryGiveItemstack(newStack, true))
                    {
                        world.BlockAccessor.SetBlock(0, blockSel.Position);
                        world.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
                        handling = EnumHandling.PreventDefault;
                        return true;
                    }
                }
                handling = EnumHandling.PreventDefault;
                return true;
            }
            return false;
        }


        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            return base.OnPickBlock(world, pos, ref handling);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handled)
        {
            return new WorldInteraction[]
            {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-behavior-rightclickpickup",
                        MouseButton = EnumMouseButton.Right,
                        RequireFreeHand = true
                    }
            }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handled));
        }
    }
}

