namespace PrimitiveSurvival.ModSystem
{
    //using System;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    //using System.Diagnostics;

    public class BEFuse : BlockEntity
    {
        private float remainingSeconds = 0;
        private string ignitedByPlayerUid;
        private float blastRadius;
        private float injureRadius;
        private EnumBlastType blastType;
        private ILoadedSound fuseSound;

        public static SimpleParticleProperties smallSparks;

        public ICoreClientAPI capi;

        public IPlayer IgnitedPlayer;


        static BEFuse()
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
            smallSparks.AddPos.Set(0f, -0.5f, 0f);
            smallSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.05f);
        }


        public virtual float FuseTimeSeconds => 2;

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
                    Volume = 0.1f,
                    Range = 16,
                });
            }
            this.blastRadius = this.Block.Attributes["blastRadius"].AsInt(4);
            this.injureRadius = this.Block.Attributes["injureRadius"].AsInt(8);
            this.blastType = (EnumBlastType)this.Block.Attributes["blastType"].AsInt((int)EnumBlastType.OreBlast);
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
        }


        private void OnTick(float dt)
        {
            if (this.IsLit)
            {
                this.remainingSeconds -= dt;
                if (this.Api.Side == EnumAppSide.Server && this.remainingSeconds <= 0)
                {
                    this.Combust(dt);
                }

                if (this.Api.Side == EnumAppSide.Client)
                {
                    smallSparks.MinPos.Set(this.Pos.X + 0.45, this.Pos.Y + 0.53, this.Pos.Z + 0.45);
                    this.Api.World.SpawnParticles(smallSparks);
                }

                if (this.remainingSeconds < 1)
                {
                    //Check and light neighbors
                    var path = this.Block.LastCodePart();
                    BlockPos[] neibBlockPos = null;
                    foreach (var c in path)
                    {
                        //for each of these it's either beside, beside+up, or beside+down
                        if (c == 'n')
                        {
                            neibBlockPos = new BlockPos[]
                                { this.Pos.NorthCopy(), this.Pos.NorthCopy().UpCopy(), this.Pos.NorthCopy().DownCopy() };
                        }
                        else if (c == 's')
                        {
                            neibBlockPos = new BlockPos[]
                                { this.Pos.SouthCopy(), this.Pos.SouthCopy().UpCopy(), this.Pos.SouthCopy().DownCopy() };
                        }
                        else if (c == 'e')
                        {
                            neibBlockPos = new BlockPos[]
                                { this.Pos.EastCopy(), this.Pos.EastCopy().UpCopy(), this.Pos.EastCopy().DownCopy() };
                        }
                        else if (c == 'w')
                        {
                            neibBlockPos = new BlockPos[]
                                { this.Pos.WestCopy(), this.Pos.WestCopy().UpCopy(), this.Pos.WestCopy().DownCopy() };
                        }

                        if (neibBlockPos != null)
                        {
                            foreach (var neib in neibBlockPos)
                            {
                                if (neib != null && this.IgnitedPlayer != null)
                                {
                                    var befuse = this.IgnitedPlayer.Entity.World.BlockAccessor.GetBlockEntity(neib) as BEFuse;
                                    befuse?.OnIgnite(this.IgnitedPlayer);

                                    //firework?
                                    var befirework = this.IgnitedPlayer.Entity.World.BlockAccessor.GetBlockEntity(neib) as BEFirework;
                                    befirework?.OnIgnite(this.IgnitedPlayer);

                                    //orebomb?
                                    var beorebomb = this.IgnitedPlayer.Entity.World.BlockAccessor.GetBlockEntity(neib) as BEBombFuse;
                                    if (beorebomb != null)
                                    {
                                        beorebomb?.OnIgnite(this.IgnitedPlayer);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        private void GenAshes(Vec3d minPos, Vec3d maxPos)
        {
            var tmp = new Vec3f();
            for (var i = 0; i < 10; i++)
            {
                //var color  = this.Block.GetRandomColor(this.Api as ICoreClientAPI, this.Pos, BlockFacing.UP);
                var color = ColorUtil.ToRgba(255, 30, 30, 30);
                tmp.Set(
                    (0.5f - (float)this.Api.World.Rand.NextDouble()) / 3,
                    0f,
                    (0.5f - (float)this.Api.World.Rand.NextDouble()) / 3
                );
                this.Api.World.SpawnParticles(10, color, minPos, maxPos, tmp, tmp, (float)this.Api.World.Rand.NextDouble(), 1f, 0.25f + ((float)this.Api.World.Rand.NextDouble() * 0.25f), EnumParticleModel.Cube, null);
            }
        }


        private void Combust(float dt)
        {
            //ashes
            var minPos = this.Pos.ToVec3d().AddCopy(0.5f, 0f, 0.5f);
            var maxPos = this.Pos.ToVec3d().AddCopy(0.5f, 0f, 0.5f);
            var path = this.Block.LastCodePart();
            if (path == "empty")
            {
                maxPos = this.Pos.ToVec3d().AddCopy(0.5f, 0f, 0.5f);
                this.GenAshes(minPos, maxPos);
            }
            else
            {
                foreach (var c in path)
                {
                    if (c == 'n')
                    { maxPos = this.Pos.ToVec3d().AddCopy(0.5f, 0f, -0.1f); }
                    else if (c == 's')
                    { maxPos = this.Pos.ToVec3d().AddCopy(0.5f, 0f, 1.1f); }
                    else if (c == 'e')
                    { maxPos = this.Pos.ToVec3d().AddCopy(1.1f, 0f, 0.5f); }
                    else if (c == 'w')
                    { maxPos = this.Pos.ToVec3d().AddCopy(-0.1f, 0f, 0.5f); }
                    this.GenAshes(minPos, maxPos);
                }
            }
            this.Api.World.BlockAccessor.SetBlock(0, this.Pos);
        }


        internal void OnBlockExploded(BlockPos pos)
        {
            if (this.Api.Side == EnumAppSide.Server)
            {
                if (!this.IsLit || this.remainingSeconds > 0.1)
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

        private static BlockPos[] AreaAround(BlockPos pos)
        {
            return new BlockPos[]
            {  pos.WestCopy(), pos.SouthCopy(), pos.EastCopy(), pos.NorthCopy() };
        }

        private static BlockPos[] AreaAroundUp(BlockPos pos)
        {
            return new BlockPos[]
            {  pos.WestCopy().UpCopy(), pos.SouthCopy().UpCopy(), pos.EastCopy().UpCopy(), pos.NorthCopy().UpCopy() };
        }

        private static BlockPos[] AreaAroundDown(BlockPos pos)
        {
            return new BlockPos[]
            {  pos.WestCopy().DownCopy(), pos.SouthCopy().DownCopy(), pos.EastCopy().DownCopy(), pos.NorthCopy().DownCopy() };
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            MeshData mesh;
            var shapePathUp = "primitivesurvival:shapes/block/fuse/u";
            var shapePathLeadFuse = "primitivesurvival:shapes/block/fuse/leadfuse";
            var shapePathLeadBomb = "primitivesurvival:shapes/block/fuse/leadbomb";
            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockFuse;
            var texture = tesselator.GetTexSource(block);

            Block blockChk;
            var around = AreaAround(this.Pos);
            var rot = 0;
            foreach (var neighbor in around)
            {
                blockChk = this.Api.World.BlockAccessor.GetBlock(neighbor, BlockLayersAccess.Default);
                if (blockChk != null)
                {
                    if (blockChk.Class == "blockfirework")
                    {
                        mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePathLeadFuse, texture, rot, 0);
                        mesher.AddMeshData(mesh);
                    }
                    else if (blockChk.Class == "blockbombfuse")
                    {
                        mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePathLeadBomb, texture, rot, 0);
                        mesher.AddMeshData(mesh);
                    }
                }
                rot += 90;
            }

            around = AreaAroundUp(this.Pos);
            rot = 0;
            foreach (var neighbor in around)
            {
                blockChk = this.Api.World.BlockAccessor.GetBlock(neighbor, BlockLayersAccess.Default);
                if (blockChk != null)
                {
                    if (blockChk.Class == "blockfuse" || blockChk.Class == "blockfirework" || blockChk.Class == "blockbombfuse")
                    {
                        mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePathUp, texture, rot, 0);
                        mesher.AddMeshData(mesh);
                    }
                    if (blockChk.Class == "blockfirework")
                    {
                        mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePathLeadFuse, texture, rot, 1f);
                        mesher.AddMeshData(mesh);
                    }
                    else if (blockChk.Class == "blockbombfuse")
                    {
                        mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePathLeadBomb, texture, rot, 1f);
                        mesher.AddMeshData(mesh);
                    }
                }
                rot += 90;
            }

            around = AreaAroundDown(this.Pos);
            rot = 0;
            foreach (var neighbor in around)
            {
                blockChk = this.Api.World.BlockAccessor.GetBlock(neighbor, BlockLayersAccess.Default);
                if (blockChk != null)
                {
                    if (blockChk.Class == "blockfuse" || blockChk.Class == "blockfirework" || blockChk.Class == "blockbombfuse")
                    {
                        mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePathUp, texture, rot, -1);
                        mesher.AddMeshData(mesh);
                    }
                    if (blockChk.Class == "blockfirework")
                    {
                        mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePathLeadFuse, texture, rot, -1f);
                        mesher.AddMeshData(mesh);
                    }
                    else if (blockChk.Class == "blockbombfuse")
                    {
                        mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePathLeadBomb, texture, rot, -1f);
                        mesher.AddMeshData(mesh);
                    }
                }
                rot += 90;
            }
            return base.OnTesselation(mesher, tesselator);
        }


        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("remainingSeconds", this.remainingSeconds);
            tree.SetInt("lit", this.IsLit ? 1 : 0);
            tree.SetString("ignitedByPlayerUid", this.ignitedByPlayerUid);
        }


        ~BEFuse()
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
            { this.fuseSound.Stop(); }
        }
    }
}
