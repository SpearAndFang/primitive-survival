namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.MathTools;
    //using System.Diagnostics;


    public class EntityFireflies : EntityGenericGlowingAgent
    {
        public EntityFireflies()
        { }


        public override void Initialize(EntityProperties properties, ICoreAPI api, long inChunkIndex3d)
        {
            base.Initialize(properties, api, inChunkIndex3d);
        }


        public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode)
        {
            if (!this.Alive || this.World.Side == EnumAppSide.Client || mode == 0)
            {
                base.OnInteract(byEntity, slot, hitPosition, mode);
                return;
            }
            var location = new AssetLocation(this.Code.Domain, this.Code.Path).ToString() + "-straight";
            //Debug.WriteLine(location);
            var stack = new ItemStack(byEntity.World.GetBlock(new AssetLocation(location)));
            if (!byEntity.TryGiveItemStack(stack))
            { byEntity.World.SpawnItemEntity(stack, this.ServerPos.XYZ); }
            this.World.PlaySoundAt(new AssetLocation("game:sounds/effect/latch"), this.Pos.X + 0.5, this.Pos.Y + 0.5, this.Pos.Z + 0.5, null, false, 16);
            this.Die(); //remove from the ground
            
            return;
        }
    }
}

