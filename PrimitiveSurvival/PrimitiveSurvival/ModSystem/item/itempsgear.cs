namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;

    public class ItemPSGear : Item
    {


        public SimpleParticleProperties particlesHeld;


        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            this.particlesHeld = new SimpleParticleProperties(
                1, 1,
                ColorUtil.ToRgba(50, 220, 220, 220),
                new Vec3d(),
                new Vec3d(),
                new Vec3f(-0.1f, -0.1f, -0.1f),
                new Vec3f(0.1f, 0.1f, 0.1f),
                1.5f,
                0,
                0.5f,
                0.75f,
                EnumParticleModel.Cube
            )
            {
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.6f)
            };
            this.particlesHeld.AddPos.Set(0.1f, 0.1f, 0.1f);
            this.particlesHeld.addLifeLength = 0.5f;
            this.particlesHeld.RandomVelocityChange = true;
        }


        public override void InGuiIdle(IWorldAccessor world, ItemStack stack)
        {
            this.GuiTransform.Rotation.Y = GameMath.Mod(world.ElapsedMilliseconds / 50f, 360);
        }

        public override void OnGroundIdle(EntityItem entityItem)
        {
            this.GroundTransform.Rotation.Y = -GameMath.Mod(entityItem.World.ElapsedMilliseconds / 50f, 360);

            if (entityItem.World is IClientWorldAccessor)
            {
                this.particlesHeld.MinQuantity = 1;
                var pos = entityItem.SidedPos.XYZ;
                this.SpawnParticles(entityItem.World, pos, false);
            }
        }


        public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
        {
            if (byEntity.World is IClientWorldAccessor)
            {
                this.FpHandTransform.Rotation.Y = GameMath.Mod(-byEntity.World.ElapsedMilliseconds / 50f, 360);
                this.TpHandTransform.Rotation.Y = GameMath.Mod(-byEntity.World.ElapsedMilliseconds / 50f, 360);
            }
        }


        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefaultAction;
        }

        private void SpawnParticles(IWorldAccessor world, Vec3d pos, bool final)
        {
            if (final || world.Rand.NextDouble() > 0.8)
            {
                var h = 110 + world.Rand.Next(15);
                var v = 100 + world.Rand.Next(50);
                this.particlesHeld.MinPos = pos;
                this.particlesHeld.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(h, 100, v));
                this.particlesHeld.MinSize = 0.2f;
                this.particlesHeld.ParticleModel = EnumParticleModel.Quad;
                this.particlesHeld.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -150);
                this.particlesHeld.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(h, 100, v, 150));
                world.SpawnParticles(this.particlesHeld);
            }
        }
    }
}
