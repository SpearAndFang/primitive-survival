namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Text;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Client;
    using Vintagestory.GameContent;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.Config;

    //public class BEWoodSupportSpikes : BlockEntityDisplayCase, IAnimalFoodSource //1.18
    public class BEWoodSupportSpikes : BlockEntityDisplayCase, IAnimalFoodSource, ITexPositionSource
    {

        protected static readonly Random Rnd = new Random();
        private readonly int maxSlots = 4;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (this.inventory != null)
            {
                if (!this.inventory[this.maxSlots - 1].Empty) //camouflaged means poi
                {
                    if (this.Api.Side == EnumAppSide.Server)
                    { this.Api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this); }
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
            //TryClearContents();
            return 1f; //Was 0f
        }

        public string Type => "food";

        public Vec3d Position => this.Pos.ToVec3d().Add(0.5, 0.5, 0.5);
        #endregion


        public override string InventoryClassName => "woodsupportspikes";
        protected new InventoryGeneric inventory; //1.18


        public override InventoryBase Inventory => this.inventory;


        public BEWoodSupportSpikes()
        {
            this.inventory = new InventoryGeneric(this.maxSlots, null, null);
            //this.meshes = new MeshData[this.maxSlots]; //1.18
            var meshes  = new MeshData[this.maxSlots];
        }


        internal bool OnInteract(IPlayer byPlayer) //, BlockSelection blockSel)
        {
            var playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (playerSlot.Empty)
            {
                if (this.TryTake(byPlayer))
                {
                    if (this.inventory[this.maxSlots - 1].Empty) //camouflaged means poi
                    {
                        if (this.Api.Side == EnumAppSide.Server)
                        { this.Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this); }
                    }
                    return true;
                }
                return false;
            }
            else
            {
                if (this.TryPut(playerSlot))
                {
                    if (!this.inventory[this.maxSlots - 1].Empty) //camouflaged means poi
                    {
                        if (this.Api.Side == EnumAppSide.Server)
                        { this.Api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this); }
                    }
                    return true;
                }
                return false;

            }
        }


        private bool TryPut(ItemSlot playerSlot)
        {
            var playerStack = playerSlot.Itemstack;
            var availSlot = this.maxSlots;
            for (var slot = this.maxSlots - 1; slot >= 0; slot--)
            {
                if (this.inventory[slot].Empty)
                { availSlot = slot; }
            }

            if (availSlot == this.maxSlots)
            { return false; }

            var canPlace = false;
            if (availSlot < this.maxSlots - 1)
            {
                if (playerStack.Item != null)
                {
                    if (playerStack.Item.Code.Path == "drygrass")
                    { canPlace = true; }
                }
                else if (playerStack.Block.BlockMaterial == EnumBlockMaterial.Plant)
                { canPlace = true; }
            }
            else
            {
                if (playerStack.Block != null)
                {
                    if (playerStack.Block.Fertility > 0)
                    { canPlace = true; }
                }
            }

            if (canPlace)
            {
                playerSlot.TryPutInto(this.Api.World, this.inventory[availSlot]);
                this.MarkDirty(true);
                return true;
            }
            return false;
        }


        private bool TryTake(IPlayer byPlayer)
        {
            var usedSlot = -1;
            for (var slot = 0; slot < this.maxSlots; slot++)
            {
                if (!this.inventory[slot].Empty)
                { usedSlot = slot; }
            }

            if (usedSlot > -1)
            {
                var stack = this.inventory[usedSlot].TakeOut(1);
                if (stack.StackSize > 0)
                { byPlayer.InventoryManager.TryGiveItemstack(stack); }
                this.MarkDirty(true);
                return true;
            }
            return false;
        }

        public string GetBlockName(IWorldAccessor world, BlockPos pos)
        {
            if (this.inventory != null)
            {
                if (!this.inventory[3].Empty)
                {
                    var stack = this.inventory[3].Itemstack;
                    if (stack.Block != null)
                    {
                        var msg = Lang.Get(GlobalConstants.DefaultDomain + ":block-" + stack.Block.Code.Path);
                        return msg;
                    }
                }
            }
            return Lang.Get("primitivesurvival:blockdesc-woodsuppportspikes-concealment-needed");
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            if (this.inventory != null)
            {
                var msg = "";
                for (var i = 0; i < 3; i++)
                {
                    if (this.inventory[i].Empty)
                    {
                        msg = Lang.Get("primitivesurvival:blockdesc-woodsuppportspikes-concealment-size-" + i);
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(msg))
                {
                    sb.Append(Lang.Get("primitivesurvival:blockdesc-woodsuppportspikes-concealment-add") + " ").Append(msg).Append(" " + Lang.Get("primitivesurvival:blockdesc-woodsuppportspikes-concealment-plants"));
                }
                else if (this.inventory[3].Empty)
                {
                    sb.AppendLine(Lang.Get("primitivesurvival:blockdesc-woodsuppportspikes-concealment-dirt"));
                }

                if (this.inventory[3].Empty)
                { sb.AppendLine().AppendLine(); }
            }
        }


        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            var shapeBase = "primitivesurvival:shapes/";
            string shapePath;
            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockWoodSupportSpikes;
            var texture = tesselator.GetTextureSource(block);
            ITexPositionSource tmpTextureSource;
            shapePath = "block/woodsupportspikes";
            var mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase + shapePath, texture, -1); //, tesselator);
            mesher.AddMeshData(mesh);

            if (this.inventory != null)
            {
                var usedSlots = 0;
                for (var i = 0; i < this.maxSlots; i++)
                {
                    if (!this.inventory[i].Empty)
                    { usedSlots++; }
                }

                if (usedSlots > 0 && usedSlots < 4)
                {
                    var tmpBlock = this.Api.World.GetBlock(block.CodeWithPath("foilage-" + usedSlots.ToString()));
                    shapePath = "block/foilage";
                    tmpTextureSource = tesselator.GetTextureSource(tmpBlock);
                    mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase + shapePath, tmpTextureSource, usedSlots); //, tesselator);
                    mesher.AddMeshData(mesh);
                }
                else if (usedSlots == 4)
                {
                    shapePath = "block/foilage";
                    tmpTextureSource = tesselator.GetTextureSource(this.inventory[3].Itemstack.Block);
                    mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase + shapePath, tmpTextureSource, usedSlots); //, tesselator);
                    mesher.AddMeshData(mesh);
                }
            }
            return true;
        }
    }
}
