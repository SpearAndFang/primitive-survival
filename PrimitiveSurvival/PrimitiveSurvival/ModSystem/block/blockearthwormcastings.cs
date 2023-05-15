namespace PrimitiveSurvival.ModSystem
{
    using System.Linq;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using System.Collections.Generic;
    using Vintagestory.GameContent;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Config;
    using Vintagestory.API.Client;
    using Vintagestory.API.Server;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.Util;
    //using System.Diagnostics;

    public class BlockEarthwormCastings : Block
    {
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var list = new List<ItemStack>();
            for (var i = 0; i < 16; i++)
            {
                list.Add(new ItemStack(this.api.World.GetItem(new AssetLocation("primitivesurvival:earthwormcastings")), 1));
            }
            foreach(var itemStack in list)
            {
                var rnd= this.api.World.Rand.Next(0, 10);
                double d = rnd / 10;
                world.SpawnItemEntity(itemStack, pos.ToVec3d().Add(d + 0.5, 1.3 + d, d + 0.5));
            }
            world.BlockAccessor.SetBlock(0, pos, BlockLayersAccess.Default);
            world.BlockAccessor.MarkBlockDirty(pos);
        }
    }
}
