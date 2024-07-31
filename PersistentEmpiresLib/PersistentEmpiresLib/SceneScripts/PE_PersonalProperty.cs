using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using PersistentEmpiresLib.NetworkMessages.Server;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresLib;
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
using Data;
using PersistentEmpiresMission.MissionBehaviors;
using Database.DBEntities;

namespace SceneScripts
{
    public class PE_PersonalProperty : MissionObject
    {
        public int PropertyIndex = -1; 
        public string PropertyName = "Gon's House";
        public string PropertyBanner = "10.31.143.1408.1101.764.764.1.0.0.327.128.116.461.461.764.764.1.0.0";
        public string OnwerId = "";


        protected override void OnInit()
        {
            base.OnInit();
            UpdateBannerFromProperty();
        }

        public void UpdateBannerFromProperty()
        {
            if (GameNetwork.IsClient)
            { 
                try
                { 
                    Banner banner = new Banner(this.PropertyBanner);
                    BannerRenderer.RequestRenderBanner(banner, this.GameEntity);
                }
                catch (Exception ex)
                {
                    Debug.Print("[ERROR PropertyBanner LOG] " + ex.Message);
                } 
            }
        }
    }
}

