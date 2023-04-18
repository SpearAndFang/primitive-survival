namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Collections.Generic;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    using Vintagestory.GameContent;
    using Vintagestory.API.Config;
    using System.Diagnostics;

    //using System.Diagnostics;


    public class ItemHoeExtended : ItemHoe
    {
        WorldInteraction[] interactions;


        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api.Side != EnumAppSide.Client)
            { return; }
            var capi = api as ICoreClientAPI;

            ObjectCacheUtil.Delete(api, "hoeInteractions");
            this.interactions = ObjectCacheUtil.GetOrCreate(api, "hoeInteractions", () =>
            {
                var stacks = new List<ItemStack>();
                var farmlandStacks = new List<ItemStack>();
                foreach (var block in api.World.Blocks)
                {
                    if (block.Code == null)
                    { continue; }
                    if (block.Code.Path.StartsWith("soil"))
                    { stacks.Add(new ItemStack(block, 1)); }
                    if (block.Code.Path.StartsWith("farmland"))
                    { farmlandStacks.Add(new ItemStack(block, 1)); }
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "heldhelp-till",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stacks.ToArray()
                    },
                    new WorldInteraction()
                    {
                        HotKeyCode = "shift",
                        ActionLangCode = Lang.Get("handhelp-trench"),
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = farmlandStacks.ToArray()
                    }
                };
            });
        }

        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null)
            { return; }

            if (byEntity.Controls.ShiftKey && byEntity.Controls.CtrlKey)
            {
                base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                return;
            }

            var pos = blockSel.Position;
            var block = byEntity.World.BlockAccessor.GetBlock(pos);

            byEntity.Attributes.SetInt("didtill", 0);

            if (block.Code.Path.StartsWith("soil"))
            { handHandling = EnumHandHandling.PreventDefault; }

            //farmland also, but sneak click required
            if (block.Code.Path.StartsWith("farmland"))
            {
                if (byEntity.Controls.ShiftKey)
                { handHandling = EnumHandHandling.PreventDefault; }
            }
        }


        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null)
            { return false; }
            if (byEntity.Controls.ShiftKey && byEntity.Controls.CtrlKey)
            { return false; }

            var byPlayer = (byEntity as EntityPlayer).Player;
            if (byEntity.World is IClientWorldAccessor)
            {
                var tf = new ModelTransform();
                tf.EnsureDefaultValues();

                var rotateToTill = GameMath.Clamp(secondsUsed * 18, 0, 2f);
                var scrape = GameMath.SmoothStep(1 / 0.4f * GameMath.Clamp(secondsUsed - 0.35f, 0, 1));
                var scrapeShake = secondsUsed > 0.35f && secondsUsed < 0.75f ? (float)(GameMath.Sin(secondsUsed * 50) / 60f) : 0;

                var rotateWithReset = Math.Max(0, rotateToTill - GameMath.Clamp(24 * (secondsUsed - 0.75f), 0, 2));
                var scrapeWithReset = Math.Max(0, scrape - Math.Max(0, 20 * (secondsUsed - 0.75f)));

                tf.Origin.Set(0f, 0, 0.5f);
                tf.Rotation.Set(0, rotateWithReset * 45, 0);
                tf.Translation.Set(scrapeShake, 0, scrapeWithReset / 2);
                byEntity.Controls.UsingHeldItemTransformBefore = tf;
            }
            if (secondsUsed > 0.35f && secondsUsed < 0.87f)
            {
                var dir = new Vec3d().AheadCopy(1, 0, byEntity.SidedPos.Yaw - GameMath.PI);
                var pos = blockSel.Position.ToVec3d().Add(0.5 + dir.X, 1.03, 0.5 + dir.Z);

                pos.X -= dir.X * secondsUsed * 1 / 0.75f * 1.2f;
                pos.Z -= dir.Z * secondsUsed * 1 / 0.75f * 1.2f;

                byEntity.World.SpawnCubeParticles(blockSel.Position, pos, 0.25f, 3, 0.5f, byPlayer);
            }
            if (secondsUsed > 0.6f && byEntity.Attributes.GetInt("didtill") == 0 && byEntity.World.Side == EnumAppSide.Server)
            {
                byEntity.Attributes.SetInt("didtill", 1);
                this.DoTill(secondsUsed, slot, byEntity, blockSel, entitySel);
            }

            return secondsUsed < 1;
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            return false;
        }



        public virtual void DoTill(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null)
            { return; }
            var pos = blockSel.Position;
            var block = byEntity.World.BlockAccessor.GetBlock(pos);

            if (block.Code.Path.StartsWith("farmland"))
            {
                var groundIrrigationAsset = "primitivesurvival:furrowedland-" + block.LastCodePart() + "-free";

                //Farmland to ground irrigation
                var channelBlock = byEntity.World.GetBlock(new AssetLocation(groundIrrigationAsset));

                byEntity.World.BlockAccessor.SetBlock(channelBlock.BlockId, blockSel.Position, BlockLayersAccess.Default);

                /*
                var waterBlock = byEntity.World.GetBlock(new AssetLocation("game:water-still-7"));
                byEntity.World.BlockAccessor.SetBlock(waterBlock.BlockId, blockSel.Position, BlockLayersAccess.Fluid);
                */

                byEntity.World.BlockAccessor.MarkBlockDirty(pos);
                //byEntity.World.BlockAccessor.TriggerNeighbourBlockUpdate(pos);

                //now do some of the normal hoe stuff
                if (block.Sounds != null)
                {
                    byEntity.World.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z, null);
                }

                var byPlayer2 = (byEntity as EntityPlayer).Player;
                slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, byPlayer2.InventoryManager.ActiveHotbarSlot);
                if (slot.Empty)
                {
                    byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);
                }
                return;
            }

            if (!block.Code.Path.StartsWith("soil"))
            { return; }

            var fertility = block.LastCodePart(1);
            var farmland = byEntity.World.GetBlock(new AssetLocation("farmland-dry-" + fertility));
            var byPlayer = (byEntity as EntityPlayer).Player;
            if (farmland == null || byPlayer == null)
            { return; }

            if (block.Sounds != null)
            {
                byEntity.World.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z, null);
            }
            byEntity.World.BlockAccessor.SetBlock(farmland.BlockId, pos);
            slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, byPlayer.InventoryManager.ActiveHotbarSlot);

            if (slot.Empty)
            {
                byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);
            }

            var be = byEntity.World.BlockAccessor.GetBlockEntity(pos);
            if (be is BlockEntityFarmland farmland1)
            {
                farmland1.OnCreatedFromSoil(block);
            }
            byEntity.World.BlockAccessor.MarkBlockDirty(pos);
        }
    }
}
