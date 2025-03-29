namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Linq;
    using System.Text;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Client;
    using Vintagestory.GameContent;
    using Vintagestory.API.Config;
    using Vintagestory.API.Common.Entities;
    //using System.Diagnostics;

    // public class BESnare : BlockEntityDisplayCase, IAnimalFoodSource //1.18
    public class BESnare : BlockEntityDisplayCase, IAnimalFoodSource, ITexPositionSource
    {

        private readonly string[] baitTypes = { "fruit", "grain", "legume", "meat", "vegetable", "jerky", "mushroom", "bread", "poultry", "pickledvegetable", "redmeat", "bushmeat", "cheese", "fishfillet", "fisheggs", "fisheggscooked" };
        protected static readonly Random Rnd = new Random();
        private readonly int maxSlots = 1;

        public ItemSlot BaitSlot => this.inventory[0];

        public ItemStack BaitStack
        {
            get => this.inventory[0].Itemstack;
            set => this.inventory[0].Itemstack = value;
        }


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (this.inventory != null)
            {
                if (!this.BaitSlot.Empty)
                {
                    if (this.BaitStack.Item != null)
                    {
                        if (Array.IndexOf(this.baitTypes, this.BaitStack.Item.FirstCodePart()) >= 0)
                        {
                            if (this.Api.Side == EnumAppSide.Server)
                            { this.Api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this); }
                        }
                    }
                    else if (this.BaitStack.Block != null)
                    {
                        if (Array.IndexOf(this.baitTypes, this.BaitStack.Block.FirstCodePart()) >= 0)
                        {
                            if (this.Api.Side == EnumAppSide.Server)
                            { this.Api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this); }
                        }
                    }
                }
            }
        }


        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (this.Api.Side == EnumAppSide.Server)
            { this.Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this); }
        }


        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            if (this.Api.Side == EnumAppSide.Server)
            { this.Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this); }
        }



        #region IAnimalFoodSource impl
        // ADDED DIET FOR 1.17
        public bool IsSuitableFor(Entity entity, CreatureDiet diet)
        //public bool IsSuitableFor(Entity entity)
        {
            //if (diet == null) //shouldn't need this at all
            //    return false;
            return true;
        }

        public float ConsumeOnePortion(Entity entity)
        {
            this.TryClearContents();
            return 1f;
        }

        public string Type => "food";

        public Vec3d Position => this.Pos.ToVec3d().Add(0.5, 0.5, 0.5);
        #endregion


        public override string InventoryClassName => "snare";
        protected new InventoryGeneric inventory; //1.18

        public override InventoryBase Inventory => this.inventory;


        public BESnare()
        {
            this.inventory = new InventoryGeneric(this.maxSlots, null, null);
            //this.meshes = new MeshData[this.maxSlots]; //1.18
            var meshes = new MeshData[this.maxSlots];
        }


        public bool TryClearContents()
        {
            if (!this.BaitSlot.Empty)
            {
                this.BaitSlot.TakeOut(1);
                return true;
            }
            return false;
        }


        internal bool OnInteract(IPlayer byPlayer) //, BlockSelection blockSel)
        {
            var playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (playerSlot.Empty)
            {
                if (this.TryTake()) // byPlayer, blockSel))
                {
                    if (this.Api.Side == EnumAppSide.Server)
                    { this.Api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this); }
                    return true;
                }
                return false;
            }
            else
            {
                var colObj = playerSlot.Itemstack.Collectible;
                if (colObj.Attributes != null)
                {
                    if (this.TryPut(playerSlot))
                    {
                        if (this.Api.Side == EnumAppSide.Server)
                        { this.Api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this); }
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }


        private bool TryPut(ItemSlot playerSlot)
        {
            var index = -1;
            var playerStack = playerSlot.Itemstack;
            if (this.inventory != null)
            {
                var stacks = this.inventory.Where(slot => !slot.Empty).Select(slot => slot.Itemstack).ToArray();
                index = stacks.Count();
                if (index >= this.maxSlots)
                { return false; }
            }

            if (playerStack.Item != null)
            {
                if (Array.IndexOf(this.baitTypes, playerStack.Item.FirstCodePart()) >= 0 && this.BaitSlot.Empty)
                { index = 0; }
                else
                { return false; }
            }
            else if (playerStack.Block != null)
            {
                if (Array.IndexOf(this.baitTypes, playerStack.Block.FirstCodePart()) >= 0 && this.BaitSlot.Empty)
                { index = 0; }
                else
                { return false; }
            }

            if (index != -1)
            {
                var moved = playerSlot.TryPutInto(this.Api.World, this.inventory[index]);
                if (moved > 0)
                {
                    // 1.16
                    //this.updateMesh(index);
                    this.MarkDirty(true);
                    return moved > 0;
                }
                else
                { return false; }
            }
            return false;
        }


        private bool TryTake() // IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!this.BaitSlot.Empty)
            {
                var stack = this.BaitSlot.TakeOut(1);
                if (stack.StackSize > 0)
                { this.Api.World.SpawnItemEntity(stack, this.Pos.ToVec3d().Add(0.5, 0.5, 0.5)); }
                this.updateMesh(0);
                this.MarkDirty(true);
                return true;
            }
            return false;
        }


        public void StealBait(BlockPos pos)
        {
            var block = this.Api.World.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default) as BlockSnare;
            //var stack = this.BaitSlot.TakeOut(1);
            this.BaitSlot.TakeOut(1);
            {
                if (this.Api.Side == EnumAppSide.Server)
                {
                    this.Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
                    //Debug.WriteLine("REMOVE POI");
                }
            }
            var blockPath = block.Code.Path;
            block = this.Api.World.BlockAccessor.GetBlock(block.CodeWithPath(blockPath)) as BlockSnare;
            this.Api.World.BlockAccessor.SetBlock(block.BlockId, pos);
            this.MarkDirty(true);
            this.updateMesh(0);
        }


        public void TripTrap(BlockPos pos)
        {
            var block = this.Api.World.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default) as BlockSnare;
            var stack = this.BaitSlot.TakeOut(1);
            if (stack != null)
            {
                this.Api.World.SpawnItemEntity(stack, pos.ToVec3d().Add(0.5, 0.5, 0.5));
                if (this.Api.Side == EnumAppSide.Server)
                { this.Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this); }
            }
            var blockPath = block.Code.Path;
            blockPath = blockPath.Replace("set", "tripped");
            block = this.Api.World.BlockAccessor.GetBlock(block.CodeWithPath(blockPath)) as BlockSnare;
            this.Api.World.BlockAccessor.SetBlock(block.BlockId, pos);
            this.MarkDirty(true);
            this.updateMesh(0);
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default);
            if (block.Code.Path.Contains("tripped"))
            { sb.AppendLine(Lang.Get("primitivesurvival:blockdesc-deadfall-tripped")); }
            else
            {
                if (!this.BaitSlot.Empty)
                {
                    if (this.BaitStack.Item != null)
                    {
                        if (Array.IndexOf(this.baitTypes, this.BaitStack.Item.FirstCodePart()) < 0)
                        { sb.AppendLine(Lang.Get("primitivesurvival:blockdesc-deadfall-bait-rotten")); }
                        else
                        { sb.AppendLine(Lang.Get("primitivesurvival:blockdesc-deadfall-bait-ok")); }
                    }
                    else if (this.BaitStack.Block != null)
                    {
                        if (Array.IndexOf(this.baitTypes, this.BaitStack.Block.FirstCodePart()) < 0)
                        { sb.AppendLine(Lang.Get("primitivesurvival:blockdesc-deadfall-bait-rotten")); }
                        else
                        { sb.AppendLine(Lang.Get("primitivesurvival:blockdesc-deadfall-bait-ok")); }
                    }
                }
                else if (this.BaitSlot.Empty) 
                {
                    //sb.AppendLine(Lang.Get("primitivesurvival:blockdesc-deadfall-bait-needed")); 
                }
            }
            //sb.AppendLine().AppendLine();
        }


        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            MeshData mesh;
            var shapeBase = "primitivesurvival:shapes/";
            string shapePath;
            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockSnare;
            var texture = tesselator.GetTextureSource(block);
            var tmpTextureSource = texture;

            if (block.FirstCodePart(1) == "set")
            { shapePath = "block/snare/snare-set"; }
            else
            { shapePath = "block/snare/snare-tripped"; }
            mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase + shapePath, texture, -1, false); //, tesselator);
            mesher.AddMeshData(mesh);

            if (this.inventory != null)
            {
                if (!this.BaitSlot.Empty) //bait or rot
                {
                    var tripped = true;
                    if (block.FirstCodePart(1) == "set")
                    { tripped = false; }
                    if (this.BaitStack.Item != null)
                    {
                        if (Array.IndexOf(this.baitTypes, this.BaitStack.Item.FirstCodePart()) < 0)
                        {
                            var tempblock = this.Api.World.GetBlock(block.CodeWithPath("texturerot"));
                            tmpTextureSource = ((ICoreClientAPI)this.Api).Tesselator.GetTextureSource(tempblock);
                        }
                        else
                        { tmpTextureSource = texture; }
                    }
                    else if (this.BaitStack.Block != null)
                    {
                        if (Array.IndexOf(this.baitTypes, this.BaitStack.Block.FirstCodePart()) < 0)
                        {
                            var tempblock = this.Api.World.GetBlock(block.CodeWithPath("texturerot"));
                            tmpTextureSource = ((ICoreClientAPI)this.Api).Tesselator.GetTextureSource(tempblock);
                        }
                        else
                        { tmpTextureSource = texture; }
                    }
                    shapePath = "block/trapbait"; //baited (for now)
                    mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase + shapePath, tmpTextureSource, 0, tripped); //, tesselator);
                    mesher.AddMeshData(mesh);
                }
            }
            return true;
        }
    }
}
