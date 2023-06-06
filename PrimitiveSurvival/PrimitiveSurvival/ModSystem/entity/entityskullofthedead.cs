namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.MathTools;
    //using System.Diagnostics;


    public class EntitySkullOfTheDead : EntityGenericGlowingAgent
    {
        public EntitySkullOfTheDead()
        { }

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

        private int cnt;
        public override void OnGameTick(float dt)
        {
            if (this.Api.Side == EnumAppSide.Server)
            {
                base.OnGameTick(dt);

                // Needed for GetWalkSpeedMultiplier(), less read those a little less often for performance
                if (this.cnt++ > 250)
                {
                    this.cnt = 0;
                    var targetEntity = (EntityAgent)this.Api.World.GetNearestEntity(this.Pos.XYZ, 15, 5, (e) =>
                    {
                        if (!e.Alive)
                        { return false; } //keep looking
                        var p = e.FirstCodePart();
                        if (p == "strawdummy")
                        { return true; } //straw dummy restricts attack range

                        if (p == "player" || p == "livingdead" || p == "skullofthedead" || p == "fireflies" || p == "butterfly" || p == "earthworm" || p == "beemob")
                        { return false; } //keep looking

                        return true; //found, attack, and stop looking for more
                    });

                    if (targetEntity != null)
                    {
                        if (targetEntity.FirstCodePart() != "strawdummy")
                        {
                            //Debug.WriteLine("Attacking " + targetEntity.FirstCodePart());
                            targetEntity.ReceiveDamage(new DamageSource { SourceEntity = null, Type = EnumDamageType.SlashingAttack }, 2);

                        }
                    }
                    else
                    {
                        //Debug.WriteLine("Nothing in range");
                    }
                }
            }
        }
    }
}
