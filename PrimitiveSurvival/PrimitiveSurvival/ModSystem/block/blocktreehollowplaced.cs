namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Client;
    using Vintagestory.API.Config;
    using Vintagestory.API.Util;
    //using Vintagestory.GameContent;
    //using System.Diagnostics;


    public class BlockTreeHollowPlaced : Block, ITexPositionSource
    {
        public Size2i AtlasSize => this.tmpTextureSource.AtlasSize;

        private string curType;
        private string defaultType;
        private string variantByGroup;
        private string variantByGroupInventory;
        private ITexPositionSource tmpTextureSource;

        public string Subtype => this.variantByGroup == null ? "" : this.Variant[this.variantByGroup];

        public string SubtypeInventory => this.variantByGroupInventory == null ? "" : this.Variant[this.variantByGroupInventory];

        public TextureAtlasPosition this[string textureCode] => this.tmpTextureSource[this.curType + "-" + textureCode];

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            this.defaultType = this.Attributes["defaultType"].AsString("normal-generic");
            this.variantByGroup = this.Attributes["variantByGroup"].AsString(null);
            this.variantByGroupInventory = this.Attributes["variantByGroupInventory"].AsString(null);
        }


        public string GetType(IBlockAccessor blockAccessor, BlockPos pos)
        {
            if (blockAccessor.GetBlockEntity(pos) is BETreeHollowPlaced be)
            {
                return be.type;
            }
            return this.defaultType;
        }


        public override List<ItemStack> GetHandBookStacks(ICoreClientAPI capi)
        {
            return base.GetHandBookStacks(capi);
        }

        private Cuboidf[] BuildCollisionSelectionBoxes(BETreeHollowPlaced be)
        {
            Cuboidf[] boxes;
            var angle = 0;
            switch (be.Block.LastCodePart())
            {
                case "south":
                { angle = 180; break; }
                case "east":
                { angle = 270; break; }
                case "west":
                { angle = 90; break; }
                default:
                    break;
            }
            if (be.type.Contains("up"))
            {
                boxes = new Cuboidf[5];
                boxes[0] = new Cuboidf(0f, 0.7799f, 0f, 1f, 1f, 1f).RotatedCopy(0, angle, 0, new Vec3d(0.5, 0.5, 0.5));
                boxes[1] = new Cuboidf(0.25f, 0.23f, 0f, 0f, 0.7799f, 1f).RotatedCopy(0, angle, 0, new Vec3d(0.5, 0.5, 0.5));
                boxes[2] = new Cuboidf(1f, 0.23f, 0f, 0.75f, 0.7799f, 1f).RotatedCopy(0, angle, 0, new Vec3d(0.5, 0.5, 0.5));
                boxes[3] = new Cuboidf(0f, 0f, 0f, 1f, 0.23f, 1f).RotatedCopy(0, angle, 0, new Vec3d(0.5, 0.5, 0.5));
                boxes[4] = new Cuboidf(0.25f, 0.23f, 0f, 0.75f, 0.78f, 0.25f).RotatedCopy(0, angle, 0, new Vec3d(0.5, 0.5, 0.5));
            }
            else //base
            {
                boxes = new Cuboidf[7];
                boxes[0] = new Cuboidf(0f, 0.8114f, 0f, 1f, 1f, 1f).RotatedCopy(0, angle, 0, new Vec3d(0.5, 0.5, 0.5));
                boxes[1] = new Cuboidf(0.7185f, 0.03f, 0f, 1f, 0.8114f, 1f).RotatedCopy(0, angle, 0, new Vec3d(0.5, 0.5, 0.5));
                boxes[2] = new Cuboidf(0f, 0.03f, 0f, 0.2873f, 0.8114f, 1f).RotatedCopy(0, angle, 0, new Vec3d(0.5, 0.5, 0.5));
                boxes[3] = new Cuboidf(0f, 0f, 0f, 1f, 0.03f, 1f).RotatedCopy(0, angle, 0, new Vec3d(0.5, 0.5, 0.5));
                boxes[4] = new Cuboidf(0.2873f, 0.03f, 0f, 0.7185f, 0.8114f, 0.25f).RotatedCopy(0, angle, 0, new Vec3d(0.5, 0.5, 0.5));
                boxes[5] = new Cuboidf(0.5935f, 0.625f, 0.75f, 0.7185f, 0.8125f, 1f).RotatedCopy(0, angle, 0, new Vec3d(0.5, 0.5, 0.5));
                boxes[6] = new Cuboidf(0.2873f, 0.5625f, 0.75f, 0.406f, 0.8125f, 1f).RotatedCopy(0, angle, 0, new Vec3d(0.5, 0.5, 0.5));
            }
            return boxes;
        }


        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            if (blockAccessor.GetBlockEntity(pos) is BETreeHollowPlaced be)
            {
                return this.BuildCollisionSelectionBoxes(be);
            }
            return base.GetCollisionBoxes(blockAccessor, pos);
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            if (blockAccessor.GetBlockEntity(pos) is BETreeHollowPlaced be)
            {
                return this.BuildCollisionSelectionBoxes(be);
            }
            return base.GetSelectionBoxes(blockAccessor, pos);
        }


        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            var val = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            //Debug.WriteLine(byItemStack.Block.Code.Path);
            if (val)
            {
                if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BETreeHollowPlaced bect)
                {
                    var targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
                    var dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
                    var dz = (float)byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
                    var angleHor = (float)Math.Atan2(dx, dz);
                    var type = bect.type;
                    var deg22dot5rad = GameMath.PIHALF; // / 4;
                    var roundRad = ((int)Math.Round(angleHor / deg22dot5rad)) * deg22dot5rad;
                    bect.MeshAngle = roundRad;
                }
            }
            return val;
        }


        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            var meshrefs = new Dictionary<string, MeshRef>();
            var key = "genericTypedContainerMeshRefs" + this.FirstCodePart() + this.SubtypeInventory;
            meshrefs = ObjectCacheUtil.GetOrCreate(capi, key, () =>
            {
                var meshes = this.GenGuiMeshes(capi);
                foreach (var val in meshes)
                {
                    meshrefs[val.Key] = capi.Render.UploadMesh(val.Value);
                }
                return meshrefs;
            });


            var type = itemstack.Attributes.GetString("type", this.defaultType);
            if (!meshrefs.TryGetValue(type, out renderinfo.ModelRef))
            {
                var mesh = this.GenGuiMesh(capi, type);
                meshrefs[type] = renderinfo.ModelRef = capi.Render.UploadMesh(mesh);
            }

        }

        public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
        {
            base.OnDecalTesselation(world, decalMesh, pos);
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            if (!(api is ICoreClientAPI capi))
            { return; }
            var key = "genericTypedContainerMeshRefs" + this.FirstCodePart() + this.SubtypeInventory;
            var meshrefs = ObjectCacheUtil.TryGet<Dictionary<string, MeshRef>>(api, key);
            if (meshrefs != null)
            {
                foreach (var val in meshrefs)
                {
                    val.Value.Dispose();
                }
                capi.ObjectCache.Remove(key);
            }
        }

        private MeshData GenGuiMesh(ICoreClientAPI capi, string type)
        {
            var shapename = this.Attributes["shape"][type].AsString();
            return this.GenMesh(capi, type, shapename);
        }

        public Dictionary<string, MeshData> GenGuiMeshes(ICoreClientAPI capi)
        {
            var types = this.Attributes["types"].AsArray<string>();
            var meshes = new Dictionary<string, MeshData>();
            foreach (var type in types)
            {
                var shapename = this.Attributes["shape"][type].AsString();
                meshes[type] = this.GenMesh(capi, type, shapename, null, this.ShapeInventory == null ? null : new Vec3f(this.ShapeInventory.rotateX, this.ShapeInventory.rotateY, this.ShapeInventory.rotateZ));
            }
            return meshes;
        }

        public Shape GetShape(ICoreClientAPI capi, string type, string shapename, ITesselatorAPI tesselator = null, int altTexNumber = 0)
        {
            if (shapename == null)
            { return null; }
            if (tesselator == null)
            { tesselator = capi.Tesselator; }

            this.tmpTextureSource = tesselator.GetTexSource(this, altTexNumber);
            var shapeloc = AssetLocation.Create(shapename, this.Code.Domain).WithPathPrefix("shapes/");
            var shape = capi.Assets.TryGet(shapeloc + ".json")?.ToObject<Shape>();
            if (shape == null)
            {
                shape = capi.Assets.TryGet(shapeloc + "1.json")?.ToObject<Shape>();
            }
            this.curType = type;
            return shape;
        }

        public MeshData GenMesh(ICoreClientAPI capi, string type, string shapename, ITesselatorAPI tesselator = null, Vec3f rotation = null, int altTexNumber = 0)
        {
            var shape = this.GetShape(capi, type, shapename, tesselator, altTexNumber);
            if (tesselator == null)
            { tesselator = capi.Tesselator; }
            if (shape == null)
            { return new MeshData(); }
            this.curType = type;
            tesselator.TesselateShape("typedcontainer", shape, out var mesh, this, rotation ?? new Vec3f(this.Shape.rotateX, this.Shape.rotateY, this.Shape.rotateZ));
            return mesh;
        }

        public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
        {
            if (world.BlockAccessor.GetBlockEntity(pos) is BETreeHollowPlaced be)
            {
                var capi = this.api as ICoreClientAPI;
                var shapename = this.Attributes["shape"][be.type].AsString();
                if (shapename == null)
                {
                    base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
                    return;
                }
                blockModelData = this.GenMesh(capi, be.type, shapename);
                var shapeloc = new AssetLocation(shapename).WithPathPrefix("shapes/");
                var shape = capi.Assets.TryGet(shapeloc + ".json")?.ToObject<Shape>();
                if (shape == null)
                {
                    shape = capi.Assets.TryGet(shapeloc + "1.json").ToObject<Shape>();
                }
                capi.Tesselator.TesselateShape("typedcontainer-decal", shape, out var md, decalTexSource);
                decalModelData = md;
                decalModelData.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, be.MeshAngle, 0);
                return;
            }
            base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            var stack = new ItemStack(world.GetBlock(this.CodeWithVariant("side", "north")));
            if (world.BlockAccessor.GetBlockEntity(pos) is BETreeHollowPlaced be)
            {
                stack.Attributes.SetString("type", be.type);
            }
            else
            {
                stack.Attributes.SetString("type", this.defaultType);
            }
            return stack;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var preventDefault = false;
            foreach (var behavior in this.BlockBehaviors)
            {
                var handled = EnumHandling.PassThrough;
                behavior.OnBlockBroken(world, pos, byPlayer, ref handled);
                if (handled == EnumHandling.PreventDefault)
                { preventDefault = true; }
                if (handled == EnumHandling.PreventSubsequent)
                { return; }
            }

            if (preventDefault)
            { return; }

            if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
            {
                var drops = new ItemStack[] { this.OnPickBlock(world, pos) };
                if (this.Attributes["drop"]?[this.GetType(world.BlockAccessor, pos)]?.AsBool() == true && drops != null)
                {
                    for (var i = 0; i < drops.Length; i++)
                    {
                        world.SpawnItemEntity(drops[i], new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
                    }
                }
                world.PlaySoundAt(this.Sounds.GetBreakSound(byPlayer), pos.X, pos.Y, pos.Z, byPlayer);
            }
            if (this.EntityClass != null)
            {
                var entity = world.BlockAccessor.GetBlockEntity(pos);
                if (entity != null)
                { entity.OnBlockBroken(); }
            }
            world.BlockAccessor.SetBlock(0, pos);
        }

        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            var type = handbookStack.Attributes?.GetString("type");
            if (type == null)
            {
                this.api.World.Logger.Warning("BlockGenericTypedContainer.GetDropsForHandbook(): type not set for block " + handbookStack.Collectible?.Code);
                return new BlockDropItemStack[0];
            }
            if (this.Attributes?["drop"]?[type]?.AsBool() == false)
            {
                return new BlockDropItemStack[0];
            }
            else
            {
                return new BlockDropItemStack[] { new BlockDropItemStack(handbookStack) };
            }
        }



        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            return new ItemStack[] { new ItemStack(world.GetBlock(this.CodeWithVariant("side", "north"))) };
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            var type = itemStack.Attributes.GetString("type");
            return Lang.GetMatching(this.Code?.Domain + AssetLocation.LocationSeparator + "block-" + type + "-" + this.Code?.Path);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            var type = inSlot.Itemstack.Attributes.GetString("type");
            if (type != null)
            {
                var qslots = inSlot.Itemstack.ItemAttributes?["quantitySlots"]?[type]?.AsInt(0);
                dsc.AppendLine("\n" + Lang.Get("Quantity Slots: {0}", qslots));
            }
        }


        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return new WorldInteraction[]
            {
            new WorldInteraction()
            {
                ActionLangCode = "blockhelp-chest-open",
                MouseButton = EnumMouseButton.Right
            }
            }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}



