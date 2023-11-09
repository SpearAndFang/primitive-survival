namespace PrimitiveSurvival.ModSystem
{
    using System.Collections.Generic;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    using Vintagestory.GameContent;

    public class BlockLiquidIrrigationVesselTopOpened : BlockLiquidIrrigationVesselBase, IContainedMeshSource, IContainedCustomName
    {
        LiquidTopOpenContainerProps Props;
        protected virtual string meshRefsCacheKey => this.Code.ToShortString() + "meshRefs";
        protected virtual AssetLocation emptyShapeLoc => this.Props.EmptyShapeLoc;
        protected virtual AssetLocation contentShapeLoc => this.Props.OpaqueContentShapeLoc;
        protected virtual AssetLocation liquidContentShapeLoc => this.Props.LiquidContentShapeLoc;
        public override float TransferSizeLitres => this.Props.TransferSizeLitres;
        public override float CapacityLitres => this.Props.CapacityLitres;
        public override bool CanDrinkFrom => true;
        public override bool IsTopOpened => true;
        public override bool AllowHeldLiquidTransfer => true;
        protected virtual float liquidMaxYTranslate => this.Props.LiquidMaxYTranslate;
        protected virtual float liquidYTranslatePerLitre => this.liquidMaxYTranslate / this.CapacityLitres;


        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (this.Attributes?["liquidContainerProps"].Exists == true)
            {
                this.Props = this.Attributes["liquidContainerProps"].AsObject<LiquidTopOpenContainerProps>(null, this.Code.Domain);
            }
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            Dictionary<int, MultiTextureMeshRef> meshrefs;
            if (capi.ObjectCache.TryGetValue(this.meshRefsCacheKey, out var obj))
            { meshrefs = obj as Dictionary<int, MultiTextureMeshRef>; }
            else
            { capi.ObjectCache[this.meshRefsCacheKey] = meshrefs = new Dictionary<int, MultiTextureMeshRef>(); }

            var contentStack = this.GetContent(itemstack);
            if (contentStack == null)
            { return; }

            var hashcode = this.GetStackCacheHashCode(contentStack);
            if (!meshrefs.TryGetValue(hashcode, out var meshRef))
            {
                var meshdata = this.GenMesh(capi, contentStack);
                meshrefs[hashcode] = meshRef = capi.Render.UploadMultiTextureMesh(meshdata);
            }
            renderinfo.ModelRef = meshRef;
        }


        protected int GetStackCacheHashCode(ItemStack contentStack)
        {
            var s = contentStack.StackSize + "x" + contentStack.Collectible.Code.ToShortString();
            return s.GetHashCode();
        }



        public override void OnUnloaded(ICoreAPI api)
        {
            if (!(api is ICoreClientAPI capi))
            { return; }

            if (capi.ObjectCache.TryGetValue(this.meshRefsCacheKey, out var obj))
            {
                var meshrefs = obj as Dictionary<int, MultiTextureMeshRef>;
                if (meshrefs != null)
                {
                    foreach (var val in meshrefs)
                    { val.Value.Dispose(); }
                }
                capi.ObjectCache.Remove(this.meshRefsCacheKey);
            }
        }

        public MeshData GenMesh(ICoreClientAPI capi, ItemStack contentStack, BlockPos forBlockPos = null)
        {
            var shape = Vintagestory.API.Common.Shape.TryGet(capi, this.emptyShapeLoc.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"));
            if (shape == null)
            {
                capi.World.Logger.Error("Empty shape {0} not found. Liquid container {1} will be invisible.", this.emptyShapeLoc, this.Code);
                return new MeshData();
            }
            capi.Tesselator.TesselateShape(this, shape, out var bucketmesh, new Vec3f(this.Shape.rotateX, this.Shape.rotateY, this.Shape.rotateZ));

            if (contentStack != null)
            {
                var props = GetContainableProps(contentStack);
                if (props == null)
                {
                    capi.World.Logger.Error("Contents ('{0}') has no liquid properties, contents of liquid container {1} will be invisible.", contentStack.GetName(), this.Code);
                    return bucketmesh;
                }

                var contentSource = new ContainerTextureSource(capi, contentStack, props.Texture);

                var loc = props.IsOpaque ? this.contentShapeLoc : this.liquidContentShapeLoc;
                shape = Vintagestory.API.Common.Shape.TryGet(capi, loc.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"));

                if (shape == null)
                {
                    capi.World.Logger.Error("Content shape {0} not found. Contents of liquid container {1} will be invisible.", loc, this.Code);
                    return bucketmesh;
                }

                capi.Tesselator.TesselateShape(this.GetType().Name, shape, out var contentMesh, contentSource, new Vec3f(this.Shape.rotateX, this.Shape.rotateY, this.Shape.rotateZ), props.GlowLevel);

                contentMesh.Translate(0, GameMath.Min(this.liquidMaxYTranslate, contentStack.StackSize / props.ItemsPerLitre * this.liquidYTranslatePerLitre), 0);

                if (props.ClimateColorMap != null)
                {
                    int col;
                    if (forBlockPos != null)
                    {
                        col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, ColorUtil.WhiteArgb, forBlockPos.X, forBlockPos.Y, forBlockPos.Z, false);
                    }
                    else
                    {
                        col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, ColorUtil.WhiteArgb, 196, 128, false);
                    }

                    var rgba = ColorUtil.ToBGRABytes(col);
                    for (var i = 0; i < contentMesh.Rgba.Length; i++)
                    {
                        contentMesh.Rgba[i] = (byte)(contentMesh.Rgba[i] * rgba[i % 4] / 255);
                    }
                }

                for (var i = 0; i < contentMesh.Flags.Length; i++)
                {
                    contentMesh.Flags[i] = contentMesh.Flags[i] & ~(1 << 12); // Remove water waving flag
                }
                bucketmesh.AddMeshData(contentMesh);

                // Water flags
                if (forBlockPos != null)
                {
                    bucketmesh.CustomInts = new CustomMeshDataPartInt(bucketmesh.FlagsCount)
                    { Count = bucketmesh.FlagsCount };
                    bucketmesh.CustomInts.Values.Fill(0x4000000); // light foam only
                    bucketmesh.CustomFloats = new CustomMeshDataPartFloat(bucketmesh.FlagsCount * 2)
                    { Count = bucketmesh.FlagsCount * 2 };
                }
            }
            return bucketmesh;
        }

        public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos forBlockPos = null)
        {
            var contentStack = this.GetContent(itemstack);
            return this.GenMesh(this.api as ICoreClientAPI, contentStack, forBlockPos);
        }

        public string GetMeshCacheKey(ItemStack itemstack)
        {
            var contentStack = this.GetContent(itemstack);
            var s = itemstack.Collectible.Code.ToShortString() + "-" + contentStack?.StackSize + "x" + contentStack?.Collectible.Code.ToShortString();
            return s;
        }

        public string GetContainedInfo(ItemSlot inSlot)
        {
            var litres = this.GetCurrentLitres(inSlot.Itemstack);
            var contentStack = this.GetContent(inSlot.Itemstack);

            if (litres <= 0)
            { return Lang.Get("{0} (Empty)", inSlot.Itemstack.GetName()); }

            var incontainername = Lang.Get(contentStack.Collectible.Code.Domain + ":incontainer-" + contentStack.Class.ToString().ToLowerInvariant() + "-" + contentStack.Collectible.Code.Path);

            if (litres == 1)
            {
                return Lang.Get("{0} ({1} litre of {2})", inSlot.Itemstack.GetName(), litres, incontainername);
            }
            return Lang.Get("{0} ({1} litres of {2})", inSlot.Itemstack.GetName(), litres, incontainername);
        }

        public string GetContainedName(ItemSlot inSlot, int quantity)
        { return inSlot.Itemstack.GetName(); }
    }
}
