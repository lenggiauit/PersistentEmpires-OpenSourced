using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresMission.MissionBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SceneScripts
{
    internal class PE_PersonalDoor : UsableMissionObject
    {
        public float Duration = 1f;
        public Vec3 Axis = new Vec3(z: 1f);
        public float Angle = 90f;
        public float Delay = 500f;
        public bool Lockpickable = false;
        public int PropertyIndex = -1;
        private bool isOpen;
        private long lastOpened;
        private MatrixFrame openFrame;
        private MatrixFrame closedFrame;

        protected override void OnInit()
        {
            base.OnInit();
            this.ActionMessage = new TextObject("Door");
            TextObject textObject = new TextObject("Press {KEY} To Use");
            textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            this.DescriptionMessage = textObject;
            this.closedFrame = this.GameEntity.GetFrame();
            MatrixFrame frame = this.GameEntity.GetFrame();
            frame.Rotate(this.Angle.ToRadians(), this.Axis);
            this.openFrame = frame;
        }

        public override bool IsDisabledForAgent(Agent agent)
        {
            return this.IsDeactivated || this.IsDisabledForPlayers && !agent.IsAIControlled || !agent.IsOnLand();
        }

        public override void OnUse(Agent userAgent)
        {
            base.OnUse(userAgent);
            Debug.Print("[USING LOG] AGENT USE " + this.GetType().Name);
            userAgent.StopUsingGameObjectMT();
            if (!GameNetwork.IsServer || (double)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - this.lastOpened) <= (double)this.Delay)
                return; 
            NetworkCommunicator networkPeer = userAgent.MissionPeer.GetNetworkPeer();

            PersonalPropertyBehavior personalPropertyBehavior = Mission.Current.GetMissionBehavior<PersonalPropertyBehavior>();
             
            if (personalPropertyBehavior.PropertyData.Where(p => p.Value.PropertyIndex.Equals(this.PropertyIndex) && p.Value.OwnerId.Equals(networkPeer.VirtualPlayer.Id.ToString())).Count()> 0)
            {
                this.ToggleDoor();
            }  
            else
            {
                Mission.Current.MakeSound(SoundEvent.GetEventIdFromString("event:/mission/movement/foley/door_close"), this.GameEntity.GetGlobalFrame().origin, false, true, -1, -1);
                InformationComponent.Instance.SendMessage("This door is locked", 101106393U, networkPeer);
            }
        }

        public void ToggleDoor()
        {
            this.lastOpened = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (this.isOpen)
                this.CloseDoor();
            else
                this.OpenDoor();
        }

        public void OpenDoor()
        {
            this.SetFrameSynchedOverTime(ref this.openFrame, this.Duration);
            this.isOpen = true;
            Mission.Current.MakeSound(SoundEvent.GetEventIdFromString("event:/mission/movement/foley/door_open"), this.GameEntity.GetGlobalFrame().origin, false, true, -1, -1);
        }

        public void CloseDoor()
        {
            this.SetFrameSynchedOverTime(ref this.closedFrame, this.Duration);
            this.isOpen = false;
            Mission.Current.MakeSound(SoundEvent.GetEventIdFromString("event:/mission/movement/foley/door_close"), this.GameEntity.GetGlobalFrame().origin, false, true, -1, -1);
        }

        protected override bool OnHit(
          Agent attackerAgent,
          int damage,
          Vec3 impactPosition,
          Vec3 impactDirection,
          in MissionWeapon weapon,
          ScriptComponentBehavior attackerScriptComponentBehavior,
          out bool reportDamage)
        {
            reportDamage = false;
            try
            {
                if (Lockpickable && LockpickingBehavior.Instance.Lockpick(attackerAgent, weapon))
                    this.ToggleDoor();
            }
            catch (Exception ex)
            {
                return false;
            }
            return false;
        }

        public override string GetDescriptionText(GameEntity gameEntity = null) => "Use Door"; 
        
    }
}
