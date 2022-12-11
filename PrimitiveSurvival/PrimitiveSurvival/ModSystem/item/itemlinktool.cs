namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    //using System.Diagnostics;

    public class ItemLinkTool : Item
    {
        private BlockPos GetStoredBlockPos(ItemStack itemStack)
        {
            if (!itemStack.Attributes.HasAttribute("blockPos"))
            { return null; }
            return SerializerUtil.Deserialize<BlockPos>(itemStack.Attributes.GetBytes("blockPos"), null);
        }


        private void SetStoredBlockPos(ItemStack itemStack, BlockPos blockPos)
        {
            if (blockPos == null)
            { itemStack.Attributes.RemoveAttribute("blockPos"); }
            else
            { itemStack.Attributes.SetBytes("blockPos", SerializerUtil.Serialize(blockPos)); }
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            var capi = this.api as ICoreClientAPI;
            var itemStack = slot.Itemstack;
            var storedPos = this.GetStoredBlockPos(itemStack);

            if (byEntity.Controls.Sneak)
            {
                //clear the link tool
                if (storedPos != null)
                {
                    this.SetStoredBlockPos(itemStack, null);
                }
                handling = EnumHandHandling.PreventDefault;
                byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/effect/squish2"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z, null, true, 32f, 1f);
                //Debug.WriteLine("link tool cleared");
                return;
            }

            if (blockSel?.Position == null)
            { return; }


            if (storedPos != null)
            {
                //Debug.WriteLine("position {0},{1},{2} WAS stored", storedPos.X, storedPos.Y, storedPos.Z);
            }


            var blockChk = byEntity.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
            //Debug.WriteLine(blockChk.Code.Path);
            if (!blockChk.Code.Path.Contains("particulator") || (blockChk.Code.Path.Contains("particulator") && storedPos == null))
            {
                this.SetStoredBlockPos(itemStack, blockSel.Position);
                handling = EnumHandHandling.PreventDefault;
                //Debug.WriteLine("position {0},{1},{2} stored", blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
                byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/bow-release"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z, null, true, 32f, 1f);
                return;
            }
            else
            {
                if (!(this.api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BEParticulator be))
                { return; }

                if (storedPos != null)
                {
                    be.Link(storedPos);
                    this.SetStoredBlockPos(itemStack, null);
                    //Debug.WriteLine("link tool cleared");
                    if (this.api.Side == EnumAppSide.Server)
                    {
                        byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/wearable/plate3"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z, null, true, 32f, 1f);
                    }
                }
                handling = EnumHandHandling.PreventDefault;
            }
        }


        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            WorldInteraction[] interactions;
            interactions = new WorldInteraction[] {
              new WorldInteraction
              {
                ActionLangCode = "Link", // "heldhelp-linktool-link",
                MouseButton = (EnumMouseButton)2,
                //Itemstacks = translocatorItemStacks
              },
              new WorldInteraction
              {
                ActionLangCode = "Clear", // "heldhelp-linktool-clear",
                HotKeyCode = "sneak",
                MouseButton = (EnumMouseButton)2
              }
            };
            return ArrayExtensions.Append(interactions, base.GetHeldInteractionHelp(inSlot));
        }


        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemStack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            var storedSrcPos = this.GetStoredBlockPos(itemStack);
            if (storedSrcPos == null)
            { renderinfo.ModelRef = this.meshrefs[0]; } //off
            else
            { renderinfo.ModelRef = this.meshrefs[1]; } //on
        }


        private MeshRef[] meshrefs;
        public override void OnLoaded(ICoreAPI api)
        {
            if (api.Side == EnumAppSide.Client)
            { this.OnLoadedClientSide(api as ICoreClientAPI); }
        }


        private void OnLoadedClientSide(ICoreClientAPI capi)
        {
            this.meshrefs = new MeshRef[2];
            var key = this.Code.ToString() + "-meshes";
            var shape = capi.Assets.TryGet("primitivesurvival:shapes/item/linktool.json").ToObject<Shape>().Clone();
            this.meshrefs[0] = TesselateAndUpload(this, shape, capi);

            // OBSOLETE IN 1.17
            //shape.GetElementByName("light1").Faces["up"].Glow = 120;
            shape.GetElementByName("light1", StringComparison.InvariantCultureIgnoreCase).FacesResolved[4].Glow = 120;

            this.meshrefs[1] = TesselateAndUpload(this, shape, capi);
            ShapeElementAdjustUpFaceGlowAndTexture(shape, "light1", 255, "#fire-green");
        }


        private static MeshRef TesselateAndUpload(CollectibleObject collectible, Shape shape, ICoreClientAPI capi)
        {
            capi.Tesselator.TesselateShape(collectible, shape, out var meshData, new Vec3f(0, 0, 0));
            return capi.Render.UploadMesh(meshData);
        }


        private static void ShapeElementAdjustUpFaceGlowAndTexture(Shape shape, string name, int glow, string newTexture)
        {
            // CHANGE FOR 1.17
            //var upFace = shape.GetElementByName(name).Faces["up"];
            var upFace = shape.GetElementByName(name, StringComparison.InvariantCultureIgnoreCase).FacesResolved[4];
            upFace.Glow = glow;
            if (newTexture != null)
            { upFace.Texture = newTexture; }
        }
    }
}

