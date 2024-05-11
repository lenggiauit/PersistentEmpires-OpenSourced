using Helpers;
using PersistentEmpiresLib.Helpers;
using PersistentEmpiresLib.NetworkMessages.Client;
using PersistentEmpiresLib.PersistentEmpiresMission;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresLib.SceneScripts.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using Timer = TaleWorlds.Core.Timer;

namespace PersistentEmpiresLib.SceneScripts
{
    public class PE_Lifering : PE_MoveableMachine
    {   
        public bool isPlayerUsing = false;
        public string Animation = "";
        public int StrayDurationSeconds = 7200;  
       
        public string ParticleEffectOnDestroy = "psys_game_wooden_merlon_destruction";

        public string ParticleEffectOnWater = "psys_game_water_splash_2";


        public string SoundEffectOnDestroy = "";
        public string ParticleEffectOnRepair = "";
        public string SoundEffectOnRepair = "";
        public bool DestroyedByStoneOnly = false;

        private long WillBeDeletedAt = 0;
        private SkillObject RidingSkill; 
        private bool destroyed = false;
        private float defaultShipCollisionDistance = 0.1f;
        private bool isHitting = false;
        private string CollisionCheckPointTag = "collision_check_point";
        private float currentPositionZ = 0;
        private float waterLevelAdj = ConfigManager.GetFloatConfig("WaterLevelAdj", 0.5f);

        private bool CheckCanElevate()
        {    
            if (base.GameEntity.GlobalPosition.Z >= Mission.Current.Scene.GetWaterLevel() - waterLevelAdj)
            {
                return false;
            }
            else
            {
                return true;
            } 
        }

        public override ScriptComponentBehavior.TickRequirement GetTickRequirement() => !this.GameEntity.IsVisibleIncludeParents() ? base.GetTickRequirement() : ScriptComponentBehavior.TickRequirement.Tick | ScriptComponentBehavior.TickRequirement.TickParallel;

          
        private void CheckInTheWater(MatrixFrame oldFrame) 
        {
            if (base.GameEntity == null) return;
            if (oldFrame == null) return;
            if (!CheckCanElevate())
            { 
                if (this.IsMovingDown)
                {
                    this.StopMovingDown();
                    base.GameEntity.SetFrame(ref oldFrame);
                } 
                if (this.IsMovingUp)
                {
                    this.StopMovingUp();
                    base.GameEntity.SetFrame(ref oldFrame);
                } 
            }
            CanElevate = CheckCanElevate();
        }
          
    
        private void checkHittingObject(MatrixFrame oldFrame)
        {

            if (base.GameEntity == null) return;
            if (oldFrame == null) return;

            List<GameEntity> listEntity = new List<GameEntity>();
            Vec3 entityOrigin = this.GameEntity.GetGlobalFrame().origin;
            Mission.Current.Scene.GetAllEntitiesWithScriptComponent<PE_BlocShip>(ref listEntity);
            List<GameEntity> listEntity2 = new List<GameEntity>();
            Mission.Current.Scene.GetAllEntitiesWithScriptComponent<PE_ShipCanon>(ref listEntity2);
            listEntity.AddRange(listEntity2);

            listEntity = listEntity.Where(e => entityOrigin.Distance(e.GetGlobalFrame().origin) <= 20 && e != this.GameEntity).ToList();

            List<Vec3> currentEntityCheckPointList = GetCollisionCheckPoints(this.GameEntity);

            if (listEntity.Count > 0)
            {
                if (this.PilotAgent != null)
                {
                    NetworkCommunicator player = this.PilotAgent.MissionPeer.GetNetworkPeer();
                    PersistentEmpireRepresentative persistentEmpireRepresentative = player.GetComponent<PersistentEmpireRepresentative>();

                    foreach (GameEntity entity in listEntity)
                    {

                        List<Vec3> entityCheckPointList = GetCollisionCheckPoints(entity);

                        if (Helpers.Utilities.HasClosestToDistanceAsVec2(currentEntityCheckPointList, entityCheckPointList, defaultShipCollisionDistance))
                        {
                            if (this.IsMovingBackward)
                            {
                                this.StopMovingBackward();
                            }
                            if (this.IsMovingDown)
                            {
                                this.StopMovingDown();
                            }
                            if (this.IsMovingForward)
                            {
                                this.StopMovingForward();
                            }
                            if (this.IsMovingUp)
                            {
                                this.StopMovingUp();
                            }
                            if (this.IsTurningLeft)
                            {
                                this.StopTurningLeft();
                            }
                            if (this.IsTurningRight)
                            {
                                this.StopTurningRight();
                            }
                            base.GameEntity.SetFrame(ref oldFrame);
                            Mission.Current.MakeSound(SoundEvent.GetEventIdFromString("event:/mission/siege/merlon/wood_destroy"), this.GameEntity.GlobalPosition, false, true, -1, -1);
                            this.SetHitPoint(this.HitPoint - 10, new Vec3(0, 0, 0));
                            if (this.GetPilotAgent() != null)
                            {
                                this.GetPilotAgent().StopUsingGameObjectMT(false);
                            }
                            this.isHitting = true;
                            break;

                        }


                    }
                }

            }
        }
         

