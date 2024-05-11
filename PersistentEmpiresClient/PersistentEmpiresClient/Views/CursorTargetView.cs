using PersistentEmpires.Views.ViewsVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace PersistentEmpiresClient.Views
{
    public class CursorTargetView : MissionView
    {
        private GauntletLayer _gauntletLayer;
        
        private bool IsActive;
        public CursorTargetView() { }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize(); 
        }
        private void Close()
        {
            this.IsActive = false;
            this._gauntletLayer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(this._gauntletLayer);
            this._gauntletLayer.InputRestrictions.ResetInputRestrictions();
            base.MissionScreen.RemoveLayer(this._gauntletLayer);
            this._gauntletLayer = null;
            Mission.GetMissionBehavior<MissionMainAgentController>().IsChatOpen = false;
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
            if (affectedAgent.IsMine && this.IsActive)
            {
                this.Close();
            }
        }

    }
}
