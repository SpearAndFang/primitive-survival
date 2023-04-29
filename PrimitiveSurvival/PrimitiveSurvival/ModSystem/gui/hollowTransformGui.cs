namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;
    using Vintagestory.API.Util;
    using Vintagestory.Client;

    public class GuiDialogHollowTransform : GuiDialog
    {
        private ModelTransform originalTransform;

        private CollectibleObject oldCollectible;

        private BlockPos oldPos;

        private ModelTransform currentTransform = new ModelTransform();

        private ModelTransform TargetTransform
        {
            get => this.oldCollectible.GetBehavior<BehaviorInTreeHollowTransform>()?.Transform ?? ModelTransform.NoTransform;
            set
            {
                if (!this.oldCollectible.HasBehavior<BehaviorInTreeHollowTransform>())
                {
                    this.oldCollectible.CollectibleBehaviors = this.oldCollectible.CollectibleBehaviors.Append(new BehaviorInTreeHollowTransform(this.oldCollectible));
                }
                this.oldCollectible.GetBehavior<BehaviorInTreeHollowTransform>().Transform = value;
            }
        }

        public override string ToggleKeyCombinationCode => null;

        public override bool PrefersUngrabbedMouse => true;

        public GuiDialogHollowTransform(ICoreClientAPI capi) : base(capi)
        {
            this.capi = capi;
            capi.ChatCommands.Create("hollowTfEdit")
                .WithDescription("Edit the model transform for the item displayed in the currently targeted Tree Hollow.")
                .HandleWith(this.OnCommandTransformEditor)
                .RequiresPrivilege(Privilege.useblock)
                .Validate();
        }

        private TextCommandResult OnCommandTransformEditor(TextCommandCallingArgs args)
        {
            if (this.capi.World.Player.CurrentBlockSelection?.Position is BlockPos blockPos
                && this.capi.World.BlockAccessor.GetBlockEntity(blockPos) is BETreeHollowGrown treeHollowGrown
                && treeHollowGrown.GetDisplayedCollectible() is CollectibleObject collectible)
            {
                this.currentTransform = new ModelTransform
                {
                    Rotation = new Vec3f(),
                    Translation = new Vec3f()
                };
                this.oldCollectible = collectible;
                this.oldPos = blockPos;
                this.originalTransform = this.TargetTransform;
                this.TargetTransform = this.currentTransform = this.originalTransform.Clone();
                this.TryOpen();
                return TextCommandResult.Success("Opened Tree Hollow Transform dialog.");
            }
            return TextCommandResult.Error("Must be targeting a Tree Hollow that contains an item to use that command.");
        }

        public override void OnGuiOpened()
        {
            this.ComposeDialog();
        }

        private void ComposeDialog()
        {
            this.ClearComposers();
            var line = ElementBounds.Fixed(0.0, 22.0, 500.0, 20.0);
            var inputBnds = ElementBounds.Fixed(0.0, 11.0, 230.0, 30.0);
            var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftTop).WithFixedAlignmentOffset(110.0 + GuiStyle.DialogToScreenPadding, GuiStyle.DialogToScreenPadding);
            var textAreaBounds = ElementBounds.FixedSize(500.0, 200.0);
            var clippingBounds = ElementBounds.FixedSize(500.0, 200.0);
            var btnBounds = ElementBounds.FixedSize(200.0, 20.0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0);

            this.SingleComposer = this.capi.Gui.CreateCompo("transformeditor", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar("Transform Editor (Tree Hollows)", OnTitleBarClose)
                .BeginChildElements(bgBounds)
                .AddStaticText("Translation X", CairoFont.WhiteDetailText(), line = line.FlatCopy().WithFixedWidth(230.0))
                .AddNumberInput(inputBnds = inputBnds.BelowCopy(), this.OnTranslateX, CairoFont.WhiteDetailText(), "translatex")
                .AddStaticText("Origin X", CairoFont.WhiteDetailText(), line.RightCopy(40.0))
                .AddNumberInput(inputBnds.RightCopy(40.0), this.OnOriginX, CairoFont.WhiteDetailText(), "originx")
                .AddStaticText("Translation Y", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 33.0))
                .AddNumberInput(inputBnds = inputBnds.BelowCopy(0.0, 22.0), this.OnTranslateY, CairoFont.WhiteDetailText(), "translatey")
                .AddStaticText("Origin Y", CairoFont.WhiteDetailText(), line.RightCopy(40.0))
                .AddNumberInput(inputBnds.RightCopy(40.0), this.OnOriginY, CairoFont.WhiteDetailText(), "originy")
                .AddStaticText("Translation Z", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 32.0))
                .AddNumberInput(inputBnds = inputBnds.BelowCopy(0.0, 22.0), this.OnTranslateZ, CairoFont.WhiteDetailText(), "translatez")
                .AddStaticText("Origin Z", CairoFont.WhiteDetailText(), line.RightCopy(40.0))
                .AddNumberInput(inputBnds.RightCopy(40.0), this.OnOriginZ, CairoFont.WhiteDetailText(), "originz")
                .AddStaticText("Rotation X", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 33.0).WithFixedWidth(500.0))
                .AddSlider(this.OnRotateX, inputBnds = inputBnds.BelowCopy(0.0, 22.0).WithFixedWidth(500.0), "rotatex")
                .AddStaticText("Rotation Y", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 32.0))
                .AddSlider(this.OnRotateY, inputBnds = inputBnds.BelowCopy(0.0, 22.0), "rotatey")
                .AddStaticText("Rotation Z", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 32.0))
                .AddSlider(this.OnRotateZ, inputBnds = inputBnds.BelowCopy(0.0, 22.0), "rotatez")
                .AddStaticText("Scale", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 32.0))
                .AddSlider(this.OnScale, inputBnds = inputBnds.BelowCopy(0.0, 22.0), "scale")
                .AddSwitch(this.onFlipXAxis, inputBnds = inputBnds.BelowCopy(0.0, 10.0), "flipx", 20.0)
                .AddStaticText("Flip on X-Axis", CairoFont.WhiteDetailText(), inputBnds.RightCopy(10.0, 1.0).WithFixedWidth(200.0))
                .AddStaticText("Json Code (added as a behavior to the collectible)", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 72.0))
                .BeginClip(clippingBounds.FixedUnder(inputBnds, 37.0))
                .AddTextArea(textAreaBounds, null, CairoFont.WhiteSmallText(), "textarea")
                .EndClip()
                .AddSmallButton("Close & Apply", this.OnApplyJson, btnBounds = btnBounds.FlatCopy().FixedUnder(clippingBounds, 15.0))
                .AddSmallButton("Copy JSON", this.OnCopyJson, btnBounds = btnBounds.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
                .EndChildElements()
                .Compose();
            this.SingleComposer.GetTextInput("translatex").SetValue(this.currentTransform.Translation.X.ToString(GlobalConstants.DefaultCultureInfo));
            this.SingleComposer.GetTextInput("translatey").SetValue(this.currentTransform.Translation.Y.ToString(GlobalConstants.DefaultCultureInfo));
            this.SingleComposer.GetTextInput("translatez").SetValue(this.currentTransform.Translation.Z.ToString(GlobalConstants.DefaultCultureInfo));
            this.SingleComposer.GetTextInput("originx").SetValue(this.currentTransform.Origin.X.ToString(GlobalConstants.DefaultCultureInfo));
            this.SingleComposer.GetTextInput("originy").SetValue(this.currentTransform.Origin.Y.ToString(GlobalConstants.DefaultCultureInfo));
            this.SingleComposer.GetTextInput("originz").SetValue(this.currentTransform.Origin.Z.ToString(GlobalConstants.DefaultCultureInfo));
            this.SingleComposer.GetSlider("rotatex").SetValues((int)this.currentTransform.Rotation.X, -180, 180, 1);
            this.SingleComposer.GetSlider("rotatey").SetValues((int)this.currentTransform.Rotation.Y, -180, 180, 1);
            this.SingleComposer.GetSlider("rotatez").SetValues((int)this.currentTransform.Rotation.Z, -180, 180, 1);
            this.SingleComposer.GetSlider("scale").SetValues((int)Math.Abs(100f * this.currentTransform.ScaleXYZ.X), 25, 600, 1);
            this.SingleComposer.GetSwitch("flipx").On = this.currentTransform.ScaleXYZ.X < 0f;
        }

        private void onFlipXAxis(bool on)
        {
            this.currentTransform.ScaleXYZ.X *= -1f;
            this.updateJson();
        }

        private void OnOriginX(string val)
        {
            this.currentTransform.Origin.X = val.ToFloat();
            this.updateJson();
        }

        private void OnOriginY(string val)
        {
            this.currentTransform.Origin.Y = val.ToFloat();
            this.updateJson();
        }

        private void OnOriginZ(string val)
        {
            this.currentTransform.Origin.Z = val.ToFloat();
            this.updateJson();
        }

        private bool OnApplyJson()
        {
            this.TargetTransform = this.originalTransform = this.currentTransform;
            this.currentTransform = null;
            this.TryClose();
            return true;
        }

        private bool OnCopyJson()
        {
            ScreenManager.Platform.XPlatInterface.SetClipboardText(this.getJson());
            return true;
        }

        private void updateJson()
        {
            this.SingleComposer.GetTextArea("textarea").SetValue(this.getJson());
            this.capi.World.BlockAccessor.MarkBlockDirty(this.oldPos);
        }

        private string getJson()
        {
            var json = new StringBuilder();
            var def = ModelTransform.NoTransform;
            var indent = "\t";
            json.Append("{\n");
            json.Append(indent + "\"name\": \"intreeHollowTransform\",\n");
            json.Append(indent + "\"properties\":\n");
            json.Append(indent + "{\n");
            indent = "\t\t";
            var added = false;
            if (this.currentTransform.Translation.X != def.Translation.X || this.currentTransform.Translation.Y != def.Translation.Y || this.currentTransform.Translation.Z != def.Translation.Z)
            {
                json.Append(string.Format(GlobalConstants.DefaultCultureInfo, indent + "translation: {{ x: {0}, y: {1}, z: {2} }}", this.currentTransform.Translation.X, this.currentTransform.Translation.Y, this.currentTransform.Translation.Z));
                added = true;
            }
            if (this.currentTransform.Rotation.X != def.Rotation.X || this.currentTransform.Rotation.Y != def.Rotation.Y || this.currentTransform.Rotation.Z != def.Rotation.Z)
            {
                if (added)
                {
                    json.Append(",\n");
                }
                json.Append(string.Format(GlobalConstants.DefaultCultureInfo, indent + "rotation: {{ x: {0}, y: {1}, z: {2} }}", this.currentTransform.Rotation.X, this.currentTransform.Rotation.Y, this.currentTransform.Rotation.Z));
                added = true;
            }
            if (this.currentTransform.Origin.X != def.Origin.X || this.currentTransform.Origin.Y != def.Origin.Y || this.currentTransform.Origin.Z != def.Origin.Z)
            {
                if (added)
                {
                    json.Append(",\n");
                }
                json.Append(string.Format(GlobalConstants.DefaultCultureInfo, indent + "origin: {{ x: {0}, y: {1}, z: {2} }}", this.currentTransform.Origin.X, this.currentTransform.Origin.Y, this.currentTransform.Origin.Z));
                added = true;
            }
            if (this.currentTransform.ScaleXYZ.X != def.ScaleXYZ.X)
            {
                if (added)
                {
                    json.Append(",\n");
                }
                if (this.currentTransform.ScaleXYZ.X != this.currentTransform.ScaleXYZ.Y || this.currentTransform.ScaleXYZ.X != this.currentTransform.ScaleXYZ.Z)
                {
                    json.Append(string.Format(GlobalConstants.DefaultCultureInfo, indent + "scaleXyz: {{ x: {0}, y: {1}, z: {2} }}", this.currentTransform.ScaleXYZ.X, this.currentTransform.ScaleXYZ.Y, this.currentTransform.ScaleXYZ.Z));
                }
                else
                {
                    json.Append(string.Format(GlobalConstants.DefaultCultureInfo, indent + "scale: {0}", this.currentTransform.ScaleXYZ.X));
                }
            }
            indent = "\t";
            json.Append("\n");
            json.Append(indent + "}\n");
            json.Append("}");
            var jsonstr = json.ToString();
            var tree = new TreeAttribute();
            tree.SetString("json", jsonstr);
            return tree.GetString("json");
        }

        private bool OnScale(int val)
        {
            this.currentTransform.Scale = val / 100f;
            if (this.SingleComposer.GetSwitch("flipx").On)
            {
                this.currentTransform.ScaleXYZ.X *= -1f;
            }
            this.updateJson();
            return true;
        }

        private bool OnRotateX(int deg)
        {
            this.currentTransform.Rotation.X = deg;
            this.updateJson();
            return true;
        }

        private bool OnRotateY(int deg)
        {
            this.currentTransform.Rotation.Y = deg;
            this.updateJson();
            return true;
        }

        private bool OnRotateZ(int deg)
        {
            this.currentTransform.Rotation.Z = deg;
            this.updateJson();
            return true;
        }

        private void OnTranslateX(string val)
        {
            this.currentTransform.Translation.X = val.ToFloat();
            this.updateJson();
        }

        private void OnTranslateY(string val)
        {
            this.currentTransform.Translation.Y = val.ToFloat();
            this.updateJson();
        }

        private void OnTranslateZ(string val)
        {
            this.currentTransform.Translation.Z = val.ToFloat();
            this.updateJson();
        }

        private void OnTitleBarClose()
        {
            this.TryClose();
        }

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
            if (this.oldCollectible != null)
            {
                this.TargetTransform = this.originalTransform;
            }
            this.capi.World.BlockAccessor.MarkBlockDirty(this.oldPos);
        }

        public override void OnMouseWheel(MouseWheelEventArgs args)
        {
            base.OnMouseWheel(args);
            args.SetHandled();
        }
    }
}
