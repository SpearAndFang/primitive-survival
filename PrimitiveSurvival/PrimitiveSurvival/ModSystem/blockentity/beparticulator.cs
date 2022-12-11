namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Text;
    using System.Linq;
    using ProtoBuf;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    using Vintagestory.GameContent;
    //using System.Diagnostics;

    [ProtoContract]
    public class BEParticleData
    {
        // Particle data
        [ProtoMember(1)]

        public float minPosX;

        [ProtoMember(2)]

        public float minPosY;

        [ProtoMember(3)]

        public float minPosZ;


        [ProtoMember(4)]

        public float addPosX;

        [ProtoMember(5)]

        public float addPosY;

        [ProtoMember(6)]

        public float addPosZ;


        [ProtoMember(7)]

        public float minVelocityX;

        [ProtoMember(8)]

        public float minVelocityY;

        [ProtoMember(9)]

        public float minVelocityZ;


        [ProtoMember(10)]

        public float addVelocityX;

        [ProtoMember(11)]

        public float addVelocityY;

        [ProtoMember(12)]

        public float addVelocityZ;


        [ProtoMember(13)]

        public int colorA;

        [ProtoMember(14)]

        public int colorR;

        [ProtoMember(15)]

        public int colorG;

        [ProtoMember(16)]

        public int colorB;


        [ProtoMember(17)]

        public bool toggleRandomColor;

        [ProtoMember(18)]

        public int colorRndA;

        [ProtoMember(19)]

        public int colorRndR;

        [ProtoMember(20)]

        public int colorRndG;

        [ProtoMember(21)]

        public int colorRndB;


        [ProtoMember(22)]

        public int minQuantity;

        [ProtoMember(23)]

        public int maxQuantity;


        //life length needs max for the json i.e.  lifeLength: { avg: 2.5, var: 0.25 }
        [ProtoMember(24)]

        public float lifeLength;

        [ProtoMember(25)]

        public float maxLifeLength;


        //gravity effect can get complicated real fast gravityEffect: { dist: \"invexp\", avg: -0.07, var: 0.4 },\n";
        // make it like life length at the very least
        [ProtoMember(26)]

        public float gravityEffect;

        [ProtoMember(27)]

        public float maxGravityEffect;


        [ProtoMember(28)]

        public float minSize;

        [ProtoMember(29)]

        public float maxSize;


        [ProtoMember(30)]

        public bool toggleSizeTransform;

        [ProtoMember(31)]

        public string sizeTransform;

        [ProtoMember(32)]

        public float sizeEvolve;


        [ProtoMember(33)]

        public bool toggleOpacityTransform;

        [ProtoMember(34)]

        public string opacityTransform;

        [ProtoMember(35)]

        public float opacityEvolve;


        [ProtoMember(36)]

        public bool toggleRedTransform;

        [ProtoMember(37)]

        public string redTransform;

        [ProtoMember(38)]

        public float redEvolve;


        [ProtoMember(39)]

        public bool toggleGreenTransform;

        [ProtoMember(40)]

        public string greenTransform;

        [ProtoMember(41)]

        public float greenEvolve;


        [ProtoMember(42)]

        public bool toggleBlueTransform;

        [ProtoMember(43)]

        public string blueTransform;

        [ProtoMember(44)]

        public float blueEvolve;


        [ProtoMember(45)]

        public string model;


        [ProtoMember(46)]

        public bool selfPropelled;


        [ProtoMember(47)]

        public bool shouldDieInAir;


        [ProtoMember(48)]

        public bool shouldDieInLiquid;


        [ProtoMember(49)]

        public bool shouldSwimOnLiquid;


        [ProtoMember(50)]

        public bool bouncy;


        [ProtoMember(51)]

        public bool randomVelocityChange;


        [ProtoMember(52)]

        public bool noTerrainCollision;


        [ProtoMember(53)]

        public bool windAffected;

        [ProtoMember(54)]

        public float windAffectednes;


        [ProtoMember(55)]

        public bool toggleVertexFlags;


        [ProtoMember(56)]

        public int vertexFlags;


        [ProtoMember(57)]

        public string selectedParticle;


        [ProtoMember(58)]

        public bool particleEnabled;


        [ProtoMember(59)]

        public float particleInterval;


        [ProtoMember(60)]

        public string particleType;


        [ProtoMember(61)]

        public int linkX;


        [ProtoMember(62)]

        public int linkY;


        [ProtoMember(63)]

        public int linkZ;


        [ProtoMember(64)]

        public bool mainThread;


        [ProtoMember(65)]

        public bool useBlockColor;


        [ProtoMember(66)]

        public bool randomParticleColors;


        [ProtoMember(67)]

        public float timerPreDelay;

        [ProtoMember(68)]

        public float timerPostDelay;

        [ProtoMember(69)]

        public float timerDuration;

        [ProtoMember(70)]

        public bool timerLoop;


        [ProtoMember(71)]

        public int neibDeathX;


        [ProtoMember(72)]

        public int neibDeathY;


        [ProtoMember(73)]

        public int neibDeathZ;


        [ProtoMember(74)]

        public int neibSecX;


        [ProtoMember(75)]

        public int neibSecY;


        [ProtoMember(76)]

        public int neibSecZ;


        [ProtoMember(77)]

        public string name;


        public BEParticleData InitDefaults()
        {
            this.minPosX = 0.5f;
            this.minPosY = 0.5f;
            this.minPosZ = 0.5f;
            this.addPosX = 0f;
            this.addPosY = 0f;
            this.addPosZ = 0f;

            this.minVelocityX = 1f;
            this.minVelocityY = 1f;
            this.minVelocityZ = 1f;
            this.addVelocityX = -2f;
            this.addVelocityY = -2f;
            this.addVelocityZ = -2f;

            this.colorA = 0;
            this.colorR = 0;
            this.colorG = 0;
            this.colorB = 0;
            this.toggleRandomColor = true;
            this.colorRndA = 255;
            this.colorRndR = 255;
            this.colorRndG = 255;
            this.colorRndB = 255;

            this.minQuantity = 100;
            this.maxQuantity = 0;
            this.lifeLength = 0.15f;
            this.maxLifeLength = 0.2f;
            this.gravityEffect = 0f;
            this.maxGravityEffect = 0f;
            this.minSize = 0.2f;
            this.maxSize = 0.8f;

            this.toggleSizeTransform = false;
            this.sizeTransform = "Linear";
            this.sizeEvolve = 0f;

            this.toggleOpacityTransform = false;
            this.opacityTransform = "Linear";
            this.opacityEvolve = 0f;

            this.toggleRedTransform = false;
            this.redTransform = "Linear";
            this.redEvolve = 0f;

            this.toggleGreenTransform = false;
            this.greenTransform = "Linear";
            this.greenEvolve = 0f;

            this.toggleBlueTransform = false;
            this.blueTransform = "Linear";
            this.blueEvolve = 0f;

            this.model = "Cube";

            this.selfPropelled = false;
            this.shouldDieInAir = false;
            this.shouldDieInLiquid = false;
            this.shouldSwimOnLiquid = false;
            this.bouncy = false;
            this.randomVelocityChange = false;
            this.noTerrainCollision = false;
            this.windAffected = false;
            this.windAffectednes = 0f;
            this.toggleVertexFlags = true;
            this.vertexFlags = 255;

            this.selectedParticle = "";
            this.particleEnabled = true;
            this.particleType = "Primary";
            this.randomParticleColors = false;

            this.timerPreDelay = 0f;
            this.timerPostDelay = 0f;
            this.timerDuration = 0f;
            this.timerLoop = true;

            this.particleInterval = 0.05f;
            this.mainThread = false;
            this.useBlockColor = false;
            return this;
        }
    }

    public class BEParticulator : BlockEntity
    {

        public BEParticleData Data = new BEParticleData();


        private GuiDialogParticulator invDialog;
        private static readonly Random Rnd = new Random();


        public long particleUpdateTick = 0;


        //public readonly int intervalMultiplier = 1000;
        private float offThreadInterval;

        public bool killOffThread;



        public double timeElapsed;

        //private BlockParticulator ownBlock;


        public ICoreClientAPI capi;

        private readonly bool testMode = false;  //TEST SOME AUTOGENERATED CODE

        public BEParticulator()
        { }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            //this.ownBlock = api.World.BlockAccessor.GetBlock(this.Pos) as BlockParticulator;
            this.capi = api as ICoreClientAPI;

            if (this.Api.Side.IsClient())
            {
                if (this.Data.particleEnabled)
                {
                    BlockParticulator.ScanAround(this.Api.World, this.Pos);
                }
            }
            this.Link(new BlockPos(this.Data.linkX, this.Data.linkY, this.Data.linkZ));
            this.InitOffthread();
        }


        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            if ((byItemStack.Block as BlockParticulator) != null)
            {
                var settings = (byItemStack.Block as BlockParticulator).GetSettings(byItemStack);

                if (settings == null)
                {
                    this.Data = new BEParticleData().InitDefaults();
                    this.Data.linkX = this.Pos.X;
                    this.Data.linkY = this.Pos.Y;
                    this.Data.linkZ = this.Pos.Y;
                }
                else
                {
                    this.Data = SerializerUtil.Deserialize<BEParticleData>(settings);
                }
                if (this.Api.Side.IsClient())
                {
                    if (this.Data.particleEnabled)
                    {
                        BlockParticulator.ScanAround(this.Api.World, this.Pos);
                    }
                }
                this.Link(new BlockPos(this.Data.linkX, this.Data.linkY, this.Data.linkZ));
                this.InitOffthread();
            }
        }

        public void InitOffthread()
        {
            if (this.Api.Side.IsClient())
            {
                this.capi.Event.RegisterAsyncParticleSpawner(this.OffThreadParticleUpdate);
            }
        }


        public void Link(BlockPos linkPos)
        {
            //Debug.WriteLine("Link");
            this.Data.linkX = linkPos.X;
            this.Data.linkY = linkPos.Y;
            this.Data.linkZ = linkPos.Z;

            //loop init
            this.timeElapsed = 0;

            if (this.Api.Side.IsServer())
            {
                this.UnregisterGameTickListener(this.particleUpdateTick);
            }

            if (this.Data.mainThread)
            {
                if (this.Api.Side.IsServer())
                {
                    this.particleUpdateTick = this.RegisterGameTickListener(this.MainThreadParticleUpdate, (int)(this.Data.particleInterval * 1000));
                }
            }
            else
            {
                this.offThreadInterval = this.Data.particleInterval * 1000 / 2500f;
            }
            //does this need to be here?
            this.MarkDirty();
        }


        private void MainThreadParticleUpdate(float dt)
        {
            if (this.testMode)
            {
                //this.MainThreadListenerTest(dt);
                return;
            }

            if (!this.Data.particleEnabled || (!this.Data.timerLoop && this.Data.timerDuration == 0))
            { return; }

            var totalTime = this.Data.timerDuration + this.Data.timerPreDelay + this.Data.timerPostDelay;

            if (this.Data.timerDuration == 0)
            { totalTime = 0; }

            //loop or no loop, always elapse time until we hit the max
            if (this.timeElapsed <= totalTime)
            { this.timeElapsed += this.Data.particleInterval; }
            else
            {
                //reset the timer when looping
                if (this.Data.timerLoop)
                { this.timeElapsed = 0; }
            }

            if (totalTime > 0)
            {
                // handle no timerLoop, predelay, postdelay
                if ((this.timeElapsed > totalTime) || (this.timeElapsed <= this.Data.timerPreDelay) || (this.timeElapsed > this.Data.timerPreDelay + this.Data.timerDuration))
                { return; }
            }

            //made it this far, I think we can actually generate a particle
            if (this.particleUpdateTick >= 0)
            {
                this.GenerateParticles(this.Data, null);
            }
        }


        private bool OffThreadParticleUpdate(float dt, IAsyncParticleManager manager)
        {

            if (this.testMode)
            {
                //this.OffThreadListenerTest(dt, manager);
                return true;
            }

            //returning false kills the listener completely
            if (this.killOffThread)
            { return false; }

            //particle disabled OR not offthread
            if (!this.Data.particleEnabled || this.Data.mainThread || (!this.Data.timerLoop && this.Data.timerDuration == 0))
            { return true; }

            this.offThreadInterval -= dt;
            if (this.offThreadInterval > 0f)
            { return true; }
            else
            { this.offThreadInterval = this.Data.particleInterval * 1000 / 2500f; }

            var totalTime = this.Data.timerDuration + this.Data.timerPreDelay + this.Data.timerPostDelay;

            if (this.Data.timerDuration == 0)
            { totalTime = 0; }

            //loop or no loop, always elapse time until we hit the max
            if (this.timeElapsed <= totalTime)
            { this.timeElapsed += this.Data.particleInterval; }
            else
            {
                //reset the timer when looping
                if (this.Data.timerLoop)
                { this.timeElapsed = 0; }
            }

            if (totalTime > 0)
            {
                // handle no timerLoop, predelay, postdelay
                if ((this.timeElapsed > totalTime) || (this.timeElapsed <= this.Data.timerPreDelay) || (this.timeElapsed > this.Data.timerPreDelay + this.Data.timerDuration))
                { return true; }
            }

            //made it this far, I think we can actually generate a particle
            this.GenerateParticles(this.Data, manager);
            return true;
        }


        public int Highest(params int[] inputs)
        {
            return inputs.Max();
        }

        public int Lowest(params int[] inputs)
        {
            return inputs.Min();
        }

        private SimpleParticleProperties BuildSimpleParticles(BEParticleData temp, string ptype)
        {
            Vec3d minPos, maxPos;
            minPos = new Vec3d(0.5f, 0.5f, 0.5f);
            maxPos = new Vec3d(); //new Vec3d(0.5f, 0.5f, 0.5f);

            if (ptype == "Primary")
            {
                minPos = new Vec3d(temp.minPosX, temp.minPosY, temp.minPosZ);
            }

            float minQuantity = temp.minQuantity;
            float maxQuantity = temp.maxQuantity;

            int color;
            if (temp.useBlockColor)
            {
                var block = this.Api.World.BlockAccessor.GetBlock(temp.linkX, temp.linkY, temp.linkZ, BlockLayersAccess.Default);
                color = block.GetRandomColor(this.Api as ICoreClientAPI, this.Pos, BlockFacing.UP);// | (0xff << 24);
            }
            else
            {
                if (!temp.toggleRandomColor)
                {
                    temp.colorRndA = temp.colorA;
                    temp.colorRndR = temp.colorR;
                    temp.colorRndG = temp.colorG;
                    temp.colorRndB = temp.colorB;
                }
                var a = new[] { temp.colorA, temp.colorRndA };
                var r = new[] { temp.colorR, temp.colorRndR };
                var g = new[] { temp.colorG, temp.colorRndG };
                var b = new[] { temp.colorB, temp.colorRndB };
                color = ColorUtil.ToRgba(Rnd.Next(a.Min(), a.Max()), Rnd.Next(r.Min(), r.Max()), Rnd.Next(g.Min(), g.Max()), Rnd.Next(b.Min(), b.Max()));
            }

            double windAdj = 0;
            if (temp.windAffected && temp.mainThread) //only need to set this in the main thread
            {
                var windspeed = this.Api.ModLoader.GetModSystem<WeatherSystemBase>()?.WeatherDataSlowAccess.GetWindSpeed(this.Pos.ToVec3d()) ?? 0;
                windAdj = windspeed * temp.windAffectednes;
            }
            var minVelocity = new Vec3f(temp.minVelocityX + (float)windAdj, temp.minVelocityY, temp.minVelocityZ);
            var maxVelocity = new Vec3f();

            //simple particles don't have a lifelength variance, so I did this
            var dLL = temp.lifeLength;
            var dMLL = temp.lifeLength;
            if (temp.maxLifeLength != 0)
            {
                dMLL = temp.maxLifeLength;
                if (dLL > dMLL)
                { dLL = dMLL; dMLL = temp.lifeLength; }
            }

            //simple particles don't have a gravity effect variance, so I did this
            var dGE = temp.gravityEffect;
            var dMGE = temp.gravityEffect;
            if (temp.maxGravityEffect != 0)
            {
                dMGE = temp.maxGravityEffect;
                if (dGE > dMGE)
                { dGE = dMGE; dMGE = temp.gravityEffect; }
            }

            //var gravityEffect = temp.gravityEffect;

            Enum.TryParse(temp.model, out EnumParticleModel thisModel);

            var particles = new SimpleParticleProperties(
                minQuantity, maxQuantity,
                color,
                minPos, maxPos,  //maxPos is fucky - see above
                minVelocity, maxVelocity, //maxVelocity same?
                (float)((Rnd.NextDouble() * (dMLL - dLL)) + dLL), //lifelength
                (float)((Rnd.NextDouble() * (dMGE - dGE)) + dGE), //graavity effect
                temp.minSize, temp.maxSize,
                thisModel
                );

            if (ptype == "Primary")
            {
                particles.MinPos.Add(new Vec3d(this.Data.linkX, this.Data.linkY, this.Data.linkZ));

            }
            particles.AddPos.Set(new Vec3d(temp.addPosX, temp.addPosY, temp.addPosZ));
            particles.AddVelocity.Set(new Vec3f(temp.addVelocityX + (float)windAdj, temp.addVelocityY, temp.addVelocityZ));

            if (temp.toggleSizeTransform)
            {
                Enum.TryParse(temp.sizeTransform.ToUpper(), out EnumTransformFunction thisSizeTransform);
                particles.SizeEvolve = new EvolvingNatFloat(thisSizeTransform, temp.sizeEvolve);
            }
            if (temp.toggleOpacityTransform)
            {
                Enum.TryParse(temp.opacityTransform.ToUpper(), out EnumTransformFunction thisOpacityTransform);
                particles.OpacityEvolve = new EvolvingNatFloat(thisOpacityTransform, temp.opacityEvolve);
            }

            if (temp.toggleRedTransform)
            {
                Enum.TryParse(temp.redTransform.ToUpper(), out EnumTransformFunction thisRedTransform);
                particles.RedEvolve = new EvolvingNatFloat(thisRedTransform, temp.redEvolve);
            }
            if (temp.toggleGreenTransform)
            {
                Enum.TryParse(temp.greenTransform.ToUpper(), out EnumTransformFunction thisGreenTransform);
                particles.GreenEvolve = new EvolvingNatFloat(thisGreenTransform, temp.greenEvolve);
            }
            if (temp.toggleBlueTransform)
            {
                Enum.TryParse(temp.blueTransform.ToUpper(), out EnumTransformFunction thisBlueTransform);
                particles.BlueEvolve = new EvolvingNatFloat(thisBlueTransform, temp.blueEvolve);
            }

            if (temp.toggleVertexFlags)
            {
                particles.VertexFlags = temp.vertexFlags;
            }

            if (temp.selfPropelled)
            { particles.SelfPropelled = true; }
            if (temp.shouldDieInAir)
            { particles.ShouldDieInAir = true; }
            if (temp.shouldDieInLiquid)
            { particles.ShouldDieInLiquid = true; }

            if (!temp.mainThread) //none of the stuff below works in Main Thread
            {
                if (temp.windAffected) // handled above for main thread
                {
                    particles.WindAffected = temp.windAffected;
                    particles.WindAffectednes = temp.windAffectednes;
                }

                if (temp.noTerrainCollision)
                { particles.WithTerrainCollision = false; }
                if (temp.shouldSwimOnLiquid)
                { particles.ShouldSwimOnLiquid = true; }
                if (temp.bouncy)
                {

                    // CHANGED FOR 1.17
                    particles.Bounciness = 0.7f;
                    //particles.Bouncy = true;
                }
                if (temp.randomVelocityChange)
                { particles.RandomVelocityChange = true; }
            }

            return particles;
        }

        //ONLY USED FOR SECONDARY/DEATH PARTICLES
        private AdvancedParticleProperties BuildAdvancedParticles(BEParticleData temp, string ptype)
        {
            //It appears that death particles don't even need a position (or simply ignore it)
            //they might support add though, so lets set minpos anyways.
            if (!temp.toggleRandomColor && !temp.randomParticleColors)
            {
                temp.colorRndA = temp.colorA;
                temp.colorRndR = temp.colorR;
                temp.colorRndG = temp.colorG;
                temp.colorRndB = temp.colorB;
            }
            var a = new[] { temp.colorA, temp.colorRndA };
            var r = new[] { temp.colorR, temp.colorRndR };
            var g = new[] { temp.colorG, temp.colorRndG };
            var b = new[] { temp.colorB, temp.colorRndB };

            Enum.TryParse(temp.model, out EnumParticleModel thisModel);
            var particles = new AdvancedParticleProperties()
            {
                GravityEffect = NatFloat.createUniform(temp.gravityEffect, temp.maxGravityEffect),
                Size = NatFloat.createUniform(temp.minSize, temp.maxSize),
                LifeLength = NatFloat.createUniform(temp.lifeLength, temp.maxLifeLength),
                Quantity = NatFloat.createUniform(temp.minQuantity, temp.maxQuantity),
                Velocity = new NatFloat[] { NatFloat.createUniform(temp.minVelocityX, temp.addVelocityX), NatFloat.createUniform(temp.minVelocityY, temp.addVelocityY), NatFloat.createUniform(temp.minVelocityZ, temp.addVelocityZ) },
                ParticleModel = thisModel
            };

            if (temp.randomParticleColors)
            {
                particles.HsvaColor = new NatFloat[] {
                    NatFloat.createUniform(0, 255),
                    NatFloat.createUniform(0, 255),
                    NatFloat.createUniform(0, 255),
                    NatFloat.createUniform(255, 255) }; //a
            }
            else
            {
                particles.HsvaColor = null; //YOU MUST SET THIS TO NULL TO USE Color
                particles.Color = ColorUtil.ToRgba(Rnd.Next(a.Min(), a.Max()), Rnd.Next(r.Min(), r.Max()), Rnd.Next(g.Min(), g.Max()), Rnd.Next(b.Min(), b.Max()));
            }

            if (ptype == "Secondary")
            {
                particles.SecondarySpawnInterval = NatFloat.createUniform(temp.particleInterval, 0);
            }

            var minPos = new Vec3d(this.Data.linkX, this.Data.linkY, this.Data.linkZ).AddCopy(temp.minPosX, temp.minPosY, temp.minPosZ);
            particles.basePos = minPos;
            particles.basePos = particles.basePos.AddCopy(temp.minPosX + (temp.addPosX * Rnd.NextDouble()), temp.minPosY + (temp.addPosY * Rnd.NextDouble()), temp.minPosZ + (temp.addPosZ * Rnd.NextDouble()));

            if (temp.toggleRedTransform)
            {
                Enum.TryParse(temp.redTransform.ToUpper(), out EnumTransformFunction thisRedTransform);
                particles.RedEvolve = new EvolvingNatFloat(thisRedTransform, temp.redEvolve);
            }
            if (temp.toggleGreenTransform)
            {
                Enum.TryParse(temp.greenTransform.ToUpper(), out EnumTransformFunction thisGreenTransform);
                particles.GreenEvolve = new EvolvingNatFloat(thisGreenTransform, temp.greenEvolve);
            }
            if (temp.toggleBlueTransform)
            {
                Enum.TryParse(temp.blueTransform.ToUpper(), out EnumTransformFunction thisBlueTransform);
                particles.BlueEvolve = new EvolvingNatFloat(thisBlueTransform, temp.blueEvolve);
            }

            if (temp.toggleSizeTransform)
            {
                Enum.TryParse(temp.sizeTransform.ToUpper(), out EnumTransformFunction thisSizeTransform);
                particles.SizeEvolve = new EvolvingNatFloat(thisSizeTransform, temp.sizeEvolve);
            }
            if (temp.toggleOpacityTransform)
            {
                Enum.TryParse(temp.opacityTransform.ToUpper(), out EnumTransformFunction thisOpacityTransform);
                particles.OpacityEvolve = new EvolvingNatFloat(thisOpacityTransform, temp.opacityEvolve);
            }

            if (temp.toggleVertexFlags)
            {
                particles.VertexFlags = temp.vertexFlags;
            }
            if (temp.selfPropelled)
            { particles.SelfPropelled = true; }
            if (temp.shouldDieInAir)
            { particles.DieInAir = true; }
            if (temp.shouldDieInLiquid)
            { particles.DieInLiquid = true; }
            if (temp.windAffected)
            {
                particles.WindAffectednes = temp.windAffectednes;
            }
            if (temp.noTerrainCollision)
            { particles.TerrainCollision = false; }
            //else
            //{ particles.TerrainCollision = true; }
            if (temp.shouldSwimOnLiquid)
            { particles.SwimOnLiquid = true; }
            if (temp.bouncy)
            {
                // CHANGED FOR 1.17
                particles.Bounciness = 0.7f;
                //particles.Bouncy = true;
            }

            if (temp.randomVelocityChange)
            { particles.RandomVelocityChange = true; }

            return particles;
        }

        /*
        private void MainThreadListenerTest(float dt)
        {
            //Debug.WriteLine("Testing timer logic - main thread");
            this.TestGenerateParticles(null);
        }
        */

        /*
        private void OffThreadListenerTest(float dt, IAsyncParticleManager manager)
        {
            Debug.WriteLine("Testing timer logic - off thread");
            this.TestGenerateParticles(manager);
        }
        */

        /*
        private void TestGenerateParticles(IAsyncParticleManager manager)
        {
            // listener interval: 0.05

            var rand = new Random();

            var ptc = new SimpleParticleProperties(
                100, 0, // min quantity, max quantity
                ColorUtil.ToRgba(rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255)),
                new Vec3d(), //min position - see below
                new Vec3d(0, 0, 0), //max position
                new Vec3f(1f, 1f, 1f), //min velocity
                new Vec3f(-2f, -2f, -2f), //add velocity
                (float)((rand.NextDouble() * ((0.2) - (0.15))) + (0.15)), //life length
                0f, //gravity effect
                0.2f, 0.8f, //min size, add size
                EnumParticleModel.Cube); // quad or cube

            ptc.MinPos.Set(this.Pos.ToVec3d()); //THE BLOCK'S CURRENT POSITION
            ptc.MinPos.AddCopy(0.5, 0.5, 0.5); //min position

            ptc.AddPos.Set(new Vec3d(0, 0, 0)); //add position
            ptc.WithTerrainCollision = true; // ignored in Main Thread
            ptc.VertexFlags = 255;
            this.Api.World.SpawnParticles(ptc);
        }
        */



        private void GenerateParticles(BEParticleData data, IAsyncParticleManager manager)
        {
            //primary particle
            var particles = this.BuildSimpleParticles(data, "Primary");

            //get neighbors if any, and build its particle
            BEParticleData neibData;
            if (data.neibDeathX != -1)
            {
                if (this.Api.World.BlockAccessor.GetBlockEntity(new BlockPos(data.neibDeathX, data.neibDeathY, data.neibDeathZ)) is BEParticulator be)
                {
                    neibData = be.Data;
                    if (neibData != null && !this.Data.mainThread)
                    {
                        if (neibData.particleEnabled)
                        {
                            var neib = new AdvancedParticleProperties[]
                            {
                                this.BuildAdvancedParticles(neibData, neibData.particleType)
                            };
                            particles.DeathParticles = neib;
                        }
                    }
                }
            }
            if (data.neibSecX != -1)
            {
                if (this.Api.World.BlockAccessor.GetBlockEntity(new BlockPos(data.neibSecX, data.neibSecY, data.neibSecZ)) is BEParticulator be)
                {
                    neibData = be.Data;
                    if (neibData != null && !this.Data.mainThread)
                    {
                        if (neibData.particleEnabled)
                        {
                            var neib = new AdvancedParticleProperties[]
                            {
                                this.BuildAdvancedParticles(neibData, neibData.particleType)
                            };
                            particles.SecondaryParticles = neib;
                        }
                    }
                }
            }

            //spawn primary particles only, either in main thread or off thread
            if (this.Data.particleType == "Primary")
            {
                if (this.Data.mainThread)
                {
                    this.Api.World.SpawnParticles(particles);
                }
                else
                {
                    manager.Spawn(particles);
                }
            }
        }


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            this.Data = new BEParticleData()
            {
                minPosX = tree.GetFloat("minPosX", 0.5f),
                minPosY = tree.GetFloat("minPosY", 0.5f),
                minPosZ = tree.GetFloat("minPosZ", 0.5f),

                addPosX = tree.GetFloat("addPosX", 0f),
                addPosY = tree.GetFloat("addPosY", 0f),
                addPosZ = tree.GetFloat("addPosZ", 0f),

                minVelocityX = tree.GetFloat("minVelocityX", 1f),
                minVelocityY = tree.GetFloat("minVelocityY", 1f),
                minVelocityZ = tree.GetFloat("minVelocityZ", 1f),

                addVelocityX = tree.GetFloat("addVelocityX", -2f),
                addVelocityY = tree.GetFloat("addVelocityY", -2f),
                addVelocityZ = tree.GetFloat("addVelocityZ", -2f),

                colorA = tree.GetInt("colorA", 0),
                colorR = tree.GetInt("colorR", 0),
                colorG = tree.GetInt("colorG", 0),
                colorB = tree.GetInt("colorB", 0),

                toggleRandomColor = tree.GetBool("toggleRandomColor", true),
                colorRndA = tree.GetInt("colorRndA", 255),
                colorRndR = tree.GetInt("colorRndR", 255),
                colorRndG = tree.GetInt("colorRndG", 255),
                colorRndB = tree.GetInt("colorRndB", 255),

                toggleRedTransform = tree.GetBool("toggleRedTransform", false),
                redTransform = tree.GetString("redTransform", "Linear"),
                redEvolve = tree.GetFloat("redEvolve", 0f),

                toggleGreenTransform = tree.GetBool("toggleGreenTransform", false),
                greenTransform = tree.GetString("greenTransform", "Linear"),
                greenEvolve = tree.GetFloat("greenEvolve", 0f),

                toggleBlueTransform = tree.GetBool("toggleBlueTransform", false),
                blueTransform = tree.GetString("blueTransform", "Linear"),
                blueEvolve = tree.GetFloat("blueEvolve", 0f),

                minQuantity = tree.GetInt("minQuantity", 100),
                maxQuantity = tree.GetInt("maxQuantity", 0),

                lifeLength = tree.GetFloat("lifeLength", 0.15f),
                maxLifeLength = tree.GetFloat("maxLifeLength", 0.2f),

                gravityEffect = tree.GetFloat("gravityEffect", 0f),
                maxGravityEffect = tree.GetFloat("maxGravityEffect", 0f),

                minSize = tree.GetFloat("minSize", 0.2f),
                maxSize = tree.GetFloat("maxSize", 0.8f),

                model = tree.GetString("model", "Cube"),

                toggleSizeTransform = tree.GetBool("toggleSizeTransform", true),
                sizeTransform = tree.GetString("sizeTransform", "Linear"),
                sizeEvolve = tree.GetFloat("sizeEvolve", 0f),

                toggleOpacityTransform = tree.GetBool("toggleOpacityTransform", true),
                opacityTransform = tree.GetString("opacityTransform", "Linear"),
                opacityEvolve = tree.GetFloat("opacityEvolve", 0f),

                selfPropelled = tree.GetBool("selfPropelled", false),
                shouldDieInAir = tree.GetBool("shouldDieInAir", false),
                shouldDieInLiquid = tree.GetBool("shouldDieInLiquid", false),

                bouncy = tree.GetBool("bouncy", false),
                randomVelocityChange = tree.GetBool("randomVelocityChange", false),
                shouldSwimOnLiquid = tree.GetBool("shouldSwimOnLiquid", false),
                noTerrainCollision = tree.GetBool("noTerrainCollision", false),
                windAffected = tree.GetBool("windAffected", false),
                windAffectednes = tree.GetFloat("windAffectednes", 0.5f),

                toggleVertexFlags = tree.GetBool("toggleVertexFlags", true),
                vertexFlags = tree.GetInt("vertexFlags", 255),

                selectedParticle = tree.GetString("selectedParticle", ""),
                particleEnabled = tree.GetBool("particleEnabled", true),
                particleType = tree.GetString("particleType", "Primary"),
                randomParticleColors = tree.GetBool("randomParticleColors", false),

                timerPreDelay = tree.GetFloat("timerPreDelay", 0f),
                timerPostDelay = tree.GetFloat("timerPostDelay", 0f),
                timerDuration = tree.GetFloat("timerDuration", 0f),
                timerLoop = tree.GetBool("timerLoop", true),

                particleInterval = tree.GetFloat("particleInterval", 0.05f),

                linkX = tree.GetInt("linkX", -1),
                linkY = tree.GetInt("linkY", -1),
                linkZ = tree.GetInt("linkZ", -1),

                mainThread = tree.GetBool("mainThread", false),
                useBlockColor = tree.GetBool("useBlockColor", false),

                neibDeathX = tree.GetInt("neibDeathX", -1),
                neibDeathY = tree.GetInt("neibDeathY", -1),
                neibDeathZ = tree.GetInt("neibDeathZ", -1),
                neibSecX = tree.GetInt("neibSecX", -1),
                neibSecY = tree.GetInt("neibSecY", -1),
                neibSecZ = tree.GetInt("neibSecZ", -1),

                name = tree.GetString("name", "")
            };

            //long[] values = (tree["spawnedEntities"] as LongArrayAttribute)?.value;
            //lastSpawnTotalHours = tree.GetDecimal("lastSpawnTotalHours");
            //this.spawnedEntities = new List<long>(values == null ? new long[0] : values);
        }


        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            // the exact position where particles will spawn
            tree.SetFloat("minPosX", this.Data.minPosX);
            tree.SetFloat("minPosY", this.Data.minPosY);
            tree.SetFloat("minPosZ", this.Data.minPosZ);

            //addPos is relative to minPos, once a particle will spawn it will be randomized and added to the particle's position
            tree.SetFloat("addPosX", this.Data.addPosX);
            tree.SetFloat("addPosY", this.Data.addPosY);
            tree.SetFloat("addPosZ", this.Data.addPosZ);

            // minimum Velocity
            tree.SetFloat("minVelocityX", this.Data.minVelocityX);
            tree.SetFloat("minVelocityY", this.Data.minVelocityY);
            tree.SetFloat("minVelocityZ", this.Data.minVelocityZ);
            // additional Velocity
            tree.SetFloat("addVelocityX", this.Data.addVelocityX);
            tree.SetFloat("addVelocityY", this.Data.addVelocityY);
            tree.SetFloat("addVelocityZ", this.Data.addVelocityZ);

            // how many particles will spawn
            tree.SetInt("minQuantity", this.Data.minQuantity);
            // additional quantity chance
            tree.SetInt("maxQuantity", this.Data.maxQuantity);

            // colors
            tree.SetInt("colorA", this.Data.colorA);
            tree.SetInt("colorR", this.Data.colorR);
            tree.SetInt("colorG", this.Data.colorG);
            tree.SetInt("colorB", this.Data.colorB);

            tree.SetBool("toggleRandomColor", this.Data.toggleRandomColor);
            tree.SetInt("colorRndA", this.Data.colorRndA);
            tree.SetInt("colorRndR", this.Data.colorRndR);
            tree.SetInt("colorRndG", this.Data.colorRndG);
            tree.SetInt("colorRndB", this.Data.colorRndB);

            tree.SetBool("toggleRedTransform", this.Data.toggleRedTransform);
            tree.SetString("redTransform", this.Data.redTransform);
            tree.SetFloat("redEvolve", this.Data.redEvolve);

            tree.SetBool("toggleGreenTransform", this.Data.toggleGreenTransform);
            tree.SetString("greenTransform", this.Data.greenTransform);
            tree.SetFloat("greenEvolve", this.Data.greenEvolve);

            tree.SetBool("toggleBlueTransform", this.Data.toggleBlueTransform);
            tree.SetString("blueTransform", this.Data.blueTransform);
            tree.SetFloat("blueEvolve", this.Data.blueEvolve);

            tree.SetFloat("lifeLength", this.Data.lifeLength);
            tree.SetFloat("maxLifeLength", this.Data.maxLifeLength);

            // gravity or none or inverted
            tree.SetFloat("gravityEffect", this.Data.gravityEffect);
            tree.SetFloat("maxGravityEffect", this.Data.maxGravityEffect);

            tree.SetFloat("minSize", this.Data.minSize);
            tree.SetFloat("maxSize", this.Data.maxSize);

            tree.SetString("model", this.Data.model);

            tree.SetBool("toggleSizeTransform", this.Data.toggleSizeTransform);
            tree.SetString("sizeTransform", this.Data.sizeTransform);
            tree.SetFloat("sizeEvolve", this.Data.sizeEvolve);


            tree.SetBool("toggleOpacityTransform", this.Data.toggleOpacityTransform);
            tree.SetString("opacityTransform", this.Data.opacityTransform);
            tree.SetFloat("opacityEvolve", this.Data.opacityEvolve);

            tree.SetBool("selfPropelled", this.Data.selfPropelled);
            tree.SetBool("shouldDieInAir", this.Data.shouldDieInAir);
            tree.SetBool("shouldDieInLiquid", this.Data.shouldDieInLiquid);

            tree.SetBool("bouncy", this.Data.bouncy);
            tree.SetBool("randomVelocityChange", this.Data.randomVelocityChange);
            tree.SetBool("shouldSwimOnLiquid", this.Data.shouldSwimOnLiquid);
            tree.SetBool("noTerrainCollision", this.Data.noTerrainCollision);
            tree.SetBool("windAffected", this.Data.windAffected);
            tree.SetFloat("windAffectednes", this.Data.windAffectednes);

            tree.SetBool("toggleVertexFlags", this.Data.toggleVertexFlags);
            tree.SetInt("vertexFlags", this.Data.vertexFlags);

            tree.SetString("selectedParticle", this.Data.selectedParticle);
            tree.SetBool("particleEnabled", this.Data.particleEnabled);

            tree.SetFloat("particleInterval", this.Data.particleInterval);
            tree.SetString("particleType", this.Data.particleType);
            tree.SetBool("randomParticleColors", this.Data.randomParticleColors);

            tree.SetFloat("timerPreDelay", this.Data.timerPreDelay);
            tree.SetFloat("timerPostDelay", this.Data.timerPostDelay);
            tree.SetFloat("timerDuration", this.Data.timerDuration);
            tree.SetBool("timerLoop", this.Data.timerLoop);

            tree.SetInt("linkX", this.Data.linkX);
            tree.SetInt("linkY", this.Data.linkY);
            tree.SetInt("linkZ", this.Data.linkZ);

            tree.SetBool("mainThread", this.Data.mainThread);
            tree.SetBool("useBlockColor", this.Data.useBlockColor);

            tree.SetInt("neibDeathX", this.Data.neibDeathX);
            tree.SetInt("neibDeathY", this.Data.neibDeathY);
            tree.SetInt("neibDeathZ", this.Data.neibDeathZ);
            tree.SetInt("neibSecX", this.Data.neibSecX);
            tree.SetInt("neibSecY", this.Data.neibSecY);
            tree.SetInt("neibSecZ", this.Data.neibSecZ);

            tree.SetString("name", this.Data.name);
        }

        public void UpdateNeib(BlockPos neighbor, string ptype)
        {
            if (ptype == "Death")
            {
                this.Data.neibDeathX = neighbor.X;
                this.Data.neibDeathY = neighbor.Y;
                this.Data.neibDeathZ = neighbor.Z;
            }
            else
            {
                this.Data.neibSecX = neighbor.X;
                this.Data.neibSecY = neighbor.Y;
                this.Data.neibSecZ = neighbor.Z;
            }
            //Debug.WriteLine("Update Neighbor");
            this.Link(new BlockPos(this.Data.linkX, this.Data.linkY, this.Data.linkZ));
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            var name = this.Data.name;
            if (name == "")
            { name = "-"; }
            dsc.AppendLine(Lang.GetMatching("primitivesurvival:particle-name") + ": " + name);
            dsc.AppendLine(Lang.GetMatching("primitivesurvival:particle-type") + ": " + Lang.Get(this.Data.particleType));
            if (this.Data.particleType != "Primary")
            {
                dsc.Append("\n");
                dsc.AppendLine(Lang.GetMatching("primitivesurvival:place-beside"));
            }
            else //Primary
            {
                if (this.Data.linkX == this.Pos.X && this.Data.linkY == this.Pos.Y && this.Data.linkZ == this.Pos.Z)
                {
                    dsc.AppendLine(Lang.GetMatching("primitivesurvival:linked-toself"));
                }
                else
                {
                    var msg = Lang.GetMatching("primitivesurvival:linked-to") + " x:" + this.Data.linkX + " y:" + this.Data.linkY + " z:" + this.Data.linkZ;
                    dsc.AppendLine(msg);
                }
                dsc.Append("\n");
                dsc.AppendLine(Lang.GetMatching("primitivesurvival:generates-atlinkedpos"));
            }
        }

        // 1.16
        // public override void OnBlockBroken()
        public override void OnBlockBroken(IPlayer forPlayer)
        {
            this.invDialog?.TryClose();
            this.invDialog = null;

            //offthread particles need to be forced to stop
            if (this.Data.mainThread == false)
            {
                this.killOffThread = true;
            }
            base.OnBlockBroken();
        }


        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            this.invDialog?.Dispose();
        }


        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            this.invDialog?.Dispose();
        }


        public void OnBlockInteract()
        {
            var guiOpened = false;

            if (this.Api.Side.IsClient())
            {
                if (this.invDialog == null)
                {
                    this.invDialog = new GuiDialogParticulator(this.Pos, this.capi)
                    {
                        particleData = Data
                    };
                    guiOpened = this.invDialog.TryOpen();
                }
            }
            if (guiOpened)
            {
                this.invDialog.OnClosed += () =>
                {
                    this.invDialog?.Dispose();
                    this.invDialog = null;
                };
            }
        }


        public override void OnReceivedServerPacket(int packetid, byte[] bytes)
        {
            if (packetid == 1090)
            {
                this.Data = SerializerUtil.Deserialize<BEParticleData>(bytes);
                this.invDialog.UpdateFromServer(this.Data);
            }
        }


        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] bytes)
        {
            if (packetid == 1091)
            {
                //var oldInterval = this.Data.particleInterval;
                //var oldThread = this.Data.mainThread;
                //var oldEnabled = this.Data.particleEnabled;
                this.Data = SerializerUtil.Deserialize<BEParticleData>(bytes);
                this.MarkDirty();
                //this.Api.World.BlockAccessor.MarkBlockDirty(this.Pos); //really?
                this.Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(this.Pos);
                //if ((oldInterval != this.Data.particleInterval) || (oldThread != this.Data.mainThread) || (oldEnabled != this.Data.particleEnabled))
                //{
                this.Link(new BlockPos(this.Data.linkX, this.Data.linkY, this.Data.linkZ));
                // }
            }
        }
    }
}
