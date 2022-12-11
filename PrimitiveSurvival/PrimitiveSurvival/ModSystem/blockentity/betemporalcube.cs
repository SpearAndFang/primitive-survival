namespace PrimitiveSurvival.ModSystem
{
    using System.Linq;
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    //using Vintagestory.API.MathTools;
    using Vintagestory.GameContent;
    using Vintagestory.API.Config;


    public class BETemporalCube : BlockEntityDisplayCase
    {

        private readonly int maxSlots = 4;

        public override string InventoryClassName => "temporalcube";
        //protected InventoryGeneric inventory;

        public override InventoryBase Inventory => this.inventory;


        public BETemporalCube()
        {
            this.inventory = new InventoryGeneric(this.maxSlots, null, null);
            this.meshes = new MeshData[this.maxSlots];
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
            var index = -1;
            var playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            var playerStack = playerSlot.Itemstack;

            if (this.inventory != null)
            {
                var stacks = this.inventory.Where(slot => !slot.Empty).Select(slot => slot.Itemstack).ToArray();
                if (stacks.Count() >= this.maxSlots)
                { return false; }
            }
            if (playerStack.Item != null)
            {
                var path = playerStack.Item.Code.Path;
                if (path.Contains("gear-"))
                {
                    var facing = byPlayer.CurrentBlockSelection.Face.Opposite;
                    var playerFacing = facing.ToString();

                    if (playerFacing == "north")
                    { index = 0; }
                    else if (playerFacing == "east")
                    { index = 1; }
                    else if (playerFacing == "south")
                    { index = 2; }
                    else if (playerFacing == "west")
                    { index = 3; }

                    if (index >= 0)
                    {
                        if (this.inventory[index].Empty)
                        {
                            var moved = playerSlot.TryPutInto(this.Api.World, this.inventory[index]);
                            if (moved > 0)
                            {
                                this.MarkDirty(true);
                                return moved > 0;
                            }
                        }
                    }
                }
            }
            return false;
        }


        private bool TryTake(IPlayer byPlayer) //, BlockSelection blockSel)
        {
            var facing = byPlayer.CurrentBlockSelection.Face.Opposite;
            var index = -1;
            var playerFacing = facing.ToString();

            if (playerFacing == "north")
            { index = 0; }
            else if (playerFacing == "east")
            { index = 1; }
            else if (playerFacing == "south")
            { index = 2; }
            else if (playerFacing == "west")
            { index = 3; }

            if (index >= 0)
            {
                if (!this.inventory[index].Empty)
                {
                    byPlayer.InventoryManager.TryGiveItemstack(this.inventory[index].Itemstack);
                    this.inventory[index].TakeOut(1);
                    this.MarkDirty(true);
                    return true;
                }
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
            MeshData mesh;
            var shapeBase = "primitivesurvival:shapes/";
            string shapePath; // = "";
            var index = -1;

            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockTemporalCube;
            Block tmpBlock;
            var texture = tesselator.GetTexSource(block);

            var newPath = "temporalcube";
            shapePath = "block/relic/" + newPath;
            mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase + shapePath, texture, index); //, tesselator);
            mesher.AddMeshData(mesh);

            if (this.inventory != null)
            {
                for (var i = 0; i < this.maxSlots; i++)
                {
                    if (!this.inventory[i].Empty) //gear - temporal or rusty
                    {
                        var gearType = this.inventory[i].Itemstack.Item.FirstCodePart(1);
                        tmpBlock = this.Api.World.GetBlock(block.CodeWithPath("texture" + gearType));
                        if (gearType != "rusty")
                        { gearType = "temporal"; }
                        shapePath = "game:shapes/item/gear-" + gearType;
                        texture = ((ICoreClientAPI)this.Api).Tesselator.GetTexSource(tmpBlock);
                        mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePath, texture, i); //, tesselator);
                        mesher.AddMeshData(mesh);
                    }
                }
            }
            return true;
        }
    }
}

