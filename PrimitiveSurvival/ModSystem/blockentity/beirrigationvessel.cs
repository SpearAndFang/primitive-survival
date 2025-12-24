namespace PrimitiveSurvival.ModSystem
{
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.GameContent;
    
    //using System.Diagnostics;

    public class BEIrrigationVessel : BlockEntityLiquidContainer
    {
        public int CapacityLitres { get; set; } = 50;
        public override string InventoryClassName => "irrigationvessel";
        private MeshData currentMesh;
        private BlockIrrigationVessel ownBlock;
        public float MeshAngle;
        public bool Buried;
        public string SoilType;

        private readonly double updateMinutes = 1.0; //update neighbors once per minute
        private readonly BlockPos[] tmpNeibPos;

        public BEIrrigationVessel()
        {
            this.inventory = new InventoryGeneric(1, null, null);
            this.tmpNeibPos = new BlockPos[8];
            for (int i = 0; i < this.tmpNeibPos.Length; i++)
            {
                this.tmpNeibPos[i] = new BlockPos(0);
            }
        }


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.ownBlock = this.Block as BlockIrrigationVessel;
            if (api.Side == EnumAppSide.Client)
            {
                this.currentMesh = this.GenMesh();
                this.MarkDirty(true);
            }
            else //server
            {
                // was 60000 rboys2
                var updateTick = this.RegisterGameTickListener(this.IrrigationVesselUpdate, (int)(this.updateMinutes * 40000));
            }
        }


        public void IrrigationVesselUpdate(float par)
        {
            //MARK rboys2
            if (this.inventory[0].Empty && !this.Buried)
            {
                this.Api.World.BlockAccessor.SetBlock(0, this.Pos, BlockLayersAccess.Fluid); //clear water layer
                return;
            }
            //MARK rboys2 end

            if (this.inventory[0].Empty || !this.Buried)
            { return; } //empty or not buried
            if (!this.inventory[0].Itemstack.Collectible.Code.Path.Contains("water"))
            { return; } //not water

            if (this.inventory[0].Itemstack.Collectible.Code.Path.Contains("saltwater"))
            { return; } //salt water - abort for now


            //must have water
            //MARK rboys2
            var assetCode = "game:water-still-6";
            var waterBlock = this.Api.World.GetBlock(new AssetLocation(assetCode));
            this.Api.World.BlockAccessor.SetBlock(waterBlock.BlockId, this.Pos, BlockLayersAccess.Fluid); //fill water layer
            //MARK rboys2 end

            var baseX = this.Pos.X;
            var baseY = this.Pos.Y;
            var baseZ = this.Pos.Z;
            var dim = this.Pos.dimension;
            var neibPos = this.tmpNeibPos;
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
            //immediate neighbors

            double waterUsed = 0;
            // Examine sides of immediate neighbors
            foreach (var neib in neibPos)
            {
                if (this.Api.World.BlockAccessor.GetBlockEntity(neib) is BlockEntityFarmland be)
                {
                    //MARK rboys2
                    var topUp = 1.0f - be.MoistureLevel + 0.2f;
                    be.WaterFarmland(topUp, true); //neighbors too now
                    waterUsed += topUp * 2.5f; //use less water in this scenario
                    //MARK rboys2 end
                    var tree = new TreeAttribute();
                    be.ToTreeAttributes(tree);
                }
            }
            this.inventory[0].TakeOut((int)waterUsed);
            this.MarkDirty(true);
        }

        public override void OnBlockBroken(IPlayer forPlayer)
        {
            //MARK rboys2
            this.Api.World.BlockAccessor.SetBlock(0, this.Pos, BlockLayersAccess.Fluid); //clear water layer
            //MARK rboys2 end
        }


        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            this.Buried = false;
            base.OnBlockPlaced(byItemStack);
            if (this.Api.Side == EnumAppSide.Client)
            {
                this.currentMesh = this.GenMesh();
                this.MarkDirty(true);
            }
        }


        internal MeshData GenMesh()
        {
            if (this.ownBlock == null)
            { return null; }
            var mesh = this.ownBlock.GenMesh(this.Api as ICoreClientAPI, this.GetContent(), this.Pos);
            if (mesh.CustomInts != null)
            {
                for (var i = 0; i < mesh.CustomInts.Count; i++)
                {
                    mesh.CustomInts.Values[i] |= 1 << 27; // Disable water wavy
                    mesh.CustomInts.Values[i] |= 1 << 26; // Enabled weak foam
                }
            }
            return mesh;
        }


        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            ITexPositionSource tmpTextureSource;
            MeshData mesh;
            var shapePath = "primitivesurvival:shapes/block/clay/irrigationvessel/irrigationvessel";
            if (this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) is BlockIrrigationVessel block)
            {
                tmpTextureSource = tesselator.GetTextureSource(block);
                mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePath, tmpTextureSource);
                mesher.AddMeshData(mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, this.MeshAngle, 0));
                //add dirt
                if (this.Buried)
                {
                    //default
                    shapePath = "primitivesurvival:shapes/block/clay/irrigationvessel/soildry";
                    if (this.SoilType != null)
                    {
                        if (this.SoilType == "")
                        {
                            this.SoilType = "low";
                        }
                        string finalBlock = "game:farmland-dry-" + this.SoilType;
                        var assetLoc = new AssetLocation(finalBlock);
                        if (assetLoc != null)
                        {
                            var tempBlock = this.Api.World.GetBlock(assetLoc);
                            shapePath = "game:shapes/" + tempBlock.Shape.Base.Path;
                            tmpTextureSource = ((ICoreClientAPI)this.Api).Tesselator.GetTextureSource(tempBlock);
                        }
                    }

                    if (!this.inventory.Empty)
                    {
                        if (this.inventory[0].Itemstack.Collectible.Code.Path.Contains("water"))
                        {
                            //wet default
                            shapePath = "primitivesurvival:shapes/block/clay/irrigationvessel/soilwet";
                            if (this.SoilType != null)
                            {
                                if (this.SoilType == "")
                                {
                                    this.SoilType = "low";
                                }
                                string finalBlock = "game:farmland-moist-" + this.SoilType;
                                var assetLoc = new AssetLocation(finalBlock);
                                if (assetLoc != null)
                                {
                                    var tempBlock = this.Api.World.GetBlock(assetLoc);
                                    shapePath = "game:shapes/" + tempBlock.Shape.Base.Path;
                                    
                                    tmpTextureSource = ((ICoreClientAPI)this.Api).Tesselator.GetTextureSource(tempBlock);
                                }
                            }
                        }
                    }

                    mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePath, tmpTextureSource);
                    mesher.AddMeshData(mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, this.MeshAngle, 0));
                }
            }
            // fruit press or something else?
            else
            {
                tmpTextureSource = tesselator.GetTextureSource(this.ownBlock);
                mesh = this.ownBlock.GenMesh(this.Api as ICoreClientAPI, shapePath, tmpTextureSource); //, tesselator);
                mesher.AddMeshData(mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, this.MeshAngle, 0));
            }

            if (this.GetContent() != null)
            {
                shapePath = "primitivesurvival:shapes/block/clay/contents";
                if (this.GetContent().Block != null)
                {
                    tmpTextureSource = ((ICoreClientAPI)this.Api).Tesselator.GetTextureSource(this.GetContent().Block);
                    //fuck this outside of main thread shit again
                    try
                    {
                        mesh = this.ownBlock.GenMesh(this.Api as ICoreClientAPI, this.GetContent(), this.Pos);
                        mesher.AddMeshData(mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, this.MeshAngle, 0));
                    }
                    catch
                    { }
                }
                else if (this.GetContent().Item != null)
                {
                    tmpTextureSource = ((ICoreClientAPI)this.Api).Tesselator.GetTextureSource(this.GetContent().Item);
                    //fuck this outside of main thread shit again
                    try
                    {
                        mesh = this.ownBlock.GenMesh(this.Api as ICoreClientAPI, this.GetContent(), this.Pos);
                        mesher.AddMeshData(mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, this.MeshAngle, 0));
                    }
                    catch { }
                }
            }
            return true;
        }


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            this.MeshAngle = tree.GetFloat("meshAngle", this.MeshAngle);
            this.Buried = tree.GetBool("buried", this.Buried);
            this.SoilType = tree.GetString("soiltype", this.SoilType);

            if (this.SoilType == null ) 
            {
                this.SoilType = "low";
            }

            if (this.Api != null)
            {
                if (this.Api.Side == EnumAppSide.Client)
                {
                    this.currentMesh = this.GenMesh();
                    this.MarkDirty(true);
                }
            }
        }


        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("meshAngle", this.MeshAngle);
            tree.SetBool("buried", this.Buried);
            tree.SetString("soiltype", this.SoilType);
        }


        //Pretty sure this aint doing shit - moved to the block side
        //public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        //{
        //   
        //}
    }
}
