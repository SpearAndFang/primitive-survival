namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    using Vintagestory.API.Common.Entities;
    //using System.Diagnostics;

    public class ItemStick : Item
    {
        private bool canGrunt;
        private bool isGrunting;
        private ILoadedSound wsound;
        public long lookCount;
        public float spawnInterval = 0f;
        public BlockSelection targetBlockSel;
        static Random rand = new Random();

        public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
        {
            this.canGrunt = byEntity.LeftHandItemSlot?.Itemstack?.Collectible.Code.Path == "grunter";
            if (this.canGrunt)
            {
                this.GetHeldInteractionHelp(slot);
                if (byEntity.World is IClientWorldAccessor)
                {
                    this.FpHandTransform.Translation.X = -0.7f;
                    this.FpHandTransform.Translation.Y = 1f;
                    this.FpHandTransform.Translation.Z = -1.4f;
                    this.FpHandTransform.Rotation.X = 180;
                    this.FpHandTransform.Rotation.Y = -107;
                    this.FpHandTransform.Rotation.Z = 2;
                    this.FpHandTransform.Origin.X = 0.5f;
                    this.FpHandTransform.Origin.Y = 0f;
                    this.FpHandTransform.Origin.Z = 0.4f;
                    this.FpHandTransform.Scale = 5.15f;

                    this.TpHandTransform.Translation.X = -0.42f;
                    this.TpHandTransform.Translation.Y = -0.64f;
                    this.TpHandTransform.Translation.Z = -0.69f;
                    this.TpHandTransform.Rotation.X = -6;
                    this.TpHandTransform.Rotation.Y = 118;
                    this.TpHandTransform.Rotation.Z = -68;
                    this.TpHandTransform.Origin.X = 0.1f;
                    this.TpHandTransform.Origin.Y = 0.8f;
                    this.TpHandTransform.Origin.Z = 0.9f;
                    this.TpHandTransform.Scale = 0.63f;
                }
            }
            else
            {
                this.FpHandTransform.Translation.X = 0f;
                this.FpHandTransform.Translation.Y = 0f;
                this.FpHandTransform.Translation.Z = 0f;
                this.FpHandTransform.Rotation.X = 72;
                this.FpHandTransform.Rotation.Y = 143;
                this.FpHandTransform.Rotation.Z = -30;
                this.FpHandTransform.Origin.X = 0.5f;
                this.FpHandTransform.Origin.Y = 0.3f;
                this.FpHandTransform.Origin.Z = 0.5f;
                this.FpHandTransform.Scale = 1.53f;

                this.TpHandTransform.Translation.X = -1.22f;
                this.TpHandTransform.Translation.Y = 0.06f;
                this.TpHandTransform.Translation.Z = -0.89f;
                this.TpHandTransform.Rotation.X = -6;
                this.TpHandTransform.Rotation.Y = 30;
                this.TpHandTransform.Rotation.Z = 18;
                this.TpHandTransform.Origin.X = 0.5f;
                this.TpHandTransform.Origin.Y = 0f;
                this.TpHandTransform.Origin.Z = 0.5f;
                this.TpHandTransform.Scale = 0.5899f;
            }
            base.OnHeldIdle(slot, byEntity);
        }


        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefault;
            if (byEntity.LeftHandItemSlot?.Itemstack?.Collectible.Code.Path != "grunter")
            {
                //return;
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }
            if (!firstEvent)
            { return; }

            // prime the pump.  What the actual fuck
            // also why is the durability in the json always doubled in game
            var gruntDurability = byEntity.LeftHandItemSlot.Itemstack.Attributes.GetInt("durability", 1000) - 1;
            if (gruntDurability < 1)
            { gruntDurability = 1; }
            byEntity.LeftHandItemSlot.Itemstack.Attributes.SetInt("durability", gruntDurability);


            if (byEntity.World is IClientWorldAccessor)
            {
                if (this.wsound == null)
                {
                    this.wsound = (this.api as ICoreClientAPI).World.LoadSound(new SoundParams()
                    {
                        Location = new AssetLocation("primitivesurvival:sounds/grunting.ogg"),
                        ShouldLoop = true,
                        RelativePosition = true,
                        Position = new Vec3f(),
                        DisposeOnFinish = true,
                        Volume = 0.3f,
                        Range = 8
                    });

                    this.wsound.Start();
                }
            }

            if (blockSel?.Position == null || !this.canGrunt || !byEntity.Controls.Sneak)
            { return; }

            this.targetBlockSel = blockSel;
            this.isGrunting = true;

            ((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
        }


        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (((byEntity.Controls.TriesToMove || byEntity.Controls.Jump) && !byEntity.Controls.Sneak) || blockSel == null || !byEntity.Controls.Sneak || byEntity.LeftHandItemSlot?.Itemstack?.Collectible.Code.Path != "grunter")
            {
                this.CancelGrunting(byEntity);
                return false; // Cancel if the player begins walking, stops sneaking, or stops targeting a block
            }

            this.targetBlockSel = blockSel;
            ((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

            this.spawnInterval += 1f;

            if ((secondsUsed > 4.0) && this.spawnInterval > 10)
            {
                this.spawnInterval = 0f;
                if (this.targetBlockSel != null && this.isGrunting)
                {
                    var blockChk = byEntity.World.BlockAccessor.GetBlock(this.targetBlockSel.Position, BlockLayersAccess.Default);
                    var chanceMultiplier = 0f;
                    if (blockChk.Code.Path.Contains("forestfloor"))
                    { chanceMultiplier = 1f; }
                    else if (blockChk.Code.Path.Contains("soil-high"))
                    { chanceMultiplier = 0.8f; }
                    else if (blockChk.Code.Path.Contains("soil-compost"))
                    { chanceMultiplier = 0.6f; }
                    else if (blockChk.Code.Path.Contains("soil-medium"))
                    { chanceMultiplier = 0.4f; }
                    else if (blockChk.Code.Path.Contains("soil-low"))
                    { chanceMultiplier = 0.2f; }
                    else if (blockChk.Code.Path.Contains("soil-verylow"))
                    { chanceMultiplier = 0.1f; }

                    var conds = byEntity.World.BlockAccessor.GetClimateAt(this.targetBlockSel.Position, EnumGetClimateMode.NowValues); //get the temperature and rainfall too

                    if (chanceMultiplier > 0 && conds.Temperature > 0 && conds.Temperature < 35)
                    {
                        chanceMultiplier *= 1f - conds.Rainfall;
                        var rando = rand.Next(100) + 1;
                        if (rando < (chanceMultiplier * 10)) // max 10% chance
                        {
                            //just need to find a nearby suitable block to spawn worm
                            var pos = this.targetBlockSel.Position;
                            var type = byEntity.World.GetEntityType(new AssetLocation("primitivesurvival:earthworm"));
                            var entity = byEntity.World.ClassRegistry.CreateEntity(type);
                            if (entity != null)
                            {
                                var values = new[] { 1, 2, -1, -2 };
                                var xOff = values[rand.Next(values.Length)];
                                var zOff = values[rand.Next(values.Length)];

                                entity.ServerPos.X = pos.X + 0.5f + xOff;
                                entity.ServerPos.Y = pos.Y + 0f;
                                entity.ServerPos.Z = pos.Z + 0.5f + zOff;

                                Block blockSpwn;
                                do
                                {
                                    blockSpwn = byEntity.World.BlockAccessor.GetBlock((int)entity.ServerPos.X, (int)entity.ServerPos.Y, (int)entity.ServerPos.Z, BlockLayersAccess.Default);
                                    if (blockSpwn.BlockId == 0)
                                    { entity.ServerPos.Y -= 1; }
                                }
                                while (blockSpwn.BlockId <= 0);
                                entity.ServerPos.Y += 1;
                                entity.ServerPos.Yaw = (float)rand.NextDouble() * 2 * GameMath.PI;
                                if (blockSpwn.Code.Path.Contains("soil") || blockSpwn.Code.Path.Contains("forestfloor") || blockSpwn.Code.Path.Contains("grass"))
                                {
                                    byEntity.World.SpawnEntity(entity);
                                }
                            }
                        }
                    }
                    if (byEntity.LeftHandItemSlot.Empty)
                    {
                        this.CancelGrunting(byEntity);
                        return false;
                    }
                    else
                    {
                        this.DamageItem(byEntity.World, byEntity, byEntity.LeftHandItemSlot, 1);
                        if (byEntity.LeftHandItemSlot.Empty)
                        {
                            this.CancelGrunting(byEntity);
                            return false;
                        }
                    }
                }
            }
            return true;
        }


        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            if (cancelReason == EnumItemUseCancelReason.ReleasedMouse)
            {
                return false;
            }
            this.CancelGrunting(byEntity);
            return true;
        }


        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            this.CancelGrunting(byEntity);
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
        }


        private void CancelGrunting(EntityAgent byEntity)
        {
            this.isGrunting = false;
            if (byEntity.World is IClientWorldAccessor)
            {
                if (this.wsound != null)
                {
                    if (this.wsound.IsPlaying)
                    {
                        this.wsound?.Stop();
                        this.wsound?.Dispose();
                        this.wsound = null;
                    }
                }
            }
        }

        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
        {
            return "knap";
        }


        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            if (!this.canGrunt)
            { return base.GetHeldInteractionHelp(inSlot); }

            WorldInteraction[] interactions;
            interactions = new WorldInteraction[] {
              new WorldInteraction
              {
                ActionLangCode = "grunt", // "heldhelp-grunter-grunt",
                HotKeyCode = "sneak",
                MouseButton = (EnumMouseButton)2
              }
            };
            return ArrayExtensions.Append(interactions, base.GetHeldInteractionHelp(inSlot));
        }
    }
}

