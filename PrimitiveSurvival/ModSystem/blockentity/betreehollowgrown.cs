namespace PrimitiveSurvival.ModSystem
{
    using System.Text;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Config;
    using Vintagestory.API.Client;
    using Vintagestory.API.Server;
    using Vintagestory.GameContent;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.Util;
    using PrimitiveSurvival.ModConfig;
    //using System.Diagnostics;

    #nullable disable
    //public class BETreeHollowGrown : BlockEntityDisplayCase //1.18
    public class BETreeHollowGrown : BlockEntityDisplayCase, ITexPositionSource
    {
        private readonly int maxSlots = 8;
        public override string InventoryClassName => "treehollowgrown";
        protected new InventoryGeneric inventory;

        private double lastEmptied = 0;
        private double irlDayLengthMinutes = 2880; //will be overridden with actual setting

        private readonly double updateMinutes = ModConfig.Loaded.TreeHollowsUpdateMinutes;
        private readonly bool devToolsEnabled = ModConfig.Loaded.TreeHollowsEnableDeveloperTools;
        private long updateTick;

        //private const int MinItems = 1;
        //private const int MaxItems = 8;
        public override InventoryBase Inventory => this.inventory;

        public BETreeHollowGrown()
        {
            this.inventory = new InventoryGeneric(this.maxSlots, null, null);
            //this.meshes = new MeshData[this.maxSlots]; //1.18
            var meshes  = new MeshData[this.maxSlots];
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side.IsServer())
            {
                var updateTick = this.RegisterGameTickListener(this.TreeHollowUpdate, 20000);
            }

            this.irlDayLengthMinutes = ((Vintagestory.Common.GameCalendar)this.Api.World.Calendar).DayLengthInRealLifeSeconds / 60;

            if (this.lastEmptied == 0)
            {
                //Debug.WriteLine("Initialize lastEmptied if necessary");
                this.lastEmptied = this.Api.World.Calendar.TotalHours;
                this.MarkDirty();
            }
        }

        private BlockPos RelativeToSpawn(BlockPos pos)
        {
            var worldSpawn = this.Api.World.DefaultSpawnPosition.XYZ.AsBlockPos;
            var blockPos = pos.SubCopy(worldSpawn);
            return new BlockPos(blockPos.X, pos.Y, blockPos.Z, 0);
        }


        public void TreeHollowUpdate(float par)
        {
            if (this.inventory.Empty)
            {
                var gameCurrentHours = this.Api.World.Calendar.TotalHours;
                var updateEveryGameHours = ((this.updateMinutes > 0 ? this.updateMinutes : 360f) / this.irlDayLengthMinutes) * 24;

                if (this.lastEmptied <= 0)
                {
                    this.lastEmptied = gameCurrentHours;
                    this.MarkDirty();
                    return;
                }

                var gameHoursPassed = gameCurrentHours - this.lastEmptied;
                var updatingInHours = updateEveryGameHours - gameHoursPassed;

                if (updatingInHours < 0)
                {
                    try
                    {
                        var uf = new TreeHollows();
                        uf.AddItemStacks(this, uf.MakeItemStacks(this.Block, this.Api as ICoreServerAPI));
                        this.MarkDirty();
                    }
                    catch
                    {
                        // WHY DOES THIS SOMETIMES FAIL IN MULTIPLAYER?
                    }

                    this.lastEmptied = this.Api.World.Calendar.TotalHours;
                    this.MarkDirty();
                }
            }
        }



        internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (this.Api.Side.IsServer()) //reset the listener on interact
            {
                this.UnregisterGameTickListener(this.updateTick);
                this.updateTick = this.RegisterGameTickListener(this.TreeHollowUpdate, (int)(this.updateMinutes * 60000));
            }

            var playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (playerSlot.Empty)
            {
                if (this.TryTake(byPlayer)) //, blockSel))
                { return true; }
                return false;
            }
            else
            {
                if (this.devToolsEnabled)
                {
                    var colObj = playerSlot.Itemstack.Collectible;
                    if (colObj.Attributes != null)
                    {
                        {
                            if (this.TryPut(playerSlot, blockSel))
                            {
                                var sound = this.Block.Sounds.Place;
                                Api.World.PlaySoundAt(sound, this.Pos.X, this.Pos.Y, this.Pos.Z, 0, null);
                                //this.Api.World.PlaySoundAt(sound ?? new AssetLocation("game:sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                                return true;
                            }
                        }
                        return false;
                    }
                }
            }
            return false;
        }

        internal void OnBreak() // IPlayer byPlayer, BlockPos pos)
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

        // Returns the first available empty inventory slot
        // returns -1 if all slots are full
        private int FirstFreeSlot()
        {
            var slot = 0;
            var found = false;
            do
            {
                if (this.inventory[slot].Empty)
                { found = true; }
                else
                { slot++; }
            }
            while (slot < this.maxSlots && found == false);
            if (!found)
            { slot = -1; }
            //Debug.WriteLine("Free Slot:" + slot);
            return slot;
        }


        // Returns the last filled inventory slot
        // returns -1 if all slots are empty
        private int LastFilledSlot()
        {
            var slot = this.maxSlots - 1;
            var found = false;
            do
            {
                if (!this.inventory[slot].Empty)
                { found = true; }
                else
                { slot--; }
            }
            while (slot > -1 && found == false);
            //Debug.WriteLine("Last Slot:" + slot);
            return slot;
        }


        //JHR
        internal CollectibleObject GetDisplayedCollectible()
        {
            var index = this.LastFilledSlot();
            if (index < 0)
            {
                return null;
            }
            return this.inventory[index].Itemstack.Collectible;
        }
        //END JHR



        private bool TryPut(ItemSlot slot, BlockSelection blockSel)
        {
            var index = this.FirstFreeSlot();
            if (index == -1)
            { return false; } //inventory full

            var moved = slot.TryPutInto(this.Api.World, this.inventory[index]);
            if (moved > 0)
            {
                //this.updateMesh(index);
                this.MarkDirty(true);
            }
            return moved > 0;
        }


        private bool TryTake(IPlayer byPlayer) //, BlockSelection blockSel)
        {
            var index = this.LastFilledSlot();
            if (index == -1)
            { return false; } //inventory empty

            if (!this.inventory[index].Empty)
            {
                var stack = this.inventory[index].TakeOut(1);
                if (byPlayer.InventoryManager.TryGiveItemstack(stack))
                {
                    if (index == 0) //last slot emptied
                    {
                        //reset the lastEmptied timer
                        //Debug.WriteLine("----EMPTIED");
                        this.lastEmptied = this.Api.World.Calendar.TotalHours;
                        this.MarkDirty();
                    }

                    var sound = this.Block.Sounds.Place;
                    Api.World.PlaySoundAt(sound, this.Pos.X, this.Pos.Y, this.Pos.Z, 0, null);
                }

                if (stack.StackSize > 0)
                {
                    this.Api.World.SpawnItemEntity(stack, this.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }

                //this.updateMesh(index);
                this.MarkDirty();
                return true;
            }
            return false;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
        {
            base.FromTreeAttributes(tree, worldForResolve);
            this.lastEmptied = tree.GetDouble("lastEmptied");
            if (this.Api != null)
            {
                if (this.Api.Side == EnumAppSide.Client)
                { this.Api.World.BlockAccessor.MarkBlockDirty(this.Pos); }
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("lastEmptied", this.lastEmptied);
        }

        public override void updateMeshes()
        {
            //try/catch an attempt to prevent the obscure crash reported by TRexTheHunter - Dec 29, 2023
            // 
            try
            {
                var index = this.LastFilledSlot() + 1;
                if (index == 0)
                { return; } //inventory empty
                for (var slot = 0; slot < index; slot++)
                {
                    if (!this.inventory[slot].Empty)
                    {
                        var stack = this.inventory[slot].Itemstack;
                        if (stack?.Item?.Shape != null)
                        {
                            if ((stack.Collectible.GetCollectibleInterface<IWearableStatsSupplier>() is IWearableStatsSupplier wearableStats))
                            {
                                this.updateMesh(slot);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }


        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            MeshData mesh = null;
            if (!(this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) is BlockTreeHollowGrown block))
            { return base.OnTesselation(mesher, tesselator); }
           // mesh = this.capi.TesselatorManager.GetDefaultBlockMesh(block); //add tree hollow
           // mesher.AddMeshData(mesh);

            //only render what is in the highest inventory slot
            var index = this.LastFilledSlot();
            if (index > -1)
            {
                var stack = this.inventory[index].Itemstack;
                if (stack != null)
                {
                    if (stack.Class == EnumItemClass.Block)
                    {
                        mesh = this.capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
                    }
                    else
                    { 
                        //THIS IS THE PROBLEM

                        this.nowTesselatingObj = stack.Item;
                        //var itemPath = stack.Item.Code.Path;
                        //var itemPathPart = stack.Item.FirstCodePart(1);
                        if (stack?.Item?.Shape != null)
                        {
                           // if ((stack.Collectible.GetCollectibleInterface<IWearableStatsSupplier>() is IWearableStatsSupplier wearableStats))
                           // {
                                if (stack?.Item != null)
                                {
                                    try //occasional random crash but why?????
                                    {
                                        this.capi.Tesselator.TesselateItem(stack.Item, out mesh, this);
                                        if (mesh != null)
                                        {
                                            mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);
                                        }
                                    }
                                    catch
                                    {}
                                }
                           // }
                        }
                    else
                    {
                    // seeds prolly
                    var shapeBase = "primitivesurvival:shapes/";
                            var shapePath = "block/trapbait"; //baited (for now)
                            var texture = tesselator.GetTextureSource(block);
                            mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase + shapePath, texture, tesselator);
                            if (mesh != null)
                            {
                                mesh.Translate(new Vec3f(0f, 0.03f, 0f));
                            }
                        }
                    }
                    
                    
                    if (mesh != null)
                    {
                        if (block.FirstCodePart(1).Contains("base"))
                        { 
                            mesh.Translate(new Vec3f(0f, 0.03f, 0f)); 
                        }
                        else //up has thicker bottom
                        { 
                            mesh.Translate(new Vec3f(0f, 0.23f, 0f)); 
                        }

                        //JHR
                        if (stack.Collectible.GetBehavior<BehaviorInTreeHollowTransform>()?.Transform is ModelTransform transform)
                        {
                            mesh.ModelTransform(transform);
                        }
                        //END JHR

                        if (block.LastCodePart() == "north" || block.LastCodePart() == "south")
                        {
                            mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, 90, 0);
                        }
                        mesher.AddMeshData(mesh);
                    }
                }
            } 
            return false;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            //base.GetBlockInfo(forPlayer, sb);
            var index = this.LastFilledSlot();
            if (index == -1)
            { sb.AppendLine(Lang.Get("Empty")); }
            else
            {
                var desc = this.inventory[index].Itemstack.GetName();
                sb.AppendLine(Lang.Get(desc));
            }
            sb.AppendLine();
            if (forPlayer?.CurrentBlockSelection == null)
            { return; }
        }
    }
}
