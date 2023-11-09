namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    using Vintagestory.API.Client;

    public class ItemEarthworm : Item
    {

        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
        {
            return null;
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (byEntity.Controls.Sneak && blockSel != null)
            {
                var player = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID);
                if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
                { return; }

                if (!(byEntity is EntityPlayer) || player.WorldData.CurrentGameMode != EnumGameMode.Creative)
                {
                    slot.TakeOut(1);
                    slot.MarkDirty();
                }

                var location = new AssetLocation(this.Code.Domain, this.CodeEndWithoutParts(1));
                var type = byEntity.World.GetEntityType(location);
                if (type == null)
                {
                    byEntity.World.Logger.Error("ItemCreature: No such entity - {0}", location);
                    if (this.api.World.Side == EnumAppSide.Client)
                    { (this.api as ICoreClientAPI).TriggerIngameError(this, "nosuchentity", "No such entity '{0}' loaded."); }
                    return;
                }

                var entity = byEntity.World.ClassRegistry.CreateEntity(type);
                if (entity != null)
                {
                    entity.ServerPos.X = blockSel.Position.X + (blockSel.DidOffset ? 0 : blockSel.Face.Normali.X) + 0.5f;
                    entity.ServerPos.Y = blockSel.Position.Y + (blockSel.DidOffset ? 0 : blockSel.Face.Normali.Y);
                    entity.ServerPos.Z = blockSel.Position.Z + (blockSel.DidOffset ? 0 : blockSel.Face.Normali.Z) + 0.5f;
                    entity.ServerPos.Yaw = (float)byEntity.World.Rand.NextDouble() * 2 * GameMath.PI;
                    entity.Pos.SetFrom(entity.ServerPos);
                    entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
                    entity.Attributes.SetString("origin", "playerplaced");
                    byEntity.World.SpawnEntity(entity);
                    handHandling = EnumHandHandling.PreventDefaultAction;
                }
            }
            else
            { base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling); }
        }


        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        HotKeyCode = "sneak",
                        ActionLangCode = "heldhelp-place",
                        MouseButton = EnumMouseButton.Right,
                    }
                }.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}
