namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;
    using Vintagestory.GameContent;

    //using System.Diagnostics;

    public class BEFirework : BlockEntity
    {
        private float remainingSeconds = 0;
        private string ignitedByPlayerUid;
        private float blastRadius;
        private float injureRadius;
        private EnumBlastType blastType;
        private ILoadedSound fuseSound;
        private ILoadedSound fountainSound;
        private ILoadedSound beehiveSound;
        private ILoadedSound bamboobarrageSound;
        private ILoadedSound cherrybombSound;

        public static SimpleParticleProperties smallSparks;

        public IPlayer IgnitedPlayer;

        public ICoreClientAPI capi;


        // Initialize Variables

        private static readonly Random rand = new Random();

        private double storedInterval;
        private double interval;
        private bool killOffThread = false;
        private bool particleInitialized = false;
        public bool NeibChecked = false;

        static BEFirework()
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
            smallSparks.AddPos.Set(-0.1f, -0.3f, -0.1f);
            smallSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.05f);
        }


        public virtual float FuseTimeSeconds => 10;

        public virtual EnumBlastType BlastType => this.blastType;

        public virtual float BlastRadius => this.blastRadius;

        public virtual float InjureRadius => this.injureRadius;


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.capi = api as ICoreClientAPI;
            this.RegisterGameTickListener(this.OnTick, 50);
            if (this.fuseSound == null && api.Side == EnumAppSide.Client)
            {
                this.fuseSound = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("game:sounds/effect/fuse"),
                    ShouldLoop = true,
                    Position = this.Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 0.2f,
                    Range = 16,
                });
            }
            if (this.Block.LastCodePart() == "fountain")
            {
                if (this.fountainSound == null && api.Side == EnumAppSide.Client)
                {
                    this.fountainSound = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams()
                    {
                        Location = new AssetLocation("primitivesurvival:sounds/fireworks/fountain"),
                        ShouldLoop = true,
                        Position = this.Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = 0.7f,
                        Range = 16,
                    });
                }
            }
            else if (this.Block.LastCodePart() == "beehive")
            {
                if (this.beehiveSound == null && api.Side == EnumAppSide.Client)
                {
                    this.beehiveSound = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams()
                    {
                        Location = new AssetLocation("primitivesurvival:sounds/fireworks/beehive"),
                        ShouldLoop = true,
                        Position = this.Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = 0.9f,
                        Range = 16,
                    });
                }
            }
            else if (this.Block.LastCodePart() == "bamboobarrage")
            {
                if (this.bamboobarrageSound == null && api.Side == EnumAppSide.Client)
                {
                    this.bamboobarrageSound = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams()
                    {
                        Location = new AssetLocation("primitivesurvival:sounds/fireworks/bamboobarrage"),
                        ShouldLoop = true,
                        Position = this.Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = 0.9f,
                        Range = 16,
                    });
                }
            }
            else if (this.Block.LastCodePart() == "cherrybomb")
            {
                if (this.cherrybombSound == null && api.Side == EnumAppSide.Client)
                {
                    this.cherrybombSound = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams()
                    {

                        Location = new AssetLocation("primitivesurvival:sounds/fireworks/cherrybomb"),
                        ShouldLoop = false,
                        Position = this.Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = 0.9f,
                        Range = 16,
                    });
                }
            }
            this.blastRadius = this.Block.Attributes["blastRadius"].AsInt(4);
            this.injureRadius = this.Block.Attributes["injureRadius"].AsInt(8);
            this.blastType = (EnumBlastType)this.Block.Attributes["blastType"].AsInt((int)EnumBlastType.OreBlast);

            if (this.Block.LastCodePart() == "missile" || this.Block.LastCodePart() == "boomer")
            { this.storedInterval = 0.1 * 1000 / 2500; }
            else if (this.Block.LastCodePart() == "cherrybomb")
            { this.storedInterval = 0.05 * 1000 / 2500; }
            else //fountain, beehive, bamboobarrage
            { this.storedInterval = 0.01 * 1000 / 2500; }
            this.interval = this.storedInterval; //initialize interval
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
        }

        private void InitializeTimer()
        {
            if (this.Api.Side.IsClient())
            {
                this.capi.Event.RegisterAsyncParticleSpawner(this.UpdateParticles);
            }
        }


        private bool UpdateParticles(float dt, IAsyncParticleManager manager)
        {
            if (this.killOffThread)
            { return false; }

            this.interval -= dt;
            if (this.interval > 0)
            { return true; }
            else
            { this.interval = this.storedInterval; }
            this.GenerateParticles(manager);
            if (this.Block.LastCodePart() != "missile" && this.Block.LastCodePart() != "boomer")
            { return true; }
            return false;
        }



        private SimpleParticleProperties BuildPrimaryParticles(string type)
        {
            SimpleParticleProperties particles;
            if (type == "missile")
            {
                var color = ColorUtil.ToRgba(rand.Next(100, 255), rand.Next(100, 255), rand.Next(100, 255), rand.Next(100, 255));
                particles = new SimpleParticleProperties(
                    1, 1, // quantity
                    color,
                    new Vec3d(0.5, 1.5, 0.5), //min position
                    new Vec3d(), //add position - see below
                    new Vec3f(1.5f, 40f, 1.5f), //min velocity
                    new Vec3f(), //add velocity - see below
                    (float)((rand.NextDouble() * 0.25f) + 0.1f), //life length
                    (float)((rand.NextDouble() * 3f) + 0f), //gravity effect 
                    5f, 5f, //size
                    EnumParticleModel.Cube); // model

                particles.MinPos.Add(this.Pos); //add block position
                particles.AddPos.Set(new Vec3d(0, 0, 0)); //add position
                particles.AddVelocity.Set(new Vec3f(-1.5f, 10f, -1.5f)); //add velocity
                particles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 3f);
                particles.VertexFlags = 255;
                particles.WithTerrainCollision = true;
                particles.RandomVelocityChange = true;
            }
            else if (type == "fountain")
            {
                var color = ColorUtil.ToRgba(255, 50, rand.Next(200, 240), 0);

                particles = new SimpleParticleProperties(
                        30, 100, // quantity
                        color,
                        new Vec3d(0.5, 2, 0.5), //min position
                        new Vec3d(), //add position - see below
                        new Vec3f(0.5f, 7f, 0.5f), //min velocity
                        new Vec3f(), //add velocity - see below
                        (float)((rand.NextDouble() * 0.5f) + 0f), //life length
                        (float)((rand.NextDouble() * 0.3f) + 0f), //gravity effect 
                        0.1f, 1.2f, //size
                        EnumParticleModel.Cube); // model

                particles.MinPos.Add(this.Pos); //add block position
                particles.AddPos.Set(new Vec3d(0, -1.5, 0)); //add position
                particles.AddVelocity.Set(new Vec3f(-1f, -3f, -1f)); //add velocity
                particles.VertexFlags = 255;
                particles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 0.3f);
                particles.WithTerrainCollision = true;
                particles.RandomVelocityChange = true;

            }
            else if (type == "beehive")
            {
                var color = ColorUtil.ToRgba(rand.Next(100, 255), 255, 255, 10);

                particles = new SimpleParticleProperties(
                        4, 1, // quantity
                        color,
                        new Vec3d(0.5, 0.5, 0.5), //min position
                        new Vec3d(), //add position - see below
                        new Vec3f(4f, 20f, 4f), //min velocity
                        new Vec3f(), //add velocity - see below
                        (float)((rand.NextDouble() * 0.25f) + 0.1f), //life length
                        (float)((rand.NextDouble() * 3f) + 0f), //gravity effect 
                        4f, 4f, //size
                        EnumParticleModel.Cube); // model

                particles.MinPos.Add(this.Pos); //add block position
                particles.AddPos.Set(new Vec3d(0, 0, 0)); //add position
                particles.AddVelocity.Set(new Vec3f(-8f, 30f, -8f)); //add velocity
                particles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 3f);
                particles.VertexFlags = 255;
                particles.WithTerrainCollision = true;
                particles.RandomVelocityChange = true;

            }
            else if (type == "boomer")
            {
                var color = ColorUtil.ToRgba(rand.Next(140, 255), rand.Next(100, 255), rand.Next(100, 255), rand.Next(100, 255));

                particles = new SimpleParticleProperties(
                    1000, 0, // quantity
                    color,
                    new Vec3d(0.5, 0.5, 0.5), //min position
                    new Vec3d(), //add position - see below
                    new Vec3f(3f, 5f, 3f), //min velocity
                    new Vec3f(), //add velocity - see below
                    (float)((rand.NextDouble() * 0.95f) + 3.2f), //life length
                    0f, //gravity effect
                    1.8f, 0.8f, //size
                    EnumParticleModel.Cube); // model

                particles.MinPos.Add(this.Pos); //add block position
                particles.AddPos.Set(new Vec3d(0, 0, 0)); //add position
                particles.AddVelocity.Set(new Vec3f(-6f, -6f, -6f)); //add velocity
                particles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 0.3f);
                particles.VertexFlags = 255;
                particles.SelfPropelled = true;
                particles.ShouldDieInLiquid = true;
                particles.WindAffected = true;
                particles.WindAffectednes = 0.5f;
                particles.WithTerrainCollision = true;
                particles.RandomVelocityChange = true;

            }
            else if (type == "bamboobarrage")
            {
                var color = ColorUtil.ToRgba(rand.Next(150, 255), 255, rand.Next(100, 120), rand.Next(100, 120));
                particles = new SimpleParticleProperties(
                    1, 1, // quantity
                    color,
                    new Vec3d(0.5, 1.5, 0.5), //min position
                    new Vec3d(), //add position - see below
                    new Vec3f(5f, 60f, 5f), //min velocity
                    new Vec3f(), //add velocity - see below
                    (float)((rand.NextDouble() * 0.25f) + 1.5f), //life length
                    (float)((rand.NextDouble() * 0.8f) + 0.3f), //gravity effect 
                    1f, 1f, //size
                    EnumParticleModel.Quad); // model

                particles.MinPos.Add(this.Pos); //add block position
                particles.AddPos.Set(new Vec3d(0, 0, 0)); //add position
                particles.AddVelocity.Set(new Vec3f(-10f, -10f, -10f)); //add velocity
                particles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 6f);
                particles.VertexFlags = 255;
                particles.WithTerrainCollision = true;
                particles.RandomVelocityChange = true;
            }
            else //cherrybomb
            {
                var color = ColorUtil.ToRgba(rand.Next(100, 255), rand.Next(100, 255), rand.Next(100, 255), rand.Next(100, 255));

                particles = new SimpleParticleProperties(
                    1, 1, // quantity
                    color,
                    new Vec3d(0.5, 1, 0.5), //min position
                    new Vec3d(), //add position - see below
                    new Vec3f(13.5f, 15f, 13.5f), //min velocity
                    new Vec3f(), //add velocity - see below
                    (float)(rand.NextDouble() * .03f), //life length
                    (float)((rand.NextDouble() * 3f) + 0f), //gravity effect 
                    5f, 8f, //size
                    EnumParticleModel.Cube); // model

                particles.MinPos.Add(this.Pos); //add block position
                particles.AddPos.Set(new Vec3d(0, 0, 0)); //add position
                particles.AddVelocity.Set(new Vec3f(-27f, 10f, -27f)); //add velocity
                particles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 3f);
                particles.VertexFlags = 255;
                particles.SelfPropelled = true;
                particles.WindAffected = true;
                particles.WindAffectednes = 0.2f;
                particles.RandomVelocityChange = true;
            }

            return particles;
        }


        // Generate Particles - called from the timer (UpdateParticles)
        private void GenerateParticles(IAsyncParticleManager manager)
        {
            // Primary Particles
            var particles = this.BuildPrimaryParticles(this.Block.LastCodePart());

            if (this.Block.LastCodePart() == "missile")
            {
                var death = new AdvancedParticleProperties[]
                {
                        this.BuildDeathMissileParticles()
                };
                particles.DeathParticles = death;
            }
            else if (this.Block.LastCodePart() == "fountain")
            {
                var death = new AdvancedParticleProperties[]
                {
                    this.BuildDeathFountainParticles()
                };
                particles.DeathParticles = death;
            }
            else if (this.Block.LastCodePart() == "beehive")
            {
                var death = new AdvancedParticleProperties[]
                {
                    this.BuildDeathBeehiveParticles()
                };
                particles.DeathParticles = death;
            }
            else if (this.Block.LastCodePart() == "boomer")
            {
                //no death particles
            }
            else if (this.Block.LastCodePart() == "bamboobarrage")
            {
                var death = new AdvancedParticleProperties[]
                {
                    this.BuildDeathBambooBarrageParticles()
                };
                particles.DeathParticles = death;
            }
            else //charry bomb
            {
                var death = new AdvancedParticleProperties[]
                {
                    this.BuildDeathCherryBombParticles()
                };
                particles.DeathParticles = death;
            }
            manager.Spawn(particles);
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

                var lightTime = (int)this.FuseTimeSeconds - 1;
                if (this.Block.LastCodePart() == "missile" || this.Block.LastCodePart() == "boomer")
                {
                    lightTime = 1;  //override light time
                }
                //light neighbor fuses
                if (this.Api.Side == EnumAppSide.Server && this.remainingSeconds < lightTime && this.NeibChecked == false)
                {
                    this.NeibChecked = true; //ONLY CHECK ONCE
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

                //3.9 ashes wasn't working below - how about this
                if (this.remainingSeconds <= 0)
                {
                    Block.SpawnBlockBrokenParticles(this.Pos, null);
                    this.Api.World.BlockAccessor.BreakBlock(this.Pos, null, 0f);
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

                if (this.Block.LastCodePart() == "missile")
                {
                    if (this.remainingSeconds < 0.2)
                    {
                        if (this.Api.Side == EnumAppSide.Client)
                        { this.InitializeTimer(); }
                        else
                        {
                            var rndSound = rand.Next(1) + 1;
                            this.Api.World.PlaySoundAt(new AssetLocation("primitivesurvival:sounds/fireworks/whistlebang" + rndSound.ToString()), this.Pos.X + 0.5f, this.Pos.Y + 0.5f, this.Pos.Z + 0.5f, null);
                        }
                    }
                }
                else if (this.Block.LastCodePart() == "fountain")
                {
                    if (this.Api.Side == EnumAppSide.Client)
                    {
                        if (!this.particleInitialized)
                        {
                            this.InitializeTimer();
                            this.particleInitialized = true;
                            this.fountainSound.Start();
                        }
                        if (this.remainingSeconds < 0.1)
                        {
                            this.killOffThread = true;
                            this.fountainSound.Stop();
                        }
                    }
                }
                else if (this.Block.LastCodePart() == "beehive")
                {
                    if (this.Api.Side == EnumAppSide.Client)
                    {
                        if (!this.particleInitialized)
                        {
                            this.InitializeTimer();
                            this.particleInitialized = true;
                            this.beehiveSound.Start();
                        }
                        if (this.remainingSeconds < 0.1)
                        {
                            this.killOffThread = true;
                            this.beehiveSound.Stop();
                        }
                    }
                }
                else if (this.Block.LastCodePart() == "boomer")
                {
                    if (this.remainingSeconds < 0.2)
                    {
                        if (this.Api.Side == EnumAppSide.Client)
                        { this.InitializeTimer(); }
                        else
                        {
                            //var rndSound = rand.Next(1) + 1;
                            //this.Api.World.PlaySoundAt(new AssetLocation("primitivesurvival:sounds/fireworks/whistlebang" + rndSound.ToString()), this.Pos.X + 0.5f, this.Pos.Y + 0.5f, this.Pos.Z + 0.5f, null);
                        }
                    }
                }
                else if (this.Block.LastCodePart() == "bamboobarrage")
                {
                    if (this.Api.Side == EnumAppSide.Client)
                    {
                        if (!this.particleInitialized)
                        {
                            this.InitializeTimer();
                            this.particleInitialized = true;
                            this.bamboobarrageSound.Start();
                        }
                        if (this.remainingSeconds < 0.1)
                        {
                            this.killOffThread = true;
                            this.bamboobarrageSound.Stop();
                        }
                    }
                }
                else //cherry bomb
                {
                    if (this.Api.Side == EnumAppSide.Client)
                    {
                        if (!this.particleInitialized)
                        {
                            this.InitializeTimer();
                            this.particleInitialized = true;
                            this.cherrybombSound.Start();
                        }
                        if (this.remainingSeconds < 0.1)
                        {
                            this.killOffThread = true;
                            this.cherrybombSound.Stop();
                        }
                    }
                }
            }
        }

        private void Combust(float dt)
        {
            if (this.Block.LastCodePart() == "missile" || this.Block.LastCodePart() == "boomer")
            {
                ((IServerWorldAccessor)this.Api.World).CreateExplosion(this.Pos, this.BlastType, this.BlastRadius, this.InjureRadius); 
            }

            /* 3.9 wtf
            //ashes
            var minPos = this.Pos.ToVec3d().AddCopy(1f, 1f, 1f);
            var maxPos = this.Pos.ToVec3d().AddCopy(0f, -0.3f, -0f);
            var tmp = new Vec3f();
            for (var i = 0; i < 10; i++)
            {
                var color = this.Block.GetRandomColor(this.Api as ICoreClientAPI, this.Pos, BlockFacing.UP);
                tmp.Set(
                    1f - 2 * (float)this.Api.World.Rand.NextDouble(),
                    2 * (float)this.Api.World.Rand.NextDouble(),
                    1f - 2 * (float)this.Api.World.Rand.NextDouble()
                );
               // this.Api.World.SpawnParticles(10, color, minPos, maxPos, tmp, tmp, 1.5f, 1f, 0.25f + (float)this.Api.World.Rand.NextDouble() * 0.25f, EnumParticleModel.Cube, null);
            }
        
            this.Api.World.BlockAccessor.SetBlock(0, this.Pos);
            */
        }

        // 3.9
        internal void OnBlockExploded(BlockPos pos, string ignitedByPlayerUid)
        {
            //consider improving land claim via ignitedByPlayerUid
            if (this.Api.Side == EnumAppSide.Server)
            {
                if (!this.IsLit || this.remainingSeconds > 0.3)
                {
                    this.Api.World.RegisterCallback(this.Combust, 250);
                }
            }
        }

        public bool IsLit { get; private set; }

        internal void OnIgnite(IPlayer byPlayer)
        {
            if (this.IsLit)
            { return; }

            if (this.Api.Side == EnumAppSide.Client)
            { this.fuseSound.Start(); }
            this.IsLit = true;
            this.NeibChecked = false;
            
            this.remainingSeconds = this.FuseTimeSeconds;
            if (this.Block.LastCodePart() == "missile" || this.Block.LastCodePart() == "boomer")
            {
                this.remainingSeconds = 2; //override remaining seconds
            }
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


        private AdvancedParticleProperties BuildDeathMissileParticles()
        {
            var particles = new AdvancedParticleProperties()
            {
                GravityEffect = NatFloat.createUniform(0.28f, 0.1f),
                Size = NatFloat.createUniform(0.5f, 1.5f),
                LifeLength = NatFloat.createUniform(0.5f, 0.2f),
                Quantity = NatFloat.createUniform(800, 100),
                Velocity = new NatFloat[]
               {
            NatFloat.createUniform(5f, -10f),
            NatFloat.createUniform(3f, -10f),
            NatFloat.createUniform(5f, -10f)
               },
                ParticleModel = EnumParticleModel.Quad
            };
            particles.HsvaColor = null; //This must be null to use the Color parameter
            particles.Color = ColorUtil.ToRgba(rand.Next(150, 255), rand.Next(0, 255), rand.Next(0, 255), rand.Next(50, 255));
            particles.basePos = this.Pos.ToVec3d().AddCopy(0.5, 1.5, 0.5);
            particles.basePos = particles.basePos.Add(0.5 + (0 * rand.NextDouble()), 1.5 + (0 * rand.NextDouble()), 0.5 + (0 * rand.NextDouble()));
            particles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 1f);
            particles.VertexFlags = 255;
            particles.TerrainCollision = true;
            particles.WindAffectednes = 0.1f;
            return particles;
        }

        private AdvancedParticleProperties BuildDeathFountainParticles()
        {
            var particles = new AdvancedParticleProperties()
            {
                GravityEffect = NatFloat.createUniform(-0.1f, -0.2f),
                Size = NatFloat.createUniform(0.2f, 0.1f),
                LifeLength = NatFloat.createUniform(0.3f, 0.1f),
                Quantity = NatFloat.createUniform(1, 0),
                Velocity = new NatFloat[]
                {
                    NatFloat.createUniform(0.3f, -0.6f),
                    NatFloat.createUniform(1.1f, 0.1f),
                    NatFloat.createUniform(0.3f, -0.6f)
                },
                ParticleModel = EnumParticleModel.Quad
            };
            particles.HsvaColor = null; //This must be null to use the Color parameter
            particles.Color = ColorUtil.ToRgba(122, 22, rand.Next(122, 255), 22);
            particles.basePos = this.Pos.ToVec3d().AddCopy(0, 0, 0);
            particles.basePos = particles.basePos.Add(0 + (1 * rand.NextDouble()), 0 + (0 * rand.NextDouble()), 0 + (1 * rand.NextDouble()));
            //particles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.COSINUS, 1.5f);
            particles.VertexFlags = 194;
            particles.TerrainCollision = true;
            particles.WindAffectednes = 0.2f;
            particles.RandomVelocityChange = true;
            return particles;
        }

        private AdvancedParticleProperties BuildDeathBeehiveParticles()
        {
            var particles = new AdvancedParticleProperties()
            {
                GravityEffect = NatFloat.createUniform(-0.1f, 0f),
                Size = NatFloat.createUniform(0.5f, 1.5f),
                LifeLength = NatFloat.createUniform(1.3f, 0f),
                Quantity = NatFloat.createUniform(10, 0),
                Velocity = new NatFloat[]
                {
                    NatFloat.createUniform(5f, -10f),
                    NatFloat.createUniform(3f, -10f),
                    NatFloat.createUniform(5f, -10f)
                },
                ParticleModel = EnumParticleModel.Quad
            };
            particles.HsvaColor = null; //This must be null to use the Color parameter
            particles.Color = ColorUtil.ToRgba(rand.Next(150, 255), rand.Next(220, 255), rand.Next(220, 255), 50);
            particles.basePos = this.Pos.ToVec3d().AddCopy(0.5, 1.5, 0.5);
            particles.basePos = particles.basePos.Add(0.5 + (0 * rand.NextDouble()), 1.5 + (0 * rand.NextDouble()), 0.5 + (0 * rand.NextDouble()));
            particles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 1f);
            particles.VertexFlags = 255;
            particles.TerrainCollision = true;
            particles.WindAffectednes = 0.1f;
            particles.RandomVelocityChange = true;
            return particles;
        }


        private AdvancedParticleProperties BuildDeathBambooBarrageParticles()
        {
            var particles = new AdvancedParticleProperties()
            {
                GravityEffect = NatFloat.createUniform(0.28f, 0.1f),
                Size = NatFloat.createUniform(1f, 2f),
                LifeLength = NatFloat.createUniform(0.1f, 0.2f),
                Quantity = NatFloat.createUniform(100, 50),
                Velocity = new NatFloat[]
               {
            NatFloat.createUniform(5f, -10f),
            NatFloat.createUniform(3f, -10f),
            NatFloat.createUniform(5f, -10f)
               },
                ParticleModel = EnumParticleModel.Cube
            };
            particles.HsvaColor = null; //This must be null to use the Color parameter
            particles.Color = ColorUtil.ToRgba(rand.Next(150, 255), 255, rand.Next(100, 120), rand.Next(100, 120));
            particles.basePos = this.Pos.ToVec3d().AddCopy(0.5, 1.5, 0.5);
            particles.basePos = particles.basePos.Add(0.5 + (0 * rand.NextDouble()), 1.5 + (0 * rand.NextDouble()), 0.5 + (0 * rand.NextDouble()));
            particles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 1f);
            particles.VertexFlags = 255;
            particles.TerrainCollision = true;
            particles.WindAffectednes = 0.1f;
            return particles;
        }


        private AdvancedParticleProperties BuildDeathCherryBombParticles()
        {
            var particles = new AdvancedParticleProperties()
            {
                GravityEffect = NatFloat.createUniform(0.28f, 0f),
                Size = NatFloat.createUniform(0.2f, 0.5f),
                LifeLength = NatFloat.createUniform(0.1f, 0.05f),
                Quantity = NatFloat.createUniform(300, 700),
                Velocity = new NatFloat[]
                {
                    NatFloat.createUniform(3f, -6f),
                    NatFloat.createUniform(3f, -6f),
                    NatFloat.createUniform(3f, -6f)
                },
                ParticleModel = EnumParticleModel.Quad
            };
            particles.HsvaColor = null; //This must be null to use the Color parameter

            int r = 255;
            int g = 255;
            int b = 255;

            int flip = rand.Next(7);
            if (flip == 0 || flip == 3 || flip == 5)
            { r = 0; }
            if (flip == 1 || flip == 3 || flip == 4)
            { g = 0; }
            if (flip == 2 || flip == 4 || flip == 5)
            { b = 0; }

            particles.Color = ColorUtil.ToRgba(255, r, g, b);
            particles.basePos = this.Pos.ToVec3d().AddCopy(0.5, 1.5, 0.5);
            particles.basePos = particles.basePos.Add(0.5 + (0 * rand.NextDouble()), 1.5 + (0 * rand.NextDouble()), 0.5 + (0 * rand.NextDouble()));
            particles.VertexFlags = 255;
            particles.WindAffectednes = 0.1f;
            particles.RandomVelocityChange = true;
            return particles;
        }


        ~BEFirework()
        {
            if (this.fuseSound != null)
            {
                this.fuseSound.Dispose();
            }
            if (this.fountainSound != null)
            {
                this.fountainSound.Dispose();
            }
            if (this.beehiveSound != null)
            {
                this.beehiveSound.Dispose();
            }
            if (this.bamboobarrageSound != null)
            {
                this.bamboobarrageSound.Dispose();
            }
            if (this.particleInitialized)
            {
                this.killOffThread = true;
                this.InitializeTimer();
            }
        }


        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (this.fuseSound != null)
            { this.fuseSound.Stop(); }
            if (this.fountainSound != null)
            { this.fountainSound.Stop(); }
            if (this.beehiveSound != null)
            { this.beehiveSound.Stop(); }
            if (this.bamboobarrageSound != null)
            { this.bamboobarrageSound.Stop(); }
            if (this.particleInitialized)
            {
                this.killOffThread = true;
                this.InitializeTimer();
            }
        }
    }
}
