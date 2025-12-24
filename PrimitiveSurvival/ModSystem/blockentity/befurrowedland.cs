namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Linq;
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.Config;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;
    using Vintagestory.GameContent;
    using Vintagestory.API.Util;
    using PrimitiveSurvival.ModConfig;
    //using System.Diagnostics;

    //public class BEFurrowedLand : BlockEntityDisplayCase //1.18
    public class BEFurrowedLand : BlockEntityDisplayCase, ITexPositionSource
    {
        private readonly int FurrowedLandUpdateFrequency = ModConfig.Loaded.FurrowedLandUpdateFrequency;
        private readonly double FurrowedLandBlockageChancePercent = ModConfig.Loaded.FurrowedLandBlockageChancePercent;

        private readonly double FurrowedLandMinMoistureClose = ModConfig.Loaded.FurrowedLandMinMoistureClose;
        private readonly double FurrowedLandMinMoistureFar = ModConfig.Loaded.FurrowedLandMinMoistureFar;

        public new ICoreClientAPI capi;
        public ICoreServerAPI sapi;
        private static readonly Random Rnd = new Random();
        private readonly string[] blockageTypes = { "rot", "stick", "drygrass" };
        private AssetLocation takeSound;
        private readonly BlockPos tmpUpPos = new BlockPos(0);
        private readonly BlockPos[] tmpCloseNeibPos;
        private readonly BlockPos[] tmpFarNeibPos;
        private RoomRegistry roomRegistry;


        //inventory setup
        private readonly int maxSlots = 1;
        public override string InventoryClassName => "furrowedland";
        protected new InventoryGeneric inventory; //1.18

        public BEFurrowedLand() //1.18
        {
            this.inventory = new InventoryGeneric(this.maxSlots, null, null);
            var meshes = new MeshData[this.maxSlots];
            this.tmpCloseNeibPos = new BlockPos[8];
            for (int i = 0; i < this.tmpCloseNeibPos.Length; i++)
            {
                this.tmpCloseNeibPos[i] = new BlockPos(0);
            }
            this.tmpFarNeibPos = new BlockPos[12];
            for (int i = 0; i < this.tmpFarNeibPos.Length; i++)
            {
                this.tmpFarNeibPos[i] = new BlockPos(0);
            }
        }
        public override InventoryBase Inventory => this.inventory;

        public ItemSlot OtherSlot => this.inventory[0]; //i.e. blockage

        public ItemStack OtherStack
        {
            get => this.inventory[0].Itemstack;
            set => this.inventory[0].Itemstack = value;
        }


        //attributes
        private string renderSides = "nsew";
        //the shape file(s)
        private string sideShape = "";


        //tree attributes
        private string currentConnections = ""; //i.e. "" for none, or  "nsew" for all


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.capi = api as ICoreClientAPI;
            this.takeSound = new AssetLocation("game", "sounds/block/stickplace");
            this.roomRegistry = this.Api.ModLoader.GetModSystem<RoomRegistry>();

            if (this.Block.Attributes != null)
            {
                if (this.Block.Attributes["sideShape"].Exists)
                { this.sideShape = this.Block.Attributes["sideShape"].AsString(); }

                if (this.Block.Attributes["renderSides"].Exists)
                { this.renderSides = this.Block.Attributes["renderSides"].AsString(); }
            }
            this.RegisterGameTickListener(this.OnPipeTick, this.FurrowedLandUpdateFrequency * 1000);
        }


        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            //here we need to look around and then set connections accordingly
            this.currentConnections = "";
            //this.MarkDirty(true);
            base.OnBlockPlaced(byItemStack);
        }

        /*
        public BlockPos RelativeToSpawn(BlockPos pos)
        {
            var worldSpawn = this.Api.World.DefaultSpawnPosition.XYZ.AsBlockPos;
            var blockPos = pos.SubCopy(worldSpawn);
            return new BlockPos(blockPos.X, pos.Y, blockPos.Z);
        }
        */

        private void OnPipeTick(float dt)
        {
            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Fluid);
            if (block.Code.Path.Contains("water") && this.OtherSlot.Empty && !block.Code.Path.Contains("saltwater"))
            {
                var baseX = this.Pos.X;
                var baseY = this.Pos.Y;
                var baseZ = this.Pos.Z;
                var dim = this.Pos.dimension;
                //contains water and no blockage
                if (Rnd.NextDouble() < (this.FurrowedLandBlockageChancePercent / 100))
                {
                    //check for room and block above
                    bool createblockage = true;
                    var upPos = this.tmpUpPos;
                    upPos.dimension = dim;
                    upPos.Set(baseX, baseY + 1, baseZ);
                    var blockabove = this.Api.World.BlockAccessor.GetBlock(upPos, BlockLayersAccess.Default);
                    if (blockabove.BlockId != 0)
                    { createblockage = false; }

                    var registry = this.roomRegistry ?? (this.roomRegistry = this.Api.ModLoader.GetModSystem<RoomRegistry>());
                    Room room = registry.GetRoomForPosition(upPos);
                    if (room != null)
                    {
                        if (room.ExitCount == 0)
                        { createblockage = false; }
                    }
                    

                    if (createblockage)
                    {
                        //create a blockage
                        var blockageItem = this.Api.World.GetItem(new AssetLocation("game:" + this.blockageTypes[Rnd.Next(this.blockageTypes.Count())]));
                        if (blockageItem != null)
                        {
                            this.OtherStack = new ItemStack(blockageItem, Rnd.Next(3) + 1);
                        }

                        this.Api.World.BlockAccessor.SetBlock(0, this.Pos, BlockLayersAccess.Fluid);
                        this.MarkDirty(true);
                    }
                }
                else
                {
                    //water neighbors

                    //check all immediate neighbors (8 sides) - if moisture < 80 add some
                    var neibPos = this.tmpCloseNeibPos;
                    for (int i = 0; i < neibPos.Length; i++)
                    {
                        neibPos[i].dimension = dim;
                    }
                    neibPos[0].Set(baseX + 1, baseY, baseZ);
                    neibPos[1].Set(baseX + 1, baseY, baseZ - 1);
                    neibPos[2].Set(baseX, baseY, baseZ - 1);
                    neibPos[3].Set(baseX - 1, baseY, baseZ - 1);
                    neibPos[4].Set(baseX, baseY, baseZ + 1);
                    neibPos[5].Set(baseX + 1, baseY, baseZ + 1);
                    neibPos[6].Set(baseX - 1, baseY, baseZ);
                    neibPos[7].Set(baseX - 1, baseY, baseZ + 1);

                    // Examine sides of immediate neighbors
                    foreach (var neib in neibPos)
                    {
                        if (this.Api.World.BlockAccessor.GetBlockEntity(neib) is BlockEntityFarmland be)
                        {
                            //Debug.WriteLine(be.MoistureLevel);
                            //var testBlock = this.Api.World.BlockAccessor.GetBlock(neib);
                            if (be.MoistureLevel < this.FurrowedLandMinMoistureClose)
                            {
                                // add 0.1 to it
                                be.WaterFarmland(0.1f, false); //no neighbors until nearby watered
                            }
                            var tree = new TreeAttribute();
                            be.ToTreeAttributes(tree);
                        }
                    }

                    //check a little deeper - if moisture < 60 add some
                    neibPos = this.tmpFarNeibPos;
                    for (int i = 0; i < neibPos.Length; i++)
                    {
                        neibPos[i].dimension = dim;
                    }
                    neibPos[0].Set(baseX + 2, baseY, baseZ);
                    neibPos[1].Set(baseX + 2, baseY, baseZ - 1);
                    neibPos[2].Set(baseX + 2, baseY, baseZ + 1);
                    neibPos[3].Set(baseX, baseY, baseZ - 2);
                    neibPos[4].Set(baseX + 1, baseY, baseZ - 2);
                    neibPos[5].Set(baseX - 1, baseY, baseZ - 2);
                    neibPos[6].Set(baseX, baseY, baseZ + 2);
                    neibPos[7].Set(baseX + 1, baseY, baseZ + 2);
                    neibPos[8].Set(baseX - 1, baseY, baseZ + 2);
                    neibPos[9].Set(baseX - 2, baseY, baseZ);
                    neibPos[10].Set(baseX - 2, baseY, baseZ - 1);
                    neibPos[11].Set(baseX - 2, baseY, baseZ + 1);

                    // Examine sides of neighbors neighbors
                    foreach (var neib in neibPos)
                    {
                        if (this.Api.World.BlockAccessor.GetBlockEntity(neib) is BlockEntityFarmland be)
                        {
                            //Debug.WriteLine(be.MoistureLevel);
                            //var testBlock = this.Api.World.BlockAccessor.GetBlock(neib);
                            if (be.MoistureLevel < this.FurrowedLandMinMoistureFar)
                            {
                                // add 0.05 to it
                                be.WaterFarmland(0.05f, false); //no neighbors until nearby watered
                            }
                            var tree = new TreeAttribute();
                            be.ToTreeAttributes(tree);
                        }
                    }
                }
            }
            //DEBUG
            //var relPos = this.RelativeToSpawn(this.Pos);
            //Debug.WriteLine("location:" + relPos.X + "," + relPos.Y + "," + relPos.Z);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            //DEBUG
            //sb.AppendLine("Connections: " + this.currentConnections);
            if (!this.OtherSlot.Empty)
            {
                sb.AppendLine(Lang.Get("primitivesurvival:item-blockage"));
            }
            sb.AppendLine();
        }


        internal string GetConnections()
        {
            return this.currentConnections;
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


        internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            //prevent dumping water onto debris
            var playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (playerSlot.Itemstack?.Collectible != null)
            {
                if (playerSlot.Itemstack.Collectible.Attributes?["liquidContainerProps"].Exists == true)
                {
                    if (this.OtherStack?.Collectible != null)
                    {
                        if (byPlayer.Entity.Controls.Sprint)
                        {
                            //debris in the way, can't dump water
                            return true;
                        }
                    }
                }
            }

            if (this.OtherStack?.Collectible != null)
            {
                var result = this.TryTake(byPlayer, this.OtherSlot);
                if (result)
                {
                    this.Api.World.PlaySoundAt(this.takeSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);

                    //put water back if there's some nearby
                    var thisblock = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockFurrowedLand;
                    thisblock.FillPlacedBlock(this.Api.World, this.Pos);
                    return true;
                }
            }
            return false;
        }


        private bool TryTake(IPlayer byPlayer, ItemSlot sourceSlot)
        {
            if (!sourceSlot.Empty)
            {
                var stackCode = sourceSlot.Itemstack.Collectible.Code.Path;
                var newAsset = new AssetLocation(stackCode);
                var item = this.Api.World.GetItem(newAsset);
                if (item != null)
                {
                    var tempStack = new ItemStack(item, sourceSlot.StackSize);
                    var takeOK = byPlayer.InventoryManager.TryGiveItemstack(tempStack);
                    if (!takeOK) //player has no free slots
                    {
                        this.Api.World.SpawnItemEntity(tempStack, byPlayer.Entity.Pos.XYZ.Add(0, 0.5, 0));
                    }
                    sourceSlot.Itemstack = null;
                    this.MarkDirty(true);
                    return true;
                }
            }
            return false;
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
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 0, 90 * GameMath.DEG2RAD);
                        mesh.Translate(new Vec3f(0.0015f, 0.0015f, 0.0015f));
                        break;
                    }
                    case 'w':
                    {
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 0, -90 * GameMath.DEG2RAD);
                        mesh.Translate(new Vec3f(0.0015f, 0.0015f, 0.0015f));
                        break;
                    }
                    case 'n':
                    {
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 90 * GameMath.DEG2RAD, 0, 0);
                        break;
                    }
                    case 's':
                    {
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), -90 * GameMath.DEG2RAD, 0, 0);
                        break;
                    }
                    case 'u':
                    {
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 0, 180 * GameMath.DEG2RAD);
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


        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            //uses all textures from the base shape, not the actual rendered shapes
            MeshData mesh;

            //Base
            this.capi.Tesselator.TesselateBlock(this.Block, out mesh);
            if (mesh != null)
            { mesher.AddMeshData(mesh); }

            //Blockage
            if (!this.OtherSlot.Empty)
            {
                var othertexture = tesselator.GetTextureSource(this.Block);
                var othershapePath = this.BuildShapePath("block/pipe/soil/blockage-" + this.OtherStack.Collectible.Code.Path);
                mesh = this.GenMesh(this.capi, othershapePath, othertexture, 'd');
                if (mesh != null)
                {
                    mesh.Translate(new Vec3f(0f, 0.85f - (this.OtherSlot.StackSize * 0.05f), 0f));
                    mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.85f, 0.85f, 0.85f);
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, this.OtherSlot.StackSize * 45 * GameMath.DEG2RAD, 0);
                    mesher.AddMeshData(mesh);
                }
            }

            //Sides
            var texture = tesselator.GetTextureSource(this.Block);
            var shapePath = this.BuildShapePath(this.sideShape); //this uses a single down facing side to render all sides

            foreach (var side in this.renderSides)
            {
                mesh = null;
                if (!this.currentConnections.Contains(side) && shapePath != "")
                { mesh = this.GenMesh(this.capi, shapePath, texture, side); }
                if (mesh != null)
                { mesher.AddMeshData(mesh); }
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
