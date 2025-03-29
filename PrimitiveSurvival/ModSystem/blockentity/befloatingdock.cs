namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;
    using Vintagestory.GameContent;
    //using System.Diagnostics;


    public class BEFloatingDock : BlockEntityDisplayCase, ITexPositionSource
    {
        public new ICoreClientAPI capi;
        public ICoreServerAPI sapi;
        //public bool playerIsWalking = false;
        private bool playerIsWalking = false;

        public BEFloatingDock() 
        {
            
        }

        //attributes
        private string renderSides = "nsew";
        //the shape file(s)
        private string sideShape = "";
        private string baseShape = "";

        
        private long soundTick;

        //tree attributes
        private string currentConnections = ""; //i.e. "" for none, or  "nsew" for all
        
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.capi = api as ICoreClientAPI;
     
            if (this.Block.Attributes != null)
            {
                if (this.Block.Attributes["sideShape"].Exists)
                { this.sideShape = this.Block.Attributes["sideShape"].AsString(); }

                if (this.Block.Attributes["baseShape"].Exists)
                { this.baseShape = this.Block.Attributes["baseShape"].AsString(); }

                if (this.Block.Attributes["renderSides"].Exists)
                { this.renderSides = this.Block.Attributes["renderSides"].AsString(); }
            }
            if (api.Side.IsServer())
            {
                this.soundTick = this.RegisterGameTickListener(this.soundUpdate, 500);
            }
        }

        public void soundUpdate(float par)
        { 
            if (playerIsWalking)
            {
                BlockSelection blockSel = new BlockSelection() { Position = this.Pos, Face = BlockFacing.UP };
                var soundWalkLoc = Block.GetSounds(Api.World.BlockAccessor, blockSel)?.Walk;
                if (soundWalkLoc != null)
                {
                    Api.World.PlaySoundAt(soundWalkLoc, this.Pos.X, this.Pos.Y, this.Pos.Z, null, (float)this.Api.World.Rand.NextDouble() * 0.5f + 0.7f, 4f, 0.5f);
                }
                //random creak sounds
                var rnd = this.Api.World.Rand.Next(0,4);
                if (rnd == 0 && IsFloating())
                {
                    var variant = this.Api.World.Rand.Next(1, 4);
                    soundWalkLoc = new AssetLocation("game", "sounds/block/woodcreak_" + variant);
                    Api.World.PlaySoundAt(soundWalkLoc, this.Pos.X, this.Pos.Y, this.Pos.Z, null, (float)this.Api.World.Rand.NextDouble() * 0.5f + 0.7f, 12f, 1f);
                }
                playerIsWalking = false;
            }
        }


        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            this.currentConnections = "";
            base.OnBlockPlaced(byItemStack);
        }


        internal string GetConnections()
        {
            return this.currentConnections;
        }

        internal void PlayerIsWalking(bool walking)
        {
            this.playerIsWalking = walking;
        }

        internal bool AddConnection(BlockPos pos, BlockFacing facing)
        {
            var face = char.ToLower(facing.ToString()[0]);
            if (!this.currentConnections.Contains(face))
            {
                this.currentConnections += face;
                return true;
            }
            return false;
        }


        internal bool RemoveConnection(BlockPos pos, BlockFacing facing)
        {
            var face = char.ToLower(facing.ToString()[0]).ToString();
            if (this.currentConnections.Contains(face))
            {
                this.currentConnections = this.currentConnections.Replace(face, string.Empty);
                return true;
            }
            return false;
        }


        public virtual MeshData GenBaseMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture)
        {
            Shape shape;
            var tesselator = capi.Tesselator;
            shape = capi.Assets.TryGet(shapePath + ".json").ToObject<Shape>();
            tesselator.TesselateShape(shapePath, shape, out var mesh, texture, null, 0);

            var thisBlock = Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default);
            if ( thisBlock.LastCodePart() == "we")
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 90 * GameMath.DEG2RAD, 0);
            }


            return mesh;
        }


        public virtual MeshData GenMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, char side)
        {
            Shape shape;
            var tesselator = capi.Tesselator;
            shape = capi.Assets.TryGet(shapePath + ".json").ToObject<Shape>();
            tesselator.TesselateShape(shapePath, shape, out var mesh, texture, null, 0);
            if (mesh != null)
            {
                mesh.Translate(0f, 0f, 0f);
                   switch (side)
                    {
                        case 'e':
                        {
                            mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 90 * GameMath.DEG2RAD, 0);
                            mesh.Translate(new Vec3f(0.435f, 0.0018f, 0.0018f));
                            break;
                        }
                        case 'w':
                        {
                            mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 90 * GameMath.DEG2RAD, 0);
                            mesh.Translate(new Vec3f(-0.435f, 0.0015f, 0.0015f));
                            break;
                        }
                        case 'n':
                        {
                            mesh.Translate(new Vec3f(0.0010f, 0.0010f, -0.435f));
                            //mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 90 * GameMath.DEG2RAD, 0, 0);
                            break;
                        }
                        case 's':
                        {
                            mesh.Translate(new Vec3f(0.0013f, 0.0013f, 0.435f));
                            //mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), -90 * GameMath.DEG2RAD, 0, 0);
                            break;
                        }
                        default:
                        { break; }
                }
            }
            return mesh;
        }


        public virtual string BuildShapePath(string partialPath)
        {
            if (partialPath == "")
            { return ""; }
            var fullPath = "";
            if (!partialPath.Contains(":"))
            { fullPath = this.Block.Code.Domain + ":"; }
            if (!partialPath.Contains("shapes/"))
            { fullPath += "shapes/"; }
            fullPath += partialPath;
            return fullPath;
        }


        private bool IsFloating()
        {
            var belowBlock = Api.World.BlockAccessor.GetBlock(this.Pos.DownCopy(), BlockLayersAccess.Fluid);
            if (belowBlock != null)
            {
                if (belowBlock.Code.Path.Contains("water-"))
                { return true; }
            }
            return false;
        }


        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            //uses all textures from the base shape, not the actual rendered shapes
            MeshData mesh;
            var posBelow = this.Pos.DownCopy();
            var belowBlock = Api.World.BlockAccessor.GetBlock(posBelow, BlockLayersAccess.Fluid);
            var floating = IsFloating();


            //Base
            //this.capi.Tesselator.TesselateBlock(this.Block, out mesh); //not the default mesh

            var texture = tesselator.GetTextureSource(this.Block);
            var shapePath = this.BuildShapePath(this.baseShape); //this uses a single down facing side to render all sides
            mesh = this.GenBaseMesh(this.capi, shapePath, texture);
            if (mesh != null)
            {
                if (floating)
                {
                    var waterWave = EnumWindBitModeMask.Water;
                    for (var vertexNum = 0; vertexNum < mesh.GetVerticesCount(); vertexNum++)
                    {
                        mesh.Flags[vertexNum] |= waterWave;
                    }
                }
                mesher.AddMeshData(mesh); 
            }


            //Sides
            if (floating)
            {

                shapePath = this.BuildShapePath(this.sideShape); //this uses a single down facing side to render all sides
                foreach (var side in this.renderSides)
                {
                    mesh = null;
                    if (!this.currentConnections.Contains(side) && shapePath != "")
                    { mesh = this.GenMesh(this.capi, shapePath, texture, side); }
                    if (mesh != null)
                    {
                        var waterWave = EnumWindBitModeMask.Water;
                        for (var vertexNum = 0; vertexNum < mesh.GetVerticesCount(); vertexNum++)
                        {
                            mesh.Flags[vertexNum] |= waterWave;
                        }
                        mesher.AddMeshData(mesh);
                    }
                }
            }
            return true;
        }


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            this.currentConnections = tree.GetString("currentConnections");
        }


        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("currentConnections", this.currentConnections);
        }
    }
}
