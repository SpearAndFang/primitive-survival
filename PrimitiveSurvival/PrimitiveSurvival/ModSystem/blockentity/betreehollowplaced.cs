namespace PrimitiveSurvival.ModSystem
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Config;
    using Vintagestory.API.Client;
    using Vintagestory.GameContent;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.Util;
    using Vintagestory.API.Server;
    //using System.Diagnostics;


    public class BETreeHollowPlaced : BlockEntityGenericTypedContainer
    {

        internal InventoryGeneric inventory;

        public string type = "normal-generic";
        public string defaultType;
        public int quantitySlots = 10;
        public string inventoryClassName = "chest";
        public string dialogTitleLangCode = "chestcontents";


        public bool retrieveOnly = false;
        private float meshangle;
        public virtual float MeshAngle
        {
            get => this.meshangle;
            set
            {
                this.meshangle = value;
                this.rendererRot.Y = value * GameMath.RAD2DEG;
            }
        }

        private MeshData ownMesh;

        public Cuboidf[] collisionSelectionBoxes;


        public virtual string DialogTitle => Lang.Get(this.dialogTitleLangCode);

        public override InventoryBase Inventory => this.inventory;

        public override string InventoryClassName => this.inventoryClassName;

        private BlockEntityAnimationUtil AnimUtil => this.GetBehavior<BEBehaviorAnimatable>()?.animUtil;

        private readonly Vec3f rendererRot = new Vec3f();

        public BETreeHollowPlaced() : base()
        {
        }


        public override void Initialize(ICoreAPI api)
        {
            this.defaultType = this.Block.Attributes?["defaultType"]?.AsString("normal-generic");
            if (this.defaultType == null)
            { this.defaultType = "normal-generic"; }
            // Newly placed 
            if (this.inventory == null)
            { this.InitInventory(this.Block); }
            base.Initialize(api);
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            if (byItemStack?.Attributes != null)
            {
                var nowType = byItemStack.Attributes.GetString("type", this.defaultType);
                //Debug.WriteLine(nowType);
                if (nowType != this.type)
                {
                    this.type = nowType;
                    this.InitInventory(this.Block);
                    this.Inventory.LateInitialize(this.InventoryClassName + "-" + this.Pos.X + "/" + this.Pos.Y + "/" + this.Pos.Z, this.Api);
                    this.Inventory.ResolveBlocksOrItems();
                    this.Inventory.OnAcquireTransitionSpeed = this.Inventory_OnAcquireTransitionSpeed;
                    this.MarkDirty();
                }
            }
            base.OnBlockPlaced();
        }


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            this.type = tree.GetString("type", this.defaultType);
            this.MeshAngle = tree.GetFloat("meshAngle", this.MeshAngle);
            if (this.inventory == null)
            {
                if (tree.HasAttribute("forBlockId"))
                {
                    this.InitInventory(worldForResolving.GetBlock((ushort)tree.GetInt("forBlockId")));
                }
                else if (tree.HasAttribute("forBlockCode"))
                {
                    this.InitInventory(worldForResolving.GetBlock(new AssetLocation(tree.GetString("forBlockCode"))));
                }
                else
                {
                    var inventroytree = tree.GetTreeAttribute("inventory");
                    var qslots = inventroytree.GetInt("qslots");
                    this.InitInventory(null);
                }
            }

            if (this.Api != null && this.Api.Side == EnumAppSide.Client)
            {
                this.ownMesh = null;
                this.MarkDirty(true);
            }
            base.FromTreeAttributes(tree, worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (this.Block != null)
            { tree.SetString("forBlockCode", this.Block.Code.ToShortString()); }
            if (this.type == null)
            { this.type = this.defaultType; } // No idea why. Somewhere something has no type. Probably some worldgen ruins
            tree.SetString("type", this.type);
            tree.SetFloat("meshAngle", this.MeshAngle);
        }

        protected virtual void InitInventory(Block block)
        {
            block = this.Block;
            if (block?.Attributes != null)
            {
                this.collisionSelectionBoxes = block.Attributes["collisionSelectionBoxes"]?[this.type]?.AsObject<Cuboidf[]>();
                this.inventoryClassName = block.Attributes["inventoryClassName"].AsString(this.inventoryClassName);
                this.dialogTitleLangCode = block.Attributes["dialogTitleLangCode"][this.type].AsString(this.dialogTitleLangCode);
                this.quantitySlots = block.Attributes["quantitySlots"][this.type].AsInt(this.quantitySlots);
                this.retrieveOnly = block.Attributes["retrieveOnly"][this.type].AsBool(false);

                if (block.Attributes["typedOpenSound"][this.type].Exists)
                {
                    this.OpenSound = AssetLocation.Create(block.Attributes["typedOpenSound"][this.type].AsString(this.OpenSound.ToShortString()), this.Block.Code.Domain);
                }
                if (block.Attributes["typedCloseSound"][this.type].Exists)
                {
                    this.CloseSound = AssetLocation.Create(block.Attributes["typedCloseSound"][this.type].AsString(this.CloseSound.ToShortString()), this.Block.Code.Domain);
                }
            }

            this.inventory = new InventoryGeneric(this.quantitySlots, null, null, null)
            {
                BaseWeight = 1f
            };
            this.inventory.OnGetSuitability = (sourceSlot, targetSlot, isMerge) => (isMerge ? (this.inventory.BaseWeight + 3) : (this.inventory.BaseWeight + 1)) + (sourceSlot.Inventory is InventoryBasePlayer ? 1 : 0);
            this.inventory.OnGetAutoPullFromSlot = this.GetAutoPullFromSlot;
            if (block?.Attributes != null)
            {
                if (block.Attributes["spoilSpeedMulByFoodCat"][this.type].Exists)
                {
                    this.inventory.PerishableFactorByFoodCategory = block.Attributes["spoilSpeedMulByFoodCat"][this.type].AsObject<Dictionary<EnumFoodCategory, float>>();
                }
                if (block.Attributes["transitionSpeedMulByType"][this.type].Exists)
                {
                    this.inventory.TransitionableSpeedMulByType = block.Attributes["transitionSpeedMulByType"][this.type].AsObject<Dictionary<EnumTransitionType, float>>();
                }
            }
            this.inventory.PutLocked = this.retrieveOnly;
            this.inventory.OnInventoryClosed += this.OnInvClosed;
            this.inventory.OnInventoryOpened += this.OnInvOpened;
        }

        private ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
        {
            if (atBlockFace == BlockFacing.DOWN)
            {
                return this.inventory.FirstOrDefault(slot => !slot.Empty);
            }
            return null;
        }

        protected virtual void OnInvOpened(IPlayer player)
        {
            this.inventory.PutLocked = this.retrieveOnly && player.WorldData.CurrentGameMode != EnumGameMode.Creative;
            if (this.Api.Side == EnumAppSide.Client)
            {
                this.AnimUtil?.StartAnimation(new AnimationMetaData()
                {
                    Animation = "lidopen",
                    Code = "lidopen",
                    AnimationSpeed = 1.8f,
                    EaseOutSpeed = 6,
                    EaseInSpeed = 15
                });
            }
        }

        protected virtual void OnInvClosed(IPlayer player)
        {
            this.AnimUtil?.StopAnimation("lidopen");
            this.inventory.PutLocked = this.retrieveOnly;
            // This is already handled elsewhere and also causes a stackoverflowexception, but seems needed somehow?
            var inv = this.invDialog;
            this.invDialog = null; // Weird handling because to prevent endless recursion
            if (inv?.IsOpened() == true)
            { inv?.TryClose(); }
            inv?.Dispose();
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            { this.inventory.PutLocked = false; }
            if (this.inventory.PutLocked && this.inventory.Empty)
            { return false; }

            if (this.Api.World is IServerWorldAccessor)
            {
                byte[] data;
                using (var ms = new MemoryStream())
                {
                    var writer = new BinaryWriter(ms);
                    writer.Write("BlockEntityInventory");
                    writer.Write(this.DialogTitle);
                    writer.Write((byte)4);
                    var tree = new TreeAttribute();
                    this.inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }
                ((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket(
                    (IServerPlayer)byPlayer,
                    this.Pos.X, this.Pos.Y, this.Pos.Z,
                    (int)EnumBlockContainerPacketId.OpenInventory,
                    data
                );
                byPlayer.InventoryManager.OpenInventory(this.inventory);
            }
            return true;
        }



        private MeshData GenMesh(ITesselatorAPI tesselator)
        {
            var block = this.Block as BlockTreeHollowPlaced;
            if (this.Block == null)
            {
                block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockTreeHollowPlaced;
                this.Block = block;
            }
            if (block == null)
            { return null; }
            var rndTexNum = this.Block.Attributes?["rndTexNum"][this.type]?.AsInt(0) ?? 0;

            var key = "typedContainerMeshes" + this.Block.Code.ToShortString();
            var meshes = ObjectCacheUtil.GetOrCreate(this.Api, key, () =>
            {
                return new Dictionary<string, MeshData>();
            });
            var shapename = this.Block.Attributes?["shape"][this.type].AsString();
            if (shapename == null)
            { return null; }
            Shape shape = null;
            if (this.AnimUtil != null)
            {
                var skeydict = "typedContainerShapes";
                var shapes = ObjectCacheUtil.GetOrCreate(this.Api, skeydict, () =>
                {
                    return new Dictionary<string, Shape>();
                });
                var skey = this.Block.FirstCodePart() + block.Subtype + "-" + "-" + shapename + "-" + rndTexNum;
                if (!shapes.TryGetValue(skey, out shape))
                {
                    shapes[skey] = shape = block.GetShape(this.Api as ICoreClientAPI, this.type, shapename, tesselator, rndTexNum);
                }
            }

            var meshKey = this.type + block.Subtype + "-" + rndTexNum;
            if (meshes.TryGetValue(meshKey, out var mesh))
            {
                if (this.AnimUtil != null && this.AnimUtil.renderer == null)
                {
                    this.AnimUtil.InitializeAnimator(this.type + "-" + key, mesh, shape, this.rendererRot);
                }
                return mesh;
            }
            if (rndTexNum > 0)
            { rndTexNum = GameMath.MurmurHash3Mod(this.Pos.X, this.Pos.Y, this.Pos.Z, rndTexNum); }
            if (this.AnimUtil != null)
            {
                if (this.AnimUtil.renderer == null)
                {
                    mesh = this.AnimUtil.InitializeAnimator(this.type + "-" + key, shape, block, this.rendererRot);
                }
                return meshes[meshKey] = mesh;
            }
            else
            {
                return meshes[meshKey] = block.GenMesh(this.Api as ICoreClientAPI, this.type, shapename, tesselator, new Vec3f(), rndTexNum);
            }
        }



        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            var skipmesh = base.OnTesselation(mesher, tesselator);
            if (!skipmesh)
            {
                if (this.ownMesh == null)
                {
                    this.ownMesh = this.GenMesh(tesselator);
                    if (this.ownMesh == null)
                    { return false; }
                }
                mesher.AddMeshData(this.ownMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, this.MeshAngle, 0));
            }
            return true;
        }
    }
}

