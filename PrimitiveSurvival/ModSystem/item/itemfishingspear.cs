namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Client;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.Common;
    using Vintagestory.API.Server;
    using Vintagestory.API.Util;
    using Vintagestory.GameContent;

    public class ItemFishingSpear : Item
    {
        public override void OnLoaded(ICoreAPI api) 
        {
            base.OnLoaded(api);
            var bh = GetCollectibleBehavior<CollectibleBehaviorAnimationAuthoritative>(true);
            if (bh == null)
            {
                api.World.Logger.Warning("Spear {0} uses ItemSpear class, but lacks required AnimationAuthoritative behavior. I'll take the freedom to add this behavior, but please fix json item type.", Code);
                bh = new CollectibleBehaviorAnimationAuthoritative(this);
                bh.OnLoaded(api);
                CollectibleBehaviors = CollectibleBehaviors.Append(bh);
            }
            bh.OnBeginHitEntity += ItemSpear_OnBeginHitEntity;
        }

              

        private void ItemSpear_OnBeginHitEntity(EntityAgent byEntity, ref EnumHandling handling)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
            { return; }

            var slot = byEntity.ActiveHandItemSlot;
            if (slot.Itemstack?.Collectible?.LastCodePart() != "empty")
            { return; }

            var entitySel = (byEntity as EntityPlayer)?.EntitySelection;
            

            if (byEntity.Attributes.GetInt("didattack") == 0 && entitySel != null)
            {
                //bool swimmer = entitySel.Entity.Swimming;
                byEntity.Attributes.SetInt("didattack", 1);

                if (entitySel.Entity.Code.Path =="salmon")
                {
                    if (!byEntity.IsEyesSubmerged() && entitySel.Entity.FeetInLiquid)
                    {
                        byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/effect/water-fill2"), (float)byEntity.Pos.X, (float)byEntity.Pos.Y, (float)byEntity.Pos.Z, null);
                    }
                    var prevDura = slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack);
                    ICoreServerAPI sapi = api as ICoreServerAPI;
                    string newcode = slot.Itemstack.Collectible.Code.Path.Replace("empty", "salmon");
                    var spearStack = new ItemStack(byEntity.World.GetItem(new AssetLocation("primitivesurvival:" + newcode)), 1);
                    slot.Itemstack.SetFrom(spearStack);
                    slot.Itemstack.Attributes.SetInt("durability", prevDura);
                    slot.MarkDirty();


                    sapi.World.DespawnEntity(entitySel.Entity, new EntityDespawnData() { Reason = EnumDespawnReason.Removed });
                }
            }
        }


        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
        { return null; }


        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            if (handling == EnumHandHandling.PreventDefault) return;
            handling = EnumHandHandling.PreventDefault;
        }


        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            return true;
        }


        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        { 
            return true; 
        }


        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (secondsUsed < 0.35f) return;
            if (slot.Itemstack.Collectible.LastCodePart() == "empty")
            { return; }

            //switch spear back to empty
            var prevDura = slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack);

            string newcode = slot.Itemstack.Collectible.Code.Path.Replace("salmon", "empty");
            var spearStack = new ItemStack(byEntity.World.GetItem(new AssetLocation("primitivesurvival:" + newcode)), 1);
            slot.Itemstack = spearStack;
            slot.Itemstack.Attributes.SetInt("durability", prevDura);
            slot.MarkDirty();

            //transfer fish to empty inventory firstorempty inventory slot (or failing that, drop it)
            var newItem = byEntity.World.GetItem(new AssetLocation("primitivesurvival:psfish-salmon-raw"));
            if (newItem == null)
            { return; }
            var giveStack = new ItemStack(newItem, 1);
            if (!byEntity.TryGiveItemStack(giveStack))
            {
                api.World.SpawnItemEntity(giveStack, byEntity.Pos.XYZ.AddCopy(0, 0.5, 0));
            }
        }


        public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
        }

       
        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            if (this.Code.Path.Contains("empty"))
            {
                return new WorldInteraction[] {
                new WorldInteraction()
                    {
                        ActionLangCode = "handhelp-spearfish", 
                        MouseButton = EnumMouseButton.Left,
                    }
                }.Append(base.GetHeldInteractionHelp(inSlot));
            }
            else
            {
                return new WorldInteraction[] {
                new WorldInteraction()
                    {
                        ActionLangCode = "handhelp-removefish", 
                        MouseButton = EnumMouseButton.Right,
                    }
                }.Append(base.GetHeldInteractionHelp(inSlot));
            }
        }
    }
}