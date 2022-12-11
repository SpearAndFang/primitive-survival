namespace PrimitiveSurvival.ModSystem
{
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    //using System.Diagnostics;

    public class BEMetalBucketFilled : BlockEntity
    {
        internal InventoryGeneric inventory;
        private MeshData currentMesh;
        private BlockMetalBucketFilled ownBlock;
        public float MeshAngle;

        public BEMetalBucketFilled()
        {
            this.inventory = new InventoryGeneric(1, null, null);
        }


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.ownBlock = this.Block as BlockMetalBucketFilled;
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


        public ItemStack GetContent()
        {
            return this.inventory[0].Itemstack;
        }


        internal void SetContent(ItemStack stack)
        {
            this.inventory[0].Itemstack = stack;
            this.MarkDirty(true);
        }


        internal MeshData GenMesh()
        {
            if (this.ownBlock == null)
            { return null; }
            var mesh = this.ownBlock.GenMesh(this.Api as ICoreClientAPI);
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
            if (this.currentMesh != null)
            {
                mesher.AddMeshData(this.currentMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, this.MeshAngle, 0));
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
            //do nothing to override perishable info
        }
    }
}
