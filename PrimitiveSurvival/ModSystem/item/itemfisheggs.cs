namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Common;
    using Vintagestory.API.Server;
    using Vintagestory.API.MathTools;
    using PrimitiveSurvival.ModConfig;
    //using System.Diagnostics;

    public class ItemFishEggs : Item
    {
        //If you drop the ovulated eggs in water, replete that chunk by RepleteRate
        public override void OnGroundIdle(EntityItem entityItem)
        {
            base.OnGroundIdle(entityItem);

            var world = entityItem.World;
            if (world.Side != EnumAppSide.Server)
            { return; }

            if (entityItem.Swimming && world.Rand.NextDouble() < 0.01 && entityItem.Itemstack.Item.LastCodePart() == "ovulated")
            {
                //replete
                PrimitiveSurvivalSystem.UpdateChunkInDictionary(this.api as ICoreServerAPI, entityItem.ServerPos.AsBlockPos, -ModConfig.Loaded.FishEggsChunkRepletionRate);
                this.GenerateWaterParticles(entityItem.ServerPos.AsBlockPos, world);
                entityItem.Die(EnumDespawnReason.Removed, null);
            }
        }


        private void GenerateWaterParticles(BlockPos pos, IWorldAccessor world)
        {
            float minQuantity = 6;
            float maxQuantity = 12;
            var color = ColorUtil.ToRgba(44, 255, 196, 170);
            var minPos = new Vec3d();
            var addPos = new Vec3d();
            var minVelocity = new Vec3f(0f, 0f, 0f);
            var maxVelocity = new Vec3f(0.3f, 0.3f, 0.3f);
            var lifeLength = 2f;
            var gravityEffect = 0.03f;
            var minSize = 0.2f;
            var maxSize = 0.2f;

            var waterParticles = new SimpleParticleProperties(
                minQuantity, maxQuantity,
                color,
                minPos, addPos,
                minVelocity, maxVelocity,
                lifeLength,
                gravityEffect,
                minSize, maxSize,
                EnumParticleModel.Quad
            );
            waterParticles.MinPos.Set(pos.ToVec3d().AddCopy(0.5, 0, 0.5));
            //waterParticles.AddPos.Set(new Vec3d(0.5f, 02f, 0.5f));
            waterParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.IDENTICAL, 0.3f);
            waterParticles.ShouldDieInAir = false;
            waterParticles.SelfPropelled = true;
            world.SpawnParticles(waterParticles);
        }
    }
}
