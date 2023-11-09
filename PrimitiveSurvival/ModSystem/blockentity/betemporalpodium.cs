namespace PrimitiveSurvival.ModSystem
{
    using System.Linq;
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    //using Vintagestory.API.MathTools;
    using Vintagestory.GameContent;
    using Vintagestory.API.Config;

    //public class BETemporallectern : BlockEntityDisplayCase //1.18
    public class BETemporallectern : BlockEntityDisplayCase, ITexPositionSource
    {

        private readonly int maxSlots = 2;

        public override string InventoryClassName => "temporallectern";
        protected new InventoryGeneric inventory; //1.18

        public override InventoryBase Inventory => this.inventory;


        public BETemporallectern()
        {
            this.inventory = new InventoryGeneric(this.maxSlots, null, null);
            //this.meshes = new MeshData[this.maxSlots];
            var meshes  = new MeshData[this.maxSlots];
        }


        public ItemSlot TopSlot => this.inventory[0];

        public ItemSlot GearSlot => this.inventory[1];

        public ItemStack TopStack
        {
            get => this.inventory[0].Itemstack;
            set => this.inventory[0].Itemstack = value;
        }

        public ItemStack GearStack
        {
            get => this.inventory[1].Itemstack;
            set => this.inventory[1].Itemstack = value;
        }


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
        }

        internal bool OnInteract(IPlayer byPlayer) //, BlockSelection blockSel)
        {
            var playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (playerSlot.Empty)
            {
                if (this.TryTake(byPlayer)) //, blockSel))
                { return true; }
                return false;
            }
            else
            {
                if (this.TryPut(byPlayer))
                { return true; }
                return false;
            }
        }


        internal void OnBreak() //IPlayer byPlayer, BlockPos pos)
        {
            for (var index = this.maxSlots - 1; index >= 0; index--)
            {
                if (!this.inventory[index].Empty)
                {
                    var stack = this.inventory[index].TakeOut(1);
                    if (stack.StackSize > 0)
                    { this.Api.World.SpawnItemEntity(stack, this.Pos.ToVec3d().Add(0.5, 0.5, 0.5)); }
                    this.MarkDirty(true);
                }
            }
        }


        private bool TryPut(IPlayer byPlayer)
        {
            var playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            var playerStack = playerSlot.Itemstack;

            if (this.inventory != null)
            {
                var stacks = this.inventory.Where(slot => !slot.Empty).Select(slot => slot.Itemstack).ToArray();
                if (stacks.Count() >= this.maxSlots)
                { return false; }
            }
            if (playerStack.Block != null)
            {
                if (playerStack.Block.Code.Path.Contains("necronomicon") && this.TopSlot.Empty)
                {
                    var moved = playerSlot.TryPutInto(this.Api.World, this.TopSlot);
                    if (moved > 0)
                    {
                        this.MarkDirty(true);
                        return moved > 0;
                    }
                }
            }
            else if (playerStack.Item != null)
            {
                var path = playerStack.Item.Code.Path;
                if (path.Contains("gear-"))
                {
                    var dir = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default).LastCodePart();
                    var facing = byPlayer.CurrentBlockSelection.Face.Opposite.ToString();

                    if (facing == dir && this.GearSlot.Empty)
                    {
                        var moved = playerSlot.TryPutInto(this.Api.World, this.GearSlot);
                        if (moved > 0)
                        {
                            this.MarkDirty(true);
                            return moved > 0;
                        }
                    }
                }
            }
            return false;
        }


        private bool TryTake(IPlayer byPlayer) //, BlockSelection blockSel)
        {
            var facing = byPlayer.CurrentBlockSelection.Face.Opposite;
            var playerFacing = facing.ToString();
            var tmpblock = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default);
            var blockFacing = tmpblock.LastCodePart();

            if (playerFacing == blockFacing && !this.GearSlot.Empty)
            {
                byPlayer.InventoryManager.TryGiveItemstack(this.GearStack);
                this.GearSlot.TakeOut(1);
                this.MarkDirty(true);
                return true;
            }
            else if (!this.TopSlot.Empty)
            {
                byPlayer.InventoryManager.TryGiveItemstack(this.TopStack);
                this.TopSlot.TakeOut(1);
                this.MarkDirty(true);
                return true;
            }
            return false;
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            sb.Append(Lang.Get("primitivesurvival:blockdesc-temporalbase-incomplete"));
            sb.AppendLine().AppendLine();
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            var shapeBase = "primitivesurvival:shapes/";
            string shapePath;
            //var index = -1;

            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockTemporallectern;
            Block tmpBlock;
            var texture = tesselator.GetTextureSource(block);

            var newPath = "temporallectern";
            shapePath = "block/relic/" + newPath;
            var mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase + shapePath, texture); //, index, tesselator);
            mesher.AddMeshData(mesh);

            if (this.inventory != null)
            {
                if (!this.GearSlot.Empty) //gear - temporal or rusty
                {
                    var gearType = this.GearStack.Item.FirstCodePart(1);
                    tmpBlock = this.Api.World.GetBlock(block.CodeWithPath("texture" + gearType));
                    if (gearType != "rusty")
                    { gearType = "temporal"; }
                    shapePath = "game:shapes/item/gear-" + gearType;
                    texture = ((ICoreClientAPI)this.Api).Tesselator.GetTextureSource(tmpBlock);
                    mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePath, texture); // ,1, tesselator);
                    mesher.AddMeshData(mesh);
                }

                if (!this.TopSlot.Empty)
                {
                    newPath = this.TopStack.Block.FirstCodePart();
                    if (newPath.Contains("necronomicon"))
                    {
                        tmpBlock = this.Api.World.GetBlock(block.CodeWithPath("necronomicon-north"));
                        texture = ((ICoreClientAPI)this.Api).Tesselator.GetTextureSource(tmpBlock);
                        shapePath = "block/relic/" + newPath + "-closed";
                        mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase + shapePath, texture); //, index, tesselator);
                        mesher.AddMeshData(mesh);
                    }
                }
            }
            return true;
        }
    }
}

