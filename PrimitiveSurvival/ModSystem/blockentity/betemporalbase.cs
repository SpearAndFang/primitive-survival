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
    //using Vintagestory.API.Datastructures;
    using PrimitiveSurvival.ModConfig;
    //using System.Diagnostics;

    //public class BETemporalBase : BlockEntityDisplayCase //1.18
    public class BETemporalBase : BlockEntityDisplayCase, ITexPositionSource
    {
        public new ICoreClientAPI capi;

        private readonly int tickSeconds = 1;
        private readonly int maxSlots = 6;
        private static readonly Random Rnd = new Random();
        private readonly bool dropsGold = ModConfig.Loaded.AltarDropsGold;
        private readonly bool dropsVegetables = ModConfig.Loaded.AltarDropsVegetables;
        private readonly bool dropsFish = ModConfig.Loaded.AltarDropsFish;

        //sky particle related
        //private int etherealGearCount;
        private double interval = 0.004;
        private bool killOffThread = false;

        private readonly string[] nonEntities = { "fireflies", "earthworm", "willowisp", "bioluminescent", "strawdummy", "armorstand", "skullofthedead" };
        public override string InventoryClassName => "temporalbase";
        protected new InventoryGeneric inventory; //1.18

        public override InventoryBase Inventory => this.inventory;


        public BETemporalBase()
        {
            this.inventory = new InventoryGeneric(this.maxSlots, null, null);
            //this.meshes = new MeshData[this.maxSlots]; //1.18
            var meshes  = new MeshData[this.maxSlots];
        }

        public ItemSlot MiddleSlot => this.inventory[0];

        public ItemSlot TopSlot => this.inventory[1];

        public ItemStack MiddleStack
        {
            get => this.inventory[0].Itemstack;
            set => this.inventory[0].Itemstack = value;
        }

        public ItemStack TopStack
        {
            get => this.inventory[1].Itemstack;
            set => this.inventory[1].Itemstack = value;
        }


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.capi = api as ICoreClientAPI;
            if (api.Side.IsServer())
            { this.RegisterGameTickListener(this.TemporalUpdate, this.tickSeconds * 1000); }
            if (api.Side.IsClient())
            {
                this.capi.Event.RegisterAsyncParticleSpawner(this.UpdateParticles);
            }
        }


        // Update Particles - called by the timer registered in InitializeTimer
        private bool UpdateParticles(float dt, IAsyncParticleManager manager)
        {
            // Note: returning false will kill the timer
            if (this.killOffThread)
            { return false; }
            this.interval -= dt;
            if (this.interval > 0)
            { return true; }
            else
            { this.interval = 0.004; }

            //made it this far...generate a particle
            this.GenerateParticles(manager);
            return true;
        }

        // Generate Particles - called from the timer (UpdateParticles)
        //	manager: off thread particle manager interface
        private void GenerateParticles(IAsyncParticleManager manager)
        {
            var topType = this.GetTopType();
            var areaOK = true;
            var gearCount = 0;
            if (topType == "dagon" || topType == "hydra")
            {
                gearCount = this.GearCount("astral");
                areaOK = this.SurroundingAreaOK(this.Pos, "water");
            }
            else if (topType == "cthulu")
            {
                gearCount = this.GearCount("temporal");
                areaOK = this.SurroundingAreaOK(this.Pos, "ground");
            }
            else if (topType == "nephrenka")
            { gearCount = this.GearCount("ethereal"); }

            if (gearCount == 0 || areaOK == false)
            { return; }

            if (topType == "nephrenka")
            {
                // gearCount = 1
                var aColor = Rnd.Next(25, 100);
                var minSize = 0.2f;
                var maxSize = 0.3f;
                var vFlags = 125;
                var model = EnumParticleModel.Quad;
                var lifelength = 0.75f;

                if (gearCount == 2)
                {
                    aColor = Rnd.Next(50, 100);
                    minSize = 0.5f;
                    maxSize = 1.0f;
                    vFlags = 150;
                    model = EnumParticleModel.Quad;
                    lifelength = 075f;
                }
                else if (gearCount == 3)
                {
                    aColor = Rnd.Next(75, 100);
                    minSize = 2.75f;
                    maxSize = 4.0f;
                    vFlags = 175;
                    model = EnumParticleModel.Cube;
                    lifelength = 075f;
                }
                else if (gearCount == 4)
                {
                    aColor = Rnd.Next(75, 100);
                    minSize = 2.75f;
                    maxSize = 10.0f;
                    vFlags = 175;
                    model = EnumParticleModel.Cube;
                    lifelength = 075f;
                }

                var color = ColorUtil.ToRgba(aColor, Rnd.Next(200, 255), Rnd.Next(200, 255), Rnd.Next(200, 255));
                var particles = new SimpleParticleProperties(
                    40, 0, // quantity
                    color,
                    new Vec3d(0.5, 50, 0.5), //min position
                    new Vec3d(), //add position - see below
                    new Vec3f(0.05f, -1f, 0.05f), //min velocity
                    new Vec3f(), //add velocity - see below
                    0f, //life length below
                    (float)((Rnd.NextDouble() * 1f) + 0f), //gravity effect 
                    minSize, maxSize, //size
                    model); // model

                particles.MinPos.Add(this.Pos); //add block position
                particles.AddPos.Set(new Vec3d(0, 0, 0)); //add position
                particles.AddVelocity.Set(new Vec3f(-0.1f, -4f, -0.1f)); //add velocity
                particles.VertexFlags = vFlags;
                particles.LifeLength = lifelength;
                particles.ShouldDieInLiquid = true;
                particles.WithTerrainCollision = false;
                manager.Spawn(particles);
            }
            else
            {
                //cthulu defaults
                var color = ColorUtil.ToRgba(180, 50, 120, 50);
                var gravityEffect = 0.1f;

                if (topType == "dagon" || topType == "hydra")
                {
                    color = ColorUtil.ToRgba(255, 30, 30, 30);
                    gravityEffect = 0.6f;
                }

                float minQuantity = 1;
                float maxQuantity = gearCount * gearCount * 5;
                var minPos = new Vec3d();
                var addPos = new Vec3d();
                var minVelocity = new Vec3f(0.1f, 0.0f, 0.1f);
                var maxVelocity = new Vec3f(0.5f, 0.5f, 0.5f);
                float lifeLength = 2 * gearCount;
                var minSize = 0.5f;
                var maxSize = 1.8f;

                var particles = new SimpleParticleProperties(
                    minQuantity, maxQuantity,
                    color,
                    minPos, addPos,
                    minVelocity, maxVelocity,
                    lifeLength,
                    gravityEffect,
                    minSize, maxSize,
                    EnumParticleModel.Cube
                );
                particles.MinPos.Set(this.Pos.ToVec3d().AddCopy(-(gearCount - 0.9), 0f, -(gearCount - 0.9)));
                particles.AddPos.Set(new Vec3d(gearCount + gearCount - 1, 0f, gearCount + gearCount - 1));
                particles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -2);
                particles.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255);
                particles.ShouldDieInAir = false;
                particles.SelfPropelled = false;
                manager.Spawn(particles);
            }
        }


        public int GearCount(string type)
        {
            var count = 0;
            for (var i = 2; i <= 5; i++)
            {
                if (!this.inventory[i].Empty) //a gear
                {
                    if (this.inventory[i].Itemstack.Item.FirstCodePart(1).Contains(type))
                    { count++; }
                }
            }
            return count;
        }

        public string GetTopType()
        {
            var thisTop = "none";
            if (!this.inventory[1].Empty)
            {
                if (this.TopStack.Block.FirstCodePart(1) == "statue")
                { thisTop = this.TopStack.Block.FirstCodePart(); }
            }
            return thisTop;
        }

        public bool SurroundingAreaOK(BlockPos pos, string type)
        {
            var areaOK = true;
            Block testBlock;
            var downpos = pos.DownCopy();
            var neibPos = new BlockPos[] { downpos.NorthCopy(), downpos.SouthCopy(), downpos.EastCopy(), downpos.WestCopy(), downpos.NorthCopy().EastCopy(), downpos.SouthCopy().WestCopy(), downpos.SouthCopy().EastCopy(), downpos.NorthCopy().WestCopy() };
            foreach (var neib in neibPos)
            {
                testBlock = this.Api.World.BlockAccessor.GetBlock(neib, BlockLayersAccess.Default);
                //Debug.WriteLine(testBlock.Code.Path);
                if (type == "water")
                {
                    if (testBlock.LiquidCode != "water")
                    { areaOK = false; }
                }
                else //fertile ground
                {
                    if (testBlock.Fertility <= 0)
                    { areaOK = false; }
                }
            }
            return areaOK;
        }


        public void TemporalUpdate(float par)
        {
            int gearcount; // = 0;
            string[] temporalTypes = { "game:vegetable-cabbage", "game:vegetable-carrot", "game:vegetable-onion", "game:vegetable-parsnip", "game:vegetable-turnip", "game:vegetable-pumpkin" };
            string[] astralTypes = { "primitivesurvival:psfish-trout-raw", "primitivesurvival:psfish-perch-raw", "primitivesurvival:psfish-salmon-raw", "primitivesurvival:psfish-carp-raw", "primitivesurvival:psfish-bass-raw", "primitivesurvival:psfish-pike-raw", "primitivesurvival:psfish-arcticchar-raw", "primitivesurvival:psfish-catfish-raw", "primitivesurvival:psfish-bluegill-raw", "primitivesurvival:psfish-mutant-raw", "primitivesurvival:psfish-mutant-raw" };

            if (this.dropsGold)
            {
                for (var i = 1; i < 4; i++)
                {
                    Array.Resize(ref temporalTypes, temporalTypes.Length + 1);
                    temporalTypes[temporalTypes.Length - 1] = "game:nugget-nativegold";
                    Array.Resize(ref astralTypes, astralTypes.Length + 1);
                    astralTypes[astralTypes.Length - 1] = "game:nugget-nativegold";
                }
            }

            var topType = this.GetTopType();
            if (topType == "dagon" || topType == "hydra")
            {
                var areaOK = this.SurroundingAreaOK(this.Pos, "water");
                if (areaOK)
                {
                    gearcount = this.GearCount("astral");
                    var entity = this.Api.World.GetNearestEntity(this.Pos.ToVec3d(), 1 + gearcount, 1 + gearcount, null);

                    if (entity != null)
                    {
                        if (this.nonEntities.Contains(entity.FirstCodePart()))
                        { return; }

                        var dmg = Rnd.Next(3) + 1;
                        var damaged = entity.ReceiveDamage(new DamageSource()
                        {
                            Source = EnumDamageSource.Void,
                            Type = EnumDamageType.PiercingAttack
                        }, dmg);
                        if (damaged) //drop astralType
                        {
                            var dropType = astralTypes[Rnd.Next(astralTypes.Count())];
                            var dropCount = 1;
                            if (this.dropsFish == false)
                            { dropCount = 0; }
                            if (dropType == "game:nugget-nativegold")
                            { dropCount = Rnd.Next(5) + 1; }
                            if (dropCount >= 1)
                            {
                                var dropItem = this.Api.World.GetItem(new AssetLocation(dropType));
                                if (dropItem != null)
                                {
                                    var newStack = new ItemStack(dropItem, dropCount);
                                    this.Api.World.SpawnItemEntity(newStack, this.Pos.ToVec3d().Add(0.5, 10, 0.5));
                                }
                            }
                        }
                    }
                }
            }
            else if (topType == "cthulu")
            {
                var areaOK = this.SurroundingAreaOK(this.Pos, "ground");
                if (areaOK)
                {
                    gearcount = this.GearCount("temporal");
                    var entity = this.Api.World.GetNearestEntity(this.Pos.ToVec3d(), 1 + gearcount, 1 + gearcount, null);
                    if (entity != null)
                    {
                        if (this.nonEntities.Contains(entity.FirstCodePart()))
                        { return; }

                        var dmg = Rnd.Next(3) + 1;
                        var damaged = entity.ReceiveDamage(new DamageSource()
                        {
                            Source = EnumDamageSource.Void,
                            Type = EnumDamageType.PiercingAttack
                        }, dmg);
                        if (damaged) //drop temporalType
                        {
                            var dropType = temporalTypes[Rnd.Next(temporalTypes.Count())];
                            var dropCount = 1;
                            if (this.dropsVegetables == false)
                            { dropCount = 0; }
                            if (dropType == "game:nugget-nativegold")
                            { dropCount = Rnd.Next(5) + 1; }
                            if (dropCount >= 1)
                            {
                                var dropItem = this.Api.World.GetItem(new AssetLocation(dropType));
                                if (dropItem != null)
                                {
                                    var newStack = new ItemStack(dropItem, dropCount);
                                    this.Api.World.SpawnItemEntity(newStack, this.Pos.ToVec3d().Add(0.5, 10, 0.5));
                                }
                            }
                        }
                    }
                }
            }
            else if (topType == "nephrenka")
            {
                gearcount = this.GearCount("ethereal");
                if (gearcount == 4)
                {
                    var rndSpawn = Rnd.Next(10); //random delay before spawning monsters
                    if (rndSpawn == 0)
                    {
                        var toRemove = Rnd.Next(4) + 1;
                        for (var index = this.maxSlots - 1; index >= (this.maxSlots - toRemove); index--)
                        {
                            if (!this.inventory[index].Empty)
                            {
                                if (index <= 1)
                                {
                                    var tmpPath = this.inventory[index].Itemstack.Collectible.Code.Path;
                                    var lastPart = this.inventory[index].Itemstack.Collectible.LastCodePart();
                                    tmpPath = tmpPath.Replace(lastPart, "north"); //shouldn't need this
                                    var tmpBlock = this.Api.World.GetBlock(this.Block.CodeWithPath(tmpPath));
                                    this.inventory[index].Itemstack = new ItemStack(tmpBlock);
                                }
                                var stack = this.inventory[index].TakeOut(1);
                                //gear lost forever
                                //this.Api.World.SpawnItemEntity(stack, this.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                            }
                            this.MarkDirty(true);

                            var type = this.Api.World.GetEntityType(new AssetLocation("primitivesurvival:livingdead-normal"));
                            if (type == null)
                            {
                                this.Api.World.Logger.Error("BETemporalBase: No such entity - primitivesurvival:livingdead-normal");
                                continue;
                            }
                            var entity = this.Api.World.ClassRegistry.CreateEntity(type);
                            if (entity != null)
                            {
                                entity.ServerPos.X = this.Pos.X + 0.5f;
                                entity.ServerPos.Y = this.Pos.Y + 20f + (index * 4);
                                entity.ServerPos.Z = this.Pos.Z + 0.5f;
                                entity.ServerPos.Yaw = (float)Rnd.NextDouble() * 2 * GameMath.PI;
                                this.Api.World.SpawnEntity(entity);
                            }
                        }
                    }
                }
            }
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


        //internal void OnBreak(IPlayer byPlayer, BlockPos pos)
        internal void OnBreak()
        {
            string tmpPath;
            string lastPart;
            for (var index = this.maxSlots - 1; index >= 0; index--)
            {
                if (!this.inventory[index].Empty)
                {
                    if (index <= 1)
                    {
                        tmpPath = this.inventory[index].Itemstack.Collectible.Code.Path;
                        lastPart = this.inventory[index].Itemstack.Collectible.LastCodePart();
                        tmpPath = tmpPath.Replace(lastPart, "north");
                        var tmpBlock = this.Api.World.GetBlock(this.Block.CodeWithPath(tmpPath));
                        this.inventory[index].Itemstack = new ItemStack(tmpBlock);
                    }
                    var stack = this.inventory[index].TakeOut(1);
                    this.Api.World.SpawnItemEntity(stack, this.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
                this.MarkDirty(true);
            }
        }


        private bool TryPut(IPlayer byPlayer)
        {
            var index = -1;
            var bookorient = "north";
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
                if (playerStack.Block.Attributes != null)
                {
                    if (playerStack.Block.Attributes.Exists)
                    {
                        if (playerStack.Block.Attributes["placement"].Exists)
                        {
                            var placement = playerStack.Block.Attributes["placement"].ToString();
                            var placetype = playerStack.Block.Attributes["placetype"].ToString();
                            if (placement == "middle" && this.MiddleSlot.Empty)
                            { index = 0; }
                            else if (placement == "top" && this.TopSlot.Empty && !this.MiddleSlot.Empty)
                            {
                                var middletype = this.MiddleStack.Block.Attributes["placetype"].ToString();
                                if (placetype == "statue" && middletype == "cube")
                                { index = 1; }
                                else if (placetype == "book" && middletype == "lectern")
                                {
                                    index = 1;
                                    bookorient = this.MiddleStack.Block.LastCodePart();
                                }
                            }
                        }
                    }
                }
            }
            else if (playerStack.Item != null)
            {
                var path = playerStack.Item.Code.Path;
                if (path.Contains("gear-") && !this.MiddleSlot.Empty)
                {
                    var facing = byPlayer.CurrentBlockSelection.Face.Opposite;
                    var playerFacing = facing.ToString();
                    var middletype = this.MiddleStack.Block.Attributes["placetype"].ToString();
                    if (middletype == "lectern")
                    {
                        var tmpPath = this.MiddleStack.Collectible.Code.Path;
                        if (!tmpPath.Contains(playerFacing))
                        { return false; }  //not facing the one available slot
                    }

                    if (playerFacing == "north")
                    { index = 2; }
                    else if (playerFacing == "east")
                    { index = 3; }
                    else if (playerFacing == "south")
                    { index = 4; }
                    else if (playerFacing == "west")
                    { index = 5; }
                }
            }
            if (index >= 0)
            {
                if (this.inventory[index].Empty)
                {
                    var moved = playerSlot.TryPutInto(this.Api.World, this.inventory[index]);
                    if (moved > 0)
                    {
                        if (index == 0 || index == 1) //middle or top
                        {
                            //try orienting it directly in the inventory
                            var facing = byPlayer.CurrentBlockSelection.Face.Opposite;
                            var playerFacing = facing.ToString();
                            if (playerFacing != "north" && playerFacing != "south" && playerFacing != "east" && playerFacing != "west")
                            { playerFacing = "north"; }
                            var tmpPath = this.inventory[index].Itemstack.Collectible.Code.Path;
                            if (tmpPath.Contains("necronomicon"))
                            { tmpPath = tmpPath.Replace("north", bookorient); }
                            else
                            { tmpPath = tmpPath.Replace("north", playerFacing); }
                            var tmpBlock = this.Api.World.GetBlock(this.Block.CodeWithPath(tmpPath));
                            this.inventory[index].Itemstack = new ItemStack(tmpBlock);
                        }
                        this.MarkDirty(true);
                        return moved > 0;
                    }
                }
            }
            return false;
        }


        private bool TryTake(IPlayer byPlayer) //, BlockSelection blockSel)
        {
            if (byPlayer == null)
            { return false; } // should be impossible but whatevs https://github.com/SpearAndFang/primitive-survival/issues/21
            var facing = byPlayer.CurrentBlockSelection.Face.Opposite;
            var index = -1;
            var playerFacing = facing.ToString();

            if (playerFacing == "north")
            { index = 2; }
            else if (playerFacing == "east")
            { index = 3; }
            else if (playerFacing == "south")
            { index = 4; }
            else if (playerFacing == "west")
            { index = 5; }

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

            index = -1;
            var hasGear = false;
            for (var i = 2; i < this.maxSlots; i++)
            {
                if (!this.inventory[i].Empty)
                { hasGear = true; }
            }

            if (!this.TopSlot.Empty)
            { index = 1; }
            else if (!this.MiddleSlot.Empty && !hasGear)
            { index = 0; }
            if (index >= 0)
            {
                var tmpPath = this.inventory[index].Itemstack.Collectible.Code.Path;
                var lastPart = this.inventory[index].Itemstack.Collectible.LastCodePart();
                tmpPath = tmpPath.Replace(lastPart, "north");
                var tmpBlock = this.Api.World.GetBlock(this.Block.CodeWithPath(tmpPath));
                this.inventory[index].Itemstack = new ItemStack(tmpBlock);

                byPlayer.InventoryManager.TryGiveItemstack(this.inventory[index].Itemstack);
                this.inventory[index].TakeOut(1);
                this.MarkDirty(true);
                return true;
            }

            return false;
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            var gearCount = 0;
            var gearType = "";
            for (var i = 2; i < this.maxSlots; i++)
            {
                if (!this.inventory[i].Empty)
                {
                    gearType = this.inventory[i].Itemstack.Collectible.Code.Path;
                    gearCount++;
                }
            }

            if (!this.TopSlot.Empty && !this.MiddleSlot.Empty)
            {
                var middletype = this.MiddleStack.Block.Attributes["placetype"].ToString();
                if (middletype == "lectern" && gearCount == 1)
                {
                    if (gearType == "psgear-astral")
                    { sb.Append(Lang.Get("primitivesurvival:blockdesc-temporalbase-book-dagon-hint")); }
                    else if (gearType == "gear-temporal")
                    { sb.Append(Lang.Get("primitivesurvival:blockdesc-temporalbase-book-cthulhu-hint")); }
                    else if (gearType == "psgear-ethereal")
                    { sb.Append(Lang.Get("primitivesurvival:blockdesc-temporalbase-book-nephrenka-hint")); }
                    else if (gearType == "gear-rusty")
                    { sb.Append(Lang.Get("primitivesurvival:blockdesc-temporalbase-complete")); }
                }
                else //cube
                {
                    var gearcount = 0;
                    var toptype = this.TopStack.Block.FirstCodePart(1);
                    if (toptype == "statue")
                    { toptype = this.TopStack.Block.FirstCodePart(); }

                    var areaOK = false;
                    if (toptype == "dagon" || toptype == "hydra")
                    {
                        gearcount = this.GearCount("astral");
                        areaOK = this.SurroundingAreaOK(this.Pos, "water");
                    }
                    else if (toptype == "cthulu")
                    {
                        gearcount = this.GearCount("temporal");
                        areaOK = this.SurroundingAreaOK(this.Pos, "ground");
                    }
                    else if (toptype == "nephrenka")
                    {
                        gearcount = this.GearCount("ethereal");
                        areaOK = true;
                    }

                    if (gearcount > 0 && areaOK)
                    {
                        var holesize = "primitivesurvival:blockdesc-temporalbase-gateway-size-" + Math.Min(4, gearcount);

                        var endString = "primitivesurvival:blockdesc-temporalbase-gateway-open-success";
                        if (gearCount < 4 && toptype == "nephrenka")
                        {
                            endString = "primitivesurvival:blockdesc-temporalbase-gateway-open-failure";
                        }
                        sb.Append(Lang.Get("primitivesurvival:blockdesc-temporalbase-gateway-open") + " " + Lang.Get(holesize) + " " + Lang.Get(endString));
                    }
                    else if (gearcount > 0)
                    {
                        sb.Append(Lang.Get("primitivesurvival:blockdesc-temporalbase-area-needed"));
                    }
                    else
                    { sb.Append(Lang.Get("primitivesurvival:blockdesc-temporalbase-complete")); }
                }
            }
            else
            { sb.Append(Lang.Get("primitivesurvival:blockdesc-temporalbase-incomplete")); }
            sb.AppendLine().AppendLine();
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            MeshData mesh;
            var shapeBase = "primitivesurvival:shapes/";
            string shapePath; // = "";
            var index = -1;
            var type = "";

            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockTemporalBase;
            Block tmpBlock;
            var texture = tesselator.GetTextureSource(block);
            var dir = block.LastCodePart();

            var newPath = "temporalbase";
            shapePath = "block/relic/" + newPath;
            mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase + shapePath, texture, index, type, dir); //, tesselator);
            mesher.AddMeshData(mesh);

            if (this.inventory != null)
            {
                if (!this.MiddleSlot.Empty)
                {
                    newPath = this.MiddleStack.Block.FirstCodePart();
                    dir = this.MiddleStack.Block.LastCodePart();
                    shapePath = "block/relic/" + newPath;
                    type = "";
                    if (newPath.Contains("lectern"))
                    { type = "lectern"; }
                    else if (newPath.Contains("cube"))
                    { type = "cube"; }
                    mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase + shapePath, texture, index, type, dir); //, tesselator);
                    mesher.AddMeshData(mesh);
                }

                var enabled = false;
                dir = "";
                ITexPositionSource tmptexture; // = texture;
                for (var i = 2; i < this.maxSlots; i++)
                {
                    if (!this.inventory[i].Empty) //gear - temporal or rusty
                    {
                        var gearType = this.inventory[i].Itemstack.Item.FirstCodePart(1);
                        tmpBlock = this.Api.World.GetBlock(block.CodeWithPath("texture" + gearType));
                        if (gearType != "rusty")
                        {
                            gearType = "temporal";
                            enabled = true;
                        }
                        shapePath = "game:shapes/item/gear-" + gearType;
                        tmptexture = ((ICoreClientAPI)this.Api).Tesselator.GetTextureSource(tmpBlock);
                        mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePath, tmptexture, i, type, dir); //, tesselator);
                        mesher.AddMeshData(mesh);
                    }
                }

                if (!this.TopSlot.Empty)
                {
                    shapePath = "block/relic/";
                    newPath = this.TopStack.Block.FirstCodePart();
                    dir = this.TopStack.Block.LastCodePart();
                    if (this.TopStack.Block.Code.Path.Contains("statue"))
                    { shapePath += "statue/"; }
                    shapePath += newPath;
                    if (newPath.Contains("necronomicon"))
                    {
                        if (enabled)
                        { shapePath += "-open"; }
                        else
                        { shapePath += "-closed"; }
                        tmpBlock = this.Api.World.GetBlock(block.CodeWithPath("necronomicon-north"));
                        texture = ((ICoreClientAPI)this.Api).Tesselator.GetTextureSource(tmpBlock);
                    }
                    mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase + shapePath, texture, index, type, dir); //, tesselator);
                    mesher.AddMeshData(mesh);
                }
            }
            return true;
        }
    }
}
