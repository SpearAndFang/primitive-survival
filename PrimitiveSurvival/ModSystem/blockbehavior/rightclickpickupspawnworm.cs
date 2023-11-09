namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using PrimitiveSurvival.ModConfig;
    //using System.Diagnostics;

    public class RightClickPickupSpawnWorm : BlockBehavior
    {
        protected static readonly Random Rnd = new Random();
        public RightClickPickupSpawnWorm(Block block) : base(block)
        {
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            var block = world.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
            var wormOdds = ModConfig.Loaded.WormFoundPercentRock; //10
            if (!block.Code.Path.Contains("flint") && !block.Code.Path.Contains("stick"))
            { wormOdds = ModConfig.Loaded.WormFoundPercentStickFlint; } //25
            var rando = Rnd.Next(100);
            //Debug.WriteLine("worm odds:" + wormOdds);
            //Debug.WriteLine("rando:" + rando);
            if (rando < wormOdds)
            {
                if (byPlayer.InventoryManager.ActiveHotbarSlot.Empty)
                {
                    if (world.Side == EnumAppSide.Server)
                    {
                        var blockSelBelow = blockSel.Clone();
                        blockSelBelow.Position.Y -= 1;
                        var blockBelow = world.BlockAccessor.GetBlock(blockSelBelow.Position, BlockLayersAccess.Default);
                        var conds = world.BlockAccessor.GetClimateAt(blockSelBelow.Position, EnumGetClimateMode.NowValues); //get the temperature too
                        //Debug.WriteLine(blockBelow.Code.Path);
                        //Debug.WriteLine("Temp:" + conds.Temperature);
                        if (blockBelow.Code.Path.Contains("soil") || blockBelow.Code.Path.Contains("forestfloor"))
                        {
                            if (conds.Temperature > 0 && conds.Temperature < 35)
                            {
                                var pos = blockSel.Position;
                                var type = world.GetEntityType(new AssetLocation("primitivesurvival:earthworm"));
                                rando = Rnd.Next(5);
                                if (rando == 0)
                                {
                                    rando = Rnd.Next(2);
                                    if (rando == 0)
                                    { type = world.GetEntityType(new AssetLocation("primitivesurvival:coachwhip")); }
                                    else
                                    { type = world.GetEntityType(new AssetLocation("primitivesurvival:pitviper")); }
                                }
                                var entity = world.ClassRegistry.CreateEntity(type);
                                if (entity != null)
                                {
                                    entity.ServerPos.X = pos.X + 0.5f;
                                    entity.ServerPos.Y = pos.Y + 0f;
                                    entity.ServerPos.Z = pos.Z + 0.5f;
                                    entity.ServerPos.Yaw = (float)Rnd.NextDouble() * 2 * GameMath.PI;
                                    world.SpawnEntity(entity);
                                    handling = EnumHandling.PreventDefault;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}
