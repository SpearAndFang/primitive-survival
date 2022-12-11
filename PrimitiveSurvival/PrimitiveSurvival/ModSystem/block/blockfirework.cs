namespace PrimitiveSurvival.ModSystem
{
    using System.Collections.Generic;
    using System.Linq;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    using Vintagestory.GameContent;
    //using System.Diagnostics;

    public class BlockFirework : Block
    {
        private WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Client)
            { return; }
            var capi = api as ICoreClientAPI;

            this.interactions = ObjectCacheUtil.GetOrCreate(api, "fireworkInteractions", () =>
            {
                var canIgniteStacks = new List<ItemStack>();

                foreach (var obj in api.World.Collectibles)
                {
                    if (obj is Block && (obj as Block).HasBehavior<BlockBehaviorCanIgnite>())
                    {
                        var stacks = obj.GetHandBookStacks(capi);
                        if (stacks != null)
                        { canIgniteStacks.AddRange(stacks); }
                    }
                }

                return new WorldInteraction[] {
                new WorldInteraction()
                {
                    MouseButton = EnumMouseButton.Right,
                    ActionLangCode = "primitivesurvival:blockhelp-firework-ignite",
                    Itemstacks = canIgniteStacks.ToArray(),
                    GetMatchingStacks = (wi, bs, es) => !(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BEFirework befirework) || befirework.IsLit ? null : wi.Itemstacks }
                };
            });
        }


        public override EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            if (!(byEntity.World.BlockAccessor.GetBlockEntity(pos) is BEFirework befirework) || befirework.IsLit)
            { return EnumIgniteState.NotIgnitablePreventDefault; }

            if (secondsIgniting > 0.75f)
            {
                return EnumIgniteState.IgniteNow;
            }
            return EnumIgniteState.Ignitable;
        }

        public override void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            if (secondsIgniting < 0.7f)
            { return; }

            handling = EnumHandling.PreventDefault;

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer player)
            { byPlayer = byEntity.World.PlayerByUid(player.PlayerUID); }
            if (byPlayer == null)
            { return; }

            var befirework = byPlayer.Entity.World.BlockAccessor.GetBlockEntity(pos) as BEFirework;
            befirework?.OnIgnite(byPlayer);
        }


        public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType)
        {
            //IMPORTANT NOTICE FOR MODDERS: If you override Block.OnBlockExploded() and don't call the base method you now must manually delete the block with "world.BulkBlockAccessor.SetBlock(0, pos);" or your block will become a source of infinite drops
            var befirework = world.BlockAccessor.GetBlockEntity(pos) as BEFirework;
            befirework?.OnBlockExploded(pos);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return this.interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }


        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            bool inwater;
            var pos = blockSel.Position;
            var block = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            inwater = block.LiquidCode == "water";
            var blockSelBelow = blockSel.Clone();
            blockSelBelow.Position.Y -= 1;
            var blockBelow = world.BlockAccessor.GetBlock(blockSelBelow.Position, BlockLayersAccess.Default);
            if ((blockBelow.Fertility <= 0) || inwater)
            {
                failureCode = Lang.Get("primitivesurvival:blockdesc-firework-suitable-ground-needed");
                return false;
            }
            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }
    }
}


