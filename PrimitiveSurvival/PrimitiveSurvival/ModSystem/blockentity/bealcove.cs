namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Text;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Config;

    public class BEAlcove : BlockEntity
    {

        private readonly int tickSeconds = 2;
        protected static readonly Random Rnd = new Random();


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side.IsServer())
            { this.RegisterGameTickListener(this.ParticleUpdate, this.tickSeconds * 1000); }
        }


        private void GenerateSmokeParticles(BlockPos pos, IWorldAccessor world)
        {
            float minQuantity = 0;
            float maxQuantity = 3;
            var color = ColorUtil.ToRgba(40, 15, 15, 15);
            var minPos = new Vec3d();
            var addPos = new Vec3d();
            var minVelocity = new Vec3f(0.2f, 0.0f, 0.2f);
            var maxVelocity = new Vec3f(0.6f, 0.4f, 0.6f);
            var lifeLength = 1f;
            var gravityEffect = -0.05f;
            var minSize = 0.1f;
            var maxSize = 0.5f;

            var smokeParticles = new SimpleParticleProperties(
                minQuantity, maxQuantity,
                color,
                minPos, addPos,
                minVelocity, maxVelocity,
                lifeLength,
                gravityEffect,
                minSize, maxSize,
                EnumParticleModel.Quad
            );
            smokeParticles.MinPos.Set(pos.ToVec3d().AddCopy(0.5, 0.5, 0.5));
            smokeParticles.AddPos.Set(new Vec3d(0.1, 0, 0));
            smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARINCREASE, 0.5f);
            smokeParticles.ShouldDieInAir = false;
            smokeParticles.SelfPropelled = true;
            world.SpawnParticles(smokeParticles);
        }


        public void ParticleUpdate(float par)
        {
            if (this.Block.Code.Path.Contains("-lit"))
            { this.GenerateSmokeParticles(this.Pos, this.Api.World); }
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default);
            if (block.Code.Path.Contains("-unlit"))
            {
                sb.Append(Lang.Get("primitivesurvival:blockdesc-alcove-unlit"));
                sb.AppendLine().AppendLine();
            }
            else if (block.Code.Path.Contains("-lit"))
            {
                sb.Append(Lang.Get("primitivesurvival:blockdesc-alcove-lit"));
                sb.AppendLine().AppendLine();
            }
        }
    }
}
