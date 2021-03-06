namespace PrimitiveSurvival.ModSystem
{
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.GameContent;

    public class BEMetalBucket : BlockEntityContainer
    {

        internal InventoryGeneric inventory;

        public override InventoryBase Inventory => this.inventory;

        public override string InventoryClassName => "metalbucket";

        private MeshData currentMesh;
        private BlockMetalBucket ownBlock;

        public float MeshAngle;

        public BEMetalBucket() => this.inventory = new InventoryGeneric(1, null, null);


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


        public override void OnBlockBroken()
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


        public ItemStack GetContent() => this.inventory[0].Itemstack;


        internal void SetContent(ItemStack stack)
        {
            this.inventory[0].Itemstack = stack;
            this.MarkDirty(true);
        }


        internal MeshData GenMesh()
        {
            if (this.ownBlock == null) return null;
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
            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos) as BlockMetalBucket;
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
            var slot = this.inventory[0];
            var playerStack = slot.Itemstack;
            if (slot.Empty)
            { sb.AppendLine(Lang.Get("Empty")); }
            else
            { sb.AppendLine(Lang.Get("Contents: {0}x{1}", slot.Itemstack.StackSize, slot.Itemstack.GetName())); }
        }
    }
}
