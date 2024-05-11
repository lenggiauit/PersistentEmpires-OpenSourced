 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class AgentUsingDragonVItem : GameNetworkMessage
    {
        public Agent PlayerAgent;
        public int UsingDragonVItemIndex; 
        public AgentUsingDragonVItem() { }
        public AgentUsingDragonVItem(Agent agent, int index)
        {
            this.PlayerAgent = agent;
            this.UsingDragonVItemIndex = index; 
        }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Agents;
        }

        protected override string OnGetLogFormat()
        {
            return "AgentUsingDragonVItem";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.PlayerAgent = Mission.MissionNetworkHelper.GetAgentFromIndex(GameNetworkMessage.ReadAgentIndexFromPacket(ref result));
            this.UsingDragonVItemIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref result); 
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteAgentIndexToPacket(this.PlayerAgent.Index);
            GameNetworkMessage.WriteIntToPacket(this.UsingDragonVItemIndex, new CompressionInfo.Integer(0, 100, true)); 
        }
    }
}
