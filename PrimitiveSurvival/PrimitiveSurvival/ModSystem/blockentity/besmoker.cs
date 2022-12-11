namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Config;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.Util;
    using Vintagestory.GameContent;
    //using System.Diagnostics;


    public class BESmoker : BlockEntityDisplayCase
    {
        static SimpleParticleProperties breakSparks;
        static SimpleParticleProperties smallMetalSparks;
        static SimpleParticleProperties smoke;

        //first 4 for trussed meats, last 1 for firewood
        private readonly int maxSlots = 5;
        protected static readonly Random Rnd = new Random();
        public string State { get; protected set; }
        private readonly long particleTick;

        private BlockFacing ownFacing;
        private double burningUntilTotalDays;
        private double burningStartTotalDays;
        public override string InventoryClassName => "smoker";
        public override InventoryBase Inventory => this.inventory;

        private AssetLocation doorOpenSound;
        private AssetLocation doorCloseSound;
        private AssetLocation meatSound;
        private AssetLocation logSound;

        static BESmoker()
        {
            smallMetalSparks = new SimpleParticleProperties(
                2, 5,
                ColorUtil.ToRgba(255, 255, 233, 83),
                new Vec3d(), new Vec3d(),
                new Vec3f(-3f, 8f, -3f),
                new Vec3f(3f, 12f, 3f),
                0.1f,
                1f,
                0.25f, 0.25f,
                EnumParticleModel.Quad
            )
            {
                WithTerrainCollision = false,
                VertexFlags = 128
            };
            smallMetalSparks.AddPos.Set(1 / 16f, 0, 1 / 16f);
            smallMetalSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.QUADRATIC, -0.5f);
            smallMetalSparks.AddPos.Set(4 / 16.0, 3 / 16.0, 4 / 16.0);
            smallMetalSparks.ParticleModel = EnumParticleModel.Cube;
            smallMetalSparks.LifeLength = 0.04f;
            smallMetalSparks.MinQuantity = 1;
            smallMetalSparks.AddQuantity = 1;
            smallMetalSparks.MinSize = 0.2f;
            smallMetalSparks.MaxSize = 0.2f;
            smallMetalSparks.GravityEffect = 0f;

            breakSparks = new SimpleParticleProperties(
                40, 80,
                ColorUtil.ToRgba(255, 255, 233, 83),
                new Vec3d(), new Vec3d(),
                new Vec3f(-1f, 0.5f, -1f),
                new Vec3f(2f, 1.5f, 2f),
                0.5f,
                1f,
                0.25f, 0.25f
            )
            {
                VertexFlags = 128
            };
            breakSparks.AddPos.Set(4 / 16f, 4 / 16f, 4 / 16f);
            breakSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);

            smoke = new SimpleParticleProperties(
                1, 1, ColorUtil.ToRgba(128, 110, 110, 110), new Vec3d(), new Vec3d(),
                new Vec3f(-0.2f, 0.3f, -0.2f), new Vec3f(0.2f, 0.3f, 0.2f), 2, 0, 0.5f, 1f, EnumParticleModel.Quad
            )
            {
                SelfPropelled = true,
                OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255),
                SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 2)
            };
        }

        public BESmoker()
        {
            this.inventory = new InventoryGeneric(this.maxSlots, null, null);
            this.meshes = new MeshData[this.maxSlots];
        }

        public bool IsBurning { get; private set; }

        public ItemSlot WoodSlot => this.inventory[4];

        public ItemStack WoodStack
        {
            get => this.inventory[4].Itemstack;
            set => this.inventory[4].Itemstack = value;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.inventory.LateInitialize("smoker" + "-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
            this.RegisterGameTickListener(this.OnGameTick, 100);
            this.doorOpenSound = new AssetLocation("game", "sounds/block/chestopen");
            this.doorCloseSound = new AssetLocation("game", "sounds/block/chestclose");
            this.meatSound = new AssetLocation("game", "sounds/block/stickplace");
            this.logSound = new AssetLocation("game", "sounds/block/planks");

            this.ownFacing = BlockFacing.FromCode(api.World.BlockAccessor.GetBlock(this.Pos).LastCodePart());
        }


        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            this.UnregisterGameTickListener(this.particleTick);
        }

        private void EmitParticles()
        {
            if (this.Api.World.Rand.Next(5) > 0)
            {
                smoke.MinPos.Set(this.Pos.X + 0.5 - 2 / 16.0, this.Pos.Y + 10 / 16f, this.Pos.Z + 0.5 - 2 / 16.0);
                smoke.AddPos.Set(4 / 16.0, 0, 4 / 16.0);
                this.Api.World.SpawnParticles(smoke, null);
            }

            if (this.Api.World.Rand.Next(3) == 0)
            {
                var dir = this.ownFacing.Normalf;
                var particlePos = smallMetalSparks.MinPos;
                particlePos.Set(this.Pos.X + 0.5, this.Pos.Y, this.Pos.Z + 0.5);
                particlePos.Sub(dir.X * (6 / 16.0) + 2 / 16f, 0, dir.Z * (6 / 16.0) + 2 / 16f);

                smallMetalSparks.MinPos = particlePos;
                smallMetalSparks.MinVelocity = new Vec3f(-0.5f - dir.X, -0.3f, -0.5f - dir.Z);
                smallMetalSparks.AddVelocity = new Vec3f(1f - dir.X, 0.6f, 1f - dir.Z);
                this.Api.World.SpawnParticles(smallMetalSparks, null);
            }
        }

        private void OnGameTick(float dt)
        {
            if (this.IsBurning)
            { this.EmitParticles(); }
            if (!this.IsBurning)
            { return; }

            if (this.Api.Side == EnumAppSide.Server && this.burningUntilTotalDays < this.Api.World.Calendar.TotalDays)
            { this.DoSmoke(); }
        }

        private void DoSmoke()
        {
            //Convert raw meats into their smoked variants
            //remove all the firewood
            this.WoodStack = null;
            for (var cnt = 0; cnt < this.FirstFreeSlot(); cnt++)
            {
                var thisStack = this.inventory[cnt].Itemstack.Collectible.Code.Path;
                if (thisStack.Contains("trussed")) //might be rot
                {
                    thisStack = "primitivesurvival:" + thisStack.Replace("raw", "smoked");
                    //this.inventory[cnt].TakeOut(1);
                    this.inventory[cnt].Itemstack = new ItemStack(this.Api.World.GetItem(new AssetLocation(thisStack)), 1);
                }
            }
            this.IsBurning = false;
            this.burningUntilTotalDays = 0;
            this.State = "closed";
            this.MarkDirty();
        }

        public bool TryIgnite()
        {
            if (!this.CanIgnite() || this.IsBurning)
                return false;

            this.IsBurning = true;
            this.State = "lit";
            this.burningUntilTotalDays = this.Api.World.Calendar.TotalDays + 10 / 24.0;
            this.burningStartTotalDays = this.Api.World.Calendar.TotalDays;
            this.MarkDirty();
            return true;
        }

        public bool CanIgnite()
        {
            return !this.IsBurning && this.WoodSlot.StackSize == 4 && this.inventory[0].StackSize > 0;
        }

        // Returns the first available empty inventory slot, returns -1 if all slots are full
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

        internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            var result = false;
            var playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            var playerPath = "";
            if (playerSlot.Itemstack?.Collectible != null)
            { playerPath = playerSlot.Itemstack.Collectible.Code.Path; }

            var smokerPath = "";
            if (this.inventory[0].Itemstack?.Collectible != null)
            { smokerPath = this.inventory[0].Itemstack.Collectible.Code.Path; }
            if (this.State == "open")
            {
                if (playerPath == "firewood")
                {
                    //try placing or removing firewood
                    //if the firewood slot is empty or less than full add one
                    if (this.WoodSlot.Empty || this.WoodStack?.StackSize < 4)
                    { result = this.TryPut(playerSlot, this.WoodSlot); }
                    else
                    {
                        //else try removing one? Nope once the wood is in its in
                        //result = this.TryTake(byPlayer, this.WoodSlot);
                    }
                    if (result)
                    {
                        this.Api.World.PlaySoundAt(this.logSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
                        return true;
                    }
                }
                else if (smokerPath.Contains("smoked") || smokerPath.Contains("rot"))
                {
                    //try placing or removing trussed meat
                    //if there's an available trussed meat slot, try adding one
                    var cnt = this.FirstFreeSlot() - 1;
                    if (cnt == -2)
                    { cnt = 3; }
                    if (0 <= cnt && cnt <= 3)
                    { result = this.TryTake(byPlayer, this.inventory[cnt]); }
                    //else try removing one? Nah once its in its in
                    if (result)
                    {
                        this.Api.World.PlaySoundAt(this.meatSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
                        return true;
                    }
                }
                else if (playerPath.Contains("trussedmeat") && playerPath.Contains("raw"))
                {
                    //try placing or removing trussed meat
                    //if there's an available trussed meat slot, try adding one
                    var cnt = this.FirstFreeSlot();
                    if (0 <= cnt && cnt <= 3)
                    { result = this.TryPut(playerSlot, this.inventory[cnt]); }
                    //else try removing one? Nah once its in its in
                    if (result)
                    {
                        this.Api.World.PlaySoundAt(this.meatSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
                        return true;
                    }
                }
            }
            else if (this.State == "closed")
            {
                this.Api.World.PlaySoundAt(this.doorOpenSound, blockSel.Position.X, blockSel.Position.Y - 1, blockSel.Position.Z, byPlayer);
                this.State = "open";
                this.MarkDirty(true);
                return true;
            }
            if (this.State == "open")
            {
                //close the door
                this.Api.World.PlaySoundAt(this.doorCloseSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
                this.State = "closed";
                this.MarkDirty(true);
                return true;
            }
            return false;
        }

        internal void OnBreak()
        {
            var stateCode = "raw";
            for (var index = this.maxSlots - 2; index >= 0; index--)
            {
                if (!this.inventory[index].Empty)
                {
                    var stackSize = this.inventory[index].StackSize;
                    if (stackSize > 0)
                    {
                        var stack = this.inventory[index].TakeOut(1);
                        stateCode = stack.Collectible.LastCodePart();

                        if (stateCode == "raw")
                        {
                            double d = index / 10;
                            this.Api.World.SpawnItemEntity(stack, this.Pos.ToVec3d().Add(0.5, 0.5 + d, 0.5));
                        }
                        else if (stateCode == "rot")
                        {
                            var stackCode = "game:rot";
                            var newAsset = new AssetLocation(stackCode);
                            if (newAsset != null)
                            {
                                var tempStack = new ItemStack(this.Api.World.GetItem(newAsset), 4);
                                double d = index / 10;
                                this.Api.World.SpawnItemEntity(tempStack, this.Pos.ToVec3d().Add(0.5, 1.0 + d, 0.5));
                            }
                        }
                        else //smoked
                        {
                            var meatCode = stack.Collectible.LastCodePart(1);
                            var stackCode = "primitivesurvival:smokedmeat-" + meatCode + "-raw";
                            var newAsset = new AssetLocation(stackCode);
                            if (newAsset != null)
                            {
                                var tempStack = new ItemStack(this.Api.World.GetItem(newAsset), 4);
                                double d = index / 10;
                                this.Api.World.SpawnItemEntity(tempStack, this.Pos.ToVec3d().Add(0.5, 1.0 + d, 0.5));
                            }
                        }
                    }
                    this.MarkDirty(true);
                }
            }
            //if the smoker is lit DONT drop the wood
            if (!this.IsBurning)
            {
                for (var cnt = 1; cnt <= this.WoodSlot.StackSize; cnt++)
                {
                    var stack = this.WoodSlot.TakeOut(1);
                    double d = cnt / 10;
                    this.Api.World.SpawnItemEntity(stack, this.Pos.ToVec3d().Add(0.5, 0.5 + d, 0.5));
                }
                this.MarkDirty(true);
            }
            else
            {
                this.WoodStack = null;
                this.MarkDirty(true);
            }
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            this.State = "closed";
            this.MarkDirty(true);
            base.OnBlockPlaced(byItemStack);
        }

        private bool TryPut(ItemSlot playerSlot, ItemSlot targetSlot)
        {
            var moved = playerSlot.TryPutInto(this.Api.World, targetSlot);
            if (moved > 0)
            {
                this.MarkDirty(true);
                return moved > 0;
            }
            return false;
        }

        private bool TryTake(IPlayer byPlayer, ItemSlot sourceSlot)
        {
            if (!sourceSlot.Empty)
            {
                var stackCode = sourceSlot.Itemstack.Collectible.Code.Path;
                if (!stackCode.Contains("rot"))
                {
                    //convert from trussed to individual meats onTake
                    var meatCode = sourceSlot.Itemstack.Collectible.LastCodePart(1);
                    stackCode = "primitivesurvival:smokedmeat-" + meatCode + "-raw";
                }

                //Debug.WriteLine(stackCode);
                var newAsset = new AssetLocation(stackCode);
                var tempStack = new ItemStack(this.Api.World.GetItem(newAsset), 4);
                //if (tempStack != null)
                //{
                //    sourceSlot.Itemstack = tempStack;
                //}

                var takeOK = byPlayer.InventoryManager.TryGiveItemstack(tempStack);
                if (!takeOK) //player has no free slots
                {
                    //Debug.WriteLine("spawn in world");
                    Api.World.SpawnItemEntity(tempStack, byPlayer.Entity.Pos.XYZ.Add(0, 0.5, 0));
                }
                sourceSlot.Itemstack = null;
                //sourceSlot.TakeOut(1);
                this.MarkDirty(true);
                return true;
            }
            return false;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
        {
            base.FromTreeAttributes(tree, worldForResolve);
            this.State = tree.GetString("state");
            this.IsBurning = tree.GetInt("burning") > 0;
            this.burningUntilTotalDays = tree.GetDouble("burningUntilTotalDays");
            this.burningStartTotalDays = tree.GetDouble("burningStartTotalDays");
            if (this.Api != null)
            {
                if (this.Api.Side == EnumAppSide.Client)
                { this.Api.World.BlockAccessor.MarkBlockDirty(this.Pos); }
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("state", this.State);
            tree.SetInt("burning", this.IsBurning ? 1 : 0);
            tree.SetDouble("burningUntilTotalDays", this.burningUntilTotalDays);
            tree.SetDouble("burningStartTotalDays", this.burningStartTotalDays);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            var cnt = 0;
            if (!this.inventory[0].Empty)
            {
                var path = this.inventory[0].Itemstack.Item.Code.Path;
                if (path.Contains("rot"))
                { sb.Append(Lang.Get("primitivesurvival:item-trussedrot")); }
                else
                { sb.Append(Lang.Get("primitivesurvival:item-" + path)); }
                cnt++;
            }
            if (!this.inventory[1].Empty)
            {
                var path = this.inventory[1].Itemstack.Item.Code.Path;
                if (path.Contains("rot"))
                { sb.AppendLine(", " + Lang.Get("primitivesurvival:item-trussedrot")); }
                else
                { sb.AppendLine(", " + Lang.Get("primitivesurvival:item-" + path)); }
                cnt++;
            }
            if (!this.inventory[2].Empty)
            {
                var path = this.inventory[2].Itemstack.Item.Code.Path;
                if (path.Contains("rot"))
                { sb.Append(Lang.Get("primitivesurvival:item-trussedrot")); }
                else
                { sb.Append(Lang.Get("primitivesurvival:item-" + path)); }
                cnt++;
            }

            if (!this.inventory[3].Empty)
            {
                var path = this.inventory[3].Itemstack.Item.Code.Path;
                if (path.Contains("rot"))
                { sb.AppendLine(", " + Lang.Get("primitivesurvival:item-trussedrot")); }
                else
                { sb.AppendLine(", " + Lang.Get("primitivesurvival:item-" + path)); }
                cnt++;
            }
            if (cnt == 1 || cnt == 3)
            { sb.AppendLine(); }

            var WoodCount = 0;
            if (!this.WoodSlot.Empty)
            { WoodCount = this.WoodStack.StackSize; }
            sb.AppendLine(WoodCount.ToString() + "/4 " + Lang.Get("item-firewood"));
            sb.AppendLine();

            var percentComplete = Math.Round((this.Api.World.Calendar.TotalDays - this.burningStartTotalDays) / (this.burningUntilTotalDays - this.burningStartTotalDays) * 100, 0);

            if (0 <= percentComplete && percentComplete < 100 && this.IsBurning)
            {
                sb.AppendLine("" + percentComplete + "% " + Lang.Get("primitivesurvival:blockhelp-smoker-complete"));
            }
            sb.AppendLine();
            //sb.AppendLine(string.Format("DEBUG: {3} , Current total days: {0} , BurningStart total days: {1} , BurningUntil total days: {2}", this.Api.World.Calendar.TotalDays, this.burningStartTotalDays, this.burningUntilTotalDays, this.burning));
        }

        private MeshData GenItemMesh(ItemStack trussedStack, int count)
        {
            this.nowTesselatingObj = trussedStack.Item;
            MeshData mesh = null;

            if (trussedStack?.Item?.Shape != null)
            {
                try
                {
                    this.capi.Tesselator.TesselateItem(trussedStack.Item, out mesh, this);
                }
                catch { return mesh; }
                if (mesh != null)
                {
                    mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);
                }
                if (count == 0)
                {
                    mesh.Translate(0f, 1.2f, -0.2f);
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 30 * GameMath.DEG2RAD, 0);
                }
                else if (count == 1)
                {
                    mesh.Translate(0f, 1.2f, 0.3f);
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, -20 * GameMath.DEG2RAD, 0);
                }
                else if (count == 2)
                {
                    mesh.Translate(-0.5f, 1.2f, 0f);
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, -25 * GameMath.DEG2RAD, 0);
                }
                else
                {
                    mesh.Translate(-0.5f, 1.2f, 0.15f);
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 15 * GameMath.DEG2RAD, 0);
                }
                if (trussedStack.Item.Code.Path.Contains("fish")) //sigh
                {
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 180 * GameMath.DEG2RAD, 180 * GameMath.DEG2RAD, 180 * GameMath.DEG2RAD); //
                    mesh.Scale(new Vec3f(0.5f, 0, 0.5f), 0.5f, 0.5f, 0.5f);
                    mesh.Translate(0f, 0.4f, 0f);
                }
                else
                {
                    mesh.Scale(new Vec3f(0.5f, 0, 0.5f), 0.75f, 0.75f, 0.75f);
                }
                var rotate = this.Block.Shape.rotateY;
                mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, rotate * GameMath.DEG2RAD, 0); //orient based on direction
            }
            return mesh;
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            MeshData mesh;
            var shapeBase = "primitivesurvival:shapes/block/smoker/smokerblock-placed";
            var count = 0;
            if (this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) is BlockSmoker block)
            {
                var texture = tesselator.GetTexSource(block);
                //Base model
                mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase, texture, this.State, count);
                mesher.AddMeshData(mesh);

                // Door next
                shapeBase = shapeBase.Replace("placed", "door");
                mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase, texture, this.State, count);
                mesher.AddMeshData(mesh);

                if (this.inventory != null)
                {
                    // Wood 
                    if (!this.WoodSlot.Empty)
                    {
                        if (this.State == "lit")
                        { shapeBase = shapeBase.Replace("door", "lit"); }
                        else
                        { shapeBase = shapeBase.Replace("door", "log"); }
                        for (count = 1; count <= this.WoodStack.StackSize; count++)
                        {
                            mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase, texture, this.State, count);
                            mesher.AddMeshData(mesh);
                        }
                    }
                    //Meat
                    // dont render if the smoker is lit or closed
                    if (this.State != "lit")
                    {
                        for (count = 0; count < 4; count++)
                        {
                            if (!this.inventory[count].Empty)
                            {
                                var tmpStack = this.inventory[count].Itemstack;
                                var tmpPath = tmpStack.Collectible.Code.Path;
                                if (tmpPath == "rot")
                                {
                                    var newAsset = new AssetLocation("primitivesurvival:trussedrot");
                                    tmpStack = new ItemStack(this.Api.World.GetItem(newAsset));
                                }
                                mesh = this.GenItemMesh(tmpStack, count);
                                if (mesh != null)
                                { mesher.AddMeshData(mesh); }
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}

