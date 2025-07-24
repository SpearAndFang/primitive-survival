namespace PrimitiveSurvival.ModSystem
{
    using System.Collections.Generic;
    using System.Linq;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    using Vintagestory.GameContent;


    public class BlockBombFuse : Block, IIgnitable

    {
        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api.Side != EnumAppSide.Client)
            {
                return;
            }

            var capi = api as ICoreClientAPI;

            this.interactions = ObjectCacheUtil.GetOrCreate(api, "bombInteractions", () =>
            {
                var canIgniteStacks = new List<ItemStack>();

                foreach (var obj in api.World.Collectibles)
                {
                    if (obj is Block && (obj as Block).HasBehavior<BlockBehaviorCanIgnite>())
                    {
                        var stacks = obj.GetHandBookStacks(capi);
                        if (stacks != null)
                        {
                            canIgniteStacks.AddRange(stacks);
                        }
                    }
                }

                return new WorldInteraction[] {
                new WorldInteraction()
                {
                    MouseButton = EnumMouseButton.Right,
                    ActionLangCode = "blockhelp-bomb-ignite",
                    Itemstacks = canIgniteStacks.ToArray(),
                    GetMatchingStacks = (wi, bs, es) => {
                        return !(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BEBombFuse bebomb) || bebomb.IsLit ? null : wi.Itemstacks;
                    }
                }
            };
            });
        }

        // 1.19
        EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
        {
            return secondsIgniting > 2 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        }


        public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            if (!(byEntity.World.BlockAccessor.GetBlockEntity(pos) is BEBombFuse bebomb) || bebomb.IsLit)
            {
                return EnumIgniteState.NotIgnitablePreventDefault;
            }

            if (secondsIgniting > 0.75f)
            {
                return EnumIgniteState.IgniteNow;
            }

            return EnumIgniteState.Ignitable;
        }

        //public override void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling) //1.18
        {
            if (secondsIgniting < 0.7f)
            {
                return;
            }

            handling = EnumHandling.PreventDefault;

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer)
            {
                byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
            }

            if (byPlayer == null)
            { return; }

            var bebomb = byPlayer.Entity.World.BlockAccessor.GetBlockEntity(pos) as BEBombFuse;
            bebomb?.OnIgnite(byPlayer);
        }

        // 3.9
        //public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType)
        public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, string ignitedByPlayerUid)
        {
            var bebomb = world.BlockAccessor.GetBlockEntity(pos) as BEBombFuse;
            // 3.9
            //bebomb?.OnBlockExploded(pos);
            bebomb?.OnBlockExploded(pos, ignitedByPlayerUid);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return this.interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}
