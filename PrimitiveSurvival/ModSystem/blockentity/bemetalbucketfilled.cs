namespace PrimitiveSurvival.ModSystem
{
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.GameContent;

    public class BEMetalBucketFilled : BlockEntityLiquidContainer
    {
        public int CapacityLitres { get; set; } = 10;
        public override string InventoryClassName => "metalbucket";
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

            //fill the "filled" bucket with lava
            var lavaItem = api.World.GetItem(new AssetLocation("primitivesurvival:lavaportion"));
            if (lavaItem != null)
            {
                this.inventory[0].Itemstack = new ItemStack(lavaItem, 10);
            }
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

            if (currentMesh != null) mesher.AddMeshData(currentMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, MeshAngle, 0));
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
