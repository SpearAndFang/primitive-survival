namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Linq;
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.GameContent;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.Server;
    using PrimitiveSurvival.ModConfig;
    //using System.Diagnostics;

    //public class BEWeirTrap : BlockEntityDisplayCase //1.18
    public class BEWeirTrap : BlockEntityDisplayCase, ITexPositionSource
    {

        private readonly int catchPercent = ModConfig.Loaded.WeirTrapCatchPercent;
        private readonly int escapePercent = ModConfig.Loaded.WeirTrapEscapePercent;
        private readonly double updateMinutes = ModConfig.Loaded.WeirTrapUpdateMinutes;
        private readonly int rotRemovedPercent = ModConfig.Loaded.WeirTrapRotRemovedPercent;

        private readonly int tickSeconds = 3;
        private readonly int maxSlots = 2;
        private readonly string[] fishTypes = { "trout", "perch", "salmon", "carp", "bass", "pike", "arcticchar", "catfish", "bluegill" };
        private readonly string[] shellStates = { "scallop", "sundial", "turritella", "clam", "conch", "seastar", "volute" };
        private readonly string[] shellColors = { "latte", "plain", "seafoam", "darkpurple", "cinnamon", "turquoise" };
        private readonly string[] relics = { "temporalbase", "temporalcube", "temporallectern", "cthulu-statue", "dagon-statue", "hydra-statue", "nephrenka-statue", "necronomicon" };
        private static readonly Random Rnd = new Random();

        private long particleTick;

        public override string InventoryClassName => "weirtrap";
        protected InventoryGeneric inventory; //1.18

        public override InventoryBase Inventory => this.inventory;

        private AssetLocation wetPickupSound;
        public BEWeirTrap()
        {
            this.inventory = new InventoryGeneric(this.maxSlots, null, null);
            //this.meshes = new MeshData[this.maxSlots]; //1.18
            var meshes  = new MeshData[this.maxSlots];
        }

        public ItemSlot Catch1Slot => this.inventory[0];

        public ItemSlot Catch2Slot => this.inventory[1];


        public ItemStack Catch1Stack
        {
            get => this.inventory[0].Itemstack;
            set => this.inventory[0].Itemstack = value;
        }

        public ItemStack Catch2Stack
        {
            get => this.inventory[1].Itemstack;
            set => this.inventory[1].Itemstack = value;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side.IsServer())
            {
                this.particleTick = this.RegisterGameTickListener(this.ParticleUpdate, this.tickSeconds * 1000);
                var updateTick = this.RegisterGameTickListener(this.WeirTrapUpdate, (int)(this.updateMinutes * 60000));
            }
            this.wetPickupSound = new AssetLocation("game", "sounds/environment/smallsplash");
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            this.UnregisterGameTickListener(this.particleTick);
        }

        private void GenerateWaterParticles(int slot, string type, BlockPos pos, IWorldAccessor world)
        {
            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockWeirTrap;
            var dir = block.LastCodePart();
            float minQuantity = 1;
            float maxQuantity = 8;
            if (type == "seashell")
            { maxQuantity = 2; }
            var color = ColorUtil.ToRgba(40, 125, 185, 255);
            var minPos = new Vec3d();
            var addPos = new Vec3d();
            var minVelocity = new Vec3f(0.2f, 0.2f, 0.2f);
            var maxVelocity = new Vec3f(0.6f, 0.6f, 0.6f);
            var lifeLength = 5f;
            var gravityEffect = -0.1f;
            var minSize = 0.1f;
            var maxSize = 0.7f;

            var waterParticles = new SimpleParticleProperties(
                minQuantity, maxQuantity,
                color,
                minPos, addPos,
                minVelocity, maxVelocity,
                lifeLength,
                gravityEffect,
                minSize, maxSize,
                EnumParticleModel.Quad
            );
            if (slot == 0)
            {

                if (type == "fish")
                {
                    Vec3f min;
                    if (dir == "north")
                    { min = new Vec3f(1.3f, 0.2f, 1.3f); }
                    else if (dir == "south")
                    { min = new Vec3f(-0.3f, 0.2f, -0.3f); }
                    else if (dir == "east")
                    { min = new Vec3f(-0.3f, 0.2f, 1.3f); }
                    else
                    { min = new Vec3f(1.3f, 0.2f, -0.3f); }
                    waterParticles.MinPos.Set(pos.ToVec3d().AddCopy(min));
                    waterParticles.AddPos.Set(new Vec3d(0.1, 0, 0));
                }
                else
                {
                    Vec3f min;
                    if (dir == "north")
                    { min = new Vec3f(1f, 0.2f, 0.7f); }
                    else if (dir == "south")
                    { min = new Vec3f(0f, 0.2f, 0.3f); }
                    else if (dir == "east")
                    { min = new Vec3f(0.3f, 0.2f, 1f); }
                    else
                    { min = new Vec3f(0.7f, 0.2f, 0f); }
                    waterParticles.MinPos.Set(pos.ToVec3d().AddCopy(min));
                }
            }
            else
            {
                if (type == "fish")
                {
                    Vec3f min;
                    if (dir == "north")
                    { min = new Vec3f(-0.3f, 0.2f, 1.3f); }
                    else if (dir == "south")
                    { min = new Vec3f(1.3f, 0.2f, -0.3f); }
                    else if (dir == "east")
                    { min = new Vec3f(-0.3f, 0.2f, -0.3f); }
                    else
                    { min = new Vec3f(1.3f, 0.2f, 1.3f); }
                    waterParticles.MinPos.Set(pos.ToVec3d().AddCopy(min));
                    waterParticles.AddPos.Set(new Vec3d(0.1, 0, 0));
                }
                else
                {
                    Vec3f min;
                    if (dir == "north")
                    { min = new Vec3f(0f, 0.2f, 0.4f); }
                    else if (dir == "south")
                    { min = new Vec3f(1f, 0.2f, 0.6f); }
                    else if (dir == "east")
                    { min = new Vec3f(0.6f, 0.2f, 0f); }
                    else
                    { min = new Vec3f(0.4f, 0.2f, 1f); }
                    waterParticles.MinPos.Set(pos.ToVec3d().AddCopy(min));
                }
            }
            waterParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARINCREASE, 0.7f);
            waterParticles.ShouldDieInAir = true;
            waterParticles.SelfPropelled = true;
            world.SpawnParticles(waterParticles);
        }


        public void ParticleUpdate(float par)
        {
            var slot = Rnd.Next(3);
            if (slot <= 1)
            {
                if (!this.inventory[slot].Empty)
                {
                    if (this.inventory[slot].Itemstack.Item != null) //fish
                    { this.GenerateWaterParticles(slot, "fish", this.Pos, this.Api.World); }
                    else if (this.inventory[slot].Itemstack.Block != null) //seashell
                    { this.GenerateWaterParticles(slot, "seashell", this.Pos, this.Api.World); }
                }
            }
        }


        private void WeirTrapUpdate(float par)
        {
            var openWater = false;

            Block testBlock;
            //find an opening and ensure it's water
            var neibPos = new BlockPos[] { this.Pos.EastCopy(), this.Pos.NorthCopy(), this.Pos.SouthCopy(), this.Pos.WestCopy() };

            foreach (var neib in neibPos)
            {
                testBlock = this.Api.World.BlockAccessor.GetBlock(neib, BlockLayersAccess.Default);
                if (testBlock.Code.Path.Contains("open"))
                {
                    testBlock = this.Api.World.BlockAccessor.GetBlock(neib, BlockLayersAccess.Fluid);
                    if (testBlock.LiquidCode == "water")
                    {
                        openWater = true;
                    }
                }
            }

            //now check the weir trap itself
            testBlock = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Fluid);
            if (testBlock.FirstCodePart() != "water")
            {
                //Debug.WriteLine("weirtrap IS ice");
                openWater = false;
            }
            if (openWater)
            {
                var escaped = Rnd.Next(100);
                var caught = Rnd.Next(100);
                if (caught < this.catchPercent)
                { this.WorldPut(0); } //, this.Pos); }
                else if (!this.Catch1Slot.Empty || !this.Catch2Slot.Empty)
                {
                    if (escaped < this.escapePercent)
                    { this.WorldTake(1, this.Pos); }
                }
                else
                {
                    caught = Rnd.Next(100);
                    if (caught < this.catchPercent)
                    { this.WorldPut(1); } //, this.Pos); }
                    else
                    {
                        if (escaped < this.escapePercent)
                        { this.WorldTake(0, this.Pos); }
                    }
                }

                if (!this.Catch1Slot.Empty)
                {
                    if (this.Catch1Stack.Item != null)
                    {
                        if (this.Catch1Stack.Item.Code.Path == "rot")
                        {
                            escaped = Rnd.Next(100);
                            if (escaped < this.rotRemovedPercent)
                            { this.WorldTake(0, this.Pos); } //remove rot from slot 0
                        }
                    }
                }
                if (!this.Catch2Slot.Empty)
                {
                    if (this.Catch2Stack.Item != null)
                    {
                        if (this.Catch2Stack.Item.Code.Path == "rot")
                        {
                            escaped = Rnd.Next(100);
                            if (escaped < this.rotRemovedPercent)
                            { this.WorldTake(1, this.Pos); }  //remove rot from slot 1
                        }
                    }
                }
            }
        }


        public bool WorldPut(int slot) //, BlockPos pos)
        {
            ItemStack newStack = null;
            if (slot == 0 || slot == 1)
            {
                var rando = Rnd.Next(10);
                if (rando < 1) //10% chance of a seashell (or relic) 
                {
                    rando = Rnd.Next(10);
                    if (rando < 1) //10% chance of a relic
                    {
                        var thisRelic = this.relics[Rnd.Next(this.relics.Count())];
                        newStack = new ItemStack(this.Api.World.GetBlock(new AssetLocation("primitivesurvival:" + thisRelic + "-North")), 1);
                    }
                    else
                    { newStack = new ItemStack(this.Api.World.GetBlock(new AssetLocation("game:seashell-" + this.shellStates[Rnd.Next(this.shellStates.Count())] + "-" + this.shellColors[Rnd.Next(this.shellColors.Count())])), 1); }
                }
                else
                {
                    newStack = new ItemStack(this.Api.World.GetItem(new AssetLocation("primitivesurvival:psfish-" + this.fishTypes[Rnd.Next(this.fishTypes.Count())] + "-raw")), 1);
                }
            }
            if (newStack == null)
            { return false; }
            if (this.inventory[slot].Empty)
            {
                if (newStack.Collectible.Code.Path.Contains("psfish"))
                {
                    /*********************************************/
                    //depletion check last
                    var rate = PrimitiveSurvivalSystem.FishDepletedPercent(this.Api as ICoreServerAPI, this.Pos);
                    var rando = Rnd.Next(100);
                    if (rando < rate) //depleted!
                    { return false; }
                    else
                    {
                        // deplete
                        PrimitiveSurvivalSystem.UpdateChunkInDictionary(this.Api as ICoreServerAPI, this.Pos, ModConfig.Loaded.FishChunkDepletionRate);
                    }
                    /*********************************************/
                }
                this.inventory[slot].Itemstack = newStack;
                //Api.World.BlockAccessor.MarkBlockDirty(pos);
                this.MarkDirty(true);
                return true;
            }
            return false;
        }


        public bool WorldTake(int slot, BlockPos pos)
        {
            if (!this.inventory[slot].Empty)
            {
                /*********************************************/
                //Debug.WriteLine("Escaped: " + inventory[slot].Itemstack.Collectible.Code.Path);
                if (this.inventory[slot].Itemstack.Collectible.Code.Path.Contains("psfish"))
                {
                    //replete (at deplete rate)
                    PrimitiveSurvivalSystem.UpdateChunkInDictionary(this.Api as ICoreServerAPI, this.Pos, -ModConfig.Loaded.FishChunkDepletionRate);
                }
                /*********************************************/

                this.inventory[slot].TakeOut(1);
                this.Api.World.BlockAccessor.MarkBlockDirty(pos);
                this.MarkDirty();
                return true;
            }
            return false;
        }


        internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            var playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (playerSlot.Empty)
            {
                if (this.TryTake(byPlayer))
                {
                    this.Api.World.PlaySoundAt(this.wetPickupSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
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
                        this.Api.World.PlaySoundAt(this.wetPickupSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
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
            var moved = 0;
            var playerStack = playerSlot.Itemstack;
            if (this.inventory != null)
            {
                var stacks = this.inventory.Where(slot => !slot.Empty).Select(slot => slot.Itemstack).ToArray();
                if (stacks.Count() >= this.maxSlots)
                { return false; }
            }

            if (playerStack.Item != null)
            {
                if (playerStack.Item.Code.Path.Contains("psfish"))
                {
                    if (this.Catch1Slot.Empty)
                    { index = 0; }
                    else if (this.Catch2Slot.Empty)
                    { index = 1; }
                }
            }
            else if (playerStack.Block != null)
            {
                if (playerStack.Block.Code.Path.Contains("seashell"))
                {
                    if (this.Catch1Slot.Empty)
                    { index = 0; }
                    else if (this.Catch2Slot.Empty)
                    { index = 1; }
                }
            }

            if (index > -1)
            {
                moved = playerSlot.TryPutInto(this.Api.World, this.inventory[index]);
                if (moved > 0)
                {
                    this.MarkDirty(true);
                    return moved > 0;
                }
            }
            return false;
        }

        private bool TryTake(IPlayer byPlayer)
        {
            if (!this.Catch2Slot.Empty)
            {
                /*var rando = Rnd.Next(8);
                if (rando < 2 && this.Catch2Stack.Item != null) //fish
                {
                    var drop = this.Catch2Stack.Clone();
                    drop.StackSize = 1;
                    Api.World.SpawnItemEntity(drop, new Vec3d(this.Pos.X + 0.5, this.Pos.Y, this.Pos.Z + 0.5), null);
                }
                else
                {
                    byPlayer.InventoryManager.TryGiveItemstack(this.Catch2Stack);
                }*/
                byPlayer.InventoryManager.TryGiveItemstack(this.Catch2Stack);
                this.Catch2Slot.TakeOut(1);
                this.MarkDirty(true);
                return true;
            }
            else if (!this.Catch1Slot.Empty)
            {
                /*var rando = Rnd.Next(8);
                if (rando < 1 && this.Catch1Stack.Item != null) //fish
                {
                    var drop = this.Catch1Stack.Clone();
                    drop.StackSize = 1;
                    this.Api.World.SpawnItemEntity(drop, new Vec3d(this.Pos.X + 0.5, this.Pos.Y, this.Pos.Z + 0.5), null); //slippery
                }
                else
                {
                    byPlayer.InventoryManager.TryGiveItemstack(this.Catch1Stack);
                }*/
                byPlayer.InventoryManager.TryGiveItemstack(this.Catch1Stack);
                this.Catch1Slot.TakeOut(1);
                this.MarkDirty(true);
                return true;
            }
            return false;
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            var rot = false;
            if (!this.Catch1Slot.Empty || !this.Catch2Slot.Empty)
            {
                sb.Append(Lang.Get("primitivesurvival:blockdesc-weirtrap-catch"));
                if (!this.Catch1Slot.Empty)
                {
                    if (this.Catch1Stack.Block != null)
                    { }
                    else if (!this.Catch1Stack.Item.Code.Path.Contains("psfish"))
                    { sb.Append(" " + Lang.Get("primitivesurvival:blockdesc-fishbasket-catch-rotten")); }
                    //rot = true;
                }
                else if (!this.Catch2Slot.Empty && !rot)
                {
                    if (this.Catch2Stack.Block != null)
                    { }
                    else if (!this.Catch2Stack.Item.Code.Path.Contains("psfish"))
                    { sb.Append(" " + Lang.Get("primitivesurvival:blockdesc-fishbasket-catch-rotten")); }
                }
                sb.AppendLine().AppendLine();
            }
        }


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
        {
            base.FromTreeAttributes(tree, worldForResolve);
            if (this.Api != null)
            {
                if (this.Api.Side == EnumAppSide.Client)
                { this.Api.World.BlockAccessor.MarkBlockDirty(this.Pos); }
            }
        }


        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            MeshData mesh;
            string shapePath;
            Block tmpBlock;
            bool alive;
            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockWeirTrap;
            var texture = tesselator.GetTexSource(block);
            var tmpTextureSource = texture;

            if (this.inventory != null)
            {
                for (var i = 0; i <= 1; i++)
                {
                    shapePath = "";
                    alive = false;
                    if (!this.inventory[i].Empty) //fish, shell, or rot
                    {
                        if (this.inventory[i].Itemstack.Block != null) //shell or relic
                        {
                            if (this.inventory[i].Itemstack.Block.Code.Path.Contains("temporal"))
                            { shapePath = "primitivesurvival:shapes/block/relic/" + this.inventory[i].Itemstack.Block.FirstCodePart(0); }
                            else if (this.inventory[i].Itemstack.Block.Code.Path.Contains("statue"))
                            { shapePath = "primitivesurvival:shapes/block/relic/statue/" + this.inventory[i].Itemstack.Block.FirstCodePart(0); }
                            else if (this.inventory[i].Itemstack.Block.Code.Path.Contains("necronomicon"))
                            { shapePath = "primitivesurvival:shapes/block/relic/necronomicon-closed"; }
                            else
                            { shapePath = "game:shapes/block/seashell/" + this.inventory[i].Itemstack.Block.FirstCodePart(1); }
                            tmpTextureSource = tesselator.GetTexSource(this.inventory[i].Itemstack.Block);

                        }
                        else if (!this.inventory[i].Itemstack.Item.Code.Path.Contains("psfish"))
                        {
                            tmpBlock = this.Api.World.GetBlock(block.CodeWithPath("texturerot"));
                            tmpTextureSource = ((ICoreClientAPI)this.Api).Tesselator.GetTexSource(tmpBlock);
                            shapePath = "primitivesurvival:shapes/item/fishing/fish-pike";
                        }
                        else if (this.inventory[i].Itemstack.Item.Code.Path.Contains("psfish"))
                        {
                            shapePath = "primitivesurvival:shapes/item/fishing/fish-" + this.inventory[i].Itemstack.Item.LastCodePart(1).ToString();
                            if (this.inventory[i].Itemstack.Item.Code.Path.Contains("cooked"))
                            {
                                tmpBlock = this.Api.World.GetBlock(block.CodeWithPath("texturecooked"));
                                tmpTextureSource = ((ICoreClientAPI)this.Api).Tesselator.GetTexSource(tmpBlock);
                            }
                            else
                            {
                                alive = true;
                                tmpTextureSource = texture;
                            }
                        }
                        if (shapePath != "")
                        {
                            mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePath, tmpTextureSource, i, alive); //, tesselator);
                            mesher.AddMeshData(mesh);
                        }
                    }
                }
            }
            return true;
        }
    }
}
