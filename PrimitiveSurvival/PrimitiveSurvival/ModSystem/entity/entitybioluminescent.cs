namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.MathTools;
    using Vintagestory.GameContent;

    public class EntityBioluminescent : EntityGenericGlowingAgent
    {
        //double sitHeight = 1;
        public double windMotion;
        private int cnt = 0;

        //public int offset;
        //private static readonly Random Rnd = new Random();

        static EntityBioluminescent()
        {
            AiTaskRegistry.Register<AiTaskButterflyWander>("butterflywander");
            AiTaskRegistry.Register<AiTaskButterflyRest>("butterflyrest");
            AiTaskRegistry.Register<AiTaskButterflyChase>("butterflychase");
            AiTaskRegistry.Register<AiTaskButterflyFlee>("butterflyflee");
            AiTaskRegistry.Register<AiTaskButterflyFeedOnFlowers>("butterflyfeedonflowers");
        }

        public override bool IsInteractable => false;


        public override void Initialize(EntityProperties properties, ICoreAPI api, long inChunkIndex3d)
        {
            WeatherSystemBase wsys;
            RoomRegistry roomReg;
            //offset = Rnd.Next(10);
            base.Initialize(properties, api, inChunkIndex3d);

            if (api.Side == EnumAppSide.Client)
            {
                this.WatchedAttributes.RegisterModifiedListener("windWaveIntensity", () => (this.Properties.Client.Renderer as EntityShapeRenderer).WindWaveIntensity = this.WatchedAttributes.GetDouble("windWaveIntensity"));
            }

            wsys = api.ModLoader.GetModSystem<WeatherSystemBase>();
            roomReg = api.ModLoader.GetModSystem<RoomRegistry>();
        }



        public override void OnGameTick(float dt)
        {
            if (this.World.Side == EnumAppSide.Server)
            {
                base.OnGameTick(dt);
                return;
            }

            if (!this.AnimManager.ActiveAnimationsByAnimCode.ContainsKey("feed") && !this.AnimManager.ActiveAnimationsByAnimCode.ContainsKey("rest"))
            {
                if (this.ServerPos.Y < this.Pos.Y - 0.25 && !this.Collided)
                {
                    this.SetAnimation("glide", 1);
                }
                else
                {
                    this.SetAnimation("fly", 2);
                }
            }


            base.OnGameTick(dt);

            if (this.cnt++ > 30)
            {
                float affectedness = this.World.BlockAccessor.GetLightLevel(this.SidedPos.XYZ.AsBlockPos, EnumLightLevelType.OnlySunLight) < 14 ? 1 : 0;
                this.windMotion = this.Api.ModLoader.GetModSystem<WeatherSystemBase>().WeatherDataSlowAccess.GetWindSpeed(this.SidedPos.XYZ * affectedness);
                this.cnt = 0;
            }

            if (this.AnimManager.ActiveAnimationsByAnimCode.ContainsKey("fly"))
            {
                this.SidedPos.X += Math.Max(0, (this.windMotion - 0.2) / 20.0);
            }

            if (this.ServerPos.SquareDistanceTo(this.Pos.XYZ) > 0.01)
            {
                var desiredYaw = (float)Math.Atan2(this.ServerPos.X - this.Pos.X, this.ServerPos.Z - this.Pos.Z);

                var yawDist = GameMath.AngleRadDistance(this.SidedPos.Yaw, desiredYaw);
                this.Pos.Yaw += GameMath.Clamp(yawDist, -35 * dt, 35 * dt);
                this.Pos.Yaw %= GameMath.TWOPI;
            }
        }


        private void SetAnimation(string animCode, float speed)
        {
            if (!this.AnimManager.ActiveAnimationsByAnimCode.TryGetValue(animCode, out var animMeta))
            {
                animMeta = new AnimationMetaData()
                {
                    Code = animCode,
                    Animation = animCode,
                    AnimationSpeed = speed,
                };

                this.AnimManager.ActiveAnimationsByAnimCode.Clear();
                this.AnimManager.ActiveAnimationsByAnimCode[animMeta.Animation] = animMeta;
                return;
            }

            animMeta.AnimationSpeed = speed;
            this.UpdateDebugAttributes();
        }

        public override void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
        {
            // We control glide and fly animations entirely client side

            if (activeAnimationsCount == 0)
            {
                this.AnimManager.ActiveAnimationsByAnimCode.Clear();
                this.AnimManager.StartAnimation("fly");
            }

            var active = "";

            var found = false;

            for (var i = 0; i < activeAnimationsCount; i++)
            {
                var crc32 = activeAnimations[i];
                for (var j = 0; j < this.Properties.Client.LoadedShape.Animations.Length; j++)
                {
                    var anim = this.Properties.Client.LoadedShape.Animations[j];
                    var mask = ~(1 << 31); // Because I fail to get the sign bit transmitted correctly over the network T_T
                    if ((anim.CodeCrc32 & mask) == (crc32 & mask))
                    {
                        if (this.AnimManager.ActiveAnimationsByAnimCode.ContainsKey(anim.Code))
                        { break; }
                        if (anim.Code == "glide" || anim.Code == "fly")
                        { continue; }

                        var code = anim.Code ?? anim.Name.ToLowerInvariant();
                        active += ", " + code;
                        this.Properties.Client.AnimationsByMetaCode.TryGetValue(code, out var animmeta);

                        if (animmeta == null)
                        {
                            animmeta = new AnimationMetaData()
                            {
                                Code = code,
                                Animation = code,
                                CodeCrc32 = anim.CodeCrc32
                            };
                        }

                        animmeta.AnimationSpeed = activeAnimationSpeeds[i];

                        this.AnimManager.ActiveAnimationsByAnimCode[anim.Code] = animmeta;

                        found = true;
                    }
                }
            }

            if (found)
            {
                this.AnimManager.StopAnimation("fly");
                this.AnimManager.StopAnimation("glide");

                if (this.FeetInLiquid)
                {
                    (this.Properties.Client.Renderer as EntityShapeRenderer).AddRenderFlags |= 1 << 12;
                }
                else
                {
                    (this.Properties.Client.Renderer as EntityShapeRenderer).AddRenderFlags &= ~(1 << 12);
                }
            }
            else
            {
                (this.Properties.Client.Renderer as EntityShapeRenderer).AddRenderFlags = 0;
            }

        }
    }
}
