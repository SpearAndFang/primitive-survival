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
    //using Vintagestory.API.Server;
    using PrimitiveSurvival.ModConfig;
    using System.Diagnostics;
    using Vintagestory.API.Util;

    //using System.Diagnostics;

    // public class BESupport : BlockEntityDisplayCase //1.18
    public class BESupport : BlockEntityDisplayCase, ITexPositionSource
    {
        private readonly int PipeUpdateFrequency = ModConfig.Loaded.PipeUpdateFrequency;
        private readonly double PipeBlockageChancePercent = ModConfig.Loaded.PipeBlockageChancePercent;
        private readonly double PipeMinMoisture = ModConfig.Loaded.PipeMinMoisture;

        private readonly int tickSeconds = 4;
        private readonly int maxSlots = 3; //pipe, liquid, blockage
        private readonly string[] pipeTypes = { "pipe" };
        private readonly string[] blockageTypes = { "rot", "drygrass" };
        protected static readonly Random Rnd = new Random();

        //attributes
        //private readonly string renderSides = "nsew";
        //the shape file(s)
        private readonly string endShape = "block/pipe/wood/end";
        private readonly string sideShape = "block/pipe/wood/side";

        //tree attributes
        private string currentConnections = ""; //i.e. "" for none, or  "nsew" for all
        private string currentEndpoints = "";

        //tickers
        private long particleTick;
        private long pipeTick;

        //private AssetLocation wetPickupSound;
        private AssetLocation dryPickupSound;

        public BESupport()
        {
            this.inventory = new InventoryGeneric(this.maxSlots, null, null);
            //this.meshes = new MeshData[this.maxSlots]; //1.18
            var meshes = new MeshData[this.maxSlots];
        }

        //inventory setup
        public override string InventoryClassName => "support";
        protected InventoryGeneric inventory; //1.18

        public override InventoryBase Inventory => this.inventory;
        public ItemSlot PipeSlot => this.inventory[0];
        public ItemSlot LiquidSlot => this.inventory[1];
        public ItemSlot OtherSlot => this.inventory[2];

        public ItemStack PipeStack
        {
            get => this.inventory[0].Itemstack;
            set => this.inventory[0].Itemstack = value;
        }

        public ItemStack LiquidStack
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
                this.particleTick = this.RegisterGameTickListener(this.OnParticleTick, this.tickSeconds * 400); //1000
                this.pipeTick = this.RegisterGameTickListener(this.OnPipeTick, this.PipeUpdateFrequency * 1000);
            }
            //this.wetPickupSound = new AssetLocation("game", "sounds/environment/smallsplash");
            this.dryPickupSound = new AssetLocation("game", "sounds/block/cloth");
        }


        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            this.UnregisterGameTickListener(this.particleTick);
            this.UnregisterGameTickListener(this.pipeTick);
        }


        private void GenerateWaterParticles(BlockPos pos, string dir)
        {
            float minQuantity = 1;
            float maxQuantity = 1;
            var color = ColorUtil.ToRgba(80, 125, 185, 225); //40
            var minPos = new Vec3d();
            var addPos = new Vec3d();
            var minVelocity = new Vec3f(0f, 0.0f, 0f);

            var lifeLength = 2f;
            var gravityEffect = 0.5f;
            var minSize = 0.3f;
            var maxSize = 0.3f;

            float dx;
            float dy;
            float dz;
            Vec3f maxVelocity;

            if (dir == "ew")
            {
                dx = 0.95f;
                dy = 0.9f;
                dz = 0.725f;
                var vel = 1f;
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
                dx = 0.8f;
                dy = 0.08f;
                dz = 0.05f;
                var vel = 1f;
                var rando = Rnd.Next(2);
                if (rando == 0)
                { vel *= -1f; dx = 0.735f; }
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

            waterParticles.MinPos.Set(pos.ToVec3d().AddCopy(dx, dy, dz));

            waterParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 0.2f);
            waterParticles.ShouldDieInAir = false;
            waterParticles.Bounciness = 0.25f;
            waterParticles.SelfPropelled = true;
            this.Api.World.SpawnParticles(waterParticles);
        }

        private bool isSupported(BlockPos pos, string dir)
        {
            var maxLength = 3;
            var supported = true;
            var count = 0;
            do
            {
                if (dir == "n")
                { pos = pos.NorthCopy(); }
                else if (dir == "s")
                { pos = pos.SouthCopy(); }
                else if (dir == "e")
                { pos = pos.EastCopy(); }
                else
                { pos = pos.WestCopy(); }

                if (this.Api.World.BlockAccessor.GetBlockEntity(pos) is BESupport be)
                {
                    if (be.PipeStack == null)
                    { supported = false; }
                    else
                    {
                        if (!be.Block.Code.Path.Contains("none"))
                        { count = 99; }
                    }
                }
                else
                { supported = false; }
                count++;
            }
            while (supported && (count < maxLength));
            if (count == maxLength && supported)
            { supported = false; }
            return supported;
        }


        public void BreakIfUnsupported(BlockPos pos)
        {
            //check for enough support
            //as in an actual support 4 blocks in either direction
            //with a pipe connection along the way
            //the block we are checking
            var block = this.Api.World.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default) as BlockSupport;
            var dir = block.LastCodePart();
            var type = block.FirstCodePart(1);
            var rSupported = true;
            var lSupported = true;
            if (type == "none")
            {
                if (dir == "ew")
                {
                    rSupported = this.isSupported(pos, "n");
                    lSupported = this.isSupported(pos, "s");
                }
                else  //ns
                {
                    rSupported = this.isSupported(pos, "e");
                    lSupported = this.isSupported(pos, "w");
                }
            }
            if (!rSupported && !lSupported)
            {
                if (this.Api.World.BlockAccessor.GetBlockEntity(pos) is BESupport be)
                {
                    be.OnBreak();
                    //this is the actual block?, not the placeholder
                    this.Api.World.BlockAccessor.SetBlock(0, pos);
                    this.Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(pos);

                    if (dir == "ns")
                    {
                        this.Api.World.BlockAccessor.SetBlock(0, pos.NorthCopy());
                    }
                    else
                    {
                        this.Api.World.BlockAccessor.SetBlock(0, pos.EastCopy());
                    }
                }
            }
        }


        private void TryWaterFarmland(BlockPos pos, string dir)
        {
            /*
              For each of the two positions, search down until we find a block
              if the block if farmland, water it
            */
            BlockPos neibPos;
            int maxDistance;
            //find neighbor
            if (dir == "ns")
            {
                neibPos = pos.NorthCopy();
                maxDistance = 6; //only check for 6 blocks
            }
            else
            {
                neibPos = pos.EastCopy();
                maxDistance = 5; //ew blocks are one lower
            }
            BlockPos[] positions = { pos, neibPos };
            foreach (var position in positions)
            {
                var found = false;
                var distance = 0;
                var downPos = position.DownCopy();
                do
                {
                    var block = this.Api.World.BlockAccessor.GetBlock(downPos, BlockLayersAccess.Default);
                    if (block.Id != 0 && block.FirstCodePart() != "support" )
                    {
                        found = true;
                        if (block.Class == "BlockCrop" || block.Class == "BlockTallGrass" || block.Class == "BlockPlant")
                        {
                            //might have hit the crop or plant of some kind, go down once more
                            downPos = downPos.DownCopy();
                            block = this.Api.World.BlockAccessor.GetBlock(downPos, BlockLayersAccess.Default);
                        }

                        
                        if (this.Api.World.BlockAccessor.GetBlockEntity(downPos) is BlockEntityFarmland be)
                        {
                            //Debug.WriteLine("watering:" + block.Code.Path);
                            if (be.MoistureLevel <= 0.99)
                            {
                                // add 0.1 to it
                                be.WaterFarmland(0.1f, false); //no neighbors until nearby watered
                            }
                        }
                        else
                        {
                            Debug.WriteLine("IGNORING:" + block.Class);
                        }
                    }
                    distance++;
                    downPos = downPos.DownCopy();
                }
                while (!found && distance < maxDistance);

            }
        }


        private void AdjustWaterFromSourceOrNeib()
        {
            //look for barrel or irrigation vessel or water
            //do I even need supportDir here? ns or ew
            string[] sourceTypes = { "water-", "barrel", "irrigationvessel-normal" };
            var dir = this.Block.LastCodePart();
            //is an endpoint?
            if (this.currentEndpoints != null)
            {
                foreach (var side in dir)
                {
                    if (this.currentEndpoints.Contains(side))
                    {
                        //Debug.WriteLine("endpoint");
                        var newStack = new ItemStack(this.Api.World.GetBlock(new AssetLocation("game:water-still-7")), 1);
                        this.LiquidStack = newStack;
                        this.MarkDirty();
                    }
                }
            }
            /*

            var isWaterSource = false;
            BlockPos[] neibPositions = null;
            switch (side)
            {
                case 'e':
                {
                    if (supportDir == "ns")
                    {
                        neibPositions = new BlockPos[]
                        { this.Pos.WestCopy(), this.Pos.WestCopy().NorthCopy()};
                    }
                    break;
                }
                case 'w':
                {
                    if (supportDir == "ns")
                    {
                        neibPositions = new BlockPos[]
                        { this.Pos.EastCopy(), this.Pos.EastCopy().NorthCopy()};
                    }
                    break;
                }
                case 'n':
                {
                    if (supportDir == "ew")
                    {
                        neibPositions = new BlockPos[]
                        { this.Pos.SouthCopy().UpCopy(), this.Pos.SouthCopy().EastCopy().UpCopy()};
                    }
                    break;
                }
                case 's':
                {
                    if (supportDir == "ew")
                    {
                        neibPositions = new BlockPos[]
                        { this.Pos.NorthCopy().UpCopy(), this.Pos.NorthCopy().EastCopy().UpCopy()};
                    }
                    break;
                }
                default:
                { break; }
            }
            if (neibPositions != null)
            {
                foreach (var neibPos in neibPositions)
                {
                    var block = this.Api.World.BlockAccessor.GetBlock(neibPos, BlockLayersAccess.Default);
                    //Debug.WriteLine(side + ": " + block.Code.Path);
                    foreach (var sourceType in sourceTypes)
                    {
                        if (block.Code.Path.StartsWith(sourceType))
                        { isWaterSource = true; }
                    }
                }
            }
            */
        }


        public void OnParticleTick(float par)
        {
            if (this.PipeStack != null)
            {
                this.AdjustWaterFromSourceOrNeib();

                var dir = this.Block.LastCodePart();
                if (this.PipeStack.Collectible.FirstCodePart(2) == "aerated" && !this.LiquidSlot.Empty)
                {
                    var rando = Rnd.Next(3); //overall frequency of drips
                    if (rando == 0)
                    {
                        this.GenerateWaterParticles(this.Pos, dir);

                        //
                        //water farmland here
                        this.TryWaterFarmland(this.Pos, dir);
                        //
                    }
                }
                this.BreakIfUnsupported(this.Pos);
            }
        }


        public void OnPipeTick(float par)
        {
            //see bepipe's OnPipeTick for clues
            //PipeBlockageChancePercent
            //PipeMinMoisture

        }


        public bool WorldPut(int slot) //, BlockPos pos)
        {
            return false;
        }


        public bool WorldTake(int slot, BlockPos pos)
        {
            return false;
        }


        public bool OnInteract(IPlayer byPlayer, BlockPos pos)
        {
            var slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Empty)
            {
                if (this.OtherStack?.Collectible != null)
                {
                    var result = this.TryTake(byPlayer, this.OtherSlot);
                    if (result)
                    {
                        this.Api.World.PlaySoundAt(this.dryPickupSound, pos.X, pos.Y, pos.Z);

                        //put water back if there's some nearby
                        /*
                        var thisblock = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockFurrowedLand;
                        thisblock.FillPlacedBlock(this.Api.World, this.Pos);
                        */
                        return true;
                    }
                }
                return false;
            }
            else
            {
                if (this.TryPut(byPlayer, slot, pos))
                {
                    this.Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
                    this.Api.World.PlaySoundAt(this.dryPickupSound, pos.X, pos.Y, pos.Z);
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

        private bool TryPut(IPlayer byPlayer, ItemSlot playerSlot, BlockPos pos)
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

            if (playerStack.Collectible != null)
            {
                //Debug.WriteLine("Putting a " + playerStack.Item.FirstCodePart());
                if (Array.IndexOf(this.pipeTypes, playerStack.Collectible.FirstCodePart()) >= 0 && this.PipeSlot.Empty)
                { index = 0; }
                else
                {
                    if (this.LiquidSlot.Empty)
                    { index = 1; }
                    else if (this.OtherSlot.Empty)
                    { index = 2; }
                }
            }
            if (index == 0) //only accept something in the pipe slot for now
            {
                moved = playerSlot.TryPutInto(this.Api.World, this.inventory[index]);
                if (moved > 0)
                {
                    this.MarkDirty(true);
                    if (index == 0)
                    {
                        //here we need to look around and then set connections accordingly
                        this.currentConnections = "";
                        this.currentEndpoints = "";
                    }
                    return moved > 0;
                }
            }
            else
            {
                //hijack this for adding pipes
                var ba = this.Api.World.BlockAccessor;
                var block = ba.GetBlock(pos, BlockLayersAccess.Default);
                var finalFacing = block.LastCodePart();

                if (block.LastCodePart() != finalFacing)
                { return false; } //restrict direction for now

                BlockPos targetPos;
                BlockPos neibPos;
                var placeDir = "ew";
                var isFacing = byPlayer.CurrentBlockSelection?.Face.ToString();
                if (isFacing == null)
                { return false; }
                if (isFacing == "south")
                {
                    targetPos = pos.SouthCopy();
                    neibPos = targetPos.EastCopy();
                    //neibPos = pos.SouthCopy().EastCopy();
                }
                else if (isFacing == "north")
                {
                    targetPos = pos.NorthCopy();
                    neibPos = targetPos.EastCopy();
                    //neibPos = pos.NorthCopy().EastCopy();
                }
                else if (isFacing == "west")
                {
                    targetPos = pos.WestCopy();
                    neibPos = targetPos.NorthCopy();
                    //neibPos = pos.WestCopy().NorthCopy();
                    placeDir = "ns";
                }
                else if (isFacing == "east")
                {
                    targetPos = pos.EastCopy();
                    neibPos = targetPos.NorthCopy();
                    //neibPos = pos.EastCopy().NorthCopy();
                    placeDir = "ns";
                }
                else
                { return false; }

                if (finalFacing != placeDir)
                {
                    //cross pipe
                    return false;
                }

                var testBlock = ba.GetBlock(targetPos, BlockLayersAccess.Default);
                var testBlock2 = ba.GetBlock(neibPos, BlockLayersAccess.Default);
                if (testBlock.BlockId != 0 || testBlock2.BlockId != 0)
                {
                    //something in the way?
                    if (!testBlock.IsLiquid() || !testBlock2.IsLiquid())
                    { return false; }
                }

                var material = playerStack.Collectible.FirstCodePart(1);
                var type = playerStack.Collectible.FirstCodePart(2);
                var neibAsset = "primitivesurvival:support-none-empty-" + finalFacing; //none
                block = ba.GetBlock(new AssetLocation(neibAsset));
                if (block != null)
                {
                    ba.SetBlock(block.BlockId, neibPos);
                    //ba.MarkBlockDirty(neibPos);
                    neibAsset = neibAsset.Replace("empty", "main");
                    block = ba.GetBlock(new AssetLocation(neibAsset));
                    ba.SetBlock(block.BlockId, targetPos);

                    if (ba.GetBlockEntity(targetPos) is BESupport beBlock)
                    {
                        beBlock.Inventory[0].Itemstack = playerStack;
                        byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
                        beBlock.Inventory[0].MarkDirty();
                        ba.MarkBlockDirty(targetPos);
                        ba.TriggerNeighbourBlockUpdate(targetPos);
                        return true;
                    }
                    /*
                    moved = playerSlot.TryPutInto(this.Api.World, this.inventory[index]);
                    if (moved > 0)
                    {
                        this.MarkDirty(true);
                        if (index == 0)
                        {
                            //here we need to look around and then set connections accordingly
                            this.currentConnections = "";
                            this.currentEndpoints = "";
                        }
                        return moved > 0;
                    }*/
                }
            }
            return false;
        }


        internal string GetConnections()
        {
            return this.currentConnections;
        }

        internal string GetEndpoints()
        {
            return this.currentEndpoints;
        }


        internal bool AddConnection(BlockPos pos, BlockFacing facing)
        {
            var face = char.ToLower(facing.Opposite.ToString()[0]);
            if (!this.currentConnections.Contains(face))
            {
                this.currentConnections += face;
                return true;
            }
            return false;
        }

        internal bool RemoveConnection(BlockPos pos, BlockFacing facing)
        {
            var face = char.ToLower(facing.Opposite.ToString()[0]).ToString();
            if (this.currentConnections.Contains(face))
            {
                this.currentConnections = this.currentConnections.Replace(face, string.Empty);
                return true;
            }
            return false;
        }

        internal bool AddEndpoint(char face)
        {
            if (!this.currentEndpoints.Contains(face))
            {
                this.currentEndpoints += face;
                return true;
            }
            return false;
        }

        internal bool RemoveEndpoint(char face)
        {
            if (this.currentEndpoints.Contains(face))
            {
                this.currentEndpoints = this.currentEndpoints.Replace(face.ToString(), string.Empty);
                return true;
            }
            return false;
        }

        //for removing blockages
        private bool TryTake(IPlayer byPlayer, ItemSlot sourceSlot)
        {
            if (!sourceSlot.Empty)
            {
                var stackCode = sourceSlot.Itemstack.Collectible.Code.Path;
                var newAsset = new AssetLocation(stackCode);
                if (newAsset != null)
                {
                    var tempStack = new ItemStack(this.Api.World.GetItem(newAsset), sourceSlot.StackSize);
                    var takeOK = byPlayer.InventoryManager.TryGiveItemstack(tempStack);
                    if (!takeOK) //player has no free slots
                    {
                        this.Api.World.SpawnItemEntity(tempStack, byPlayer.Entity.Pos.XYZ.Add(0, 0.5, 0));
                    }
                    sourceSlot.Itemstack = null;
                    this.MarkDirty(true);
                    return true;
                }
            }
            return false;
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            sb.AppendLine("Connections: " + this.currentConnections);
            sb.AppendLine("Endpoints: " + this.currentEndpoints);
            if (!this.LiquidSlot.Empty)
            {
                sb.AppendLine(Lang.Get("functional"));
                sb.AppendLine();
            }

            //debug
            //if (!this.PipeSlot.Empty)
            //{ sb.AppendLine("pipe"); }

            if (!this.OtherSlot.Empty)
            {
                sb.AppendLine(Lang.Get("primitivesurvival:item-blockage"));
                sb.AppendLine();
            }
        }


        public virtual string BuildShapePath(string partialPath)
        {
            if (partialPath == "")
            { return ""; }
            var fullPath = "";
            if (!partialPath.Contains(":"))
            { fullPath = this.Block.Code.Domain + ":"; }
            if (!partialPath.Contains("shapes/"))
            { fullPath += "shapes/"; }
            fullPath += partialPath;
            return fullPath;
        }

        // Genmesh for pipe sides
        public virtual MeshData GenSideMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, char side, string supportDir)
        {
            //hijack this to put the new "end" shape in place of the side shape as required
            //check for a barrel or irrigation vessel or water block
            //alternatively, we could do it in addonnection or ontesselation

            Shape shape;
            var tesselator = capi.Tesselator;
            var shapeAsset = capi.Assets.TryGet(shapePath + ".json");
            if (shapeAsset != null)
            {
                shape = shapeAsset.ToObject<Shape>();
                tesselator.TesselateShape(shapePath, shape, out var mesh, texture, null, 0);
                if (mesh != null)
                {
                    mesh.Translate(0f, 0f, 0.5f);
                    switch (side)
                    {
                        case 'e':
                        {
                            mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 0 * GameMath.DEG2RAD, 0);
                            if (supportDir == "ns")
                            { mesh.Translate(0f, 0f, -1f); }
                            break;
                        }
                        case 'w':
                        {
                            mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 180 * GameMath.DEG2RAD, 0);
                            break;
                        }
                        case 'n':
                        {
                            mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 90 * GameMath.DEG2RAD, 0);
                            if (supportDir == "ew")
                            { mesh.Translate(0f, 0.85f, 0f); }
                            break;
                        }
                        case 's':
                        {
                            mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, -90 * GameMath.DEG2RAD, 0);
                            if (supportDir == "ew")
                            { mesh.Translate(1f, 0.85f, 0f); }
                            break;
                        }
                        case 'd': //blockage
                        {
                            mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 0, 0);
                            mesh.Translate(1f, 0f, 0f);
                            mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.5f, 0.5f, 0.5f);
                            break;
                        }
                        default:
                        { break; }
                    }
                    return mesh;
                }
            }
            return null;
        }


        //genmesh for other inventory slots
        public virtual MeshData GenInventoryMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, string supportDir)
        {
            Shape shape;
            var tesselator = capi.Tesselator;
            var shapeAsset = capi.Assets.TryGet(shapePath + ".json");
            if (shapeAsset != null)
            {
                shape = shapeAsset.ToObject<Shape>();
                tesselator.TesselateShape(shapePath, shape, out var mesh, texture, null, 0);
                if (mesh != null)
                {
                    if (supportDir == "ew")
                    {
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 90 * GameMath.DEG2RAD, 0);
                        mesh.Translate(0.5f, 0.85f, 0f);
                    }
                    else
                    {
                        mesh.Translate(0f, 0f, -0.5f);
                    }
                    return mesh;
                }
            }
            return null;
        }

        private void CheckForWaterSource(char side, string supportDir)
        {
            //look for barrel or irrigation vessel or water
            //do I even need supportDir here? ns or ew
            string[] sourceTypes = { "water-", "barrel", "irrigationvessel-normal" };
            BlockPos[] neibPositions = null;
            switch (side)
            {
                case 'e':
                {
                    if (supportDir == "ns")
                    {
                        neibPositions = new BlockPos[]
                        { this.Pos.WestCopy(), this.Pos.WestCopy().NorthCopy()};
                    }
                    break;
                }
                case 'w':
                {
                    if (supportDir == "ns")
                    {
                        neibPositions = new BlockPos[]
                        { this.Pos.EastCopy(), this.Pos.EastCopy().NorthCopy()};
                    }
                    break;
                }
                case 'n':
                {
                    if (supportDir == "ew")
                    {
                        neibPositions = new BlockPos[]
                        { this.Pos.SouthCopy().UpCopy(), this.Pos.SouthCopy().EastCopy().UpCopy()};
                    }
                    break;
                }
                case 's':
                {
                    if (supportDir == "ew")
                    {
                        neibPositions = new BlockPos[]
                        { this.Pos.NorthCopy().UpCopy(), this.Pos.NorthCopy().EastCopy().UpCopy()};
                    }
                    break;
                }
                default:
                { break; }
            }
            if (neibPositions != null)
            {

                foreach (var neibPos in neibPositions)
                {
                    var block = this.Api.World.BlockAccessor.GetBlock(neibPos, BlockLayersAccess.Default);
                    //Debug.WriteLine(side + ": " + block.Code.Path);
                    foreach (var sourceType in sourceTypes)
                    {
                        if (block.Code.Path.StartsWith(sourceType))
                        {
                            this.AddEndpoint(side);
                        }
                        else
                        {
                            this.RemoveEndpoint(side);
                        }
                    }
                }
            }
        return;
        }


        private MeshData RenderWater(bool pipeEnd, string supportDir)
        {
            var shapePath = "primitivesurvival:shapes/block/pipe/wood/basewater";
            if (pipeEnd)
            { shapePath = "primitivesurvival:shapes/block/pipe/wood/endwater"; }

            var tempBlock = this.Api.World.GetBlock(this.Block.CodeWithPath("texturewater"));
            var tmpTextureSource = ((ICoreClientAPI)this.Api).Tesselator.GetTextureSource(tempBlock);
            var mesh = this.GenInventoryMesh(this.capi, shapePath, tmpTextureSource, supportDir);
            if (pipeEnd && mesh != null)
            {
                if (this.currentConnections == "e")
                {
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 180 * GameMath.DEG2RAD, 0);
                    mesh.Translate(0f, 0f, -1f);
                }
                if (this.currentConnections == "n")
                {
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 180 * GameMath.DEG2RAD, 0);
                    mesh.Translate(1f, 0f, 0f);
                }
            }
            return mesh;
        }


        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            MeshData mesh;
            string shapePath;
            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockSupport;
            var supportDir = block?.LastCodePart();
            string possibleConnections;

            if (block != null)
            {
                if (block.FirstCodePart(1) != "none")
                {
                    var texture = tesselator.GetTextureSource(block);
                    shapePath = "primitivesurvival:shapes/" + block.Shape.Base.Path;
                    mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePath, texture, supportDir);
                    mesher.AddMeshData(mesh);
                }
            }

            if (this.inventory != null)
            {
                if (!this.PipeSlot.Empty)
                {
                    if (supportDir == "ns")
                    { possibleConnections = "ew"; }
                    else
                    { possibleConnections = "ns"; }
                    Block tempBlock;
                    if (this.PipeStack.Block != null)
                    {
                        tempBlock = this.Api.World.GetBlock(this.PipeStack.Block.Id);
                        var texture = ((ICoreClientAPI)this.Api).Tesselator.GetTextureSource(tempBlock);
                        shapePath = "primitivesurvival:shapes/" + tempBlock.Shape.Base.Path;
                        mesh = this.GenInventoryMesh(this.capi, shapePath, texture, supportDir);
                        mesher.AddMeshData(mesh);

                        bool pipeEnd = false;
                        //pipe ends
                        foreach (var side in possibleConnections)
                        {
                            mesh = null;
                            if (!this.currentConnections.Contains(side) && shapePath != "")
                            {
                                shapePath = this.BuildShapePath(this.sideShape); //this uses a single down facing side to render all sides

                                //this should be elsewhere
                                this.CheckForWaterSource(side, supportDir);

                                if (this.currentEndpoints.Contains(side))
                                {
                                    shapePath = this.BuildShapePath(this.endShape);
                                    pipeEnd = true;
                                }
                                mesh = this.GenSideMesh(this.capi, shapePath, texture, side, supportDir);
                            }
                            if (mesh != null)
                            {
                                mesher.AddMeshData(mesh);
                            }
                        }

                        //water TEST
                        if (!this.LiquidSlot.Empty)
                        {
                            mesh = this.RenderWater(pipeEnd, supportDir);
                            if (mesh != null)
                            { mesher.AddMeshData(mesh); }
                        }
                    }
                }

                //Blockage
                if (!this.OtherSlot.Empty)
                {
                    var tmpTextureSource = tesselator.GetTextureSource(this.Block);
                    shapePath = this.BuildShapePath("block/pipe/soil/blockage-" + this.OtherStack.Collectible.Code.Path);
                    mesh = this.GenInventoryMesh(this.capi, shapePath, tmpTextureSource, supportDir);
                    if (mesh != null)
                    {
                        mesh.Translate(new Vec3f(0f, 0.85f - (this.OtherSlot.StackSize * 0.05f), 0f));
                        mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.85f, 0.85f, 0.85f);
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, this.OtherSlot.StackSize * 45 * GameMath.DEG2RAD, 0);
                        mesher.AddMeshData(mesh);
                    }
                }
            }
            return true;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
        {
            base.FromTreeAttributes(tree, worldForResolve);
            this.currentConnections = tree.GetString("currentConnections");
            this.currentEndpoints = tree.GetString("currentEndpoints");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("currentConnections", this.currentConnections);
            tree.SetString("currentEndpoints", this.currentEndpoints);
        }
    }
}