        protected override void OnTick(float dt)
        {
            try
            {
                if (base.GameEntity == null) return;
                MatrixFrame oldFrame = base.GameEntity.GetFrame();
                base.OnTick(dt);
                if (GameNetwork.IsServer)
                {
                    if (this.PilotAgent != null)
                    { 
                        this.ResetStrayDuration();
                    }
                }

                if (GameNetwork.IsClient)
                {
                    if (Agent.Main != null && this.PilotAgent == Agent.Main)
                    {
                        if (Mission.Current.InputManager.IsKeyPressed(InputKey.W))
                        {
                            this.RequestMovingForward();
                        }
                        else if (Mission.Current.InputManager.IsKeyReleased(InputKey.W))
                        {
                            this.RequestStopMovingForward();
                        }
                        if (Mission.Current.InputManager.IsKeyPressed(InputKey.S))
                        {
                            this.RequestMovingBackward();
                        }
                        else if (Mission.Current.InputManager.IsKeyReleased(InputKey.S))
                        {
                            this.RequestStopMovingBackward();
                        }
                        if (Mission.Current.InputManager.IsKeyPressed(InputKey.A))
                        {
                            this.RequestTurningLeft();
                        }
                        else if (Mission.Current.InputManager.IsKeyReleased(InputKey.A))
                        {
                            this.RequestStopTurningLeft();
                        }
                        if (Mission.Current.InputManager.IsKeyPressed(InputKey.D))
                        {
                            this.RequestTurningRight();
                        }
                        else if (Mission.Current.InputManager.IsKeyReleased(InputKey.D))
                        {
                            this.RequestStopTurningRight();
                        }
                        if (Mission.Current.InputManager.IsKeyPressed(InputKey.Space))
                        {
                            this.RequestMovingUp();
                        }
                        else if (Mission.Current.InputManager.IsKeyReleased(InputKey.Space))
                        {
                            this.RequestStopMovingUp();
                        }
                        if (Mission.Current.InputManager.IsKeyPressed(InputKey.LeftShift))
                        {
                            this.RequestMovingDown();
                        }
                        else if (Mission.Current.InputManager.IsKeyReleased(InputKey.LeftShift))
                        {
                            this.RequestStopMovingDown();
                        }

                        if (Mission.Current.InputManager.IsKeyPressed(InputKey.F) || isHitting)
                        {
                            GameNetwork.MyPeer.ControlledAgent.HandleStopUsingAction();
                            this.isPlayerUsing = false;
                            ActionIndexCache ac = ActionIndexCache.act_none;
                            this.PilotAgent.SetActionChannel(0, ac, true, 0UL, 0.0f, 1f, -0.2f, 0.4f, 0, false, -0.2f, 0, true);
                            isHitting = false; 

                        }
                        
                    }
                }

                if (this.PilotAgent == null)
                {
                    if (base.IsMovingBackward) this.StopMovingBackward();
                    if (base.IsMovingDown) this.StopMovingDown();
                    if (base.IsMovingForward) this.StopMovingForward();
                    if (base.IsMovingUp) this.StopMovingUp();
                    if (base.IsTurningLeft) this.StopTurningLeft();
                    if (base.IsTurningRight) this.StopTurningRight();
                }
                if (GameNetwork.IsServer)
                {  
                    if (this.PilotAgent != null)
                    {
                        this.CheckInTheWater(oldFrame);
                        this.checkHittingObject(oldFrame);
                    }
                    if (base.GameEntity.GlobalPosition.Z >= Mission.Current.Scene.GetWaterLevel() - waterLevelAdj)
                    {
                        Vec3 vec3 = base.GameEntity.GetGlobalFrame().origin;
                        MatrixFrame newMatrixFrame = new MatrixFrame(base.GameEntity.GetGlobalFrame().rotation, new Vec3(vec3.X, vec3.Y, Mission.Current.Scene.GetWaterLevel() - waterLevelAdj));
                        base.GameEntity.SetGlobalFrame(newMatrixFrame);
                    }
                }

                if (GameNetwork.IsClient)
                {
                    if (CheckCanElevate() && this.PilotAgent != null)
                    {
                        this.RequestMovingUp();
                    }
                    else
                    {
                        this.RequestStopMovingUp(); 
                    }
                    //UpdateParticle();
                }
                  
                if (destroyed)
                { 
                    Mission.Current.AddParticleSystemBurstByName("psys_game_wooden_merlon_destruction", this.GameEntity.GetGlobalFrame(), true);
                    base.GameEntity.Remove(0);
                    destroyed = false; 
                }
            }
            catch (Exception e)
            { 
                // LoggerHelper.LogAnAction ("Error",  "Error", null, new object[] { this.GetType().Name + " - Message:" + e.Message });

            }
        }

