namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;
    using PrimitiveSurvival.ModConfig;
    //using System.Diagnostics;

    public class ItemPSFish : Item
    {
        private readonly int eggsPercent = ModConfig.Loaded.FishChanceOfEggsPercent;
        private readonly int repleteRate = ModConfig.Loaded.FishChunkDepletionRate;
        private static readonly Random Rnd = new Random();
        public override void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
        {
            base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);

            var rando = Rnd.Next(100);
            Item item;
            if (rando < this.eggsPercent)
            {
                rando = Rnd.Next(2);
                if (rando == 0)
                {
                    item = this.api.World.GetItem(new AssetLocation("primitivesurvival:fisheggs-raw-normal"));
                }
                else
                {
                    item = this.api.World.GetItem(new AssetLocation("primitivesurvival:fisheggs-raw-ovulated"));
                }
                var outStack = new ItemStack(item);
                this.api.World.SpawnItemEntity(outStack, new Vec3d(byPlayer.Entity.Pos.X + 0.5, byPlayer.Entity.Pos.Y + 0.5, byPlayer.Entity.Pos.Z + 0.5), null);
            }
        }


        public override void OnGroundIdle(EntityItem entityItem)
        {
            base.OnGroundIdle(entityItem);
            var world = entityItem.World;
            if (world.Side != EnumAppSide.Server)
            { return; }
            if (entityItem.Swimming && world.Rand.NextDouble() < 0.1 && entityItem.Itemstack.Item.LastCodePart() == "raw")
            {
                //replete
                PrimitiveSurvivalSystem.UpdateChunkInDictionary(this.api as ICoreServerAPI, entityItem.ServerPos.AsBlockPos, -this.repleteRate);
                this.GenerateWaterParticles(entityItem.ServerPos.AsBlockPos, world);
                entityItem.Die(EnumDespawnReason.Removed, null);
            }
        }


        private void GenerateWaterParticles(BlockPos pos, IWorldAccessor world)
        {
            float minQuantity = 16;
            float maxQuantity = 32;
            var color = ColorUtil.ToRgba(44, 231, 244, 200);
            var minPos = new Vec3d();
            var addPos = new Vec3d();
            var minVelocity = new Vec3f(0.3f, 0f, 0.3f);
            var maxVelocity = new Vec3f(2f, 0f, 2f);
            var lifeLength = 0.5f;
            var gravityEffect = 0f;
            var minSize = 0.1f;
            var maxSize = 0.3f;

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
            waterParticles.AddPos.Set(new Vec3d(0.5f, 0.5f, 0.5f));
            waterParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 0.1f);
            waterParticles.ShouldDieInAir = false;
            waterParticles.SelfPropelled = false;
            world.SpawnParticles(waterParticles);
        }
    }
}
