namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;

    public class BEBombFuse : BlockEntity
    {
        private float remainingSeconds = 0;
        private string ignitedByPlayerUid;
        private float blastRadius;
        private float injureRadius;
        private EnumBlastType blastType;
        private ILoadedSound fuseSound;
        public static SimpleParticleProperties smallSparks;

        public IPlayer IgnitedPlayer;


        static BEBombFuse()
        {
            smallSparks = new SimpleParticleProperties(
                1, 1,
                ColorUtil.ToRgba(255, 255, 233, 0),
                new Vec3d(), new Vec3d(),
                new Vec3f(-3f, 5f, -3f),
                new Vec3f(3f, 8f, 3f),
                0.03f,
                1f,
                0.05f, 0.15f,
                EnumParticleModel.Quad
            )
            {
                VertexFlags = 255
            };
            smallSparks.AddPos.Set(1 / 16f, 0, 1 / 16f);
            smallSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.05f);
        }


        public virtual float FuseTimeSeconds => 4;


        public virtual EnumBlastType BlastType => this.blastType;

        public virtual float BlastRadius => this.blastRadius;

        public virtual float InjureRadius => this.injureRadius;


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            this.RegisterGameTickListener(this.OnTick, 50);


            if (this.fuseSound == null && api.Side == EnumAppSide.Client)
            {
                this.fuseSound = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("game:sounds/effect/fuse"),
                    ShouldLoop = true,
                    Position = this.Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 0.1f,
                    Range = 16,
                });
            }

            this.blastRadius = this.Block.Attributes["blastRadius"].AsInt(4);
            this.injureRadius = this.Block.Attributes["injureRadius"].AsInt(8);
            this.blastType = (EnumBlastType)this.Block.Attributes["blastType"].AsInt((int)EnumBlastType.OreBlast);
        }


        private static BlockPos[] AreaAround(BlockPos pos)
        {
            return new BlockPos[]
            {  pos.WestCopy(), pos.SouthCopy(), pos.EastCopy(), pos.NorthCopy(),
            pos.WestCopy().UpCopy(), pos.SouthCopy().UpCopy(), pos.EastCopy().UpCopy(), pos.NorthCopy().UpCopy(),
            pos.WestCopy().DownCopy(), pos.SouthCopy().DownCopy(), pos.EastCopy().DownCopy(), pos.NorthCopy().DownCopy()};
        }


        private void OnTick(float dt)
        {
            if (this.IsLit)
            {
                this.remainingSeconds -= dt;


                //light neighbor fuses
                if (this.remainingSeconds < 2)
                {
                    var neibBlockPos = AreaAround(this.Pos);
                    if (neibBlockPos != null)
                    {
                        foreach (var neib in neibBlockPos)
                        {
                            if (neib != null && this.IgnitedPlayer != null)
                            {
                                var befuse = this.IgnitedPlayer.Entity.World.BlockAccessor.GetBlockEntity(neib) as BEFuse;
                                befuse?.OnIgnite(this.IgnitedPlayer);
                            }
                        }
                    }
                }





                if (this.Api.Side == EnumAppSide.Server && this.remainingSeconds <= 0)
                {
                    this.Combust(dt);
                }

                if (this.Api.Side == EnumAppSide.Client)
                {
                    smallSparks.MinPos.Set(this.Pos.X + 0.45, this.Pos.Y + 0.53, this.Pos.Z + 0.45);
                    this.Api.World.SpawnParticles(smallSparks);
                }
            }
        }

        private void Combust(float dt)
        {
            if (this.NearToClaimedLand())
            {
                this.Api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/extinguish"), this.Pos.X + 0.5, this.Pos.Y, this.Pos.Z + 0.5, null, false, 16);
                this.IsLit = false;
                this.MarkDirty(true);
                return;
            }

            this.Api.World.BlockAccessor.SetBlock(0, this.Pos);
            ((IServerWorldAccessor)this.Api.World).CreateExplosion(this.Pos, this.BlastType, this.BlastRadius, this.InjureRadius);
        }

        public bool NearToClaimedLand()
        {
            var rad = (int)Math.Ceiling(this.BlastRadius);
            var exploArea = new Cuboidi(this.Pos.AddCopy(-rad, -rad, -rad), this.Pos.AddCopy(rad, rad, rad));
            var claims = (this.Api as ICoreServerAPI).WorldManager.SaveGame.LandClaims;
            for (var i = 0; i < claims.Count; i++)
            {
                if (claims[i].Intersects(exploArea))
                { return true; }
            }
            return false;
        }

        internal void OnBlockExploded(BlockPos pos)
        {
            if (this.Api.Side == EnumAppSide.Server)
            {
                if ((!this.IsLit || this.remainingSeconds > 0.3) && !this.NearToClaimedLand())
                {
                    this.Api.World.RegisterCallback(this.Combust, 250);
                }

            }
        }

        public bool IsLit { get; private set; }

        internal void OnIgnite(IPlayer byPlayer)
        {
            if (this.IsLit)
            {
                return;
            }

            if (this.Api.Side == EnumAppSide.Client)
            {
                this.fuseSound.Start();
            }

            this.IsLit = true;
            this.remainingSeconds = this.FuseTimeSeconds;
            this.ignitedByPlayerUid = byPlayer?.PlayerUID;
            this.IgnitedPlayer = byPlayer;
            this.MarkDirty();
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            this.remainingSeconds = tree.GetFloat("remainingSeconds", 0);
            this.IsLit = tree.GetInt("lit") > 0;
            this.ignitedByPlayerUid = tree.GetString("ignitedByPlayerUid");

            if (!this.IsLit && this.Api?.Side == EnumAppSide.Client)
            {
                this.fuseSound.Stop();
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("remainingSeconds", this.remainingSeconds);
            tree.SetInt("lit", this.IsLit ? 1 : 0);
            tree.SetString("ignitedByPlayerUid", this.ignitedByPlayerUid);
        }



        ~BEBombFuse()
        {
            if (this.fuseSound != null)
            {
                this.fuseSound.Dispose();
            }
        }


        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (this.fuseSound != null)
            {
                this.fuseSound.Stop();
            }
        }
    }
}
