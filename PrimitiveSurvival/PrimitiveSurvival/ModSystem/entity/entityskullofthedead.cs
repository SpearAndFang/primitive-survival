namespace PrimitiveSurvival.ModSystem
{
    //using System;
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.MathTools;
    //using Vintagestory.GameContent;
    //using Vintagestory.API.Datastructures;
    //using System.Diagnostics;

    public class EntitySkullOfTheDead : EntityGenericGlowingAgent
    {
        public EntitySkullOfTheDead()
        {}

        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode)
        {
            if (!this.Alive || this.World.Side == EnumAppSide.Client || mode == 0)
            {
                base.OnInteract(byEntity, slot, hitPosition, mode);
                return;
            }
            var stack = new ItemStack(byEntity.World.GetBlock(new AssetLocation("primitivesurvival:skullofthedead-normal")));
            if (!byEntity.TryGiveItemStack(stack))
            { byEntity.World.SpawnItemEntity(stack, this.ServerPos.XYZ); }
            this.Die(); //remove from the ground
            return;
        }


        public override bool ShouldReceiveDamage(DamageSource damageSource, float damage)
        {
            //indestructable
            return false;
        }

        

        int cnt;
        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);

            // Needed for GetWalkSpeedMultiplier(), less read those a little less often for performance
            if (this.cnt++ > 500)
            {
                this.cnt = 0;
                var targetEntity = (EntitySkullOfTheDead)this.Api.World.GetNearestEntity(this.Pos.XYZ, 15, 5, (e) =>
                {
                    if (!e.Alive)
                    { return false; }
                    var p = e.FirstCodePart();
                    if (p == "player" || p == "livingdead" || p == "skullofthedead" || p == "fireflies" || p == "butterfly" || p == "earthworm" || p == "beemob" || p == "strawdummy")
                    { return false; }
                    //Debug.WriteLine(p);
                    e.ReceiveDamage(new DamageSource { SourceEntity = null, Type = EnumDamageType.SlashingAttack }, 1);
                    return false;
                });
            }
        }
    }
}
