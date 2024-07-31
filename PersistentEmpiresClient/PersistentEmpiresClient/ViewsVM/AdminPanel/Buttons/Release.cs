using PersistentEmpiresLib.NetworkMessages.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade; 

namespace PersistentEmpiresClient.ViewsVM.AdminPanel.Buttons
{
    internal class Release : PEAdminButtonVM
    {
        public override string GetCaption()
        {
            return "Release";
        }

        public override void Execute()
        {
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new RequestPrisonerRelease(SelectedPlayer.GetPeer()));
            GameNetwork.EndModuleEventAsClient();
        }
    }
}
