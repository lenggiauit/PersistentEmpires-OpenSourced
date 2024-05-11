 
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresMission.MissionBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace PersistentEmpires.Views.Views
{
    public class PEDragonVItemView : MissionView
    {
        public bool RequestedStartUsing = false;
        private DragonVItemBehavior _dragonVItemBehavior;
        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            this._dragonVItemBehavior = base.Mission.GetMissionBehavior<DragonVItemBehavior>();

        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            GameKey defendClick = HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey("Defend");
            if (base.MissionScreen.SceneLayer.Input.IsGameKeyPressed(defendClick.Id))
            {
                this.RequestedStartUsing = this._dragonVItemBehavior.RequestStartUsing();
            }
            else if (base.MissionScreen.SceneLayer.Input.IsGameKeyReleased(defendClick.Id) && this.RequestedStartUsing)
            {
                this._dragonVItemBehavior.RequestStopUsing();
            }
        }
    }
}

