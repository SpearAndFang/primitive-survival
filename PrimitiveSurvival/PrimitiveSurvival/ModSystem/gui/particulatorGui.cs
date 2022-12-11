namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Cairo;
    using Vintagestory.API.Client;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Config;
    using Vintagestory.API.Util;
    using PrimitiveSurvival.ModConfig;
    //using System.Diagnostics;

    public class GuiDialogParticulator : GuiDialogGeneric
    {


        public BEParticleData particleData = new BEParticleData();


        public BlockPos blockEntityPos;


        private string currentTab;

        private readonly int guiWidth = 425;
        private string res;

        private readonly int maxParticlesQuantity = ModConfig.Loaded.ParticulatorMaxParticlesQuantity;
        private readonly int maxParticlesSize = ModConfig.Loaded.ParticulatorMaxParticlesSize;
        private readonly bool hideCodeTabs = ModConfig.Loaded.ParticulatorHideCodeTabs;
        private string[] tabs;

        private string selDesc;

        public GuiDialogParticulator(BlockPos blockEntityPos, ICoreClientAPI capi) : base("primitivesurvival:particulator-guititle", capi)
        {
            this.blockEntityPos = blockEntityPos;
        }


        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            if (this.selDesc == null)
            { this.selDesc = ""; }
            this.particleData.selectedParticle = "";
            if (this.hideCodeTabs)
            {
                this.tabs = new string[5] { "Main", "Page 2", "Page 3", "Page 4", "Help" };
            }
            else
            {
                this.tabs = new string[8] { "Main", "Page 2", "Page 3", "Page 4", "Code", "Help", "C#", "Particulate" }; //JSON removed
            }
            this.Compose();
        }


        private void Compose()
        {
            this.ClearComposers();

            var particleCodes = new List<string>(new string[] { "",
                "Dripping Water",
                "Default",
                "Fireworks (launcher)",
                "Fireworks (explosion 1)",
                "Fireworks (explosion 2)",
                "Fireworks (explosion 3)",
                "Flame (Small)",
                "Flame (Medium)",
                "Flame (Large)",
                "Fountain",
                "Geyser",
                "Laser",
                "Popping Candy",
                "Smoke",
                "Sparks",
                "Spirit Magic (rising)",
                "Spirit Magic (falling)",
                "Stationary Cube",
                "Unstable",
                "Waterfall"});

            particleCodes.Sort();
            var transformCodes = new List<string>(new string[] { "Identical", "Linear", "Linearnullify", "Linearreduce", "Linearincrease", "Quadratic", "Inverselinear", "Root", "Sinus", "Clampedpositivesinus", "Cosinus", "Smoothstep" });
            var modelCodes = new List<string>(new string[] { "Cube", "Quad" });
            var particleTypes = new List<string>(new string[] { "Primary", "Secondary", "Death" });

            var toggleButtonBarBounds = ElementBounds
                .Fixed(0, 30, this.guiWidth, 76)
                .WithFixedPadding(GuiStyle.ElementToDialogPadding);

            var toggleButtonBounds = ElementBounds.Fixed(10, 40, 160, 40).WithFixedPadding(0, 0);
            var codeToggleButtonBounds = ElementBounds.Fixed(20, 106, 150, 40).WithFixedPadding(0, 0);

            var textBounds = ElementBounds.Fixed(15, 60, this.guiWidth - 130, 30);
            var borderBounds = ElementBounds.Fixed(0, 30, this.guiWidth, 340);
            var particleDropDownBounds = ElementBounds.Fixed(0, 0, 200, 28).FixedUnder(textBounds, 0);
            var transformDropDownBounds = ElementBounds.Fixed(10, 0, 250, 28).FixedUnder(textBounds, 0);
            var modelDropDownBounds = ElementBounds.Fixed(10, 0, 100, 28).FixedUnder(textBounds, 0);
            var particleTypeDropDownBounds = ElementBounds.Fixed(10, 0, 150, 28).FixedUnder(textBounds, 0);
            var areaTextBounds = ElementBounds.Fixed(0, 30, 300, 25).FixedUnder(particleDropDownBounds, 0);

            var selDescTextBounds = ElementBounds.Fixed(35, 180, this.guiWidth + 10, 80);
            var selNameBounds = ElementBounds.Fixed(33, 320, 250, 28);

            var closeButtonBounds = ElementBounds
                .FixedSize(0, 0)
                .WithAlignment(EnumDialogArea.RightFixed)
                .WithFixedPadding(20, 4);

            var copyButtonBounds = ElementBounds
                .FixedSize(0, 0)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(20, 4);

            // scrollbar related
            var outputTextBounds = ElementBounds.Fixed(5, 130, this.guiWidth + 10, 550);
            var clipBounds = outputTextBounds.ForkBoundingParent();
            var insetBounds = outputTextBounds.FlatCopy().FixedGrow(6).WithFixedOffset(-3, -3);
            var scrollbarBounds = clipBounds.CopyOffsetedSibling(outputTextBounds.fixedWidth + 7, -3, 0, 6).WithFixedWidth(20);
            double listHeight = 500;
            float posY = 0;

            var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding)
            .WithSizing(ElementSizing.FitToChildren)
            .WithChildren(toggleButtonBarBounds, particleDropDownBounds, transformDropDownBounds,
            modelDropDownBounds, borderBounds, textBounds, copyButtonBounds, closeButtonBounds);

            var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(-20, 0);

            ElementBounds tmpBounds;
            ElementBounds bottomBounds;

            this.SingleComposer = this.capi.Gui
                .CreateCompo("particleconfig", dialogBounds)
                .AddShadedDialogBG(bgBounds, true)

                .AddStaticCustomDraw(toggleButtonBarBounds, delegate (Context ctx, ImageSurface surface, ElementBounds bounds)
                {
                    ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
                    GuiElement.RoundRectangle(ctx, GuiElement.scaled(5.0) + bounds.bgDrawX - 20, GuiElement.scaled(3.0) + bounds.bgDrawY - 20, bounds.OuterWidth - GuiElement.scaled(10.0) + 40, GuiElement.scaled(50.0), 1.0);
                    ctx.Fill();
                })
                .AddDialogTitleBar(Lang.Get(this.DialogTitle), this.OnTitleBarClose);

            var count = 1;
            foreach (var settingGroupName in this.tabs)
            {
                if (string.IsNullOrEmpty(this.currentTab))
                {
                    this.currentTab = settingGroupName;
                }
                var buttonFont = CairoFont.WhiteSmallText();
                var textExtents = buttonFont.GetTextExtents(settingGroupName);
                var width = (textExtents.Width / RuntimeEnv.GUIScale) + 30.0;
                if (count <= 6)
                {
                    this.SingleComposer.AddToggleButton(
                    text: settingGroupName,
                    font: buttonFont,
                    onToggle: isSelected => this.OnTabToggle(settingGroupName),
                    bounds: toggleButtonBounds.WithFixedWidth(width),
                    key: settingGroupName + "-selected");
                    toggleButtonBounds = toggleButtonBounds.RightCopy(0, 0);
                }

                else if (this.currentTab == "Code" || this.currentTab == "C#" || this.currentTab == "JSON" || this.currentTab == "Particulate")
                {
                    this.SingleComposer.AddToggleButton(
                        text: settingGroupName,
                        font: buttonFont,
                        onToggle: isSelected => this.OnTabToggle(settingGroupName),
                        bounds: codeToggleButtonBounds.WithFixedWidth(width),
                        key: settingGroupName + "-selected");
                    codeToggleButtonBounds = codeToggleButtonBounds.RightCopy(0, 0);
                }
                count++;
            }

            this.SingleComposer.GetToggleButton(this.currentTab + "-selected").SetValue(true);
            if (this.currentTab == "Code")
            {
                this.SingleComposer.GetToggleButton("C#-selected").SetValue(true);
            }
            else if (this.currentTab == "C#" || this.currentTab == "JSON" || this.currentTab == "Particulate")
            {
                this.SingleComposer.GetToggleButton("Code-selected").SetValue(true);
            }

            if (this.currentTab == "Main")
            {
                this.SingleComposer.AddStaticText("Select a predefined particle", CairoFont.WhiteDetailText(), particleDropDownBounds = textBounds.BelowCopy(0, 0))
                .AddDropDown(particleCodes.ToArray(), particleCodes.ToArray(), 0, this.DidSelectParticle, particleDropDownBounds.BelowCopy(0, 0), "selectedParticle")
                //.AddTextArea(selDescTextBounds, this.OnSelDescText, CairoFont.WhiteSmallText(), "selDescText")
                .AddDynamicText("", CairoFont.WhiteSmallText(), selDescTextBounds, "selDescText")

                .AddStaticText("Name", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(0, 180))
                .AddTextInput(selNameBounds, this.OnSelName, CairoFont.WhiteSmallText(), "name")


                .AddSwitch(this.OnToggleParticleEnabled, tmpBounds = textBounds.BelowCopy(0, 270), "particleEnabled", 20)
                .AddStaticText("Enabled", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 270))

                .AddStaticText("Particle Type", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(0, 320))
                .AddDropDown(particleTypes.ToArray(), particleTypes.ToArray(), 0, this.DidSelectParticleType, particleTypeDropDownBounds.BelowCopy(20, 340), "particleType");

                if (this.particleData.particleType != "Death")
                {
                    this.SingleComposer.AddStaticText("Interval", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(0, 400))
                    .AddNumberInput(tmpBounds = tmpBounds.BelowCopy(0, 0).WithFixedSize(60, 29), this.OnParticleInterval, CairoFont.WhiteDetailText(), "particleInterval");
                }
                else
                {
                    this.SingleComposer.AddSwitch(this.OnToggleRandomParticleColors, tmpBounds = textBounds.BelowCopy(0, 400), "randomParticleColors", 20)
                    .AddStaticText("Randomize Individual Particle Colors", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 400));
                }

                if (this.particleData.particleType == "Primary")
                {
                    this.SingleComposer.AddSwitch(this.OnTimerLoop, tmpBounds = textBounds.BelowCopy(0, 490), "timerLoop", 20)
                    .AddStaticText("Loop", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 490))

                    .AddStaticText("Pre-Delay / Duration / Post-Delay", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(0, 540))
                    .AddNumberInput(tmpBounds = tmpBounds.BelowCopy(0, 0).WithFixedSize(60, 29), this.OnTimerPreDelay, CairoFont.WhiteDetailText(), "timerPreDelay")
                    .AddNumberInput(tmpBounds = tmpBounds.RightCopy(12, 0).WithFixedSize(60, 29), this.OnTimerDuration, CairoFont.WhiteDetailText(), "timerDuration")
                    .AddNumberInput(tmpBounds = tmpBounds.RightCopy(12, 0).WithFixedSize(60, 29), this.OnTimerPostDelay, CairoFont.WhiteDetailText(), "timerPostDelay");
                }

                this.SingleComposer.AddStaticText("", CairoFont.WhiteDetailText(), bottomBounds = borderBounds.BelowCopy(0, 0));
            }
            else if (this.currentTab == "Page 2")
            {

                this.SingleComposer.AddStaticText("Position (Min/Add)", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(0, 0))
                .AddStaticText("X", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.BelowCopy(0, 7).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnMinPosX, CairoFont.WhiteDetailText(), "minPosX")
                .AddStaticText("Y", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.RightCopy(10, 7).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnMinPosY, CairoFont.WhiteDetailText(), "minPosY")
                .AddStaticText("Z", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.RightCopy(10, 7).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnMinPosZ, CairoFont.WhiteDetailText(), "minPosZ")

                .AddStaticText("X", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(0, 80).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnAddPosX, CairoFont.WhiteDetailText(), "addPosX")
                .AddStaticText("Y", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.RightCopy(10, 7).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnAddPosY, CairoFont.WhiteDetailText(), "addPosY")
                .AddStaticText("Z", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.RightCopy(10, 7).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnAddPosZ, CairoFont.WhiteDetailText(), "addPosZ")

                .AddStaticText("Velocity (Min/Add)", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(0, 140))
                .AddStaticText("X", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.BelowCopy(0, 7).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnMinVelocityX, CairoFont.WhiteDetailText(), "minVelocityX")
                .AddStaticText("Y", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.RightCopy(10, 7).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnMinVelocityY, CairoFont.WhiteDetailText(), "minVelocityY")
                .AddStaticText("Z", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.RightCopy(10, 7).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnMinVelocityZ, CairoFont.WhiteDetailText(), "minVelocityZ")

                .AddStaticText("X", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(0, 210).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnAddVelocityX, CairoFont.WhiteDetailText(), "addVelocityX")
                .AddStaticText("Y", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.RightCopy(10, 7).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnAddVelocityY, CairoFont.WhiteDetailText(), "addVelocityY")
                .AddStaticText("Z", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.RightCopy(10, 7).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnAddVelocityZ, CairoFont.WhiteDetailText(), "addVelocityZ")

                .AddStaticText("Color", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(0, 270))
                .AddStaticText("A", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.BelowCopy(0, 7).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnColorA, CairoFont.WhiteDetailText(), "colorA")
                .AddStaticText("R", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.RightCopy(10, 7).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnColorR, CairoFont.WhiteDetailText(), "colorR")
                .AddStaticText("G", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.RightCopy(10, 7).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnColorG, CairoFont.WhiteDetailText(), "colorG")
                .AddStaticText("B", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.RightCopy(10, 7).WithFixedSize(20, 29))
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnColorB, CairoFont.WhiteDetailText(), "colorB")

                .AddSwitch(this.OnToggleRandomColor, tmpBounds = textBounds.BelowCopy(0, 350), "toggleRandomColor", 20)
                .AddStaticText("Color (Range)", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 350));

                if (this.particleData.toggleRandomColor)
                {
                    this.SingleComposer.AddStaticText("A", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(0, 390).WithFixedSize(20, 29))
                    .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnColorRndA, CairoFont.WhiteDetailText(), "colorRndA")
                    .AddStaticText("R", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.RightCopy(10, 7).WithFixedSize(20, 29))
                    .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnColorRndR, CairoFont.WhiteDetailText(), "colorRndR")
                    .AddStaticText("G", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.RightCopy(10, 7).WithFixedSize(20, 29))
                    .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnColorRndG, CairoFont.WhiteDetailText(), "colorRndG")
                    .AddStaticText("B", CairoFont.WhiteDetailText(), tmpBounds = tmpBounds.RightCopy(10, 7).WithFixedSize(20, 29))
                    .AddNumberInput(tmpBounds = tmpBounds.RightCopy(5, -7).WithFixedSize(60, 29), this.OnColorRndB, CairoFont.WhiteDetailText(), "colorRndB");
                }

                this.SingleComposer.AddStaticText("Quantity (Range)", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(0, 450))
                .AddNumberInput(tmpBounds = tmpBounds.BelowCopy(0, 0).WithFixedSize(60, 29), this.OnMinQuantity, CairoFont.WhiteDetailText(), "minQuantity")
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(10, 0).WithFixedSize(60, 29), this.OnMaxQuantity, CairoFont.WhiteDetailText(), "maxQuantity");

                this.SingleComposer.AddStaticText("Life Length (Range)", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(200, 450))
                .AddNumberInput(tmpBounds = tmpBounds.BelowCopy(0, 0).WithFixedSize(60, 29), this.OnLifeLength, CairoFont.WhiteDetailText(), "lifeLength")
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(10, 0).WithFixedSize(60, 29), this.OnMaxLifeLength, CairoFont.WhiteDetailText(), "maxLifeLength")

                .AddStaticText("Gravity Effect (Range)", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(0, 530))
                .AddNumberInput(tmpBounds = tmpBounds.BelowCopy(0, 0).WithFixedSize(60, 29), this.OnGravityEffect, CairoFont.WhiteDetailText(), "gravityEffect")
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(10, 0).WithFixedSize(60, 29), this.OnMaxGravityEffect, CairoFont.WhiteDetailText(), "maxGravityEffect")

                 .AddStaticText("Size (Range)", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(200, 530))
                .AddNumberInput(tmpBounds = tmpBounds.BelowCopy(0, 0).WithFixedSize(60, 29), this.OnMinSize, CairoFont.WhiteDetailText(), "minSize")
                .AddNumberInput(tmpBounds = tmpBounds.RightCopy(10, 0).WithFixedSize(60, 29), this.OnMaxSize, CairoFont.WhiteDetailText(), "maxSize");

                this.SingleComposer.AddStaticText("", CairoFont.WhiteDetailText(), bottomBounds = borderBounds.BelowCopy(0, 0));

            }
            else if (this.currentTab == "Page 3")
            {
                this.SingleComposer.AddSwitch(this.OnToggleSizeTransform, tmpBounds = textBounds.BelowCopy(0, 0), "toggleSizeTransform", 20)
                .AddStaticText("Size Transform/Evolve", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 0));

                if (this.particleData.toggleSizeTransform)
                {
                    this.SingleComposer.AddDropDown(transformCodes.ToArray(), transformCodes.ToArray(), 0, this.DidSelectSizeTransform, transformDropDownBounds.BelowCopy(0, 0), "sizeTransform")
                    .AddNumberInput(tmpBounds = textBounds.BelowCopy(270, 30).WithFixedSize(60, 29), this.OnSizeEvolve, CairoFont.WhiteDetailText(), "sizeEvolve");
                }

                this.SingleComposer.AddSwitch(this.OnToggleOpacityTransform, tmpBounds = textBounds.BelowCopy(0, 80), "toggleOpacityTransform", 20)
                 .AddStaticText("Opacity Transform/Evolve (quads only)", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 80));

                if (this.particleData.toggleOpacityTransform)
                {
                    this.SingleComposer.AddDropDown(transformCodes.ToArray(), transformCodes.ToArray(), 0, this.DidSelectOpacityTransform, transformDropDownBounds.BelowCopy(0, 80), "opacityTransform")
                    .AddNumberInput(tmpBounds = textBounds.BelowCopy(270, 110).WithFixedSize(60, 29), this.OnOpacityEvolve, CairoFont.WhiteDetailText(), "opacityEvolve");
                }

                this.SingleComposer.AddSwitch(this.OnToggleRedTransform, tmpBounds = textBounds.BelowCopy(0, 170), "toggleRedTransform", 20)
                .AddStaticText("Red Transform/Evolve", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 170));

                if (this.particleData.toggleRedTransform)
                {
                    this.SingleComposer.AddDropDown(transformCodes.ToArray(), transformCodes.ToArray(), 0, this.DidSelectRedTransform, transformDropDownBounds.BelowCopy(0, 170), "redTransform")
                    .AddNumberInput(tmpBounds = textBounds.BelowCopy(270, 200).WithFixedSize(60, 29), this.OnRedEvolve, CairoFont.WhiteDetailText(), "redEvolve");
                }

                this.SingleComposer.AddSwitch(this.OnToggleGreenTransform, tmpBounds = textBounds.BelowCopy(0, 250), "toggleGreenTransform", 20)
                .AddStaticText("Green Transform/Evolve", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 250));

                if (this.particleData.toggleGreenTransform)
                {
                    this.SingleComposer.AddDropDown(transformCodes.ToArray(), transformCodes.ToArray(), 0, this.DidSelectGreenTransform, transformDropDownBounds.BelowCopy(0, 250), "greenTransform")
                    .AddNumberInput(tmpBounds = textBounds.BelowCopy(270, 280).WithFixedSize(60, 29), this.OnGreenEvolve, CairoFont.WhiteDetailText(), "greenEvolve");
                }

                this.SingleComposer.AddSwitch(this.OnToggleBlueTransform, tmpBounds = textBounds.BelowCopy(0, 330), "toggleBlueTransform", 20)
                .AddStaticText("Blue Transform/Evolve", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 330));

                if (this.particleData.toggleBlueTransform)
                {
                    this.SingleComposer.AddDropDown(transformCodes.ToArray(), transformCodes.ToArray(), 0, this.DidSelectBlueTransform, transformDropDownBounds.BelowCopy(0, 330), "blueTransform")
                    .AddNumberInput(tmpBounds = textBounds.BelowCopy(270, 360).WithFixedSize(60, 29), this.OnBlueEvolve, CairoFont.WhiteDetailText(), "blueEvolve");
                }

                this.SingleComposer.AddSwitch(this.OnToggleVertexFlags, tmpBounds = textBounds.BelowCopy(0, 420), "toggleVertexFlags", 20)
                .AddStaticText("Vertex Flags (Glow)", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 420));

                if (this.particleData.toggleVertexFlags)
                {

                    this.SingleComposer.AddNumberInput(tmpBounds = textBounds.BelowCopy(0, 450).WithFixedSize(60, 29), this.OnVertexFlags, CairoFont.WhiteDetailText(), "vertexFlags");
                }

                this.SingleComposer.AddStaticText("Model", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(0, 510))
                .AddDropDown(modelCodes.ToArray(), modelCodes.ToArray(), 0, this.DidSelectModel, modelDropDownBounds.BelowCopy(0, 510), "model");

                this.SingleComposer.AddStaticText("", CairoFont.WhiteDetailText(), bottomBounds = borderBounds.BelowCopy(0, 0));
            }
            else if (this.currentTab == "Page 4")
            {

                this.SingleComposer.AddSwitch(this.OnToggleSelfPropelled, tmpBounds = textBounds.BelowCopy(0, 0), "selfPropelled", 20)
                .AddStaticText("Self Propelled", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 0))

                .AddSwitch(this.OnToggleShouldDieInAir, tmpBounds = textBounds.BelowCopy(0, 40), "shouldDieInAir", 20)
                .AddStaticText("Should Die In Air", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 40))

                .AddSwitch(this.OnToggleShouldDieInLiquid, tmpBounds = textBounds.BelowCopy(0, 80), "shouldDieInLiquid", 20)
                .AddStaticText("Should Die In Liquid", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 80))

                .AddSwitch(this.OnToggleUseBlockColor, tmpBounds = textBounds.BelowCopy(0, 120), "useBlockColor", 20)
                .AddStaticText("Use Colors from Linked Block", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 120))

                .AddSwitch(this.OnToggleWindAffected, tmpBounds = textBounds.BelowCopy(0, 160), "windAffected", 20)
                .AddStaticText("Wind Affected/Affectedness", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 160));

                if (this.particleData.windAffected)
                {
                    this.SingleComposer.AddNumberInput(tmpBounds = textBounds.BelowCopy(0, 190).WithFixedSize(60, 29), this.OnWindAffectednes, CairoFont.WhiteDetailText(), "windAffectednes");
                }


                this.SingleComposer.AddSwitch(this.OnToggleMainThread, tmpBounds = textBounds.BelowCopy(0, 250), "mainThread", 20)
                .AddStaticText("Run in Main Thread", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 250));

                if (!this.particleData.mainThread)
                {
                    this.SingleComposer.AddSwitch(this.OnToggleNoTerrainCollision, tmpBounds = textBounds.BelowCopy(0, 290), "noTerrainCollision", 20)
                    .AddStaticText("Disable Terrain Collision", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 290))

                    .AddSwitch(this.OnToggleShouldSwimOnLiquid, tmpBounds = textBounds.BelowCopy(0, 330), "shouldSwimOnLiquid", 20)
                    .AddStaticText("Should Swim on Liquid", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 330))

                    .AddSwitch(this.OnToggleBouncy, tmpBounds = textBounds.BelowCopy(0, 370), "bouncy", 20)
                    .AddStaticText("Bouncy", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 370))

                    .AddSwitch(this.OnToggleRandomVelocityChange, tmpBounds = textBounds.BelowCopy(0, 410), "randomVelocityChange", 20)
                    .AddStaticText("Random Velocity Change", CairoFont.WhiteDetailText(), tmpBounds = textBounds.BelowCopy(40, 410));
                }
                this.SingleComposer.AddStaticText("", CairoFont.WhiteDetailText(), bottomBounds = borderBounds.BelowCopy(0, 0));
            }
            else if (this.currentTab == "Help")
            {
                this.SingleComposer.BeginChildElements(bgBounds)
                    .BeginClip(clipBounds)
                        .AddInset(insetBounds, 3)
                        .AddRichtext(Lang.Get("primitivesurvival:particulator-guide"), CairoFont.WhiteSmallText(), outputTextBounds, "prichtext")
                    .EndClip()
                    .AddVerticalScrollbar(this.OnNewScrollbarvalueDetailPage, scrollbarBounds, "scrollbar")
                .EndChildElements();

                this.SingleComposer.AddStaticText("", CairoFont.WhiteDetailText(), bottomBounds = borderBounds.BelowCopy(0, 0));
                this.SingleComposer.AddStaticText("Quick Start Guide", CairoFont.WhiteSmallText(), textBounds.BelowCopy(-10, 0));

            }
            else //Code
            {
                this.res = this.GetCode(this.currentTab, this.particleData);
                this.SingleComposer.BeginChildElements(bgBounds)
                   .BeginClip(clipBounds)
                       .AddInset(insetBounds, 3)
                       .AddRichtext(this.res, CairoFont.WhiteDetailText(), outputTextBounds, "prichtext")
                   .EndClip()
                   .AddVerticalScrollbar(this.OnNewScrollbarvalueDetailPage, scrollbarBounds, "scrollbar")
               .EndChildElements();

                this.SingleComposer.AddStaticText("", CairoFont.WhiteDetailText(), bottomBounds = borderBounds.BelowCopy(0, 0));
                this.SingleComposer.AddSmallButton("Copy Code", this.OnButtonCopy, copyButtonBounds.FixedUnder(bottomBounds, 20));
            }

            this.SingleComposer.AddSmallButton("Close", this.OnButtonClose, closeButtonBounds.FixedUnder(bottomBounds, 20))
                .Compose();

            var richtextelem = this.SingleComposer.GetRichtext("prichtext");
            if (richtextelem != null)
            {
                this.SingleComposer.GetScrollbar("scrollbar").SetHeights(
                    (float)listHeight, (float)richtextelem.Bounds.fixedHeight
                );
                this.SingleComposer.GetScrollbar("scrollbar").CurrentYPosition = posY;
                this.OnNewScrollbarvalueDetailPage(posY);
            }

            this.UpdateFromServer(this.particleData);
        }

        private void OnNewScrollbarvalueDetailPage(float value)
        {
            var richtextElem = this.SingleComposer.GetRichtext("prichtext");
            richtextElem.Bounds.fixedY = 3 - value;
            richtextElem.Bounds.CalcWorldBounds();
        }

        public override void OnMouseWheel(MouseWheelEventArgs args)
        {
            base.OnMouseWheel(args);
            this.SyncWithServer();
        }
        public override void OnMouseUp(MouseEvent args)
        {
            base.OnMouseUp(args);
            this.SyncWithServer();
        }

        public override void OnKeyUp(KeyEvent args)
        {
            base.OnKeyUp(args);
            this.SyncWithServer();
        }

        public void OnTabToggle(string tabName)
        {
            this.currentTab = tabName;
            if (this.currentTab == "Code")
            { this.currentTab = "C#"; }
            this.Compose();
        }

        private void OnSelName(string t1)
        {
            this.particleData.name = t1;
        }

        private void OnToggleParticleEnabled(bool on)
        {
            this.particleData.particleEnabled = on;
        }

        private void DidSelectParticleType(string code, bool selected)
        {
            this.particleData.particleType = code;
            this.Compose();
        }

        private void OnToggleRandomParticleColors(bool on)
        {
            this.particleData.randomParticleColors = on;
        }

        private void OnTimerPreDelay(string t1)
        {
            var tempval = this.SingleComposer.GetNumberInput("timerPreDelay").GetValue();
            var val = tempval;
            if (tempval < 0)
            { tempval = 0f; }
            this.particleData.timerPreDelay = tempval;
            if (tempval != val)
            { this.Compose(); }
        }

        private void OnTimerPostDelay(string t1)
        {
            var tempval = this.SingleComposer.GetNumberInput("timerPostDelay").GetValue();
            var val = tempval;
            if (tempval < 0)
            { tempval = 0f; }
            this.particleData.timerPostDelay = tempval;
            if (tempval != val)
            { this.Compose(); }
        }
        private void OnTimerDuration(string t1)
        {
            var tempval = this.SingleComposer.GetNumberInput("timerDuration").GetValue();
            var val = tempval;
            if (tempval < 0)
            { tempval = 0f; }
            this.particleData.timerDuration = tempval;
            if (tempval != val)
            { this.Compose(); }
        }

        private void OnTimerLoop(bool on)
        {
            this.particleData.timerLoop = on;
        }

        private void OnParticleInterval(string t1)
        {
            var tempval = this.SingleComposer.GetNumberInput("particleInterval").GetValue();
            var val = tempval;
            if (tempval < 0.01)
            { tempval = 0.01f; }
            this.particleData.particleInterval = tempval;
            if (tempval != val)
            { this.Compose(); }
        }

        private void OnMinPosX(string val)
        {
            var x1 = this.SingleComposer.GetNumberInput("minPosX").GetValue();
            this.particleData.minPosX = x1;
        }

        private void OnMinPosY(string val)
        {
            var y1 = this.SingleComposer.GetNumberInput("minPosY").GetValue();
            this.particleData.minPosY = y1;
        }

        private void OnMinPosZ(string val)
        {
            var z1 = this.SingleComposer.GetNumberInput("minPosZ").GetValue();
            this.particleData.minPosZ = z1;
        }

        private void OnAddPosX(string val)
        {
            var x1 = this.SingleComposer.GetNumberInput("addPosX").GetValue();
            this.particleData.addPosX = x1;
        }

        private void OnAddPosY(string val)
        {
            var y1 = this.SingleComposer.GetNumberInput("addPosY").GetValue();
            this.particleData.addPosY = y1;
        }

        private void OnAddPosZ(string val)
        {
            var z1 = this.SingleComposer.GetNumberInput("addPosZ").GetValue();
            this.particleData.addPosZ = z1;
        }

        private void OnMinVelocityX(string val)
        {
            var x1 = this.SingleComposer.GetNumberInput("minVelocityX").GetValue();
            this.particleData.minVelocityX = x1;
        }

        private void OnMinVelocityY(string val)
        {
            var y1 = this.SingleComposer.GetNumberInput("minVelocityY").GetValue();
            this.particleData.minVelocityY = y1;
        }

        private void OnMinVelocityZ(string val)
        {
            var z1 = this.SingleComposer.GetNumberInput("minVelocityZ").GetValue();
            this.particleData.minVelocityZ = z1;
        }

        private void OnAddVelocityX(string val)
        {
            var x1 = this.SingleComposer.GetNumberInput("addVelocityX").GetValue();
            this.particleData.addVelocityX = x1;
        }

        private void OnAddVelocityY(string val)
        {
            var y1 = this.SingleComposer.GetNumberInput("addVelocityY").GetValue();
            this.particleData.addVelocityY = y1;
        }

        private void OnAddVelocityZ(string val)
        {
            var z1 = this.SingleComposer.GetNumberInput("addVelocityZ").GetValue();
            this.particleData.addVelocityZ = z1;
        }

        private void OnColorA(string t1) //0-255
        {
            var val = this.SingleComposer.GetNumberInput("colorA").GetValue();
            var tempVal = this.Limit0255(val);
            this.particleData.colorA = tempVal;
            if ((int)val != tempVal)
            { this.Compose(); }
        }

        private void OnColorR(string t1) //0-255
        {
            var val = this.SingleComposer.GetNumberInput("colorR").GetValue();
            var tempVal = this.Limit0255(val);
            this.particleData.colorR = tempVal;
            if ((int)val != tempVal)
            { this.Compose(); }
        }

        private void OnColorG(string t1) //0-255
        {
            var val = this.SingleComposer.GetNumberInput("colorG").GetValue();
            var tempVal = this.Limit0255(val);
            this.particleData.colorG = tempVal;
            if ((int)val != tempVal)
            { this.Compose(); }
        }

        private void OnColorB(string t1) //0-255
        {
            var val = this.SingleComposer.GetNumberInput("colorB").GetValue();
            var tempVal = this.Limit0255(val);
            this.particleData.colorB = tempVal;
            if ((int)val != tempVal)
            { this.Compose(); }
        }

        private void OnToggleRandomColor(bool on)
        {
            this.particleData.toggleRandomColor = on;
            this.Compose();
        }

        private void OnColorRndA(string t1) //0-255
        {
            var val = this.SingleComposer.GetNumberInput("colorRndA").GetValue();
            var tempVal = this.Limit0255(val);
            this.particleData.colorRndA = tempVal;
            if ((int)val != tempVal)
            { this.Compose(); }
        }

        private void OnColorRndR(string t1) //0-255
        {
            var val = this.SingleComposer.GetNumberInput("colorRndR").GetValue();
            var tempVal = this.Limit0255(val);
            this.particleData.colorRndR = tempVal;
            if ((int)val != tempVal)
            { this.Compose(); }
        }

        private void OnColorRndG(string t1) //0-255
        {
            var val = this.SingleComposer.GetNumberInput("colorRndG").GetValue();
            var tempVal = this.Limit0255(val);
            this.particleData.colorRndG = tempVal;
            if ((int)val != tempVal)
            { this.Compose(); }

        }

        private void OnColorRndB(string t1) //0-255
        {
            var val = this.SingleComposer.GetNumberInput("colorRndB").GetValue();
            var tempVal = this.Limit0255(val);
            this.particleData.colorRndB = tempVal;
            if ((int)val != tempVal)
            { this.Compose(); }
        }


        private void OnMinQuantity(string t1)
        {
            var val = this.SingleComposer.GetNumberInput("minQuantity").GetValue();
            if (val > this.maxParticlesQuantity)
            {
                val = this.maxParticlesQuantity;
                this.particleData.minQuantity = (int)val;
                this.Compose();
            }
            else
            {
                this.particleData.minQuantity = (int)val;
            }
        }

        private void OnMaxQuantity(string t1)
        {
            var val = this.SingleComposer.GetNumberInput("maxQuantity").GetValue();
            if (val > this.maxParticlesQuantity)
            {
                val = this.maxParticlesQuantity;
                this.particleData.maxQuantity = (int)val;
                this.Compose();
            }
            else
            {
                this.particleData.maxQuantity = (int)val;
            }
        }

        private void OnLifeLength(string t1)
        {
            var val = this.SingleComposer.GetNumberInput("lifeLength").GetValue();
            this.particleData.lifeLength = val;
        }

        private void OnMaxLifeLength(string t1)
        {
            var val = this.SingleComposer.GetNumberInput("maxLifeLength").GetValue();
            this.particleData.maxLifeLength = val;
        }

        private void OnGravityEffect(string t1)
        {
            var val = this.SingleComposer.GetNumberInput("gravityEffect").GetValue();
            this.particleData.gravityEffect = val;
        }

        private void OnMaxGravityEffect(string t1)
        {
            var val = this.SingleComposer.GetNumberInput("maxGravityEffect").GetValue();
            this.particleData.maxGravityEffect = val;
        }

        private void OnMinSize(string t1)
        {
            var val = this.SingleComposer.GetNumberInput("minSize").GetValue();
            this.particleData.minSize = val;
            if (val > this.maxParticlesSize)
            {
                val = this.maxParticlesSize;
                this.particleData.minSize = val;
                this.Compose();
            }
            else
            {
                this.particleData.minSize = val;
            }
        }

        private void OnMaxSize(string t1)
        {
            var val = this.SingleComposer.GetNumberInput("maxSize").GetValue();
            this.particleData.maxSize = val;
            if (val > this.maxParticlesSize)
            {
                val = this.maxParticlesSize;
                this.particleData.maxSize = val;
                this.Compose();
            }
            else
            {
                this.particleData.maxSize = val;
            }
        }

        private void OnToggleSizeTransform(bool on)
        {
            this.particleData.toggleSizeTransform = on;
            this.Compose();
        }

        private void DidSelectSizeTransform(string code, bool selected)
        {
            this.particleData.sizeTransform = code;
        }

        private void OnSizeEvolve(string t1)
        {
            var val = this.SingleComposer.GetNumberInput("sizeEvolve").GetValue();
            this.particleData.sizeEvolve = val;
        }

        private void OnToggleOpacityTransform(bool on)
        {
            this.particleData.toggleOpacityTransform = on;
            this.Compose();
        }


        private void DidSelectOpacityTransform(string code, bool selected)
        {
            this.particleData.opacityTransform = code;
        }

        private void OnOpacityEvolve(string t1)
        {
            var val = this.SingleComposer.GetNumberInput("opacityEvolve").GetValue();
            this.particleData.opacityEvolve = val;
        }

        private void OnToggleRedTransform(bool on)
        {
            this.particleData.toggleRedTransform = on;
            this.Compose();
        }

        private void DidSelectRedTransform(string code, bool selected)
        {
            this.particleData.redTransform = code;
        }

        private void OnRedEvolve(string t1)
        {
            var val = this.SingleComposer.GetNumberInput("redEvolve").GetValue();
            this.particleData.redEvolve = val;
        }

        private void OnToggleGreenTransform(bool on)
        {
            this.particleData.toggleGreenTransform = on;
            this.Compose();
        }

        private void DidSelectGreenTransform(string code, bool selected)
        {
            this.particleData.greenTransform = code;
        }

        private void OnGreenEvolve(string t1)
        {
            var val = this.SingleComposer.GetNumberInput("greenEvolve").GetValue();
            this.particleData.greenEvolve = val;
        }

        private void OnToggleBlueTransform(bool on)
        {
            this.particleData.toggleBlueTransform = on;
            this.Compose();
        }

        private void DidSelectBlueTransform(string code, bool selected)
        {
            this.particleData.blueTransform = code;
        }

        private void OnBlueEvolve(string t1)
        {
            var val = this.SingleComposer.GetNumberInput("blueEvolve").GetValue();
            this.particleData.blueEvolve = val;
        }

        private void DidSelectModel(string code, bool selected)
        {
            this.particleData.model = code;
        }

        private int Limit0255(float val)
        {
            if (val < 0)
            { return 0; }
            if (val > 255)
            { return 255; }
            return (int)val;
        }

        private void OnToggleVertexFlags(bool on)
        {
            this.particleData.toggleVertexFlags = on;
            this.Compose();
        }

        private void OnVertexFlags(string val)
        {
            var vf = this.SingleComposer.GetNumberInput("vertexFlags").GetValue();
            var tempVal = this.Limit0255(vf);
            this.particleData.vertexFlags = tempVal;
            if ((int)vf != tempVal)
            { this.Compose(); }
        }

        private void OnToggleSelfPropelled(bool on)
        {
            this.particleData.selfPropelled = on;
        }

        private void OnToggleShouldDieInAir(bool on)
        {
            this.particleData.shouldDieInAir = on;
        }

        private void OnToggleShouldDieInLiquid(bool on)
        {
            this.particleData.shouldDieInLiquid = on;
        }

        private void OnToggleMainThread(bool on)
        {
            this.particleData.mainThread = on;
            this.Compose();
        }


        private void OnToggleBouncy(bool on)
        {
            this.particleData.bouncy = on;
        }


        private void OnToggleRandomVelocityChange(bool on)
        {
            this.particleData.randomVelocityChange = on;
        }

        private void OnToggleShouldSwimOnLiquid(bool on)
        {
            this.particleData.shouldSwimOnLiquid = on;
        }

        private void OnToggleNoTerrainCollision(bool on)
        {
            this.particleData.noTerrainCollision = on;
        }

        private void OnToggleWindAffected(bool on)
        {
            this.particleData.windAffected = on;
            this.Compose();
        }

        private void OnWindAffectednes(string t1)
        {
            var val = this.SingleComposer.GetNumberInput("windAffectednes").GetValue();
            this.particleData.windAffectednes = val;
        }


        private void OnToggleUseBlockColor(bool on)
        {
            this.particleData.useBlockColor = on;
        }


        private void ColorToHSV(System.Drawing.Color color, out double hue, out double saturation, out double value)
        {
            var max = Math.Max(color.R, Math.Max(color.G, color.B));
            var min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }

        public void UpdateFromServer(BEParticleData data)
        {
            this.particleData = data;

            if (this.currentTab == "Main")
            {
                this.SingleComposer.GetDropDown("selectedParticle").SetSelectedValue(data.selectedParticle);
                this.SingleComposer.GetDynamicText("selDescText").SetNewText(this.selDesc);
                this.SingleComposer.GetTextInput("name").SetValue(data.name);
                this.SingleComposer.GetSwitch("particleEnabled").SetValue(data.particleEnabled);
                this.SingleComposer.GetDropDown("particleType").SetSelectedValue(data.particleType);

                if (data.particleType != "Death")
                {
                    this.SingleComposer.GetNumberInput("particleInterval").SetValue(data.particleInterval);
                }
                else
                {
                    this.SingleComposer.GetSwitch("randomParticleColors").SetValue(data.randomParticleColors);
                }

                if (this.particleData.particleType == "Primary")
                {
                    this.SingleComposer.GetSwitch("timerLoop").SetValue(data.timerLoop);
                    this.SingleComposer.GetNumberInput("timerPreDelay").SetValue(data.timerPreDelay);
                    this.SingleComposer.GetNumberInput("timerPostDelay").SetValue(data.timerPostDelay);
                    this.SingleComposer.GetNumberInput("timerDuration").SetValue(data.timerDuration);
                }
            }
            else if (this.currentTab == "Page 2")
            {
                this.SingleComposer.GetNumberInput("minPosX").SetValue(data.minPosX);
                this.SingleComposer.GetNumberInput("minPosY").SetValue(data.minPosY);
                this.SingleComposer.GetNumberInput("minPosZ").SetValue(data.minPosZ);

                this.SingleComposer.GetNumberInput("addPosX").SetValue(data.addPosX);
                this.SingleComposer.GetNumberInput("addPosY").SetValue(data.addPosY);
                this.SingleComposer.GetNumberInput("addPosZ").SetValue(data.addPosZ);

                this.SingleComposer.GetNumberInput("minVelocityX").SetValue(data.minVelocityX);
                this.SingleComposer.GetNumberInput("minVelocityY").SetValue(data.minVelocityY);
                this.SingleComposer.GetNumberInput("minVelocityZ").SetValue(data.minVelocityZ);

                this.SingleComposer.GetNumberInput("addVelocityX").SetValue(data.addVelocityX);
                this.SingleComposer.GetNumberInput("addVelocityY").SetValue(data.addVelocityY);
                this.SingleComposer.GetNumberInput("addVelocityZ").SetValue(data.addVelocityZ);

                this.SingleComposer.GetNumberInput("colorA").SetValue(data.colorA);
                this.SingleComposer.GetNumberInput("colorR").SetValue(data.colorR);
                this.SingleComposer.GetNumberInput("colorG").SetValue(data.colorG);
                this.SingleComposer.GetNumberInput("colorB").SetValue(data.colorB);

                this.SingleComposer.GetSwitch("toggleRandomColor").SetValue(data.toggleRandomColor);
                if (data.toggleRandomColor)
                {
                    this.SingleComposer.GetNumberInput("colorRndA").SetValue(data.colorRndA);
                    this.SingleComposer.GetNumberInput("colorRndR").SetValue(data.colorRndR);
                    this.SingleComposer.GetNumberInput("colorRndG").SetValue(data.colorRndG);
                    this.SingleComposer.GetNumberInput("colorRndB").SetValue(data.colorRndB);
                }

                this.SingleComposer.GetNumberInput("minQuantity").SetValue(data.minQuantity);
                this.SingleComposer.GetNumberInput("maxQuantity").SetValue(data.maxQuantity);

                this.SingleComposer.GetNumberInput("lifeLength").SetValue(data.lifeLength);
                this.SingleComposer.GetNumberInput("maxLifeLength").SetValue(data.maxLifeLength);

                this.SingleComposer.GetNumberInput("gravityEffect").SetValue(data.gravityEffect);
                this.SingleComposer.GetNumberInput("maxGravityEffect").SetValue(data.maxGravityEffect);

                this.SingleComposer.GetNumberInput("minSize").SetValue(data.minSize);
                this.SingleComposer.GetNumberInput("maxSize").SetValue(data.maxSize);
            }
            else if (this.currentTab == "Page 3")
            {
                this.SingleComposer.GetSwitch("toggleSizeTransform").SetValue(data.toggleSizeTransform);
                if (data.toggleSizeTransform)
                {
                    this.SingleComposer.GetDropDown("sizeTransform").SetSelectedValue(data.sizeTransform);
                    this.SingleComposer.GetNumberInput("sizeEvolve").SetValue(data.sizeEvolve);
                }

                this.SingleComposer.GetSwitch("toggleOpacityTransform").SetValue(data.toggleOpacityTransform);
                if (data.toggleOpacityTransform)
                {
                    this.SingleComposer.GetDropDown("opacityTransform").SetSelectedValue(data.opacityTransform);
                    this.SingleComposer.GetNumberInput("opacityEvolve").SetValue(data.opacityEvolve);
                }
                this.SingleComposer.GetSwitch("toggleRedTransform").SetValue(data.toggleRedTransform);
                if (data.toggleRedTransform)
                {
                    this.SingleComposer.GetDropDown("redTransform").SetSelectedValue(data.redTransform);
                    this.SingleComposer.GetNumberInput("redEvolve").SetValue(data.redEvolve);
                }

                this.SingleComposer.GetSwitch("toggleGreenTransform").SetValue(data.toggleGreenTransform);
                if (data.toggleGreenTransform)
                {
                    this.SingleComposer.GetDropDown("greenTransform").SetSelectedValue(data.greenTransform);
                    this.SingleComposer.GetNumberInput("greenEvolve").SetValue(data.greenEvolve);
                }

                this.SingleComposer.GetSwitch("toggleBlueTransform").SetValue(data.toggleBlueTransform);
                if (data.toggleBlueTransform)
                {
                    this.SingleComposer.GetDropDown("blueTransform").SetSelectedValue(data.blueTransform);
                    this.SingleComposer.GetNumberInput("blueEvolve").SetValue(data.blueEvolve);
                }
                this.SingleComposer.GetDropDown("model").SetSelectedValue(data.model);

                this.SingleComposer.GetSwitch("toggleVertexFlags").SetValue(data.toggleVertexFlags);
                if (data.toggleVertexFlags)
                {
                    this.SingleComposer.GetNumberInput("vertexFlags").SetValue(data.vertexFlags);
                }
            }
            else if (this.currentTab == "Page 4")
            {

                this.SingleComposer.GetSwitch("selfPropelled").SetValue(data.selfPropelled);
                this.SingleComposer.GetSwitch("shouldDieInAir").SetValue(data.shouldDieInAir);
                this.SingleComposer.GetSwitch("shouldDieInLiquid").SetValue(data.shouldDieInLiquid);
                this.SingleComposer.GetSwitch("useBlockColor").SetValue(data.useBlockColor);
                this.SingleComposer.GetSwitch("windAffected").SetValue(data.windAffected);
                if (data.windAffected)
                {
                    this.SingleComposer.GetNumberInput("windAffectednes").SetValue(data.windAffectednes);
                }

                this.SingleComposer.GetSwitch("mainThread").SetValue(data.mainThread);
                if (!data.mainThread)
                {
                    this.SingleComposer.GetSwitch("shouldSwimOnLiquid").SetValue(data.shouldSwimOnLiquid);
                    this.SingleComposer.GetSwitch("bouncy").SetValue(data.bouncy);
                    this.SingleComposer.GetSwitch("randomVelocityChange").SetValue(data.randomVelocityChange);
                    this.SingleComposer.GetSwitch("noTerrainCollision").SetValue(data.noTerrainCollision);
                }
            }
        }

        private string GetCode(string currentTab, BEParticleData data)
        {
            if (currentTab == "C#")
            {
                this.res = "\n";

                if (data.particleType == "Primary")
                {
                    this.res += "// Namespaces that might be required\nusing System;\nusing Vintagestory.API.Client;\nusing Vintagestory.API.Common;\n" +
                    "using Vintagestory.API.Config;\nusing Vintagestory.API.Datastructures;\nusing Vintagestory.API.MathTools;\n";
                    if (data.windAffected && data.mainThread)
                    {
                        this.res += "using Vintagestory.GameContent;\n";
                    }

                    this.res += "\n// Initialize Variables\n";

                    if (data.toggleRandomColor || data.maxLifeLength != 0 || data.maxGravityEffect != 0)
                    {
                        this.res += "static Random rand = new Random();\n";
                    }
                    if (data.mainThread)
                    {
                        this.res += "double interval = " + (data.particleInterval * 1000) + ";\n";
                        this.res += "long updateTick = 0;\n";
                        this.res += "ICoreClientAPI capi;\n";
                    }
                    else
                    {
                        this.res += "double interval = " + (data.particleInterval * 1000 / 2500) + ";\n";
                        this.res += "bool killOffThread = false;\n";
                    }
                    this.res += "bool timerLoop = " + data.timerLoop.ToString().ToLower() + ";\n";
                    this.res += "double timerDuration = " + data.timerDuration + ";\n";
                    this.res += "double timerPreDelay = " + data.timerPreDelay + ";\n";
                    this.res += "double timerPostDelay = " + data.timerPostDelay + ";\n";

                    var totalTime = 0f;
                    if (data.timerDuration > 0)
                    {
                        totalTime = data.timerDuration + data.timerPreDelay + data.timerPostDelay;
                    }
                    this.res += "\tdouble totalTime = " + totalTime + ";\n";
                    this.res += "double timeElapsed;\n";

                    this.res += "\n// Initialize Timer - call this from YOUR Initialize function for example\n";
                    this.res += "private void InitializeTimer()\n{\n";
                    if (data.mainThread)
                    {
                        this.res += "\tupdateTick = RegisterGameTickListener(UpdateParticles, interval);\n";
                        this.res += "\t// Note: call UnregisterGameTickListener(updateTick) to kill the timer\n";
                    }
                    else
                    {
                        this.res += "\tif (Api.Side.IsClient())\n\t{\n";
                        this.res += "\t\tcapi.Event.RegisterAsyncParticleSpawner(UpdateParticles);\n\t}\n";
                    }
                    this.res += "}\n";


                    this.res += "\n// Update Particles - called by the timer registered in InitializeTimer\n";

                    if (data.mainThread)
                    {
                        this.res += "private void UpdateParticles(float dt)\n{\n";

                        // WHERE'S THE REST OF THIS?
                    }
                    else
                    {
                        this.res += "private bool UpdateParticles(float dt, IAsyncParticleManager manager)\n{\n";
                        this.res += "\t// Note: returning false will kill the timer\n";
                        this.res += "\tif (killOffThread)\n\t{ return false; }\n";

                        this.res += "\tinterval -= dt;\n";
                        this.res += "\tif (interval &gt; 0)\n";
                        this.res += "\t{ return true; }\n\telse\n";
                        this.res += "\t{ interval = " + (data.particleInterval * 1000 / 2500) + "; }\n";

                        this.res += "\n\t// loop or no loop, always elapse time until we hit the max\n";
                        this.res += "\tif (timeElapsed &lt;= totalTime)\n";
                        this.res += "\t{ timeElapsed += interval; }\n";
                        this.res += "\telse\n\t{\n";
                        this.res += "\t\tif (timerLoop)\n";
                        this.res += "\t\t{ timeElapsed = 0; } //reset the timer\n\t}\n";

                        this.res += "\n\tif (totalTime &gt; 0) // handle no timerLoop, predelay, postdelay\n\t{\n";
                        this.res += "\t\tif ((timeElapsed &gt; totalTime) || (timeElapsed &lt;= timerPreDelay) || (timeElapsed &gt; timerPreDelay + timerDuration))\n";
                        this.res += "\t\t{ return true; }";
                        this.res += "\t}\n";

                        this.res += "\n\t//made it this far...generate a particle\n";
                        this.res += "\tthis.GenerateParticles(manager); \n";
                        this.res += "\treturn true;\n";

                    }
                    this.res += "}\n";


                    this.res += "\n// Generate Particles - called from the timer (UpdateParticles)\n";
                    if (data.mainThread)
                    {
                        this.res += "private void GenerateParticles()\n{\n";
                    }
                    else
                    {

                        this.res += "//\tmanager: off thread particle manager interface\n";
                        this.res += "private void GenerateParticles(IAsyncParticleManager manager)\n{\n";
                    }

                    this.res += "\t// Primary Particles\n";

                    //Sort out the colors
                    if (data.useBlockColor)
                    {
                        this.res += "\n\t// Retrieve random color from block at Pos\n";
                        this.res += "\tvar block = Api.World.BlockAccessor.GetBlock(this.Pos);\n";
                        this.res += "\tvar color = block.GetRandomColor(Api as ICoreClientAPI, this.Pos, BlockFacing.UP);\n";
                    }
                    else
                    {
                        if (!data.toggleRandomColor)
                        {
                            this.res += "\tvar color = ColorUtil.ToRgba(" + data.colorA + ", " + data.colorR + ", " + data.colorG + ", " + data.colorB + ");\n\n";
                        }
                        else
                        {
                            this.res += "\tvar color = " + this.BuildColor(data);
                        }
                    }

                    //need a custom windAffected when in mainThread
                    if (data.windAffected && data.mainThread)
                    {
                        this.res += "\n\t// Calculate windspeed manually when in the main thread\n";
                        this.res += "\tvar windSpeed = Api.ModLoader.GetModSystem<WeatherSystemBase>()?.WeatherDataSlowAccess.GetWindSpeed(this.Pos.ToVec3d()) ?? 0;\n";
                        this.res += "\tdouble windAdj = windSpeed * " + data.windAffectednes + "f;\n";
                    }

                    this.res += "\n\tvar particles = new SimpleParticleProperties(\n";
                    this.res += "\t\t" + data.minQuantity + ", " + data.maxQuantity + ", // quantity\n";
                    this.res += "\t\tcolor,\n";
                    this.res += "\t\tnew Vec3d(" + data.minPosX + ", " + data.minPosY + ", " + data.minPosZ + "), //min position\n";
                    this.res += "\t\tnew Vec3d(), //add position - see below\n";

                    if (data.windAffected)
                    {
                        this.res += "\t\tnew Vec3f(" + data.minVelocityX + "f + windAdj, " + data.minVelocityY + "f, " + data.minVelocityZ + "f), //min velocity\n";
                        this.res += "\t\tnew Vec3f(), //add velocity - see below\n";
                    }
                    else
                    {
                        this.res += "\t\tnew Vec3f(" + data.minVelocityX + "f, " + data.minVelocityY + "f, " + data.minVelocityZ + "f), //min velocity\n";
                        this.res += "\t\tnew Vec3f(), //add velocity - see below\n";
                    }

                    if (data.lifeLength != data.maxLifeLength)
                    {
                        var dLL = data.lifeLength;
                        var dMLL = data.maxLifeLength;
                        if (dLL > dMLL)
                        { dLL = dMLL; dMLL = data.lifeLength; }
                        this.res += "\t\t(float)((rand.NextDouble() * " + (dMLL - dLL) + "f) + " + dLL + "f), //life length\n";
                    }
                    else
                    {
                        this.res += "\t\t" + data.lifeLength + "f, //life length\n";
                    }

                    if (data.gravityEffect != data.maxGravityEffect)
                    {
                        var dGE = data.gravityEffect;
                        var dMGE = data.maxGravityEffect;
                        if (dGE > dMGE)
                        { dGE = dMGE; dMGE = data.gravityEffect; }
                        this.res += "\t\t(float)((rand.NextDouble() * " + (dMGE - dGE) + "f) + " + dGE + "f), //gravity effect \n";
                    }
                    else
                    {
                        this.res += "\t\t" + data.gravityEffect + "f, //gravity effect\n";
                    }

                    this.res += "\t\t" + data.minSize + "f, " + data.maxSize + "f, //size\n";
                    this.res += "\t\tEnumParticleModel." + data.model + "); // model\n\n";

                    this.res += "\tparticles.MinPos.Add(this.Pos); //add block position\n";
                    this.res += "\tparticles.AddPos.Set(new Vec3d(" + data.addPosX + ", " + data.addPosY + ", " + data.addPosZ + ")); //add position\n";


                    if (data.windAffected)
                    {
                        this.res += "\tparticles.AddVelocity.Set(new Vec3f(" + data.addVelocityX + "f + windAdj, " + data.addVelocityY + "f, " + data.addVelocityZ + "f)); //add velocity\n";
                    }
                    else
                    {
                        this.res += "\tparticles.AddVelocity.Set(new Vec3f(" + data.addVelocityX + "f, " + data.addVelocityY + "f, " + data.addVelocityZ + "f)); //add velocity\n";
                    }










                    this.res += this.BuildCSharpEvolves(data);

                    if (data.toggleVertexFlags)
                    {
                        this.res += "\tparticles.VertexFlags = " + data.vertexFlags + ";\n";
                    }


                    if (data.selfPropelled)
                    { this.res += "\tparticles.SelfPropelled = true;\n"; }
                    if (data.shouldDieInAir)
                    { this.res += "\tparticles.ShouldDieInAir = true;\n"; }
                    if (data.shouldDieInLiquid)
                    { this.res += "\tparticles.ShouldDieInLiquid = true;\n"; }

                    if (!data.mainThread)
                    {
                        if (data.windAffected)
                        {
                            this.res += "\tparticles.WindAffected = true;\n";
                            this.res += "\tparticles.WindAffectednes = " + data.windAffectednes + "f;\n";
                        }
                        if (data.noTerrainCollision)
                        { this.res += "\tparticles.WithTerrainCollision = false;\n"; }
                        if (data.shouldSwimOnLiquid)
                        { this.res += "\tparticles.ShouldSwimOnLiquid = true;\n"; }
                        if (data.bouncy)
                        // 1.17 bouncy was changed from boolean to float
                        // this is a temp fix
                        //{ this.res += "\tparticles.Bouncy = true;\n"; }
                        { this.res += "\tparticles.Bouncy = 0.7f;\n"; }
                        if (data.randomVelocityChange)
                        { this.res += "\tparticles.RandomVelocityChange = true;\n"; }
                    }


                    if (!data.mainThread)
                    {
                        //advanced particles
                        BEParticleData neibData;
                        if (data.neibSecX != -1)
                        {
                            if (this.capi.World.BlockAccessor.GetBlockEntity(new BlockPos(data.neibSecX, data.neibSecY, data.neibSecZ)) is BEParticulator be)
                            {
                                neibData = be.Data;
                                if (neibData != null)
                                {
                                    if (neibData.particleEnabled)
                                    {
                                        this.res += "\n\t// Secondary Particles\n";
                                        this.res += "\n\tvar secondaryParticles = new AdvancedParticleProperties[]\n\t{\n";
                                        this.res += "\t\tthis.BuildSecondaryParticles()\n\t};\n";
                                        this.res += "\tparticles.SecondaryParticles = secondaryParticles;\n\t}\n";
                                    }
                                }
                            }
                        }

                        if (data.neibDeathX != -1)
                        {
                            if (this.capi.World.BlockAccessor.GetBlockEntity(new BlockPos(data.neibDeathX, data.neibDeathY, data.neibDeathZ)) is BEParticulator be)
                            {
                                neibData = be.Data;
                                if (neibData != null)
                                {
                                    if (neibData.particleEnabled)
                                    {
                                        this.res += "\n\t// Death Particles\n";
                                        this.res += "\n\tvar deathParticles = new AdvancedParticleProperties[]\n\t{\n";
                                        this.res += "\t\tthis.BuildDeathParticles()\n\t};\n";
                                        this.res += "\tparticles.DeathParticles = deathParticles;\n\t}\n";
                                    }
                                }
                            }
                        }

                        this.res += "\n\tmanager.Spawn(particles);\n}\n";
                    }
                    else
                    {
                        this.res += "\n\tApi.World.SpawnParticles(particles);\n}\n";
                    }

                    if (!data.mainThread)
                    {
                        //advanced particles
                        BEParticleData neibData;
                        if (data.neibSecX != -1)
                        {
                            if (this.capi.World.BlockAccessor.GetBlockEntity(new BlockPos(data.neibSecX, data.neibSecY, data.neibSecZ)) is BEParticulator be)
                            {
                                neibData = be.Data;
                                if (neibData != null)
                                {
                                    if (neibData.particleEnabled)
                                    {
                                        this.res += "\n\n// Secondary Particles\n";
                                        this.res += this.BuildCSharpAdvancedParticles("Secondary", neibData);
                                    }
                                }
                            }
                        }

                        if (data.neibDeathX != -1)
                        {
                            if (this.capi.World.BlockAccessor.GetBlockEntity(new BlockPos(data.neibDeathX, data.neibDeathY, data.neibDeathZ)) is BEParticulator be)
                            {
                                neibData = be.Data;
                                if (neibData != null)
                                {
                                    if (neibData.particleEnabled)
                                    {
                                        this.res += "\n\n// Death Particles\n";
                                        this.res += this.BuildCSharpAdvancedParticles("Death", neibData);
                                    }
                                }
                            }
                        }
                    }

                }
                else //death or secondary particle
                {
                    this.res += "// Partial code for a secondary or death particle";
                    this.res += "\n// Refer to its primary particle for the completed code\n";
                    if (data.particleType == "Secondary")
                    {
                        this.res += this.BuildCSharpAdvancedParticles("Secondary", data);
                    }
                    else
                    {
                        this.res += this.BuildCSharpAdvancedParticles("Death", data);
                    }
                }
            }
            else if (this.currentTab == "JSON")
            {
                this.res = "\nATTENTION:  THIS CODE HAS NEVER BEEN TESTED AND IS VERY MUCH WIP. NEXT RELEASE PERHAPS\n\n";
                this.res += "\nparticleProperties: [\n";
                this.res += "\t{\n";

                var avgX = (double)Math.Round(data.minPosX * 100) / 100;
                var avgY = (double)Math.Round(data.minPosY * 100) / 100;
                var avgZ = (double)Math.Round(data.minPosZ * 100) / 100;
                double varX = 0;
                double varY = 0;
                double varZ = 0;

                if (data.addPosX != 0)
                {
                    avgX = Math.Round((data.minPosX + (data.addPosX / 2)) * 100) / 100;
                    varX = Math.Round(data.addPosX / 2 * 100) / 100;
                }
                if (data.addPosY != 0)
                {
                    avgY = Math.Round((data.minPosY + (data.addPosY / 2)) * 100) / 100;
                    varY = Math.Round(data.addPosY / 2 * 100) / 100;
                }
                if (data.addPosZ != 0)
                {
                    avgZ = Math.Round((data.minPosZ + (data.addPosZ / 2)) * 100) / 100;
                    varZ = Math.Round(data.addPosZ / 2 * 100) / 100;
                }

                this.res += "\t\tposOffset: [\n";
                this.res += "\t\t\t{ avg: " + avgX + ", var: " + varX + " },{ avg: " + avgY + ", var: " + varY + " },{ avg: " + avgZ + ", var: " + varZ + " }\n";
                this.res += "\t\t],\n";

                avgX = (double)Math.Round(data.minVelocityX * 100) / 100;
                avgY = (double)Math.Round(data.minVelocityY * 100) / 100;
                avgZ = (double)Math.Round(data.minVelocityZ * 100) / 100;
                varX = varY = varZ = 0;

                if (data.addVelocityX != 0)
                {
                    avgX = Math.Round((data.minVelocityX + (data.addVelocityX / 2)) * 100) / 100;
                    varX = Math.Round(data.addVelocityX / 2 * 100) / 100;
                }
                if (data.addVelocityY != 0)
                {
                    avgY = Math.Round((data.minVelocityY + (data.addVelocityY / 2)) * 100) / 100;
                    varY = Math.Round(data.addVelocityY / 2 * 100) / 100;
                }
                if (data.addVelocityZ != 0)
                {
                    avgZ = Math.Round((data.minVelocityZ + (data.addVelocityZ / 2)) * 100) / 100;
                    varZ = Math.Round(data.addVelocityZ / 2 * 100) / 100;
                }

                this.res += "\t\tvelocity: [\n";
                this.res += "\t\t\t{ avg: " + avgX + ", var: " + varX + " },{ avg: " + avgY + ", var: " + varY + " },{ avg: " + avgZ + ", var: " + varZ + " }\n";
                this.res += "\t\t],\n";
                this.res += "\t\thsvaColor: [\n";

                var color = System.Drawing.Color.FromArgb(data.colorR, data.colorG, data.colorB);
                this.ColorToHSV(color, out var hue, out var saturation, out var value);
                if (data.toggleRandomColor)
                {
                    var color2 = System.Drawing.Color.FromArgb(data.colorR, data.colorG, data.colorB);
                    this.ColorToHSV(color2, out var hue2, out var saturation2, out var value2);

                    //this might be the magic color calc
                    var avgHue = Math.Round((hue + hue2) / 2);
                    var varHue = Math.Round(Math.Abs(avgHue - hue));

                    var avgSat = Math.Round((saturation + saturation2) / 2);
                    var varSat = Math.Round(Math.Abs(avgSat - saturation));

                    var avgValue = Math.Round((value + value2) / 2);
                    var varValue = Math.Round(Math.Abs(avgValue - value));

                    var avgA = Math.Round((data.colorA + (double)data.colorRndA) / 2);
                    var varA = Math.Round(Math.Abs(avgA - data.colorA));

                    this.res += "\t\t\t{ avg: " + avgHue + ", var: " + varHue + " }, { avg: " + avgSat + ", var: " + varSat + " },\n";
                    this.res += "\t\t\t{ avg: " + avgValue + ", var: " + varValue + " }, { avg: " + avgA + ", var: " + varA + " }\n";
                }
                else
                {
                    this.res += "\t\t\t{ avg: " + (int)hue + ", var: 0 }, { avg: " + (int)saturation + ", var: 0 },\n";
                    this.res += "\t\t\t{ avg: " + (int)value + ", var: 0 }, { avg: " + data.colorA + ", var: 0 }\n";
                }
                this.res += "\t\t],\n";

                double avgVal = data.minQuantity;
                double varVal = 0;
                if (data.maxQuantity != 0)
                {
                    avgVal = Math.Round((data.minQuantity + ((double)data.maxQuantity / 2)) * 100) / 100;
                    varVal = Math.Round((double)data.maxQuantity / 2 * 100) / 100;
                }
                this.res += "\t\tquantity: { avg: " + avgVal + ", var: " + varVal + " },\n";

                avgVal = data.lifeLength;
                varVal = 0;
                if (data.maxLifeLength != 0)
                {
                    avgVal = Math.Round((data.lifeLength + ((double)data.maxLifeLength / 2)) * 100) / 100;
                    varVal = Math.Round((double)data.maxLifeLength / 2 * 100) / 100;
                }
                this.res += "\t\tlifeLength: { avg: " + avgVal + ", var: " + varVal + " },\n";

                avgVal = data.gravityEffect;
                varVal = 0;
                if (data.maxGravityEffect != 0)
                {
                    avgVal = Math.Round((data.gravityEffect + ((double)data.maxGravityEffect / 2)) * 100) / 100;
                    varVal = Math.Round((double)data.maxGravityEffect / 2 * 100) / 100;
                }
                this.res += "\t\tgravityEffect: { avg: " + avgVal + ", var: " + varVal + " },\n";

                avgVal = data.minSize;
                varVal = 0;
                if (data.maxSize != 0)
                {
                    avgVal = Math.Round((data.minSize + ((double)data.maxSize / 2)) * 100) / 100;
                    varVal = Math.Round((double)data.maxSize / 2 * 100) / 100;
                }
                this.res += "\t\tsize: { avg: " + avgVal + ", var: " + varVal + " },\n";

                this.res += "\t\tparticleModel: \"" + data.model + "\",\n";

                if (data.toggleRedTransform)
                {
                    this.res += "\t\tredEvolve: { transform: \"" + data.redTransform.ToLower() + "\", factor: " + data.redEvolve + " },\n";
                }
                if (data.toggleGreenTransform)
                {
                    this.res += "\t\tgreenEvolve: { transform: \"" + data.greenTransform.ToLower() + "\", factor: " + data.greenEvolve + " },\n";
                }
                if (data.toggleBlueTransform)
                {
                    this.res += "\t\tblueEvolve: { transform: \"" + data.blueTransform.ToLower() + "\", factor: " + data.blueEvolve + " },\n";
                }

                if (data.toggleSizeTransform)
                {
                    this.res += "\t\tsizeEvolve: { transform: \"" + data.sizeTransform.ToLower() + "\", factor: " + data.sizeEvolve + " },\n";
                }
                if (data.toggleOpacityTransform)
                {
                    this.res += "\t\topacityEvolve: { transform: \"" + data.opacityTransform.ToLower() + "\", factor: " + data.opacityEvolve + " },\n";
                }
                if (data.selfPropelled)
                {
                    this.res += "\t\tselfPropelled: true,\n";
                }
                if (data.shouldDieInAir)
                {
                    this.res += "\t\tdieInAir: true,\n";
                }
                if (data.shouldDieInLiquid)
                {
                    this.res += "\t\tdieInLiquid: true,\n";
                }
                if (data.shouldSwimOnLiquid)
                {
                    this.res += "\t\tswimOnLiquid: true,\n";
                }
                if (data.bouncy)
                {
                    // 1.17 bouncy was changed from boolean to float
                        // this is a temp fix
                    this.res += "\t\tbouncy: 0.7f,\n";
                }
                if (data.randomVelocityChange)
                {
                    this.res += "\t\trandomVelocityChange: true,\n";
                }
                if (data.noTerrainCollision)
                {
                    this.res += "\t\tterrainCollision: false,\n";
                }
                else
                {
                    this.res += "\t\tterrainCollision: true,\n";
                }
                if (data.windAffected)
                {
                    this.res += "\t\twindAffectednes: " + data.windAffectednes + ",\n";
                }
                if (data.toggleVertexFlags)
                {
                    this.res += "\t\tvertexFlags: " + data.vertexFlags + ",\n";
                }
                this.res += "\t}\n";
                this.res += "]\n\n";
                /*
                secondaryParticles:
                [
                    {
                        hsvaColor: [{ avg: 0, var: 0 }, { avg: 0, var: 0 }, { avg: 40, var: 30 },  { avg: 220, var: 50 }],
    				    secondarySpawnInterval: { avg: 0.15, var: 0}
                    }
				]
                deathParticles:
                [
                    {
                        hsvaColor: [{ avg: 0, var: 0 }, { avg: 0, var: 0 }, { avg: 40, var: 30 },  { avg: 220, var: 50 }],
                    }
				]
                */

                // remove last comma
                this.res = this.res.Remove(this.res.LastIndexOf(","), 1);
            }
            else if (this.currentTab == "Particulate")
            {
                this.res = "\n";
                this.res += "this.ConfigParticle(\n";
                this.res += "\t" + data.minPosX + "f, " + data.minPosY + "f, " + data.minPosZ + "f, " + data.addPosX + "f, " + data.addPosY + "f, " + data.addPosZ + "f,";
                this.res += " // min/add pos\n";
                this.res += "\t" + data.minVelocityX + "f, " + data.minVelocityY + "f, " + data.minVelocityZ + "f, " + data.addVelocityX + "f, " + data.addVelocityY + "f, " + data.addVelocityZ + "f,";
                this.res += " // min/add velocity\n";
                this.res += "\t" + data.colorA + ", " + data.colorR + ", " + data.colorG + ", " + data.colorB + ", " + data.toggleRandomColor.ToString().ToLower() + ", " + data.colorRndA + ", " + data.colorRndR + ", " + data.colorRndG + ", " + data.colorRndB + ",";
                this.res += " // colors\n";
                this.res += "\t" + data.minQuantity + ", " + data.maxQuantity + ", " + data.lifeLength + "f, " + data.maxLifeLength + "f,";
                this.res += " // min/add quantity, min/add life length\n";
                this.res += "\t" + data.gravityEffect + "f, " + data.maxGravityEffect + "f, " + data.minSize + "f, " + data.maxSize + "f,";
                this.res += " // min/add gravity, min/add size\n";

                if (!data.toggleSizeTransform)
                {
                    this.res += "\tfalse, \"LINEAR\", 0f,";
                }
                else
                {
                    this.res += "\t" + data.toggleSizeTransform.ToString().ToLower() + ", \"" + data.sizeTransform + "\", " + data.sizeEvolve + "f,";
                }
                this.res += " // size transform\n";
                if (!data.toggleOpacityTransform)
                {
                    this.res += "\tfalse, \"LINEAR\", 0f,";
                }
                else
                {
                    this.res += "\t" + data.toggleOpacityTransform.ToString().ToLower() + ", \"" + data.opacityTransform + "\", " + data.opacityEvolve + "f,";
                }
                this.res += " // opacity transform\n";
                if (!data.toggleRedTransform)
                {
                    this.res += "\tfalse, \"LINEAR\", 0f,";
                }
                else
                {
                    this.res += "\t" + data.toggleRedTransform.ToString().ToLower() + ", \"" + data.redTransform + "\", " + data.redEvolve + "f,";
                }
                this.res += " // red transform\n";
                if (!data.toggleGreenTransform)
                {
                    this.res += "\tfalse, \"LINEAR\", 0f,";
                }
                else
                {
                    this.res += "\t" + data.toggleGreenTransform.ToString().ToLower() + ", \"" + data.greenTransform + "\", " + data.greenEvolve + "f,";
                }
                this.res += " // green transform\n";
                if (!data.toggleBlueTransform)
                {
                    this.res += "\tfalse, \"LINEAR\", 0f,";
                }
                else
                {
                    this.res += "\t" + data.toggleBlueTransform.ToString().ToLower() + ", \"" + data.blueTransform + "\", " + data.blueEvolve + "f,";
                }
                this.res += " // blue transform\n";
                this.res += "\t\"" + data.particleType + "\", " + "\"" + data.model + "\", " + data.selfPropelled.ToString().ToLower() + ", // model, selfpropelled\n";
                this.res += "\t" + data.shouldDieInAir.ToString().ToLower() + ", " + data.shouldDieInLiquid.ToString().ToLower() + ", " + data.shouldSwimOnLiquid.ToString().ToLower() + ", ";
                this.res += " // dieAir, dieLiq, swimLiq\n";
                this.res += "\t" + data.bouncy.ToString().ToLower() + ", " + data.randomVelocityChange.ToString().ToLower() + ", " + data.noTerrainCollision.ToString().ToLower() + ",";
                this.res += " // bouncy, rndVel, noTerrainCol\n";
                var windAffectednes = "0";
                if (data.windAffected)
                { windAffectednes = data.windAffectednes.ToString(); }
                this.res += "\t" + data.windAffected.ToString().ToLower() + ", " + windAffectednes + "f, " + data.toggleVertexFlags.ToString().ToLower() + ", " + data.vertexFlags + ", " + data.particleInterval + "f,";
                this.res += " // windAf, windAfn, toggleVtx, VtX, pInt\n";
                this.res += "\t" + data.randomParticleColors.ToString().ToLower() + ", " + data.timerLoop.ToString().ToLower() + ", " + data.timerPreDelay + "f, " + data.timerDuration + "f, " + data.timerPostDelay + "f);";
                this.res += " // rndpcolors,loop,pre,duration,post";
            }
            return this.res;
        }

        private string BuildCSharpAdvancedParticles(string name, BEParticleData data)
        {
            //this might still need that outer [] structure
            var result = "";

            result += "private AdvancedParticleProperties Build" + name + "Particles()\n{\n";


            result += "\tvar particles = new AdvancedParticleProperties()\n\t{\n";

            result += "\t\tGravityEffect = NatFloat.createUniform(" + data.gravityEffect + "f, " + data.maxGravityEffect + "f),\n";
            result += "\t\tSize = NatFloat.createUniform(" + data.minSize + "f, " + data.maxSize + "f),\n";
            result += "\t\tLifeLength = NatFloat.createUniform(" + data.lifeLength + "f, " + data.maxLifeLength + "f),\n";
            result += "\t\tQuantity = NatFloat.createUniform(" + data.minQuantity + ", " + data.maxQuantity + "),\n";
            result += "\t\tVelocity = new NatFloat[]\n\t\t{\n\t\t\tNatFloat.createUniform(" + data.minVelocityX + "f, " + data.addVelocityX + "f),\n\t\t\tNatFloat.createUniform(" + data.minVelocityY + "f, " + data.addVelocityY + "f),\n\t\t\tNatFloat.createUniform(" + data.minVelocityZ + "f, " + data.addVelocityZ + "f)\n\t\t},\n";
            result += "\t\tParticleModel = " + "EnumParticleModel." + data.model + "\n";
            result += "\t};\n";

            if (data.randomParticleColors)
            {
                result += "\tparticles.HsvaColor = new NatFloat[] {\n";
                result += "\t\tNatFloat.createUniform(0, 255),\n";
                result += "\t\tNatFloat.createUniform(0, 255),\n";
                result += "\t\tNatFloat.createUniform(0, 255),\n";
                result += "\t\tNatFloat.createUniform(255, 255) };\n";
            }
            else
            {
                result += "\tparticles.HsvaColor = null; //This must be null to use the Color parameter\n";
                result += "\tparticles.Color = " + this.BuildColor(data);
            }

            if (data.particleType == "Secondary")
            {
                result += "\tparticles.SecondarySpawnInterval = NatFloat.createUniform(" + data.particleInterval + "f, 0);\n";
            }

            result += "\tparticles.basePos = this.Pos.ToVec3d().AddCopy(" + data.minPosX + ", " + data.minPosY + ", " + data.minPosZ + ");\n";
            result += "\tparticles.basePos = particles.basePos.Add(" + data.minPosX + " + (" + data.addPosX + " * rand.NextDouble()), " + data.minPosY + " + (" + data.addPosY + " * rand.NextDouble()), " + data.minPosZ + " + (" + data.addPosZ + " * rand.NextDouble()));\n";

            result += this.BuildCSharpEvolves(data);

            if (data.toggleVertexFlags)
            {
                result += "\tparticles.VertexFlags = " + data.vertexFlags + ";\n";
            }

            if (data.selfPropelled)
            { result += "\tparticles.SelfPropelled = true;\n"; }
            if (data.shouldDieInAir)
            { result += "\tparticles.DieInAir = true;\n"; }
            if (data.shouldDieInLiquid)
            { result += "\tparticles.DieInLiquid = true;\n"; }
            if (data.windAffected)
            {
                result += "\tparticles.WindAffectednes = " + data.windAffectednes + "f;\n";
            }

            if (data.noTerrainCollision)
            { result += "\tparticles.TerrainCollision = false;\n"; }
            if (data.shouldSwimOnLiquid)
            { result += "\tparticles.SwimOnLiquid = true;\n"; }
            if (data.bouncy)
            { 
                // 1.17 bouncy was changed from boolean to float
                // this is a temp fix
                result += "\tparticles.Bouncy = 0.7f;\n"; 
            }
            if (data.randomVelocityChange)
            { result += "\tparticles.RandomVelocityChange = true;\n"; }

            result += "\treturn particles;\n}\n\n";
            return result;
        }


        private string BuildColor(BEParticleData data)
        {
            var aCode = data.colorA.ToString();
            var rCode = data.colorR.ToString();
            var gCode = data.colorG.ToString();
            var bCode = data.colorB.ToString();

            if (data.toggleRandomColor)
            {
                if (data.colorA != data.colorRndA)
                {
                    var a = new[] { data.colorA, data.colorRndA };
                    aCode = "rand.Next(" + a.Min() + ", " + a.Max() + ")";
                }
                if (data.colorR != data.colorRndR)
                {
                    var r = new[] { data.colorR, data.colorRndR };
                    rCode = "rand.Next(" + r.Min() + ", " + r.Max() + ")";
                }
                if (data.colorG != data.colorRndG)
                {
                    var g = new[] { data.colorG, data.colorRndG };
                    gCode = "rand.Next(" + g.Min() + ", " + g.Max() + ")";
                }
                if (data.colorB != data.colorRndB)
                {
                    var b = new[] { data.colorB, data.colorRndB };
                    bCode = "rand.Next(" + b.Min() + ", " + b.Max() + ")";
                }
            }

            return "ColorUtil.ToRgba(" + aCode + ", " + rCode + ", " + gCode + ", " + bCode + ");\n";
        }

        private string BuildCSharpEvolves(BEParticleData data)
        {
            var result = "";
            if (data.toggleSizeTransform)
            {
                result += "\tparticles.SizeEvolve = new EvolvingNatFloat( EnumTransformFunction." + data.sizeTransform.ToUpper() + ", " + data.sizeEvolve + "f);\n";
            }
            if (data.toggleOpacityTransform)
            {
                result += "\tparticles.OpacityEvolve = new EvolvingNatFloat( EnumTransformFunction." + data.opacityTransform.ToUpper() + ", " + data.opacityEvolve + "f);\n";
            }
            if (data.toggleRedTransform)
            {
                result += "\tparticles.RedEvolve = new EvolvingNatFloat( EnumTransformFunction." + data.redTransform.ToUpper() + ", " + data.redEvolve + "f);\n";
            }
            if (data.toggleGreenTransform)
            {
                result += "\tparticles.GreenEvolve = new EvolvingNatFloat( EnumTransformFunction." + data.greenTransform.ToUpper() + ", " + data.greenEvolve + "f);\n";
            }
            if (data.toggleBlueTransform)
            {
                result += "\tparticles.BlueEvolve = new EvolvingNatFloat( EnumTransformFunction." + data.blueTransform.ToUpper() + ", " + data.blueEvolve + "f);\n";
            }
            return result;
        }


        private void OnTitleBarClose()
        {
            this.OnButtonClose();
        }


        private void SyncWithServer()
        {
            var updata = SerializerUtil.Serialize(this.particleData);
            if (this.blockEntityPos != null)
            {
                this.capi.Network.SendBlockEntityPacket(this.blockEntityPos.X, this.blockEntityPos.Y, this.blockEntityPos.Z, 1091, updata);
            }
        }

        private bool OnButtonClose()
        {
            this.SyncWithServer();
            this.TryClose();
            return true;
        }

        private bool OnButtonCopy()
        {
            var temp = this.res;
            temp = temp.Replace("&lt;", "<").Replace("&gt;", ">");
            this.capi.Forms.SetClipboardText(temp);
            return true;
        }

        public override bool CaptureAllInputs()
        {
            return false;
        }

        public override bool PrefersUngrabbedMouse => true; //fix for opening multiple guis

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
        }

        private void DidSelectParticle(string code, bool selected)
        {
            this.particleData.selectedParticle = code;
            if (code != "")
            { this.particleData.name = "My " + code; }
            this.particleData.particleEnabled = true;
            this.particleData.useBlockColor = false;
            this.selDesc = "Author: ";
            switch (code)
            {
                case "Default":
                    this.selDesc += "SpearAndFang\nSimple particle effect.";
                    this.ConfigParticle(
                        0.5f, 0.5f, 0.5f, 0f, 0f, 0f, // min/add pos
                        1f, 1f, 1f, -2f, -2f, -2f, // min/add velocity
                        0, 0, 0, 0, true, 255, 255, 255, 255, // colors
                        100, 0, 0.15f, 0.2f, // min/add quantity, min/add life length
                        0f, 0f, 0.2f, 0.8f, // min/add gravity, min/add size
                        false, "Linear", 0f, // size transform
                        false, "Linear", 0f, // opacity transform
                        false, "Linear", 0f, // red transform
                        false, "Linear", 0f, // green transform
                        false, "Linear", 0f, // blue transform
                        "Primary", "Cube", false, // model, selfpropelled
                        false, false, false,  // dieAir, dieLiq, swimLiq
                        false, false, false, // bouncy, rndVel, noTerrainCol
                        false, 0f, true, 255, 0.05f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Dripping Water":
                    this.selDesc += "CaptainOats\nWater dripping from above. Link this particle effect to an overhead block.";
                    this.ConfigParticle(
                        0f, 0f, 0f, 1f, 0f, 1f, // min/add pos
                        -0.1f, -0.1f, -0.1f, 0.1f, 0f, 0.1f, // min/add velocity
                        60, 100, 170, 180, true, 100, 100, 190, 200, // colors
                        1, 0, 2.5f, 2f, // min/add quantity, min/add life length
                        0.5f, 0f, 0.02f, 0.01f, // min/add gravity, min/add size
                        true, "Inverselinear", 10f, // size transform
                        false, "Linear", 1f, // opacity transform
                        false, "Linear", 0f, // red transform
                        false, "Linear", 0f, // green transform
                        false, "Linear", 0f, // blue transform
                        "Primary", "Quad", false, // model, selfpropelled
                        false, true, true,  // dieAir, dieLiq, swimLiq
                        true, false, false, // bouncy, rndVel, noTerrainCol
                        true, 0.1f, true, 200, 0.16f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Fireworks (explosion 1)":
                    this.selDesc += "SpearAndFang\nRandom secondary particle for the fireworks (launcher).";
                    this.ConfigParticle(
                        0.5f, 1.5f, 0.5f, 0f, 0f, 0f, // min/add pos
                        5f, 3f, 5f, -10f, -10f, -10f, // min/add velocity
                        150, 0, 0, 50, true, 255, 255, 255, 255, // colors
                        80, 80, 0.1f, 0.1f, // min/add quantity, min/add life length
                        0.28f, 0.1f, 0.5f, 1.5f, // min/add gravity, min/add size
                        true, "Linearreduce", 1f, // size transform
                        false, "Linear", 0f, // opacity transform
                        false, "Linear", 0f, // red transform
                        false, "Linear", 0f, // green transform
                        false, "Linear", 0f, // blue transform
                        "Secondary", "Quad", false, // model, selfpropelled
                        false, false, false,  // dieAir, dieLiq, swimLiq
                        false, false, false, // bouncy, rndVel, noTerrainCol
                        true, 0.1f, true, 255, 0.01f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Fireworks (explosion 2)":
                    this.selDesc += "SpearAndFang\nRandom death particle for the fireworks (launcher).";
                    this.ConfigParticle(
                        0.5f, 1.5f, 0.5f, 0f, 0f, 0f, // min/add pos
                        3f, 3f, 3f, -6f, -6f, -6f, // min/add velocity
                        255, 255, 50, 50, true, 255, 255, 255, 255, // colors
                        600, 400, 0.1f, 0.3f, // min/add quantity, min/add life length
                        0.28f, 0f, 0.5f, 1f, // min/add gravity, min/add size
                        false, "Linearreduce", 1f, // size transform
                        false, "Linear", 0f, // opacity transform
                        false, "Linear", 0f, // red transform
                        false, "Linear", 0f, // green transform
                        false, "Linear", 0f, // blue transform
                        "Death", "Quad", false, // model, selfpropelled
                        false, false, false,  // dieAir, dieLiq, swimLiq
                        false, true, false, // bouncy, rndVel, noTerrainCol
                        true, 0.1f, true, 255, 1f, // windAf, windAfn, toggleVtx, VtX, pInt
                        true, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Fireworks (explosion 3)":
                    this.selDesc += "SpearAndFang\nRandom death particle for the fireworks (launcher).";
                    this.ConfigParticle(
                        0.5f, 1.5f, 0.5f, 0f, 0f, 0f, // min/add pos
                        5f, 5f, 5f, -10f, -10f, -10f, // min/add velocity
                        50, 0, 0, 0, true, 255, 255, 255, 255, // colors
                        1000, 50, 0.3f, 0.6f, // min/add quantity, min/add life length
                        0.28f, 0f, 0.2f, 0.2f, // min/add gravity, min/add size
                        true, "Linearreduce", 1f, // size transform
                        false, "Linear", 0f, // opacity transform
                        false, "Linear", 0f, // red transform
                        false, "Linear", 0f, // green transform
                        false, "Linear", 0f, // blue transform
                        "Death", "Quad", false, // model, selfpropelled
                        false, false, false,  // dieAir, dieLiq, swimLiq
                        false, true, false, // bouncy, rndVel, noTerrainCol
                        true, 0.2f, true, 255, 1f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Fireworks (launcher)":
                    this.selDesc += "SpearAndFang\nPlace a fireworks (explosion) particulator next to this one and configure that one as the death particle.";
                    this.ConfigParticle(
                        0.5f, 0.5f, 0.5f, 0f, 0f, 0f, // min/add pos
                        1.5f, 40f, 1.5f, -1.5f, 10f, -1.5f, // min/add velocity
                        100, 100, 100, 100, true, 255, 255, 255, 255, // colors
                        1, 1, 0.35f, 0.1f, // min/add quantity, min/add life length
                        3f, 0f, 5f, 5f, // min/add gravity, min/add size
                        true, "Linearreduce", 3f, // size transform
                        false, "Linear", 0f, // opacity transform
                        false, "Linear", 0f, // red transform
                        false, "Linear", 0f, // green transform
                        false, "Linear", 0f, // blue transform
                        "Primary", "Cube", false, // model, selfpropelled
                        false, false, false,  // dieAir, dieLiq, swimLiq
                        false, true, true, // bouncy, rndVel, noTerrainCol
                        false, 0.2f, true, 255, 0.1f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 2.5f, 0.1f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Flame (Large)":  //me and captain oats
                    this.selDesc += "SpearAndFang / CaptainOats\nA nice cozy fire.";
                    this.ConfigParticle(
                        0.3f, 1f, 0.3f, 0.4f, 0.2f, 0.4f, // min/add pos
                        0.1f, 0.1f, 0.1f, 0f, 0f, 0f, // min/add velocity
                        0, 120, 40, 5, true, 150, 180, 60, 5, // colors
                        100, 0, 0.3f, 0.5f, // min/add quantity, min/add life length
                        -0.1f, 0f, 0.3f, 0.8f, // min/add gravity, min/add size
                        true, "Linearnullify", -1f, // size transform
                        true, "Smoothstep", -100f, // opacity transform
                        false, "Linear", 0f, // red transform
                        false, "Linear", 0f, // green transform
                        false, "Linear", 0f, // blue transform
                        "Primary", "Quad", true, // model, selfpropelled
                        false, true, false,  // dieAir, dieLiq, swimLiq
                        false, false, true, // bouncy, rndVel, noTerrainCol
                        true, 0.1f, true, 255, 0.01f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Flame (Medium)":  //me and captain oats
                    this.selDesc += "SpearAndFang / CaptainOats\nCampfire sized fire.";
                    this.ConfigParticle(
                        0.55f, 1f, 0.55f, -0.11f, 0f, -0.11f, // min/add pos
                        0.1f, 0.5f, 0.1f, -0.1f, -0.2f, -0.1f, // min/add velocity
                        0, 120, 40, 5, true, 150, 180, 60, 5, // colors
                        0, 50, 0.3f, 0.6f, // min/add quantity, min/add life length
                        -0.03f, 0f, 0.5f, 0.4f, // min/add gravity, min/add size
                        true, "Linearnullify", -1f, // size transform
                        true, "Smoothstep", -100f, // opacity transform
                        false, "Smoothstep", 0.8f, // red transform
                        false, "Smoothstep", 0.8f, // green transform
                        false, "Smoothstep", 0.8f, // blue transform
                        "Primary", "Quad", false, // model, selfpropelled
                        false, true, false,  // dieAir, dieLiq, swimLiq
                        false, false, true, // bouncy, rndVel, noTerrainCol
                        true, 0.3f, true, 255, 0.01f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Flame (Small)":
                    this.selDesc += "Melchior\nCandle sized flame.";
                    this.ConfigParticle(
                        0.5f, 1f, 0.5f, 0f, 0f, 0f, // min/add pos
                        0.01f, 0.1f, 0.1f, 0.125f, 0f, 0f, // min/add velocity
                        8, 238, 75, 0, true, 255, 250, 200, 5, // colors
                        5, 0, 0.07f, 0.14f, // min/add quantity, min/add life length
                        -0.05f, -0.09f, 0.01f, 0.03f, // min/add gravity, min/add size
                        true, "Linear", 0.09f, // size transform
                        true, "Quadratic", -21f, // opacity transform
                        false, "Smoothstep", 0.8f, // red transform
                        false, "Smoothstep", 0.8f, // green transform
                        false, "Smoothstep", 0.8f, // blue transform
                        "Primary", "Quad", false, // model, selfpropelled
                        false, true, false,  // dieAir, dieLiq, swimLiq
                        false, false, true, // bouncy, rndVel, noTerrainCol
                        true, 0.02f, true, 255, 0.01f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Fountain":
                    this.selDesc += "SpearAndFang\nWater shooting into the air.";
                    this.ConfigParticle(
                        0.5f, 1f, 0.5f, 0f, 0f, 0f, // min/add pos
                        0.5f, 0f, 0.5f, -1f, 9f, -1f, // min/add velocity
                        50, 100, 230, 240, true, 80, 100, 240, 240, // colors
                        70, 0, 0.1f, 0.3f, // min/add quantity, min/add life length
                        1.2f, 0f, 0.2f, 0.6f, // min/add gravity, min/add size
                        true, "Sinus", -0.1f, // size transform
                        true, "Quadratic", -5f, // opacity transform
                        false, "Linear", 0f, // red transform
                        false, "Linear", 1f, // green transform
                        false, "Linear", 0f, // blue transform
                        "Primary", "Quad", false, // model, selfpropelled
                        false, true, false,  // dieAir, dieLiq, swimLiq
                        false, false, false, // bouncy, rndVel, noTerrainCol
                        false, 0f, false, 32, 0.01f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Geyser":
                    this.selDesc += "CaptainOats\nSteam spewing from the belly of the earth.";
                    this.ConfigParticle(
                        0.6f, 1f, 0.6f, -0.1f, 0f, -0.1f, // min/add pos
                        0.1f, 7f, 0.1f, -0.2f, -7f, -0.2f, // min/add velocity
                        20, 200, 200, 200, true, 30, 200, 200, 200, // colors
                        600, 0, 10f, 0.2f, // min/add quantity, min/add life length
                        0.05f, 0f, 2f, 4f, // min/add gravity, min/add size
                        true, "Linearincrease", -1f, // size transform
                        true, "Quadratic", -10f, // opacity transform
                        false, "Linear", 0f, // red transform
                        false, "Linear", 1f, // green transform
                        false, "Linear", 0f, // blue transform
                        "Primary", "Quad", true, // model, selfpropelled
                        false, false, false,  // dieAir, dieLiq, swimLiq
                        false, false, false, // bouncy, rndVel, noTerrainCol
                        false, 0.1f, true, 180, 0.11f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 2f, 0.1f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Laser":
                    this.selDesc += "SpearAndFang\nGreen beam of light.";
                    this.ConfigParticle(
                        0.5f, 0.5f, 0.5f, 0f, 0f, 0f, // min/add pos
                        0f, 0f, -5f, 0f, 0f, 10f, // min/add velocity
                        100, 0, 100, 0, true, 100, 0, 100, 0, // colors
                        100, 1000, 0.25f, 0.25f, // min/add quantity, min/add life length
                        0f, 0f, 0.5f, 0.5f, // min/add gravity, min/add size
                        false, "Linear", 0f, // size transform
                        false, "Linear", 0f, // opacity transform
                        false, "Linear", 0f, // red transform
                        false, "Linear", 0f, // green transform
                        false, "Linear", 0f, // blue transform
                        "Primary", "Cube", false, // model, selfpropelled
                        false, true, false,  // dieAir, dieLiq, swimLiq
                        false, false, true, // bouncy, rndVel, noTerrainCol
                        false, 0.1f, true, 255, 0.01f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Popping Candy":
                    this.selDesc += "CaptainOats\nLiven up the area with some popping good fun.";
                    this.ConfigParticle(
                        -5f, 1f, -5f, 11f, 0f, 11f, // min/add pos
                        1f, 5f, 1f, -1f, 0f, -1f, // min/add velocity
                        40, 255, 80, 255, true, 60, 255, 255, 255, // colors
                        20, 10, 0.3f, 0.1f, // min/add quantity, min/add life length
                        0.9f, 0f, 0.4f, 0.2f, // min/add gravity, min/add size
                        true, "Sinus", 10f, // size transform
                        false, "Linear", 1f, // opacity transform
                        false, "Linear", 0f, // red transform
                        true, "Clampedpositivesinus", 1f, // green transform
                        false, "Linear", 0f, // blue transform
                        "Primary", "Cube", false, // model, selfpropelled
                        false, false, true,  // dieAir, dieLiq, swimLiq
                        true, false, false, // bouncy, rndVel, noTerrainCol
                        false, 0f, true, 120, 0.04f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Smoke":
                    this.selDesc += "SpearAndFang\nBillowing black smoke, like a small tire fire.";
                    this.ConfigParticle(
                        0f, 1.1f, 0f, 1f, 0f, 1f, // min/add pos
                        0f, 0.1f, 0f, 0.5f, 0.1f, 0.5f, // min/add velocity
                        34, 22, 22, 22, false, 34, 22, 22, 22, // colors
                        10, 0, 10f, 0f, // min/add quantity, min/add life length
                        -0.1f, 0f, 1f, 1f, // min/add gravity, min/add size
                        true, "Cosinus", 1.5f, // size transform
                        false, "Linear", 0f, // opacity transform
                        false, "Linear", 0f, // red transform
                        false, "Linear", 0f, // green transform
                        false, "Linear", 0f, // blue transform
                        "Primary", "Quad", true, // model, selfpropelled
                        false, false, false,  // dieAir, dieLiq, swimLiq
                        false, false, false, // bouncy, rndVel, noTerrainCol
                        true, 0.2f, false, 255, 0.03f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Sparks":
                    this.selDesc += "SpearAndFang\nRandom yellow sparks. Or is it popcorn?";
                    this.ConfigParticle(
                        0.6f, 1.5f, 0.6f, -0.2f, -0.4f, -0.2f, // min/add pos
                        1f, 1f, 1f, -2f, 0.3f, -2f, // min/add velocity
                        255, 100, 50, 0, true, 255, 100, 100, 0, // colors
                        100, 5, 2f, 0.2f, // min/add quantity, min/add life length
                        2.4f, -0.2f, 0.3f, 0.3f, // min/add gravity, min/add size
                        false, "Linear", 1f, // size transform
                        false, "Linear", 1f, // opacity transform
                        true, "Smoothstep", 1f, // red transform
                        false, "Linear", 1f, // green transform
                        false, "Linear", 1f, // blue transform
                        "Primary", "Cube", false, // model, selfpropelled
                        false, false, false,  // dieAir, dieLiq, swimLiq
                        false, false, false, // bouncy, rndVel, noTerrainCol
                        false, 0f, true, 128, 0.05f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Spirit Magic (falling)":
                    this.selDesc += "CaptainOats\nMagical particles falling from above over a large area. Best viewed at night.";
                    this.ConfigParticle(
                        -20f, 5f, -20f, 41f, 0f, 41f, // min/add pos
                        1f, 0.2f, 1f, -1f, 0.2f, -1f, // min/add velocity
                        100, 100, 100, 120, true, 100, 100, 250, 250, // colors
                        8, 2, 15f, 2f, // min/add quantity, min/add life length
                        0.01f, 0f, 0.5f, 0.2f, // min/add gravity, min/add size
                        true, "Cosinus", 10f, // size transform
                        false, "Linear", 1f, // opacity transform
                        false, "Linear", 0f, // red transform
                        false, "Linear", 0f, // green transform
                        false, "Linear", 0f, // blue transform
                        "Primary", "Cube", false, // model, selfpropelled
                        false, false, true,  // dieAir, dieLiq, swimLiq
                        true, false, false, // bouncy, rndVel, noTerrainCol
                        false, 0f, true, 120, 0.06f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Spirit Magic (rising)":
                    this.selDesc += "CaptainOats\nMagical particles rising over a large area. Best viewed at night.";
                    this.ConfigParticle(
                        -20f, 0f, -20f, 41f, 0.0f, 41f, //min add pos
                        1f, 1.2f, 1f, -1f, 1.2f, 1f, //min max velocity
                        100, 100, 100, 120, true, 100, 100, 250, 250, //colors
                        10, 2, 15f, 2f, //quantity, life length
                        0.01f, 0.0f, 0.8f, 0.4f, // gravity, size
                        true, "Cosinus", 10f, // size transform
                        false, "Linear", 1.0f, // opacity transform
                        false, "Linear", 0.0f, // red transform
                        false, "Linear", 0.0f, // green transform
                        false, "Linear", 0.0f, // blue transform
                        "Primary", "Cube", false, // particle type, model, selfpropelled
                        false, false, true,  // shouldDieInAir, shouldDieInLiquid, shouldSwimOnLiquid, 
                        true, false, false,  // bouncy, randomVelocityChange, noTerrainCollision
                        false, 0.0f, true, 120, 0.06f, // windAffected, windAffectednes,toggleVertexFlags, vertexFlags, particleInterval
                        false, true, 0, 0, 0); //rndColors, loop, pre, duration, post
                    break;
                case "Stationary Cube":
                    this.selDesc += "SpearAndFang\nA great starting point if you're trying to familiarize yourself with the basics.";
                    this.ConfigParticle(
                        0.5f, 1.3f, 0.5f, 0f, 0f, 0f, // min/add pos
                        0f, 0f, 0f, 0f, 0f, 0f, // min/add velocity
                        100, 40, 20, 20, true, 100, 40, 20, 20, // colors
                        1, 1, 0.05f, 0.05f, // min/add quantity, min/add life length
                        0f, 0f, 5f, 5f, // min/add gravity, min/add size
                        false, "Linear", 0f, // size transform
                        false, "Linear", 0f, // opacity transform
                        false, "Linear", 0f, // red transform
                        false, "Linear", 0f, // green transform
                        false, "Linear", 0f, // blue transform
                        "Primary", "Cube", false, // model, selfpropelled
                        false, false, false,  // dieAir, dieLiq, swimLiq
                        true, false, false, // bouncy, rndVel, noTerrainCol
                        false, 0f, true, 255, 0.01f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Unstable":
                    this.selDesc += "SpearAndFang\nYou probably don't want to let this thing out of the box.";
                    this.ConfigParticle(
                        0.5f, 0.5f, 0.5f, 0f, 0f, 0f, // min/add pos
                        5f, 5f, 5f, -10f, -10f, -10f, // min/add velocity
                        0, 0, 0, 0, true, 255, 255, 255, 255, // colors
                        100, 0, 0f, 0.1f, // min/add quantity, min/add life length
                        0f, 0f, 10f, 0f, // min/add gravity, min/add size
                        true, "Smoothstep", 2f, // size transform
                        true, "Smoothstep", 2f, // opacity transform
                        true, "Smoothstep", 2f, // red transform
                        true, "Smoothstep", 2f, // green transform
                        true, "Smoothstep", 2f, // blue transform
                        "Primary", "Cube", false, // model, selfpropelled
                        false, false, false,  // dieAir, dieLiq, swimLiq
                        false, false, false, // bouncy, rndVel, noTerrainCol
                        false, 0f, true, 255, 0.01f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;
                case "Waterfall":
                    this.selDesc += "SpearAndFang\nThe top of a waterfall, one block wide flowing north. Re-position, or link to a block and then replace that block with water.";
                    this.ConfigParticle(
                        0f, 0.9f, 0f, 1.2f, 0f, 1f, // min/add pos
                        0f, 2f, -5f, 0f, 0f, 0.1f, // min/add velocity
                        180, 140, 255, 240, true, 200, 140, 255, 240, // colors
                        50, 0, 0.15f, 0f, // min/add quantity, min/add life length
                        4f, 0f, 1f, 0.2f, // min/add gravity, min/add size
                        false, "Linear", 0f, // size transform
                        true, "Smoothstep", 6f, // opacity transform
                        false, "Linear", 0f, // red transform
                        false, "Linear", 1f, // green transform
                        false, "Linear", 0f, // blue transform
                        "Primary", "Quad", false, // model, selfpropelled
                        false, false, false,  // dieAir, dieLiq, swimLiq
                        false, false, true, // bouncy, rndVel, noTerrainCol
                        false, 0f, false, 64, 0.01f, // windAf, windAfn, toggleVtx, VtX, pInt
                        false, true, 0f, 0f, 0f); // rndpcolors,loop,pre,duration,post
                    break;

                default:
                    this.selDesc = "";
                    break;
            }
            this.Compose();
        }

        public void ConfigParticle(
        float minPosX, float minPosY, float minPosZ, float addPosX, float addPosY, float addPosZ,
        float minVelocityX, float minVelocityY, float minVelocityZ, float addVelocityX, float addVelocityY, float addVelocityZ,
        int colorA, int colorR, int colorG, int colorB, bool toggleRandomColor, int colorRndA, int colorRndR, int colorRndG, int colorRndB,
        int minQuantity, int maxQuantity, float lifeLength, float maxLifeLength,
        float gravityEffect, float maxGravityEffect, float minSize, float maxSize,
        bool toggleSizeTransform, string sizeTransform, float sizeEvolve,
        bool toggleOpacityTransform, string opacityTransform, float opacityEvolve,
        bool toggleRedTransform, string redTransform, float redEvolve,
        bool toggleGreenTransform, string greenTransform, float greenEvolve,
        bool toggleBlueTransform, string blueTransform, float blueEvolve,
        string particleType, string model,
        bool selfPropelled, bool shouldDieInAir, bool shouldDieInLiquid, bool shouldSwimOnLiquid,
        bool bouncy, bool randomVelocityChange, bool noTerrainCollision, bool windAffected, float windAffectednes,
        bool toggleVertexFlags, int vertexFlags, float particleInterval, bool randomParticleColors,
        bool timerLoop, float timerPreDelay, float timerDuration, float timerPostDelay)
        {
            this.particleData.minPosX = minPosX;
            this.particleData.minPosY = minPosY;
            this.particleData.minPosZ = minPosZ;
            this.particleData.addPosX = addPosX;
            this.particleData.addPosY = addPosY;
            this.particleData.addPosZ = addPosZ;
            this.particleData.minVelocityX = minVelocityX;
            this.particleData.minVelocityY = minVelocityY;
            this.particleData.minVelocityZ = minVelocityZ;
            this.particleData.addVelocityX = addVelocityX;
            this.particleData.addVelocityY = addVelocityY;
            this.particleData.addVelocityZ = addVelocityZ;
            this.particleData.colorA = colorA;
            this.particleData.colorR = colorR;
            this.particleData.colorG = colorG;
            this.particleData.colorB = colorB;
            this.particleData.toggleRandomColor = toggleRandomColor;
            this.particleData.colorRndA = colorRndA;
            this.particleData.colorRndR = colorRndR;
            this.particleData.colorRndG = colorRndG;
            this.particleData.colorRndB = colorRndB;
            this.particleData.minQuantity = minQuantity;
            this.particleData.maxQuantity = maxQuantity;
            this.particleData.lifeLength = lifeLength;
            this.particleData.maxLifeLength = maxLifeLength;
            this.particleData.gravityEffect = gravityEffect;
            this.particleData.maxGravityEffect = maxGravityEffect;
            this.particleData.minSize = minSize;
            this.particleData.maxSize = maxSize;
            this.particleData.toggleSizeTransform = toggleSizeTransform;
            this.particleData.sizeTransform = sizeTransform;
            this.particleData.sizeEvolve = sizeEvolve;
            this.particleData.toggleOpacityTransform = toggleOpacityTransform;
            this.particleData.opacityTransform = opacityTransform;
            this.particleData.opacityEvolve = opacityEvolve;
            this.particleData.toggleRedTransform = toggleRedTransform;
            this.particleData.redTransform = redTransform;
            this.particleData.redEvolve = redEvolve;
            this.particleData.toggleGreenTransform = toggleGreenTransform;
            this.particleData.greenTransform = greenTransform;
            this.particleData.greenEvolve = greenEvolve;
            this.particleData.toggleBlueTransform = toggleBlueTransform;
            this.particleData.blueTransform = blueTransform;
            this.particleData.blueEvolve = blueEvolve;
            this.particleData.particleType = particleType;
            this.particleData.model = model;
            this.particleData.selfPropelled = selfPropelled;
            this.particleData.shouldDieInAir = shouldDieInAir;
            this.particleData.shouldDieInLiquid = shouldDieInLiquid;
            this.particleData.shouldSwimOnLiquid = shouldSwimOnLiquid;
            this.particleData.bouncy = bouncy;
            this.particleData.randomVelocityChange = randomVelocityChange;
            this.particleData.noTerrainCollision = noTerrainCollision;
            this.particleData.windAffected = windAffected;
            this.particleData.windAffectednes = windAffectednes;
            this.particleData.toggleVertexFlags = toggleVertexFlags;
            this.particleData.vertexFlags = vertexFlags;
            this.particleData.particleInterval = particleInterval;
            this.particleData.randomParticleColors = randomParticleColors;
            this.particleData.timerLoop = timerLoop;
            this.particleData.timerPreDelay = timerPreDelay;
            this.particleData.timerDuration = timerDuration;
            this.particleData.timerPostDelay = timerPostDelay;

        }
    }
}
