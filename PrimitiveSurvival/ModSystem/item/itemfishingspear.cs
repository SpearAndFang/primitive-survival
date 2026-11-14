namespace PrimitiveSurvival.ModSystem
{
    using Cairo;
    using System.Diagnostics;
    using System.Linq;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;
    using Vintagestory.API.Util;
    using Vintagestory.GameContent;
    #nullable disable
    public class ItemFishingSpear : Item
    {
        private readonly string[] validFishTypes = { "salmon", "bream-sea", "gurnard-cape", "haddock-common", "hake-silver", "herring-atlantic", 
                "mackerel-atlantic", "pollock-alaska", "perch-pacific", "barracuda-great", "grouper-black", "snapper-red", "tuna-skipjack", "wolf-bering", 
                "amberjack-yellowtail", "mahi-mahi-common", "wreckfish-atlantic", "coelacanth-common", "sturgeon-atlantic", "alewife-shad",
                "chub-river", "crappie-black", "crappie-white", "perch-european", "perch-yellow", "piranha-black", "piranha-red", "trout-brown",  "trout-rainbow",
                "bass-largemouth", "bass-smallmouth", "carp-common", "carp-grass", "catfish-blue", "catfish-channel", "pickerel-chain", "salmon-coho",
                "tilapia-nile", "tilapia-red", "walleye-common", "pike-northern", "arapaima-arapaima", "arapaima-gigas", "sheatfish-black", "sheatfish-white" };

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
            var eplr = byEntity as EntityPlayer;
            var slot = eplr?.ActiveHandItemSlot;
            if (slot?.Itemstack == null) return;

            if (slot.Itemstack.Attributes.GetString("fish", "none") != "none") return;

            if (byEntity.World.Side == EnumAppSide.Server)
            {
                Entity target = eplr.EntitySelection?.Entity;
                if (target == null)
                {
                    target = byEntity.World.GetNearestEntity(byEntity.Pos.XYZ.Add(0, 1.0, 0), 1.0f, 1.0f, (e) => {
                        return validFishTypes.Any(s => e.Code.Path.Contains(s));
                    });
                }

                if (target != null && byEntity.Attributes.GetInt("didattack") == 0)
                {
                    string thisEntityPath = target.Code.Path;
                    bool isValidFish = validFishTypes.Any(s => thisEntityPath.Contains(s));

                    if (isValidFish)
                    {
                        byEntity.Attributes.SetInt("didattack", 1);
                        string fishType = target.FirstCodePart(2) + "-" + target.FirstCodePart(3);
                        if (fishType == "mahi-mahi")
                        {
                            fishType += "-" + target.FirstCodePart(4);
                        }
                        slot.Itemstack.Attributes.SetString("fish", fishType);
                        if (!byEntity.IsEyesSubmerged() && target.FeetInLiquid)
                        {
                            byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/effect/water-fill2"), (float)byEntity.Pos.X, (float)byEntity.Pos.Y, (float)byEntity.Pos.Z, null);
                        }
                        
     
                        slot.Itemstack.Collectible.DamageItem(api.World, byEntity, slot, 1);
                        if (slot.Itemstack == null) return;
                        
                        slot.MarkDirty(); //only sync if fish caught
                        int remainingDurability = slot.Itemstack.Attributes.GetInt("durability", 1);

                        byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/arrow-impact"), (float)byEntity.Pos.X, (float)byEntity.Pos.Y, (float)byEntity.Pos.Z, null, randomizePitch: false, volume: 0.5f);
                        ICoreServerAPI sapi = api as ICoreServerAPI;
                        (api as ICoreServerAPI).World.DespawnEntity(target, new EntityDespawnData() { Reason = EnumDespawnReason.Removed });
                    }
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

            var fishType = slot.Itemstack?.Attributes?.GetString("fish", "none") ?? "none";

            if (fishType == "none" && byEntity.World.Side == EnumAppSide.Server) return;

            slot.Itemstack.Attributes.SetString("fish", "none");

            if (byEntity.World.Side == EnumAppSide.Client)
            {
                (byEntity.World as IClientWorldAccessor).Api.Event.EnqueueMainThreadTask(() =>
                {
                    slot.Itemstack?.TempAttributes?.RemoveAttribute("meshRefId");
                }, "remeshspear");
            }
            else if (byEntity.World.Side == EnumAppSide.Server)
            {
                var fishAsset = new AssetLocation("game:fishraw-freshwater-" + fishType + "-adult");
                var newItem = byEntity.World.GetItem(fishAsset);
                if (newItem == null)
                {
                    fishAsset = new AssetLocation("game:fishraw-saltwater-" + fishType + "-adult");
                    newItem = byEntity.World.GetItem(fishAsset);
                }

                if (newItem != null)
                {
                    var giveStack = new ItemStack(newItem, 1);
                    api.World.SpawnItemEntity(giveStack, byEntity.Pos.XYZ.AddCopy(0, 0.5, 0));
                }

                slot.MarkDirty();
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