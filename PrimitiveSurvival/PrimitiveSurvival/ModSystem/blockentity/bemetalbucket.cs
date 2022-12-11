namespace PrimitiveSurvival.ModSystem
{
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.GameContent;

    public class BEMetalBucket : BlockEntityLiquidContainer
    {
        public override string InventoryClassName => "metalbucket";
        private MeshData currentMesh;
        private BlockMetalBucket ownBlock;
        public float MeshAngle;


        public BEMetalBucket()
        {
            this.inventory = new InventoryGeneric(1, null, null);
        }


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.ownBlock = this.Block as BlockMetalBucket;
            if (this.Api.Side == EnumAppSide.Client)
            {
                this.currentMesh = this.GenMesh();
                this.MarkDirty(true);
            }
        }


        public override void OnBlockBroken(IPlayer forPlayer)
        {
            // Don't drop inventory contents
        }


        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
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
            var shapePath = "primitivesurvival:shapes/block/metalbucket/empty";
            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockMetalBucket;
            if (block != null)
            {
                tmpTextureSource = tesselator.GetTexSource(block);
                mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePath, tmpTextureSource); //, tesselator);
            }
            // fruit press or something else?
            else
            {
                tmpTextureSource = tesselator.GetTexSource(this.ownBlock);
                mesh = this.ownBlock.GenMesh(this.Api as ICoreClientAPI, shapePath, tmpTextureSource); //, tesselator);
            }
            mesher.AddMeshData(mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, this.MeshAngle, 0));

            if (this.GetContent() != null)
            {
                shapePath = "game:shapes/block/wood/bucket/contents";
                if (this.GetContent().Block != null)
                {
                    tmpTextureSource = ((ICoreClientAPI)this.Api).Tesselator.GetTexSource(this.GetContent().Block);
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
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            var slot = this.inventory[0];
            if (slot.Empty)
            { sb.AppendLine(Lang.Get("Empty")); }
            else
            { sb.AppendLine(Lang.Get("Contents: {0}x{1}", slot.Itemstack.StackSize, slot.Itemstack.GetName())); }
        }
    }
}
