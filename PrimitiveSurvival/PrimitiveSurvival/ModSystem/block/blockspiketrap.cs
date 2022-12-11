namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Common.Entities;
    using PrimitiveSurvival.ModConfig;

    public class BlockSpikeTrap : Block
    {

        public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
        {
            if (world.Side == EnumAppSide.Server && isImpact && facing.Axis == EnumAxis.Y) // && Math.Abs(collideSpeed.Y * 30) >= 0.25)
            {
                base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);

                if (entity.Alive)
                {
                    double fallIntoDamageMul = ModConfig.Loaded.FallDamageMultiplierMetalSpikes;
                    var block = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
                    if (block.Code.Path.Contains("woodspikes"))
                    {
                        fallIntoDamageMul = ModConfig.Loaded.FallDamageMultiplierWoodSpikes;
                    }
                    var dmg = (float)Math.Abs(collideSpeed.Y * fallIntoDamageMul);
                    entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Block, SourceBlock = this, Type = EnumDamageType.PiercingAttack, SourcePos = pos.ToVec3d() }, dmg);
                }
            }
        }
    }
}
