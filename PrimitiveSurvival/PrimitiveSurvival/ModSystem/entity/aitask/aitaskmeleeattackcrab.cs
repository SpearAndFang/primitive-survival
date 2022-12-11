namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Common;
    using Vintagestory.GameContent;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.Config;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;
    using Vintagestory.API.Util;
    //using System.Diagnostics;


    public class AiTaskMeleeAttackCrab : AiTaskMeleeAttack
    {
        private Entity targetEntity;
        private long lastCheckOrAttackMs;
        private readonly float damage = 3f;
        private readonly float minDist = 1.5f;
        private readonly float minVerDist = 1.5f;
        private bool damageInflicted = false;
        private readonly int attackDurationMs = 500;
        private readonly int damagePlayerAtMs = 300;
        //private BlockSelection blockSel = new BlockSelection();
        //private EntitySelection entitySel = new EntitySelection();
        private readonly string[] seekEntityCodesExact = new string[] { "player" };
        private readonly string[] seekEntityCodesBeginsWith = new string[0];
        private readonly float tamingGenerations = 10f;
        private float curTurnRadPerSec;


        public AiTaskMeleeAttackCrab(EntityAgent entity) : base(entity)
        { }


        public override bool ShouldExecute()
        {
            var ellapsedMs = this.entity.World.ElapsedMilliseconds;
            if (ellapsedMs - this.lastCheckOrAttackMs < this.attackDurationMs || this.cooldownUntilMs > ellapsedMs)
            {
                return false;
            }

            // 1.16
            //if (this.whenInEmotionState != null && !this.entity.HasEmotionState(this.whenInEmotionState))
            if (this.whenInEmotionState != null && this.bhEmo?.IsInEmotionState(this.whenInEmotionState) != true)
            {
                return false;
            }

            // 1.16
            //if (this.whenNotInEmotionState != null && this.entity.HasEmotionState(this.whenNotInEmotionState))
            if (this.whenInEmotionState != null && this.bhEmo?.IsInEmotionState(this.whenNotInEmotionState) == true)
            {
                return false;
            }

            var pos = this.entity.ServerPos.XYZ.Add(0, this.entity.CollisionBox.Y2 / 2, 0).Ahead(this.entity.CollisionBox.XSize / 2, 0, this.entity.ServerPos.Yaw);

            var generation = this.entity.WatchedAttributes.GetInt("generation", 0);
            var fearReductionFactor = Math.Max(0f, (this.tamingGenerations - generation) / this.tamingGenerations);
            if (this.whenInEmotionState != null)
            {
                fearReductionFactor = 1;
            }
            if (fearReductionFactor <= 0)
            {
                return false;
            }

            this.targetEntity = this.entity.World.GetNearestEntity(pos, 3f * fearReductionFactor, 3f * fearReductionFactor, (e) =>
            {
                if (!e.Alive || !e.IsInteractable || e.EntityId == this.entity.EntityId)
                {
                    return false;
                }
                for (var i = 0; i < this.seekEntityCodesExact.Length; i++)
                {
                    if (e.Code.Path == this.seekEntityCodesExact[i])
                    {
                        if (e.Code.Path == "player")
                        {
                            var player = this.entity.World.PlayerByUid(((EntityPlayer)e).PlayerUID);
                            var okplayer =
                                player == null ||
                                (player.WorldData.CurrentGameMode != EnumGameMode.Creative && player.WorldData.CurrentGameMode != EnumGameMode.Spectator && (player as IServerPlayer).ConnectionState == EnumClientState.Playing);

                            return okplayer && this.HasDirectContact(e);
                        }
                        if (this.HasDirectContact(e))
                        {
                            return true;
                        }
                    }
                }

                for (var i = 0; i < this.seekEntityCodesBeginsWith.Length; i++)
                {
                    if (e.Code.Path.StartsWithFast(this.seekEntityCodesBeginsWith[i]) && this.HasDirectContact(e))
                    {
                        return true;
                    }
                }
                return false;
            });

            this.lastCheckOrAttackMs = this.entity.World.ElapsedMilliseconds;
            this.damageInflicted = false;

            return this.targetEntity != null;
        }


        public override bool ContinueExecute(float dt)
        {
            var own = this.entity.ServerPos;
            var his = this.targetEntity.ServerPos;
            var desiredYaw = (float)Math.Atan2(his.X - own.X, his.Z - own.Z);
            var yawDist = GameMath.AngleRadDistance(this.entity.ServerPos.Yaw, desiredYaw);
            this.entity.ServerPos.Yaw += GameMath.Clamp(yawDist, -this.curTurnRadPerSec * dt * GlobalConstants.OverallSpeedMultiplier, this.curTurnRadPerSec * dt * GlobalConstants.OverallSpeedMultiplier);
            this.entity.ServerPos.Yaw %= GameMath.TWOPI;

            if (this.lastCheckOrAttackMs + this.damagePlayerAtMs > this.entity.World.ElapsedMilliseconds)
            {
                return true;
            }
            if (!this.damageInflicted)
            {
                if (!this.HasDirectContact(this.targetEntity))
                {
                    return false;
                }
                var alive = this.targetEntity.Alive;

                this.targetEntity.ReceiveDamage(
                    new DamageSource()
                    {
                        Source = EnumDamageSource.Entity,
                        SourceEntity = entity,
                        Type = damageType,
                        DamageTier = damageTier
                    },
                    this.damage * GlobalConstants.CreatureDamageModifier
                );

                if (alive && !this.targetEntity.Alive)
                {
                    //1.16
                    // this.entity.GetBehavior<EntityBehaviorEmotionStates>()?.TryTriggerState("saturated");
                    this.entity.GetBehavior<EntityBehaviorEmotionStates>()?.TryTriggerState("saturated", 1);
                }

                this.damageInflicted = true;
            }

            if (this.lastCheckOrAttackMs + this.attackDurationMs > this.entity.World.ElapsedMilliseconds)
            {
                return true;
            }
            return false;
        }

        private bool HasDirectContact(Entity targetEntity)
        {
            var targetBox = targetEntity.CollisionBox.ToDouble().Translate(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z);
            var pos = this.entity.ServerPos.XYZ.Add(0, this.entity.CollisionBox.Y2 / 2, 0).Ahead(this.entity.CollisionBox.XSize / 2, 0, this.entity.ServerPos.Yaw);
            var dist = targetBox.ShortestDistanceFrom(pos);
            var vertDist = Math.Abs(targetBox.ShortestVerticalDistanceFrom(pos.Y));
            if (dist >= this.minDist || vertDist >= this.minVerDist)
            {
                return false;
            }

            var rayTraceFrom = this.entity.ServerPos.XYZ;
            rayTraceFrom.Y += 1 / 32f;
            var rayTraceTo = targetEntity.ServerPos.XYZ;
            rayTraceTo.Y += 1 / 32f;
            var directContact = false;
            this.entity.World.RayTraceForSelection(this, rayTraceFrom, rayTraceTo, ref this.blockSel, ref this.entitySel);
            directContact = this.blockSel == null;
            if (!directContact)
            {
                rayTraceFrom.Y += this.entity.CollisionBox.Y2 * 7 / 16f;
                rayTraceTo.Y += targetEntity.CollisionBox.Y2 * 7 / 16f;
                this.entity.World.RayTraceForSelection(this, rayTraceFrom, rayTraceTo, ref this.blockSel, ref this.entitySel);
                directContact = this.blockSel == null;
            }
            if (!directContact)
            {
                rayTraceFrom.Y += this.entity.CollisionBox.Y2 * 7 / 16f;
                rayTraceTo.Y += targetEntity.CollisionBox.Y2 * 7 / 16f;
                this.entity.World.RayTraceForSelection(this, rayTraceFrom, rayTraceTo, ref this.blockSel, ref this.entitySel);
                directContact = this.blockSel == null;
            }
            if (!directContact)
            {
                return false;
            }
            return true;
        }
    }
}