        private void UpdateParticle()
        {
            if (this.GameEntity == null)
                return;

            var tail = this.GameEntity.GetFirstChildEntityWithTag("shiptail_point");
            if (tail != null)
            {
                if (IsMovingBackward || IsMovingDown || IsMovingUp || IsMovingForward)
                {
                    tail.ResumeParticleSystem(true);
                }
                else
                {
                    tail.PauseParticleSystem(true);
                }
            }
        }


        private List<Vec3> GetCollisionCheckPoints(GameEntity gameEntity)
        {
            List<Vec3> list = new List<Vec3>();
            foreach (var cp in gameEntity.GetChildren().Where(c => c.HasTag(CollisionCheckPointTag)))
            {
                list.Add(cp.GetGlobalFrame().origin);
            }
            return list;
        }
         

        protected override void OnTickParallel(float dt)
        {
            base.OnTickParallel(dt);
            if (!base.GameEntity.IsVisibleIncludeParents())
            {
                return;
            }
        }
        protected override void OnInit()
        {
            base.OnInit();
            foreach (StandingPoint standingPoint in this.StandingPoints)
            {
                standingPoint.AutoSheathWeapons = true;
            }
             
            this.ResetStrayDuration();
            this.HitPoint = this.MaxHitPoint; 
        }
        public bool IsAgentFullyUsing(Agent usingAgent)
        {
            return this.PilotAgent == usingAgent;
        }
        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {

            TextObject forStandingPoint = new TextObject(this.IsAgentFullyUsing(GameNetwork.MyPeer.ControlledAgent) ? "{=bl2aRW8f}{KEY} Stop Using" : "{=bl2aRW8f}{KEY} Use Lifering");
            forStandingPoint.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            return forStandingPoint;
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            
            return new TextObject("{zAK15Sy2} Lifering").ToString();
            
        }

        public override bool IsStray()
        {
            if (this.PilotAgent != null) return false;
            return this.WillBeDeletedAt < DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public override void ResetStrayDuration()
        {
            this.WillBeDeletedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + this.StrayDurationSeconds;
        }

        public override void SetHitPoint(float hitPoint, Vec3 impactDirection)
        {
            this.HitPoint = hitPoint;
            MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();


            if (this.HitPoint > this.MaxHitPoint) this.HitPoint = this.MaxHitPoint;
            if (this.HitPoint < 0) this.HitPoint = 0;

            if (this.HitPoint == 0)
            {
                if (this.PilotAgent != null)
                {
                    this.PilotAgent.StopUsingGameObjectMT(false);
                }
                if (this.ParticleEffectOnDestroy != "")
                {
                    Mission.Current.Scene.CreateBurstParticle(ParticleSystemManager.GetRuntimeIdByName(this.ParticleEffectOnDestroy), globalFrame);
                }
                if (this.SoundEffectOnDestroy != "")
                {
                    Mission.Current.MakeSound(SoundEvent.GetEventIdFromString(this.SoundEffectOnDestroy), globalFrame.origin, false, true, -1, -1);
                }

                this.destroyed = true; 

            }
            if (this.HitPoint == this.MaxHitPoint)
            {
                if (this.ParticleEffectOnRepair != "")
                {
                    Mission.Current.Scene.CreateBurstParticle(ParticleSystemManager.GetRuntimeIdByName(this.ParticleEffectOnRepair), globalFrame);
                }
                if (this.SoundEffectOnRepair != "")
                {
                    Mission.Current.MakeSound(SoundEvent.GetEventIdFromString(this.SoundEffectOnRepair), globalFrame.origin, false, true, -1, -1);
                }
            }
        }


        protected override bool OnHit(Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, out bool reportDamage)
        {
            try
            {
                reportDamage = true;
                MissionWeapon missionWeapon = weapon; 
                if (impactDirection == null) impactDirection = Vec3.Zero; 
                this.SetHitPoint(this.HitPoint - damage, impactDirection);
                isHitting = true;  
                NetworkCommunicator player = attackerAgent.MissionPeer.GetNetworkPeer();
                PersistentEmpireRepresentative persistentEmpireRepresentative = player.GetComponent<PersistentEmpireRepresentative>();
                InformationComponent.Instance.SendMessage("Lifering health: " + HitPoint, new Color(1f, 0, 0).ToUnsignedInteger(), persistentEmpireRepresentative.MissionPeer.GetNetworkPeer());

                if (GameNetwork.IsServer)
                {
                    LoggerHelper.LogAnAction(attackerAgent.MissionPeer.GetNetworkPeer(), LogAction.PlayerHitToDestructable, null, new object[] { this.GetType().Name });
                } 
                return false;
            }
            catch (Exception e)
            {
                reportDamage = false;
                return false;
            }

        }
    }
}
