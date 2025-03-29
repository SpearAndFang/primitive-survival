namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Common.Entities;
    using PrimitiveSurvival.ModConfig;
    //using System.Diagnostics;

    public class BlockSpikeTrap : Block
    {
        public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
        {
            base.OnEntityInside(world, entity, pos);
            if (entity.Alive && entity.Class != "entityearthworm" && entity.Class != "EntityItem")
            {
                float motion = (float)Math.Abs(entity.Pos.Motion.X + entity.Pos.Motion.Y + entity.Pos.Motion.Z);
                //Debug.WriteLine(motion);
                if (motion > 0)
                {
                    //Debug.WriteLine("kill it fast" + motion.ToString() + entity.Class);
                    entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Block, SourceBlock = this, Type = EnumDamageType.PiercingAttack, SourcePos = pos.ToVec3d() }, motion + 0.1f * 10);
                }
            }
        }

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
