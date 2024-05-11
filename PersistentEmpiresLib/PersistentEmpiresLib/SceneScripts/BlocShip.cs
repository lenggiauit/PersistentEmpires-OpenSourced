using Helpers;
using PersistentEmpiresLib.Helpers;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresLib.SceneScripts.Extensions; 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
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

    public class PE_BlocShip : PE_MoveableMachine
    {
        private Timer timerDestroyShip = null;
        CountDownTimer countDownTimer = null;
        private float timerDestroyShipGameTime = 0;
        private int timerDestroyShipDuration = 20;

        // Credits to fucking Bloc
        public bool isPlayerUsing = false;
        public string Animation = "";
        public int StrayDurationSeconds = 7200;

        public string RidingSkillId = "";
        public int RidingSkillRequired = 0;

        public string RepairingSkillId = "";
        public int RepairingSkillRequired = 0;

        public string RepairItemRecipies = "pe_hardwood*2,pe_wooden_stick*1";
        public int RepairDamage = 20;
        public string RepairItem = "pe_buildhammer";
        public string ParticleEffectOnDestroy = "psys_game_wooden_merlon_destruction";
        public string SoundEffectOnDestroy = "";
        public string ParticleEffectOnRepair = "";
        public string SoundEffectOnRepair = "";
        public bool DestroyedByStoneOnly = false;

        private long WillBeDeletedAt = 0;
        private SkillObject RidingSkill;
        private SkillObject RepairSkill;

        private List<RepairReceipt> receipt = new List<RepairReceipt>();
        private bool _landed;
        private bool destroyed = false;


        private float defaultShipCollisionDistance = 0.1f;

        private float defaultShipDistance = 1.6f;
        /* horizontal  semiaxis */
        private float defaultAdjustShipHS = 2;
        /* vertical semiaxis */
        private float defaultAdjustShipVS = 3;

        private const string specialShip1 = "pe_shipdragon";
        private const string specialShip2 = "pe_shipdragon2";
        private int distanceCount = 2;
        private const string shipwc = "pe_shipwc";
        private const string mediumShip = "pe_ship1";
        private const string smallShip = "pe_ship_e";
        private string CollisionCheckPointTag = "collision_check_point";

        // public Timer DestroySpawnTimer;

        // private float DestroyDuration = 5f;


        private bool isHitting = false;

        public override ScriptComponentBehavior.TickRequirement GetTickRequirement() => !this.GameEntity.IsVisibleIncludeParents() ? base.GetTickRequirement() : ScriptComponentBehavior.TickRequirement.Tick | ScriptComponentBehavior.TickRequirement.TickParallel;

        private void ParseRepairReceipts()
        {
            string[] repairReceipt = this.RepairItemRecipies.Split(',');
            foreach (string receipt in repairReceipt)
            {
                string[] inflictedReceipt = receipt.Split('*');
                string receiptId = inflictedReceipt[0];
                int count = int.Parse(inflictedReceipt[1]);
                this.receipt.Add(new RepairReceipt(receiptId, count));
            }
        }


        private void CheckIfLanded(MatrixFrame oldFrame)
        {
            // if (this._landed) return;

            if (base.GameEntity == null) return;
            if (oldFrame == null) return;
            float heightUnder = Mission.Current.Scene.GetTerrainHeight(this.GameEntity.GlobalPosition.AsVec2, true);
            if (base.GameEntity.GlobalPosition.Z - heightUnder <= 0.2f)
            {
                Debugger.Break();
                if (base.IsMovingBackward) this.StopMovingBackward();
                if (base.IsMovingDown) this.StopMovingDown();
                if (base.IsMovingForward) this.StopMovingForward();
                if (base.IsMovingUp) this.StopMovingUp();
                if (base.IsTurningLeft) this.StopTurningLeft();
                if (base.IsTurningRight) this.StopTurningRight();
                base.GameEntity.SetFrame(ref oldFrame);
                // this._landed = true;
                Mission.Current.MakeSound(SoundEvent.GetEventIdFromString("event:/mission/siege/merlon/wood_destroy"), this.GameEntity.GlobalPosition, false, true, -1, -1);

                this.SetHitPoint(this.HitPoint - 10, new Vec3(0, 0, 0));
                isHitting = true;
                // this.Disable();
            }
        }
         

        private float GetDistanceByAngle(Vec3 centerPoint, float hs, float vs, double t_rad)
        {
            float x = centerPoint.X + (hs * (float)Math.Cos(t_rad));
            float y = centerPoint.Y + (vs * (float)Math.Sin(t_rad));
            float z = centerPoint.z;
            Vec3 newVecByAngle = new Vec3(x, y, z);
            return centerPoint.Distance(newVecByAngle);
        }


        private Vec3 GetCollisonPointByAngle(Vec3 centerPoint, float hs, float vs, double t_rad)
        {
            float x = centerPoint.X + (hs * (float)Math.Cos(t_rad));
            float y = centerPoint.Y + (vs * (float)Math.Sin(t_rad));
            float z = centerPoint.z;
            return new Vec3(x, y, z);
        }

        private double GetRadian(GameEntity e1, GameEntity e2)
        {
            Vec3 currentEnityVecRotationS = e1.GetGlobalFrame().rotation.s;
            Vec3 enityVecRotationS = e2.GetGlobalFrame().rotation.s;
            var dot = Vec3.DotProduct(currentEnityVecRotationS, enityVecRotationS);
            return Math.Acos(Vec3.DotProduct(currentEnityVecRotationS, enityVecRotationS) / (currentEnityVecRotationS.Length) * enityVecRotationS.Length);
        }

        private float GetAdjustShipHSVS(GameEntity gameEntity)
        {
            float entityLength = gameEntity.GetGlobalScale().Normalize();
            switch (gameEntity.Name)
            {
                case specialShip1:
                case specialShip2:
                    {
                        return 140f;
                    }
                case shipwc:
                    {
                        return 150f;
                    }
                case mediumShip:
                    {
                        return 160f;
                    }

                case smallShip:
                    {
                        return 180f;
                    }

                default:
                    return defaultAdjustShipHS;
            }
        }


        private float GetAdjustShipHS(GameEntity gameEntity)
        {
            float entityLength = gameEntity.GetGlobalScale().Normalize();
            switch (gameEntity.Name)
            {
                case specialShip1:
                case specialShip2:
                    {
                        return 9f;
                    }
                case shipwc:
                    {
                        return 6.2f;
                    }
                case mediumShip:
                    {
                        return 5.0f;
                    }

                case smallShip:
                    {
                        return 2.8f;
                    }

                default:
                    return defaultAdjustShipHS;
            }
        }

        private float GetAdjustShipVS(GameEntity gameEntity)
        {
            switch (gameEntity.Name)
            {
                case specialShip1:
                case specialShip2:
                    {
                        return 20f;
                    }
                case shipwc:
                    {
                        return 16.5f;
                    }
                case mediumShip:
                    {
                        return 7.8f;
                    }

                case smallShip:
                    {
                        return 5.5f;
                    }

                default:
                    return defaultAdjustShipVS;
            }

        }


        private void checkHittingObject(MatrixFrame oldFrame)
        {

            if (base.GameEntity == null) return;
            if (oldFrame == null) return;

            List<GameEntity> listEntity = new List<GameEntity>();
            Vec3 entityOrigin = this.GameEntity.GetGlobalFrame().origin; 
            Mission.Current.Scene.GetAllEntitiesWithScriptComponent<PE_BlocShip>(ref listEntity);
            List<GameEntity> listEntity2 = new List<GameEntity>();
            Mission.Current.Scene.GetAllEntitiesWithScriptComponent<PE_ShipCannon>(ref listEntity2);
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

                        if (Helpers.Utilities.HasClosestToDistanceAsVec3(currentEntityCheckPointList, entityCheckPointList, defaultShipCollisionDistance))
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
                        if (this.RidingSkill != null)
                        {
                            int skillValue = this.PilotAgent.Character.GetSkillValue(this.RidingSkill);
                            if (skillValue < this.RidingSkillRequired)
                            {
                                this.PilotAgent.StopUsingGameObjectMT(false);
                                return;
                            }
                        }
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
                    //UpdateParticle();

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
                    this.CheckIfLanded(oldFrame);
                    if (this.PilotAgent != null)
                    {
                        this.checkHittingObject(oldFrame);
                    }
                }

                if (destroyed)
                {

                   
                   Mission.Current.AddParticleSystemBurstByName("psys_game_wooden_merlon_destruction", this.GameEntity.GetGlobalFrame(), true);  
                   base.GameEntity.Remove(0);
                   destroyed = false;

                    //if (this.GameEntity.Name.Equals(specialShip1) || this.GameEntity.Name.Equals(specialShip2))
                    //{
                    //    //base.GameEntity.EntityFlags = EntityFlags.ForceAsStatic;
                    //    //destroyed = false;
                    //    if (this.DestroySpawnTimer.Check(Mission.Current.CurrentTime))
                    //    {
                    //        base.GameEntity.Remove(0);
                    //        destroyed = false;
                    //        this.DestroySpawnTimer.Reset(Mission.Current.CurrentTime, DestroyDuration);
                    //    }
                    //    else
                    //    {
                    //        base.GameEntity.EntityFlags = EntityFlags.ForceAsStatic;
                    //        //if (this.PilotAgent != null)
                    //        //{
                    //        //    NetworkCommunicator player = this.PilotAgent.MissionPeer.GetNetworkPeer();
                    //        //    PersistentEmpireRepresentative persistentEmpireRepresentative = player.GetComponent<PersistentEmpireRepresentative>();
                    //        //    InformationComponent.Instance.SendMessage("Ship will be destroy after:  " + this.DestroySpawnTimer.ElapsedTime(), new Color(1f, 0, 0).ToUnsignedInteger(), persistentEmpireRepresentative.MissionPeer.GetNetworkPeer());

                    //        //}
                    //    }

                    //}
                    //else
                    //{
                    //    //base.GameEntity.Remove(0);
                    //    //destroyed = false;
                    //    if (this.DestroySpawnTimer.Check(Mission.Current.CurrentTime))
                    //    {
                    //        base.GameEntity.Remove(0);
                    //        destroyed = false;
                    //        this.DestroySpawnTimer.Reset(Mission.Current.CurrentTime, DestroyDuration);
                    //    }
                    //    else
                    //    {
                    //        base.GameEntity.EntityFlags = EntityFlags.ForceAsStatic;
                    //        //if (this.PilotAgent != null)
                    //        //{
                    //        //    NetworkCommunicator player = this.PilotAgent.MissionPeer.GetNetworkPeer();
                    //        //    PersistentEmpireRepresentative persistentEmpireRepresentative = player.GetComponent<PersistentEmpireRepresentative>();
                    //        //    InformationComponent.Instance.SendMessage("Ship will be destroy after:  " + this.DestroySpawnTimer.ElapsedTime(), new Color(1f, 0, 0).ToUnsignedInteger(), persistentEmpireRepresentative.MissionPeer.GetNetworkPeer());

                    //        //}
                    //    }
                    //}
                }
            }
            catch (Exception e) {

               // LoggerHelper.LogAnAction ("Error",  "Error", null, new object[] { this.GetType().Name + " - Message:" + e.Message });

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
            if (this.RidingSkillId != "")
            {
                this.RidingSkill = MBObjectManager.Instance.GetObject<SkillObject>(this.RidingSkillId);
            }
            if (this.RepairingSkillId != "")
            {
                this.RepairSkill = MBObjectManager.Instance.GetObject<SkillObject>(this.RepairingSkillId);
            }
            this.ParseRepairReceipts();
            this.ResetStrayDuration();
            this.HitPoint = this.MaxHitPoint;
            defaultShipCollisionDistance = ConfigManager.GetFloatConfig("ShipCollisionDistance", defaultShipCollisionDistance);
        }
        public bool IsAgentFullyUsing(Agent usingAgent)
        {
            return this.PilotAgent == usingAgent;
        }
        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {

            TextObject forStandingPoint = new TextObject(this.IsAgentFullyUsing(GameNetwork.MyPeer.ControlledAgent) ? "{=QGdaakYW}{KEY} Stop Using" : "{=bl2aRW8f}{KEY} Command Ship");
            forStandingPoint.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            return forStandingPoint;
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            if (countDownTimer == null)
            {
                if (this.PilotAgent != null)
                {
                    return new TextObject("{=}" + PilotAgent.Name + "'s Ship").ToString();
                }
                else
                {
                    return new TextObject("{=}Ship").ToString();
                }
            }
            else
            {
                return new TextObject("{=}Ship will be destroy after " + countDownTimer.TimeLeftStr + " s").ToString();
            }
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
               // this.DestroySpawnTimer = new Timer(Mission.Current.CurrentTime, DestroyDuration, false);
                 
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
                WeaponComponentData currentUsageItem = missionWeapon.CurrentUsageItem;
                if (
                    attackerAgent != null &&
                    this.RepairSkill != null &&
                    attackerAgent.Character.GetSkillValue(this.RepairSkill) >= this.RepairingSkillRequired &&
                    missionWeapon.Item != null &&
                    missionWeapon.Item.StringId == this.RepairItem &&
                    attackerAgent.IsHuman &&
                    attackerAgent.IsPlayerControlled &&
                    this.HitPoint != this.MaxHitPoint
                    )
                {
                    // reportDamage = false;
                    NetworkCommunicator player = attackerAgent.MissionPeer.GetNetworkPeer();
                    PersistentEmpireRepresentative persistentEmpireRepresentative = player.GetComponent<PersistentEmpireRepresentative>();
                    if (persistentEmpireRepresentative == null) return false;
                    bool playerHasAllItems = this.receipt.All((r) => persistentEmpireRepresentative.GetInventory().IsInventoryIncludes(r.RepairItem, r.NeededCount));
                    if (!playerHasAllItems)
                    {
                        //TODO: Inform player
                        InformationComponent.Instance.SendMessage("Required Items:", 0x02ab89d9, player);
                        foreach (RepairReceipt r in this.receipt)
                        {
                            InformationComponent.Instance.SendMessage(r.NeededCount + " * " + r.RepairItem.Name.ToString(), 0x02ab89d9, player);
                        }
                        return false;
                    }
                    foreach (RepairReceipt r in this.receipt)
                    {
                        persistentEmpireRepresentative.GetInventory().RemoveCountedItem(r.RepairItem, r.NeededCount);
                    }
                    InformationComponent.Instance.SendMessage((this.HitPoint + this.RepairDamage).ToString() + "/" + this.MaxHitPoint + ", repaired", 0x02ab89d9, player);
                    this.SetHitPoint(this.HitPoint + this.RepairDamage, impactDirection);
                    if (GameNetwork.IsServer)
                    {
                        LoggerHelper.LogAnAction(attackerAgent.MissionPeer.GetNetworkPeer(), LogAction.PlayerRepairesTheDestructable, null, new object[] { this.GetType().Name });
                    }
                }
                else
                {
                    if (this.DestroyedByStoneOnly)
                    {
                        if (currentUsageItem == null || (currentUsageItem.WeaponClass != WeaponClass.Stone && currentUsageItem.WeaponClass != WeaponClass.Boulder) || !currentUsageItem.WeaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithOneHand))
                        {
                            damage = 0;
                        }
                    }
                    if (impactDirection == null) impactDirection = Vec3.Zero;

                    this.SetHitPoint(this.HitPoint - damage, impactDirection);
                    isHitting = true;


                    NetworkCommunicator player = attackerAgent.MissionPeer.GetNetworkPeer();
                    PersistentEmpireRepresentative persistentEmpireRepresentative = player.GetComponent<PersistentEmpireRepresentative>();
                    InformationComponent.Instance.SendMessage("Ship health: " + HitPoint, new Color(1f, 0, 0).ToUnsignedInteger(), persistentEmpireRepresentative.MissionPeer.GetNetworkPeer());

                    if (GameNetwork.IsServer)
                    {
                        LoggerHelper.LogAnAction(attackerAgent.MissionPeer.GetNetworkPeer(), LogAction.PlayerHitToDestructable, null, new object[] { this.GetType().Name });
                    }
                }
                return false;
            }
            catch (Exception e) {
                reportDamage = false;
                return false;
            }
           
        }
    }
}
