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


    public class BESupport : BlockEntityDisplayCase
    {
        private readonly int catchPercent = ModConfig.Loaded.FishBasketCatchPercent;
        private readonly int baitedCatchPercent = ModConfig.Loaded.FishBasketBaitedCatchPercent;
        private readonly int baitStolenPercent = ModConfig.Loaded.FishBasketBaitStolenPercent;
        private readonly int escapePercent = ModConfig.Loaded.FishBasketEscapePercent;
        private readonly double updateMinutes = ModConfig.Loaded.FishBasketUpdateMinutes;
        private readonly int rotRemovedPercent = ModConfig.Loaded.FishBasketRotRemovedPercent;

        private readonly int tickSeconds = 4;
        private readonly int maxSlots = 3;
        private readonly string[] pipeTypes = { "pipe" };
        protected static readonly Random Rnd = new Random();

        private long particleTick;

        public override string InventoryClassName => "support";

        //protected InventoryGeneric inventory;

        public override InventoryBase Inventory => this.inventory;

        private AssetLocation wetPickupSound;
        private AssetLocation dryPickupSound;

        public BESupport()
        {
            this.inventory = new InventoryGeneric(this.maxSlots, null, null);
            this.meshes = new MeshData[this.maxSlots];
        }


        public ItemSlot PipeSlot => this.inventory[0];

        public ItemSlot WaterSlot => this.inventory[1];

        public ItemSlot OtherSlot => this.inventory[2];

        public ItemStack PipeStack
        {
            get => this.inventory[0].Itemstack;
            set => this.inventory[0].Itemstack = value;
        }

        public ItemStack WaterStack
        {
            get => this.inventory[1].Itemstack;
            set => this.inventory[1].Itemstack = value;
        }

        public ItemStack OtherStack
        {
            get => this.inventory[2].Itemstack;
            set => this.inventory[2].Itemstack = value;
        }


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side.IsServer())
            {
                this.particleTick = this.RegisterGameTickListener(this.ParticleUpdate, this.tickSeconds * 100); //1000
                var updateTick = this.RegisterGameTickListener(this.FishBasketUpdate, (int)(this.updateMinutes * 60000));
            }
            this.wetPickupSound = new AssetLocation("game", "sounds/environment/smallsplash");
            this.dryPickupSound = new AssetLocation("game", "sounds/block/cloth");
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            this.UnregisterGameTickListener(this.particleTick);
        }


        private void GenerateWaterParticles(BlockPos pos, IWorldAccessor world)
        {
            float minQuantity = 1;
            float maxQuantity = 1;
            var color = ColorUtil.ToRgba(40, 125, 185, 255);
            var minPos = new Vec3d();
            var addPos = new Vec3d();
            var minVelocity = new Vec3f(0f, 0.0f, 0f);

            var lifeLength = 2f;
            var gravityEffect = 0.5f;
            var minSize = 0.3f;
            var maxSize = 0.3f;

            var dx = 0.825f;
            var dz = 0f;
            var dir = this.Block.LastCodePart();
            if (dir == "south")
            { dz = 1f; }
            else if (dir == "east")
            { dx = 1f; }
            else if (dir == "west")
            { dx = 0f; }

            Vec3f maxVelocity;

            if (dir == "east" || dir == "west")
            {
                dz = 0.675f;
                var vel = dz;
                var rando = Rnd.Next(2);
                if (rando == 0)
                { vel *= -1f; dz = 0.825f; }
                maxVelocity = new Vec3f(vel / 2, 0.4f, 0f);
                rando = Rnd.Next(2);
                if (rando == 0)
                { dz -= 0.5f; }
            }
            else
            {
                var vel = dx;
                var rando = Rnd.Next(2);
                if (rando == 0)
                { vel *= -1f; dx = 0.675f; }
                maxVelocity = new Vec3f(0f, 0.4f, vel / 2);
                rando = Rnd.Next(2);
                if (rando == 0)
                { dx -= 0.5f; }
            }

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

            waterParticles.MinPos.Set(pos.ToVec3d().AddCopy(dx, 0.55, dz));

            waterParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 0.2f);
            waterParticles.ShouldDieInAir = false;


            // CHANGED FOR 1.17
            waterParticles.Bounciness = 0.7f;
            //waterParticles.Bouncy = true;


            waterParticles.SelfPropelled = true;
            world.SpawnParticles(waterParticles);
        }


        public void ParticleUpdate(float par)
        {
            if (this.PipeStack != null)
            {
                if (this.PipeStack.Block.Code.Path.Contains("-aerated-"))
                {
                    var rando = Rnd.Next(2); //overall frequency of drips
                    if (rando == 0)
                    {
                        this.GenerateWaterParticles(this.Pos, this.Api.World);
                    }
                }
            }
        }


        public void FishBasketUpdate(float par)
        {
            /*
            var waterAllAround = true;
            int rando;
            Block testBlock;
            var neibPos = new BlockPos[] { this.Pos.EastCopy(), this.Pos.NorthCopy(), this.Pos.SouthCopy(), this.Pos.WestCopy() };

            // Examine sides 
            foreach (var neib in neibPos)
            {
                testBlock = this.Api.World.BlockAccessor.GetBlock(neib);
                if (testBlock.LiquidCode != "water")
                { waterAllAround = false; }
            }
            if (waterAllAround)
            {
                int escaped;
                var caught = Rnd.Next(100);
                if (!this.PipeSlot.Empty)
                {
                    rando = Rnd.Next(100);
                    if (rando < this.baitStolenPercent)
                    {
                        if (this.WorldTake(0, this.Pos))
                        { this.GenerateWaterParticles("fish", this.Pos, this.Api.World); }
                    }
                }
                var caughtOk = false;
                if ((!this.PipeSlot.Empty && caught < this.baitedCatchPercent) || (this.PipeSlot.Empty && caught < this.catchPercent))
                {
                    rando = Rnd.Next(2);
                    caughtOk = this.WorldPut(rando + 1); //, this.Pos);
                    if (!caughtOk)
                    {
                        rando = 1 - rando;
                        caughtOk = this.WorldPut(rando + 1); //, this.Pos);
                    }
                }
                if (!caughtOk && (!this.WaterSlot.Empty || !this.OtherSlot.Empty))
                {
                    escaped = Rnd.Next(100);
                    if (escaped < this.escapePercent)
                    {
                        bool escapedOk; // = false;
                        rando = Rnd.Next(2);
                        escapedOk = this.WorldTake(rando + 1, this.Pos);
                        if (!escapedOk)
                        {
                            rando = 1 - rando;
                            escapedOk = this.WorldTake(rando + 1, this.Pos);
                        }
                        if (escapedOk)
                        { this.GenerateWaterParticles("fish", this.Pos, this.Api.World); }
                    }
                }

                if (!this.WaterSlot.Empty)
                {
                    if (this.WaterStack.Item != null)
                    {
                        if (this.WaterStack.Item.Code.Path == "rot")
                        {
                            escaped = Rnd.Next(100);
                            if (escaped < this.rotRemovedPercent)
                            { this.WorldTake(1, this.Pos); } //remove rot from slot 1
                        }
                    }
                }

                if (!this.OtherSlot.Empty)
                {
                    if (this.OtherStack.Item != null)
                    {
                        if (this.OtherStack.Item.Code.Path == "rot")
                        {
                            escaped = Rnd.Next(100);
                            if (escaped < this.rotRemovedPercent)
                            { this.WorldTake(2, this.Pos); } //remove rot from slot 2
                        }
                    }
                }
            } */
        }


        public bool WorldPut(int slot) //, BlockPos pos)
        {
            /*
            ItemStack newStack; // = null;
            var rando = Rnd.Next(5);
            if (rando < 1) // 20% chance of a seashell or relic
            {
                rando = Rnd.Next(10); //10

                if (rando < 1) //10% chance of a relic
                {
                    var thisRelic = this.relics[Rnd.Next(this.relics.Count())];
                    newStack = new ItemStack(this.Api.World.GetItem(new AssetLocation("primitivesurvival:" + thisRelic)), 1);
                }
                else
                { newStack = new ItemStack(this.Api.World.GetBlock(new AssetLocation("game:seashell-" + this.shellStates[Rnd.Next(this.shellStates.Count())] + "-" + this.shellColors[Rnd.Next(this.shellColors.Count())])), 1); }
            }
            else
            { newStack = new ItemStack(this.Api.World.GetItem(new AssetLocation("primitivesurvival:psfish-" + this.fishTypes[Rnd.Next(this.fishTypes.Count())] + "-raw")), 1); }
            if (this.inventory[slot].Empty)
            {
                this.inventory[slot].Itemstack = newStack;
                this.MarkDirty(true);
                return true;
            }
            */
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


        public bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            var slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Empty)
            {
                if (this.TryTake(byPlayer))
                {
                    this.Api.World.PlaySoundAt(this.dryPickupSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
                    return true;
                }
                return false;
            }
            else
            {
                if (this.TryPut(slot))
                {
                    this.Api.World.PlaySoundAt(this.dryPickupSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
                    return true;
                }
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

        private bool TryPut(ItemSlot playerSlot)
        {
            //Debug.WriteLine("TryPut");
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
                //Debug.WriteLine("Putting a " + playerStack.Item.FirstCodePart());
                if (Array.IndexOf(this.pipeTypes, playerStack.Item.FirstCodePart()) >= 0 && this.PipeSlot.Empty)
                { index = 0; }
                else if (playerStack.Item.Code.Path.Contains("psfish"))
                {
                    if (this.WaterSlot.Empty)
                    { index = 1; }
                    else if (this.OtherSlot.Empty)
                    { index = 2; }
                }
            }
            else if (playerStack.Block != null)
            {
                //Debug.WriteLine("Putting a " + playerStack.Block.FirstCodePart());
                if (Array.IndexOf(this.pipeTypes, playerStack.Block.FirstCodePart()) >= 0 && this.PipeSlot.Empty)
                { index = 0; }
                else if (playerStack.Block.Code.Path.Contains("seashell"))
                {
                    if (this.WaterSlot.Empty)
                    { index = 1; }
                    else if (this.OtherSlot.Empty)
                    { index = 2; }
                }
            }
            if (index >= 0)
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
            return false; //can we just prevent this to ensure no mixed pipe types?

            /*
            if (!this.OtherSlot.Empty)
            {
                byPlayer.InventoryManager.TryGiveItemstack(this.OtherStack);
                this.OtherSlot.TakeOut(1);
                this.MarkDirty(true);
                return true;
            }
            else if (!this.WaterSlot.Empty)
            {
                byPlayer.InventoryManager.TryGiveItemstack(this.WaterStack);
                this.WaterSlot.TakeOut(1);
                this.MarkDirty(true);
                return true;
            }
            else if (!this.PipeSlot.Empty)
            {
                byPlayer.InventoryManager.TryGiveItemstack(this.PipeStack);
                this.PipeSlot.TakeOut(1);
                this.MarkDirty(true);
                return true;
            }
            return false;
            */
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default);
            if (block.Code.Path.Contains("inwater"))
            {
                var rot = false;
                if (!this.WaterSlot.Empty || !this.OtherSlot.Empty)
                {
                    sb.Append(Lang.Get("primitivesurvival:blockdesc-fishbasket-catch"));
                    if (!this.WaterSlot.Empty)
                    {
                        if (this.WaterStack.Block != null)
                        { }
                        else if (!this.WaterStack.Item.Code.Path.Contains("psfish"))
                        { sb.Append(" " + Lang.Get("primitivesurvival:blockdesc-fishbasket-catch-rotten")); }
                        rot = true;
                    }
                    if (!this.OtherSlot.Empty && !rot)
                    {
                        if (this.OtherStack.Block != null)
                        { }
                        else if (!this.OtherStack.Item.Code.Path.Contains("psfish"))
                        { sb.Append(" " + Lang.Get("primitivesurvival:blockdesc-fishbasket-catch-rotten")); }
                    }
                    sb.AppendLine().AppendLine();
                }
                else
                {
                    if (!this.PipeSlot.Empty)
                    {
                        if (this.PipeStack.Item != null)
                        {
                            if (Array.IndexOf(this.pipeTypes, this.PipeStack.Item.FirstCodePart()) < 0)
                            { sb.Append(Lang.Get("primitivesurvival:blockdesc-deadfall-bait-rotten")); }
                            else
                            { sb.Append(Lang.Get("primitivesurvival:blockdesc-deadfall-bait-ok")); }
                        }
                        else if (this.PipeStack.Block != null)
                        {
                            if (Array.IndexOf(this.pipeTypes, this.PipeStack.Block.FirstCodePart()) < 0)
                            { sb.Append(Lang.Get("primitivesurvival:blockdesc-deadfall-bait-rotten")); }
                            else
                            { sb.Append(Lang.Get("primitivesurvival:blockdesc-deadfall-bait-ok")); }
                        }
                    }
                    else if (this.PipeSlot.Empty)
                    { sb.Append(Lang.Get("primitivesurvival:blockdesc-deadfall-bait-needed")); }
                    sb.AppendLine().AppendLine();
                }
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
            var alive = false;
            Block tmpBlock;

            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockSupport;
            var texture = tesselator.GetTexSource(block);
            var tmpTextureSource = texture;
            shapePath = "primitivesurvival:shapes/" + block.Shape.Base.Path;
            mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePath, texture, -1, alive, tesselator);
            mesher.AddMeshData(mesh);

            if (this.inventory != null)
            {
                if (!this.PipeSlot.Empty) //bait or rot
                {
                    Block tempBlock;
                    if (this.PipeStack.Block != null)
                    {
                        tempBlock = this.Api.World.GetBlock(this.PipeStack.Block.Id);
                        tmpTextureSource = ((ICoreClientAPI)this.Api).Tesselator.GetTexSource(tempBlock);
                        if (Array.IndexOf(this.pipeTypes, this.PipeStack.Block.FirstCodePart()) < 0)
                        {
                            tempBlock = this.Api.World.GetBlock(block.CodeWithPath("texturerot"));
                            tmpTextureSource = ((ICoreClientAPI)this.Api).Tesselator.GetTexSource(tempBlock);
                        }
                        shapePath = "primitivesurvival:shapes/" + tempBlock.Shape.Base.Path;
                        mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePath, tmpTextureSource, 0, alive, tesselator);
                        mesher.AddMeshData(mesh);

                        //water
                        shapePath = "primitivesurvival:shapes/block/pipe/water";
                        tempBlock = this.Api.World.GetBlock(block.CodeWithPath("texturewater"));
                        tmpTextureSource = ((ICoreClientAPI)this.Api).Tesselator.GetTexSource(tempBlock);
                        mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePath, tmpTextureSource, 0, alive, tesselator);
                        mesher.AddMeshData(mesh);
                    }
                }

                for (var i = 1; i <= 2; i++)
                {
                    shapePath = "";
                    alive = false;
                    if (!this.inventory[i].Empty) //fish, shell, or rot
                    {
                        if (this.inventory[i].Itemstack.Block != null) //shell
                        {
                            shapePath = "game:shapes/block/seashell/" + this.inventory[i].Itemstack.Block.FirstCodePart(1);
                            tmpTextureSource = tesselator.GetTexSource(this.inventory[i].Itemstack.Block);
                        }
                        else if (this.inventory[i].Itemstack.Item.Code.Path.Contains("gear"))
                        {
                            var gearType = this.inventory[i].Itemstack.Item.FirstCodePart(1);
                            tmpBlock = this.Api.World.GetBlock(block.CodeWithPath("texture" + gearType));
                            if (gearType != "rusty")
                            { gearType = "temporal"; }
                            shapePath = "game:shapes/item/gear-" + gearType;
                            tmpTextureSource = ((ICoreClientAPI)this.Api).Tesselator.GetTexSource(tmpBlock);
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
                            mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePath, tmpTextureSource, i, alive, tesselator);
                            mesher.AddMeshData(mesh);
                        }
                    }
                }
            }
            //return base.OnTesselation(mesher, tesselator);
            return true;
        }
    }
}
