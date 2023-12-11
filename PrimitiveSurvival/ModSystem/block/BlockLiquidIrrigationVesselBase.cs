namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;
    using Vintagestory.API.Util;
    using Vintagestory.GameContent;
    //using System.Diagnostics;

    public abstract class BlockLiquidIrrigationVesselBase : BlockContainer, ILiquidSource, ILiquidSink
    {
        protected float capacityLitresFromAttributes = 50;
        public virtual float CapacityLitres => this.capacityLitresFromAttributes;
        public virtual int ContainerSlotId => 0;
        public virtual float TransferSizeLitres => 1;
        public virtual bool CanDrinkFrom => this.Attributes["canDrinkFrom"].AsBool() == true;
        public virtual bool IsTopOpened => this.Attributes["isTopOpened"].AsBool() == true;
        public virtual bool AllowHeldLiquidTransfer => this.Attributes["allowHeldLiquidTransfer"].AsBool() == true;


        public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, ItemSlot inSlot, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, bool resolveImports)
        {
            base.OnLoadCollectibleMappings(worldForResolve, inSlot, oldBlockIdMapping, oldItemIdMapping, true);
        }

        Dictionary<string, ItemStack[]> recipeLiquidContents = new Dictionary<string, ItemStack[]>();

        public override void OnHandbookRecipeRender(ICoreClientAPI capi, GridRecipe gridRecipe, ItemSlot dummyslot, double x, double y, double z, double size)
        {
            // 1.16.0: Fugly (but backwards compatible) hack: We temporarily store the ingredient index in an unused field of ItemSlot so that OnHandbookRecipeRender() has access to that number. Proper solution would be to alter the method signature to pass on this value.
            var rindex = dummyslot.BackgroundIcon.ToInt();
            var ingredient = gridRecipe.resolvedIngredients[rindex];
            var rprops = ingredient.RecipeAttributes;
            if (rprops?.Exists != true || rprops?["requiresContent"].Exists != true)
            { rprops = gridRecipe.Attributes?["liquidContainerProps"]; }

            if (rprops?.Exists != true)
            {
                base.OnHandbookRecipeRender(capi, gridRecipe, dummyslot, x, y, z, size);
                return;
            }

            var contentCode = gridRecipe.Attributes["liquidContainerProps"]["requiresContent"]["code"].AsString();
            var contentType = gridRecipe.Attributes["liquidContainerProps"]["requiresContent"]["type"].AsString();
            var litres = gridRecipe.Attributes["liquidContainerProps"]["requiresLitres"].AsFloat();

            var key = contentType + "-" + contentCode;
            ItemStack[] stacks;
            if (!this.recipeLiquidContents.TryGetValue(key, out stacks))
            {
                if (contentCode.Contains("*"))
                {
                    var contentClass = contentType == "block" ? EnumItemClass.Block : EnumItemClass.Item;
                    var lstacks = new List<ItemStack>();
                    var loc = AssetLocation.Create(contentCode, this.Code.Domain);
                    foreach (var obj in this.api.World.Collectibles)
                    {
                        if (obj.ItemClass == contentClass && WildcardUtil.Match(loc, obj.Code))
                        {
                            var stack = new ItemStack(obj);
                            var props = GetContainableProps(stack);
                            if (props == null)
                            { continue; }

                            stack.StackSize = (int)(props.ItemsPerLitre * litres);
                            lstacks.Add(stack);
                        }
                    }
                    stacks = lstacks.ToArray();
                }
                else
                {
                    this.recipeLiquidContents[key] = stacks = new ItemStack[1];
                    if (contentType == "item")
                    { stacks[0] = new ItemStack(capi.World.GetItem(new AssetLocation(contentCode))); }
                    else
                    { stacks[0] = new ItemStack(capi.World.GetBlock(new AssetLocation(contentCode))); }

                    var props = GetContainableProps(stacks[0]);
                    stacks[0].StackSize = (int)(props.ItemsPerLitre * litres);
                }
            }

            var filledContainerStack = dummyslot.Itemstack.Clone();
            var index = (int)(capi.ElapsedMilliseconds / 1000) % stacks.Length;
            this.SetContent(filledContainerStack, stacks[index]);
            dummyslot.Itemstack = filledContainerStack;

            capi.Render.RenderItemstackToGui(
                dummyslot,
                x,
                y,
                z, (float)size * 0.58f, ColorUtil.WhiteArgb,
                true, false, true
            );
        }


        public virtual int GetContainerSlotId(BlockPos pos)
        { return this.ContainerSlotId; }

        public virtual int GetContainerSlotId(ItemStack containerStack)
        { return this.ContainerSlotId; }

        #region Interaction help
        protected WorldInteraction[] interactions;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (this.Attributes?["capacityLitres"].Exists == true)
            { this.capacityLitresFromAttributes = this.Attributes["capacityLitres"].AsInt(10); }
            else
            {
                var props = this.Attributes?["liquidContainerProps"]?.AsObject<LiquidTopOpenContainerProps>(null, this.Code.Domain);
                if (props != null)
                { this.capacityLitresFromAttributes = props.CapacityLitres; }
            }

            if (api.Side != EnumAppSide.Client)
            { return; }

            var capi = api as ICoreClientAPI;
            this.interactions = ObjectCacheUtil.GetOrCreate(api, "liquidContainerBase", () =>
            {
                var liquidContainerStacks = new List<ItemStack>();
                foreach (var obj in api.World.Collectibles)
                {
                    if (obj is BlockLiquidIrrigationVesselBase blc && blc.IsTopOpened && blc.AllowHeldLiquidTransfer)
                    { liquidContainerStacks.Add(new ItemStack(obj)); }
                }
                var lcstacks = liquidContainerStacks.ToArray();

                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-bucket-rightclick",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = lcstacks
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-bucket-rightclick-sneak",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "shift",
                        Itemstacks = lcstacks
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-bucket-rightclick-sprint",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "ctrl",
                        Itemstacks = lcstacks
                    }
                };
            });
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(this.interactions);
        }


        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    ActionLangCode = "heldhelp-fill",
                    MouseButton = EnumMouseButton.Right,
                    ShouldApply = (wi, bs, es) => {
                        return this.GetCurrentLitres(inSlot.Itemstack) < this.CapacityLitres;
                    }
                },
                new WorldInteraction()
                {
                    ActionLangCode = "heldhelp-empty",
                    HotKeyCode = "ctrl",
                    MouseButton = EnumMouseButton.Right,
                    ShouldApply = (wi, bs, es) => {
                        return this.GetCurrentLitres(inSlot.Itemstack) > 0;
                    }
                },
                new WorldInteraction()
                {
                    ActionLangCode = "heldhelp-place",
                    HotKeyCode = "shift",
                    MouseButton = EnumMouseButton.Right,
                    ShouldApply = (wi, bs, es) => {
                        return true;
                    }
                }
            };
        }

        #endregion

        #region Take/Remove Contents

        public bool SetCurrentLitres(ItemStack containerStack, float litres)
        {
            var props = this.GetContentProps(containerStack);
            if (props == null)
            { return false; }

            var contentStack = this.GetContent(containerStack);
            contentStack.StackSize = (int)(litres * props.ItemsPerLitre);
            this.SetContent(containerStack, contentStack);
            return true;
        }

        public float GetCurrentLitres(ItemStack containerStack)
        {
            var props = this.GetContentProps(containerStack);
            if (props == null)
            { return 0; }
            return this.GetContent(containerStack).StackSize / props.ItemsPerLitre;
        }


        public float GetCurrentLitres(BlockPos pos)
        {
            var props = this.GetContentProps(pos);
            if (props == null)
            { return 0; }
            return this.GetContent(pos).StackSize / props.ItemsPerLitre;
        }


        public bool IsFull(ItemStack containerStack)
        { return this.GetCurrentLitres(containerStack) >= this.CapacityLitres; }

        public bool IsFull(BlockPos pos)
        { return this.GetCurrentLitres(pos) >= this.CapacityLitres; }


        public WaterTightContainableProps GetContentProps(ItemStack containerStack)
        {
            var stack = this.GetContent(containerStack);
            return GetContainableProps(stack);
        }


        public static int GetTransferStackSize(ILiquidInterface containerBlock, ItemStack contentStack, IPlayer player = null)
        {
            return GetTransferStackSize(containerBlock, contentStack, player?.Entity?.Controls.ShiftKey == true);
        }

        public static int GetTransferStackSize(ILiquidInterface containerBlock, ItemStack contentStack, bool maxCapacity)
        {
            if (contentStack == null)
            { return 0; }
            var litres = containerBlock.TransferSizeLitres;
            var liqProps = GetContainableProps(contentStack);
            var stacksize = (int)(liqProps.ItemsPerLitre * litres);

            if (maxCapacity)
            { stacksize = (int)(containerBlock.CapacityLitres * liqProps.ItemsPerLitre); }
            //Debug.WriteLine("Transfer stack size:" + stacksize);
            return stacksize;
        }


        public static WaterTightContainableProps GetContainableProps(ItemStack stack)
        {
            try
            {
                var obj = stack?.ItemAttributes?["waterTightContainerProps"];
                if (obj != null && obj.Exists)
                { return obj.AsObject<WaterTightContainableProps>(null, stack.Collectible.Code.Domain); }
                return null;
            }
            catch (Exception)
            { return null; }
        }

        // Retrives the containable properties of the currently contained itemstack of a placed water container
        public WaterTightContainableProps GetContentProps(BlockPos pos)
        {
            if (!(this.api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer becontainer))
            { return null; }

            var slotid = this.GetContainerSlotId(pos);
            if (slotid >= becontainer.Inventory.Count)
            { return null; }

            var stack = becontainer.Inventory[slotid]?.Itemstack;
            if (stack == null)
            { return null; }

            return GetContainableProps(stack);
        }

        // Sets the containers contents to given stack
        public void SetContent(ItemStack containerStack, ItemStack content)
        {
            if (content == null)
            {
                this.SetContents(containerStack, null);
                return;
            }
            this.SetContents(containerStack, new ItemStack[] { content });
        }


        // Sets the contents to placed container block
        public void SetContent(BlockPos pos, ItemStack content)
        {
            if (!(this.api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer beContainer))
            { return; }
            new DummySlot(content).TryPutInto(this.api.World, beContainer.Inventory[this.GetContainerSlotId(pos)], content.StackSize);
            beContainer.Inventory[this.GetContainerSlotId(pos)].MarkDirty();
            beContainer.MarkDirty(true);
        }


        // Retrieve the contents of the container stack
        public ItemStack GetContent(ItemStack containerStack)
        {
            var stacks = this.GetContents(this.api.World, containerStack);
            var id = this.GetContainerSlotId(containerStack);
            return (stacks != null && stacks.Length > 0) ? stacks[Math.Min(stacks.Length - 1, id)] : null;
        }

        // Retrieve the contents of a placed container
        public ItemStack GetContent(BlockPos pos)
        {
            if (!(this.api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer becontainer))
            { return null; }
            return becontainer.Inventory[this.GetContainerSlotId(pos)].Itemstack;
        }


        public override ItemStack CreateItemStackFromJson(ITreeAttribute stackAttr, IWorldAccessor world, string domain)
        {
            var stack = base.CreateItemStackFromJson(stackAttr, world, domain);
            if (stackAttr.HasAttribute("makefull"))
            {
                var props = GetContainableProps(stack);
                stack.StackSize = (int)(this.CapacityLitres * props.ItemsPerLitre);
            }
            return stack;
        }

        /// Tries to take out as much items/liquid as possible and returns it
        public ItemStack TryTakeContent(ItemStack containerStack, int quantityItems)
        {
            var stack = this.GetContent(containerStack);
            if (stack == null)
            { return null; }

            var takenStack = stack.Clone();
            takenStack.StackSize = quantityItems;
            stack.StackSize -= quantityItems;
            if (stack.StackSize <= 0)
            { this.SetContent(containerStack, null); }
            else
            { this.SetContent(containerStack, stack); }
            var toTake = takenStack.StackSize;
            //Debug.WriteLine("Storage Vessel PUTTING:" + takenStack.StackSize + "Litres");
            return takenStack;
        }


        // Tries to take out as much items/liquid as possible from a placed bucket and returns it
        public ItemStack TryTakeContent(BlockPos pos, int quantityItem)
        {
            if (!(this.api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer becontainer))
            { return null; }

            var stack = becontainer.Inventory[this.GetContainerSlotId(pos)].Itemstack;
            if (stack == null)
            { return null; }

            var takenStack = stack.Clone();
            takenStack.StackSize = quantityItem;
            stack.StackSize -= quantityItem;
            if (stack.StackSize <= 0)
            { becontainer.Inventory[this.GetContainerSlotId(pos)].Itemstack = null; }
            else
            { becontainer.Inventory[this.GetContainerSlotId(pos)].Itemstack = stack; }

            becontainer.Inventory[this.GetContainerSlotId(pos)].MarkDirty();
            becontainer.MarkDirty(true);
            var toTake = takenStack.StackSize;
            return takenStack;
        }

        public ItemStack TryTakeLiquid(ItemStack containerStack, float desiredLitres)
        {
            var props = GetContainableProps(this.GetContent(containerStack));
            if (props == null)
            { return null; }
            return this.TryTakeContent(containerStack, (int)(desiredLitres * props.ItemsPerLitre));
        }

        #endregion


        #region PutContents

        // Tries to place in items/liquid and returns actually inserted quantity
        public virtual int TryPutLiquid(ItemStack containerStack, ItemStack liquidStack, float desiredLitres)
        {
            if (liquidStack == null)
            { return 0; }

            var props = GetContainableProps(liquidStack);
            if (props == null)
            { return 0; }

            var desiredItems = (int)(props.ItemsPerLitre * desiredLitres);
            var availItems = liquidStack.StackSize;
            var stack = this.GetContent(containerStack);
            var sink = containerStack.Collectible as ILiquidSink;

            if (stack == null)
            {
                if (!props.Containable)
                { return 0; }

                var placeableItems = (int)(sink.CapacityLitres * props.ItemsPerLitre);
                var placedstack = liquidStack.Clone();
                placedstack.StackSize = GameMath.Min(availItems, desiredItems, placeableItems);
                this.SetContent(containerStack, placedstack);
                //Debug.WriteLine("Storage Vessel getting:" + Math.Min(desiredItems, placeableItems) + "litres THIS IS WHACK BUT NOT THE ISSUE");
                return Math.Min(desiredItems, placeableItems);
            }
            else
            {
                if (!stack.Equals(this.api.World, liquidStack, GlobalConstants.IgnoredStackAttributes))
                { return 0; }

                var maxItems = sink.CapacityLitres * props.ItemsPerLitre;
                var placeableItems = (int)(maxItems - stack.StackSize);

                //If the quantity isn't available dont add or return some other number ffs
                //stack.StackSize += Math.Min(placeableItems, desiredItems);
                stack.StackSize += GameMath.Min(availItems, placeableItems, desiredItems);

                //Debug.WriteLine("Getting an additional:" + GameMath.Min(availItems, placeableItems, desiredItems) + "Litres <-  THIS IS THE CULPRIT");
                //return Math.Min(placeableItems, desiredItems);
                return GameMath.Min(availItems, placeableItems, desiredItems);
            }
        }

        // Tries to put as much items/liquid as possible into a placed container and returns it how much items it actually moved
        public virtual int TryPutLiquid(BlockPos pos, ItemStack liquidStack, float desiredLitres)
        {
            if (liquidStack == null)
            { return 0; }

            var props = GetContainableProps(liquidStack);
            var desiredItems = (int)(props.ItemsPerLitre * desiredLitres);
            float availItems = liquidStack.StackSize;
            var maxItems = this.CapacityLitres * props.ItemsPerLitre;
            var stack = this.GetContent(pos);
            if (stack == null)
            {
                if (props == null || !props.Containable)
                { return 0; }

                var placeableItems = (int)GameMath.Min(desiredItems, maxItems, availItems);
                var movedItems = Math.Min(desiredItems, placeableItems);
                var placedstack = liquidStack.Clone();
                placedstack.StackSize = movedItems;
                this.SetContent(pos, placedstack);
                return movedItems;
            }
            else
            {
                if (!stack.Equals(this.api.World, liquidStack, GlobalConstants.IgnoredStackAttributes))
                { return 0; }

                var placeableItems = (int)Math.Min(availItems, maxItems - stack.StackSize);
                var movedItems = Math.Min(placeableItems, desiredItems);
                stack.StackSize += movedItems;
                this.api.World.BlockAccessor.GetBlockEntity(pos).MarkDirty(true);
                (this.api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityContainer).Inventory[this.GetContainerSlotId(pos)].MarkDirty();
                return movedItems;
            }
        }

        #endregion

        #region Block interact
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (!hotbarSlot.Empty && hotbarSlot.Itemstack.Collectible.Attributes?.IsTrue("handleLiquidContainerInteract") == true)
            {
                var handling = EnumHandHandling.NotHandled;
                hotbarSlot.Itemstack.Collectible.OnHeldInteractStart(hotbarSlot, byPlayer.Entity, blockSel, null, true, ref handling);
                if (handling == EnumHandHandling.PreventDefault || handling == EnumHandHandling.PreventDefaultAction)
                { return true; }
            }

            if (hotbarSlot.Empty || !(hotbarSlot.Itemstack.Collectible is ILiquidInterface))
            { return base.OnBlockInteractStart(world, byPlayer, blockSel); }

            var obj = hotbarSlot.Itemstack.Collectible;
            var singleTake = byPlayer.WorldData.EntityControls.ShiftKey;
            var singlePut = byPlayer.WorldData.EntityControls.CtrlKey;

            if (obj is ILiquidSource objLso && !singleTake)
            {
                if (!objLso.AllowHeldLiquidTransfer)
                { return false; }

                var contentStackToMove = objLso.GetContent(hotbarSlot.Itemstack);
                var litres = singlePut ? objLso.TransferSizeLitres : objLso.CapacityLitres;
                var moved = this.TryPutLiquid(blockSel.Position, contentStackToMove, litres);
                if (moved > 0)
                {
                    this.splitStackAndPerformAction(byPlayer.Entity, hotbarSlot, (stack) =>
                    {
                        objLso.TryTakeContent(stack, moved);
                        return moved;
                    });
                    this.DoLiquidMovedEffects(byPlayer, contentStackToMove, moved, EnumLiquidDirection.Pour);
                    return true;
                }
            }

            if (obj is ILiquidSink objLsi && !singlePut)
            {
                if (!objLsi.AllowHeldLiquidTransfer)
                { return false; }

                var owncontentStack = this.GetContent(blockSel.Position);
                if (owncontentStack == null)
                { return base.OnBlockInteractStart(world, byPlayer, blockSel); }

                var liquidStackForParticles = owncontentStack.Clone();
                var litres = singleTake ? objLsi.TransferSizeLitres : objLsi.CapacityLitres;
                var moved = this.splitStackAndPerformAction(byPlayer.Entity, hotbarSlot, (stack) => objLsi.TryPutLiquid(stack, owncontentStack, litres));

                if (moved > 0)
                {
                    this.TryTakeContent(blockSel.Position, moved);
                    this.DoLiquidMovedEffects(byPlayer, liquidStackForParticles, moved, EnumLiquidDirection.Fill);
                    return true;
                }
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public enum EnumLiquidDirection { Fill, Pour }
        public void DoLiquidMovedEffects(IPlayer player, ItemStack contentStack, int moved, EnumLiquidDirection dir)
        {
            if (player == null)
            { return; }

            var props = GetContainableProps(contentStack);
            var litresMoved = moved / props.ItemsPerLitre;
            (player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            this.api.World.PlaySoundAt(dir == EnumLiquidDirection.Fill ? props.FillSound : props.PourSound, player.Entity, player, true, 16, GameMath.Clamp(litresMoved / 5f, 0.35f, 1f));
            this.api.World.SpawnCubeParticles(player.Entity.Pos.AheadCopy(0.25).XYZ.Add(0, player.Entity.SelectionBox.Y2 / 2, 0), contentStack, 0.75f, (int)litresMoved * 2, 0.45f);
        }

        #endregion


        #region Held Interact

        protected override void tryEatBegin(ItemSlot slot, EntityAgent byEntity, ref EnumHandHandling handling, string eatSound = "eat", int eatSoundRepeats = 1)
        { base.tryEatBegin(slot, byEntity, ref handling, "drink", 4); }

        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null || byEntity.Controls.ShiftKey)
            {
                if (byEntity.Controls.ShiftKey)
                {
                    base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                }
                if (handHandling != EnumHandHandling.PreventDefaultAction && this.CanDrinkFrom && this.GetNutritionProperties(byEntity.World, itemslot.Itemstack, byEntity) != null)
                {
                    this.tryEatBegin(itemslot, byEntity, ref handHandling, "drink", 4);
                    return;
                }
                if (!byEntity.Controls.ShiftKey)
                {
                    base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                }
                return;
            }

            if (this.AllowHeldLiquidTransfer)
            {
                var byPlayer = (byEntity as EntityPlayer)?.Player;
                var contentStack = this.GetContent(itemslot.Itemstack);
                var props = contentStack == null ? null : this.GetContentProps(contentStack);
                var targetedBlock = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);

                if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
                {
                    byEntity.World.BlockAccessor.MarkBlockDirty(blockSel.Position.AddCopy(blockSel.Face));
                    byPlayer?.InventoryManager.ActiveHotbarSlot?.MarkDirty();
                    return;
                }

                if (!this.TryFillFromBlock(itemslot, byEntity, blockSel.Position))
                {
                    if (targetedBlock is BlockLiquidIrrigationVesselTopOpened targetCntBlock)
                    {
                        if (targetCntBlock.TryPutLiquid(blockSel.Position, contentStack, targetCntBlock.CapacityLitres) > 0)
                        {
                            this.TryTakeContent(itemslot.Itemstack, 1);
                            byEntity.World.PlaySoundAt(props.FillSpillSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
                        }
                    }
                    else
                    {
                        if (byEntity.Controls.CtrlKey)
                        { this.SpillContents(itemslot, byEntity, blockSel); }
                    }
                }
            }

            if (this.CanDrinkFrom && this.GetNutritionProperties(byEntity.World, itemslot.Itemstack, byEntity) != null)
            {
                this.tryEatBegin(itemslot, byEntity, ref handHandling, "drink", 4);
                return;
            }

            if (this.AllowHeldLiquidTransfer || this.CanDrinkFrom)
            {
                // Prevent placing on normal use
                handHandling = EnumHandHandling.PreventDefaultAction;
            }
        }

        protected override bool tryEatStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, ItemStack spawnParticleStack = null)
        { return base.tryEatStep(secondsUsed, slot, byEntity, this.GetContent(slot.Itemstack)); }

        protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {
            var nutriProps = this.GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity);
            if (byEntity.World is IServerWorldAccessor && nutriProps != null && secondsUsed >= 0.95f)
            {
                var drinkCapLitres = 1f;
                var litresEach = this.GetCurrentLitres(slot.Itemstack);
                var litresTotal = litresEach * slot.StackSize;

                if (litresEach > drinkCapLitres)
                {
                    nutriProps.Satiety /= litresEach;
                    nutriProps.Health /= litresEach;
                }

                var state = this.UpdateAndGetTransitionState(this.api.World, slot, EnumTransitionType.Perish);
                var spoilState = state != null ? state.TransitionLevel : 0;
                var satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, byEntity);
                var healthLossMul = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, slot.Itemstack, byEntity);

                byEntity.ReceiveSaturation(nutriProps.Satiety * satLossMul, nutriProps.FoodCategory);
                IPlayer player = null;
                if (byEntity is EntityPlayer)
                { player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID); }

                var litresToDrink = Math.Min(drinkCapLitres, litresTotal);
                this.TryTakeLiquid(slot.Itemstack, litresToDrink / slot.Itemstack.StackSize);
                this.splitStackAndPerformAction(byEntity, slot, (stack) => this.TryTakeLiquid(stack, litresToDrink)?.StackSize ?? 0);

                var healthChange = nutriProps.Health * healthLossMul;
                var intox = byEntity.WatchedAttributes.GetFloat("intoxication");
                byEntity.WatchedAttributes.SetFloat("intoxication", Math.Min(1.1f, intox + nutriProps.Intoxication));

                if (healthChange != 0)
                {
                    byEntity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal, Type = healthChange > 0 ? EnumDamageType.Heal : EnumDamageType.Poison }, Math.Abs(healthChange));
                }

                slot.MarkDirty();
                player.InventoryManager.BroadcastHotbarSlot();
            }
        }


        public override FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
        {
            var contentStack = this.GetContent(itemstack);
            var props = contentStack == null ? null : GetContainableProps(contentStack);
            if (props?.NutritionPropsPerLitre != null)
            {
                var nutriProps = props.NutritionPropsPerLitre.Clone();
                var litre = contentStack.StackSize / props.ItemsPerLitre;
                nutriProps.Health *= litre;
                nutriProps.Satiety *= litre;
                nutriProps.EatenStack = new JsonItemStack
                { ResolvedItemstack = itemstack.Clone() };
                nutriProps.EatenStack.ResolvedItemstack.StackSize = 1;

                (nutriProps.EatenStack.ResolvedItemstack.Collectible as BlockLiquidIrrigationVesselBase).SetContent(nutriProps.EatenStack.ResolvedItemstack, null);
                return nutriProps;
            }
            return base.GetNutritionProperties(world, itemstack, forEntity);
        }


        public bool TryFillFromBlock(ItemSlot itemslot, EntityAgent byEntity, BlockPos pos)
        {
            var byPlayer = (byEntity as EntityPlayer)?.Player;
            var blockAcc = byEntity.World.BlockAccessor;
            var block = blockAcc.GetBlock(pos, BlockLayersAccess.FluidOrSolid);
            if (block.Attributes?["waterTightContainerProps"].Exists == false)
            { return false; }

            var props = block.Attributes?["waterTightContainerProps"]?.AsObject<WaterTightContainableProps>();
            if (props?.WhenFilled == null || !props.Containable)
            { return false; }

            props.WhenFilled.Stack.Resolve(byEntity.World, "liquidcontainerbase");
            if (this.GetCurrentLitres(itemslot.Itemstack) >= this.CapacityLitres)
            { return false; }

            var contentStack = props.WhenFilled.Stack.ResolvedItemstack.Clone();
            var cprops = GetContainableProps(contentStack);
            contentStack.StackSize = 999999;

            var moved = this.splitStackAndPerformAction(byEntity, itemslot, (stack) => this.TryPutLiquid(stack, contentStack, this.CapacityLitres));

            if (moved > 0)
            { this.DoLiquidMovedEffects(byPlayer, contentStack, moved, EnumLiquidDirection.Fill); }
            return true;
        }


        public virtual void TryFillFromBlock(EntityItem byEntityItem, BlockPos pos)
        {
            var world = byEntityItem.World;
            var block = world.BlockAccessor.GetBlock(pos);
            if (block.Attributes?["waterTightContainerProps"].Exists == false)
            { return; }

            var props = block.Attributes?["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
            if (props?.WhenFilled == null || !props.Containable)
            { return; }

            if (props.WhenFilled.Stack.ResolvedItemstack == null)
            { props.WhenFilled.Stack.Resolve(world, "liquidcontainerbase"); }

            var whenFilledStack = props.WhenFilled.Stack.ResolvedItemstack;
            var contentStack = this.GetContent(byEntityItem.Itemstack);
            var canFill = contentStack == null || (contentStack.Equals(world, whenFilledStack, GlobalConstants.IgnoredStackAttributes) && this.GetCurrentLitres(byEntityItem.Itemstack) < this.CapacityLitres);
            if (!canFill)
            { return; }

            whenFilledStack.StackSize = 999999;
            var moved = this.splitStackAndPerformAction(byEntityItem, byEntityItem.Slot, (stack) => this.TryPutLiquid(stack, whenFilledStack, this.CapacityLitres));
            if (moved > 0)
            { world.PlaySoundAt(props.FillSound, pos.X, pos.Y, pos.Z, null); }
        }


        private bool SpillContents(ItemSlot containerSlot, EntityAgent byEntity, BlockSelection blockSel)
        {
            var pos = blockSel.Position;
            var byPlayer = (byEntity as EntityPlayer)?.Player;
            var blockAcc = byEntity.World.BlockAccessor;
            var secondPos = blockSel.Position.AddCopy(blockSel.Face);
            var contentStack = this.GetContent(containerSlot.Itemstack);
            var props = this.GetContentProps(containerSlot.Itemstack);

            if (props == null || !props.AllowSpill || props.WhenSpilled == null)
            { return false; }

            if (!byEntity.World.Claims.TryAccess(byPlayer, secondPos, EnumBlockAccessFlags.BuildOrBreak))
            { return false; }

            var action = props.WhenSpilled.Action;
            var currentlitres = this.GetCurrentLitres(containerSlot.Itemstack);
            if (currentlitres > 0 && currentlitres < 10)
            { action = WaterTightContainableProps.EnumSpilledAction.DropContents; }

            if (action == WaterTightContainableProps.EnumSpilledAction.PlaceBlock)
            {
                var waterBlock = byEntity.World.GetBlock(props.WhenSpilled.Stack.Code);
                if (props.WhenSpilled.StackByFillLevel != null)
                {
                    JsonItemStack fillLevelStack;
                    props.WhenSpilled.StackByFillLevel.TryGetValue((int)currentlitres, out fillLevelStack);
                    if (fillLevelStack != null)
                    { waterBlock = byEntity.World.GetBlock(fillLevelStack.Code); }
                }

                var currentblock = blockAcc.GetBlock(pos);
                if (!currentblock.DisplacesLiquids(blockAcc, pos))
                {
                    blockAcc.SetBlock(waterBlock.BlockId, pos, BlockLayersAccess.Fluid);
                    blockAcc.TriggerNeighbourBlockUpdate(pos);
                    waterBlock.OnNeighbourBlockChange(byEntity.World, pos, secondPos);
                    blockAcc.MarkBlockDirty(pos);   // Maybe unnecessary to call this server side as this code will be called client-side anyhow
                }
                else
                {
                    if (!blockAcc.GetBlock(secondPos).DisplacesLiquids(blockAcc, pos))
                    {
                        blockAcc.SetBlock(waterBlock.BlockId, secondPos, BlockLayersAccess.Fluid);
                        blockAcc.TriggerNeighbourBlockUpdate(secondPos);
                        waterBlock.OnNeighbourBlockChange(byEntity.World, secondPos, pos);
                        blockAcc.MarkBlockDirty(secondPos);   // Maybe unnecessary to call this server side as this code will be called client-side anyhow
                    }
                    else
                    { return false; }
                }
            }

            if (action == WaterTightContainableProps.EnumSpilledAction.DropContents)
            {
                props.WhenSpilled.Stack.Resolve(byEntity.World, "liquidcontainerbasespill");
                var stack = props.WhenSpilled.Stack.ResolvedItemstack.Clone();
                stack.StackSize = contentStack.StackSize;
                byEntity.World.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(blockSel.HitPosition));
            }

            var moved = this.splitStackAndPerformAction(byEntity, containerSlot, (stack) => { this.SetContent(stack, null); return contentStack.StackSize; });

            this.DoLiquidMovedEffects(byPlayer, contentStack, moved, EnumLiquidDirection.Pour);
            return true;
        }

        #endregion

        private int splitStackAndPerformAction(Entity byEntity, ItemSlot slot, System.Func<ItemStack, int> action)
        {
            if (slot.Itemstack == null)
            { return 0; }

            if (slot.Itemstack.StackSize == 1)
            {
                var moved = action(slot.Itemstack);
                if (moved > 0)
                {
                    var maxstacksize = slot.Itemstack.Collectible.MaxStackSize;
                    (byEntity as EntityPlayer)?.WalkInventory((pslot) =>
                    {
                        if (pslot.Empty || pslot is ItemSlotCreative || pslot.StackSize == pslot.Itemstack.Collectible.MaxStackSize)
                        { return true; }

                        var mergableq = slot.Itemstack.Collectible.GetMergableQuantity(slot.Itemstack, pslot.Itemstack, EnumMergePriority.DirectMerge);
                        if (mergableq == 0)
                        { return true; }

                        var selfLiqBlock = slot.Itemstack.Collectible as BlockLiquidIrrigationVesselBase;
                        var invLiqBlock = pslot.Itemstack.Collectible as BlockLiquidIrrigationVesselBase;

                        if ((selfLiqBlock?.GetContent(slot.Itemstack)?.StackSize ?? 0) != (invLiqBlock?.GetContent(pslot.Itemstack)?.StackSize ?? 0))
                        { return true; }

                        slot.Itemstack.StackSize += mergableq;
                        pslot.TakeOut(mergableq);
                        slot.MarkDirty();
                        pslot.MarkDirty();
                        return true;
                    });
                }
                return moved;
            }
            else
            {
                var containerStack = slot.Itemstack.Clone();
                containerStack.StackSize = 1;
                var moved = action(containerStack);
                if (moved > 0)
                {
                    slot.TakeOut(1);
                    if ((byEntity as EntityPlayer)?.Player.InventoryManager.TryGiveItemstack(containerStack, true) != true)
                    { this.api.World.SpawnItemEntity(containerStack, byEntity.SidedPos.XYZ); }
                    slot.MarkDirty();
                }
                return moved;
            }
        }

        public override void OnGroundIdle(EntityItem entityItem)
        {
            base.OnGroundIdle(entityItem);
            var world = entityItem.World;
            if (world.Side != EnumAppSide.Server)
            { return; }

            if (entityItem.Swimming && world.Rand.NextDouble() < 0.03)
            { this.TryFillFromBlock(entityItem, entityItem.SidedPos.AsBlockPos); }

            if (entityItem.Swimming && world.Rand.NextDouble() < 0.01)
            {
                var stacks = this.GetContents(world, entityItem.Itemstack);
                if (MealMeshCache.ContentsRotten(stacks))
                {
                    for (var i = 0; i < stacks.Length; i++)
                    {
                        if (stacks[i] != null && stacks[i].StackSize > 0 && stacks[i].Collectible.Code.Path == "rot")
                        { world.SpawnItemEntity(stacks[i], entityItem.ServerPos.XYZ); }
                    }
                    this.SetContent(entityItem.Itemstack, null);
                }
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            this.GetContentInfo(inSlot, dsc, world);
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            string text = base.GetPlacedBlockInfo(world, pos, forPlayer);
            string[] stringSeparators = new string[] { "\r\n" };
            text = text.Split(stringSeparators, StringSplitOptions.None).Last();
            var litres = this.GetCurrentLitres(pos);
            if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer becontainer))
            { return text; }

            var slot = becontainer.Inventory[this.GetContainerSlotId(pos)];
            var contentStack = slot.Itemstack;
            if (litres <= 0)
            {
                text += Lang.Get("Empty");
                return text;
            }

            var incontainername = Lang.Get(contentStack.Collectible.Code.Domain + ":incontainer-" + contentStack.Class.ToString().ToLowerInvariant() + "-" + contentStack.Collectible.Code.Path);
            text += Lang.Get("Contents:") + "\n" + Lang.Get("{0} litres of {1}", litres, incontainername);
            if (litres == 1)
            {
                text += Lang.Get("Contents:") + "\n" + Lang.Get("{0} litre of {1}", litres, incontainername);
            }

            text += PerishableInfoCompact(this.api, slot, 0, false);
            return text;
        }


        public virtual void GetContentInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
        {
            var litres = this.GetCurrentLitres(inSlot.Itemstack);
            var contentStack = this.GetContent(inSlot.Itemstack);
            if (litres <= 0)
            { dsc.Append(Lang.Get("Empty")); }
            else
            {
                var incontainerrname = Lang.Get(contentStack.Collectible.Code.Domain + ":incontainer-" + contentStack.Class.ToString().ToLowerInvariant() + "-" + contentStack.Collectible.Code.Path);
                if (litres == 1)
                { dsc.Append(Lang.Get("{0} litre of {1}", litres, incontainerrname)); }
                else
                { dsc.Append(Lang.Get("{0} litres of {1}", litres, incontainerrname)); }

                var dummyslot = this.GetContentInDummySlot(inSlot, contentStack);
                var states = contentStack.Collectible.UpdateAndGetTransitionStates(this.api.World, dummyslot);
                if (states != null && !dummyslot.Empty)
                {
                    dsc.AppendLine();
                    var nowSpoiling = false;
                    foreach (var state in states)
                    {
                        nowSpoiling |= this.AppendPerishableInfoText(dummyslot, dsc, world, state, nowSpoiling) > 0;
                    }
                }
            }
        }

        public override void TryMergeStacks(ItemStackMergeOperation op)
        {
            op.MovableQuantity = this.GetMergableQuantity(op.SinkSlot.Itemstack, op.SourceSlot.Itemstack, op.CurrentPriority);
            if (op.MovableQuantity == 0)
            { return; }

            if (!op.SinkSlot.CanTakeFrom(op.SourceSlot, op.CurrentPriority))
            { return; }

            var sinkContent = this.GetContent(op.SinkSlot.Itemstack);
            var sourceContent = this.GetContent(op.SourceSlot.Itemstack);
            if (sinkContent == null && sourceContent == null)
            {
                base.TryMergeStacks(op);
                return;
            }

            if (sinkContent == null || sourceContent == null)
            { op.MovableQuantity = 0; return; }

            if (!sinkContent.Equals(op.World, sourceContent, GlobalConstants.IgnoredStackAttributes))
            { op.MovableQuantity = 0; return; }

            var sourceLitres = this.GetCurrentLitres(op.SourceSlot.Itemstack) * op.SourceSlot.StackSize;
            var sinkLitres = this.GetCurrentLitres(op.SinkSlot.Itemstack) * op.SinkSlot.StackSize;

            var sourceCapLitres = op.SourceSlot.StackSize * (op.SourceSlot.Itemstack.Collectible as BlockLiquidIrrigationVesselBase)?.CapacityLitres ?? 0;
            var sinkCapLitres = op.SinkSlot.StackSize * (op.SinkSlot.Itemstack.Collectible as BlockLiquidIrrigationVesselBase)?.CapacityLitres ?? 0;

            // Containers are empty, can do a classic merge
            if (sourceCapLitres == 0 || sinkCapLitres == 0)
            {
                base.TryMergeStacks(op);
                return;
            }

            // Containers are equally full, can do a classic merge
            if (this.GetCurrentLitres(op.SourceSlot.Itemstack) == this.GetCurrentLitres(op.SinkSlot.Itemstack))
            {
                if (op.MovableQuantity > 0)
                {
                    base.TryMergeStacks(op);
                    return;
                }
                op.MovedQuantity = 0;
                return;
            }

            if (op.CurrentPriority == EnumMergePriority.DirectMerge)
            {
                var movableLitres = Math.Min(sinkCapLitres - sinkLitres, sourceLitres);
                var moved = this.TryPutLiquid(op.SinkSlot.Itemstack, sinkContent, movableLitres / op.SinkSlot.StackSize);

                this.DoLiquidMovedEffects(op.ActingPlayer, sinkContent, moved, EnumLiquidDirection.Pour);
                this.TryTakeContent(op.SourceSlot.Itemstack, moved / op.SourceSlot.StackSize);
                op.SourceSlot.MarkDirty();
                op.SinkSlot.MarkDirty();
            }
            op.MovableQuantity = 0;
            return;
        }

        public override bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
        {
            var rprops = ingredient.RecipeAttributes;
            if (rprops?.Exists != true || rprops?["requiresContent"].Exists != true)
            { rprops = gridRecipe.Attributes?["liquidContainerProps"]; }

            if (rprops?.Exists != true)
            { return base.MatchesForCrafting(inputStack, gridRecipe, ingredient); }

            var contentCode = rprops["requiresContent"]["code"].AsString();
            var contentType = rprops["requiresContent"]["type"].AsString();
            var contentStack = this.GetContent(inputStack);
            if (contentStack == null)
            { return false; }

            var litres = rprops["requiresLitres"].AsFloat();
            var props = GetContainableProps(contentStack);
            var q = (int)(props.ItemsPerLitre * litres) / inputStack.StackSize;
            var a = contentStack.Class.ToString().ToLowerInvariant() == contentType.ToLowerInvariant();
            var b = WildcardUtil.Match(new AssetLocation(contentCode), contentStack.Collectible.Code);
            var c = contentStack.StackSize >= q;
            return a && b && c;
        }

        public override void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
        {
            var rprops = fromIngredient.RecipeAttributes;
            if (rprops?.Exists != true || rprops?["requiresContent"].Exists != true)
            { rprops = gridRecipe.Attributes?["liquidContainerProps"]; }

            if (rprops?.Exists != true)
            {
                base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
                return;
            }
            var contentStack = this.GetContent(stackInSlot.Itemstack);
            var litres = rprops["requiresLitres"].AsFloat();
            var props = GetContainableProps(contentStack);
            var q = (int)(props.ItemsPerLitre * litres / stackInSlot.StackSize);
            this.TryTakeContent(stackInSlot.Itemstack, q);
        }

        public static string PerishableInfoCompact(ICoreAPI Api, ItemSlot contentSlot, float ripenRate, bool withStackName = true)
        {
            var dsc = new StringBuilder();

            if (withStackName)
            { dsc.Append(contentSlot.Itemstack.GetName()); }
            var transitionStates = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(Api.World, contentSlot);

            if (transitionStates != null)
            {
                for (var i = 0; i < transitionStates.Length; i++)
                {
                    var comma = ", ";
                    var state = transitionStates[i];
                    var prop = state.Props;
                    var perishRate = contentSlot.Itemstack.Collectible.GetTransitionRateMul(Api.World, contentSlot, prop.Type);

                    if (perishRate <= 0)
                    { continue; }

                    var transitionLevel = state.TransitionLevel;
                    var freshHoursLeft = state.FreshHoursLeft / perishRate;
                    switch (prop.Type)
                    {
                        case EnumTransitionType.Perish:
                            if (transitionLevel > 0)
                            {
                                dsc.Append(comma + Lang.Get("{0}% spoiled", (int)Math.Round(transitionLevel * 100)));
                            }
                            else
                            {
                                double hoursPerday = Api.World.Calendar.HoursPerDay;
                                if (freshHoursLeft / hoursPerday >= Api.World.Calendar.DaysPerYear)
                                {
                                    dsc.Append(comma + Lang.Get("fresh for {0} years", Math.Round(freshHoursLeft / hoursPerday / Api.World.Calendar.DaysPerYear, 1)));
                                }
                                else if (freshHoursLeft > hoursPerday)
                                {
                                    dsc.Append(comma + Lang.Get("fresh for {0} days", Math.Round(freshHoursLeft / hoursPerday, 1)));
                                }
                                else
                                {
                                    dsc.Append(comma + Lang.Get("fresh for {0} hours", Math.Round(freshHoursLeft, 1)));
                                }
                            }
                            break;

                        case EnumTransitionType.Ripen:

                            if (transitionLevel > 0)
                            {
                                dsc.Append(comma + Lang.Get("{1:0.#} days left to ripen ({0}%)", (int)Math.Round(transitionLevel * 100), (state.TransitionHours - state.TransitionedHours) / Api.World.Calendar.HoursPerDay / ripenRate));
                            }
                            else
                            {
                                double hoursPerday = Api.World.Calendar.HoursPerDay;
                                if (freshHoursLeft / hoursPerday >= Api.World.Calendar.DaysPerYear)
                                {
                                    dsc.Append(comma + Lang.Get("will ripen in {0} years", Math.Round(freshHoursLeft / hoursPerday / Api.World.Calendar.DaysPerYear, 1)));
                                }
                                else if (freshHoursLeft > hoursPerday)
                                {
                                    dsc.Append(comma + Lang.Get("will ripen in {0} days", Math.Round(freshHoursLeft / hoursPerday, 1)));
                                }
                                else
                                {
                                    dsc.Append(comma + Lang.Get("will ripen in {0} hours", Math.Round(freshHoursLeft, 1)));
                                }
                            }
                            break;
                    }
                }
            }
            return dsc.ToString();
        }
    }
}
