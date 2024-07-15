using PersistentEmpiresLib.NetworkMessages.Client;
using PersistentEmpiresLib.NetworkMessages.Server;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using System.IO; 
using TaleWorlds.Library;
using PersistentEmpiresLib.Helpers; 

namespace PersistentEmpiresMission.MissionBehaviors
{

    public struct DragonVItem
    {
        public ItemObject Item;
        public ActionIndexCache Animation;
        public float Duration;  
        public string UsingSound;
        public bool UseInWater;
        public DragonVItem(string itemId, string animation, float duration, string usingSound, bool useInWater)
        {
            this.Item = MBObjectManager.Instance.GetObject<ItemObject>(itemId); 
            this.Animation = ActionIndexCache.Create(animation);
            this.Duration = duration;
            this.UsingSound = usingSound; 
            this.UseInWater = useInWater;
        }
    }
    public class DragonVItemBehavior : MissionNetwork
    {
        private class UsingAction
        {
            public Agent PlayerAgent;
            public DragonVItem DragonVItem; 

            public UsingAction(Agent player, DragonVItem dragonVItem)
            {
                this.PlayerAgent = player;
                this.DragonVItem = dragonVItem; 
            }
        }

        public List<DragonVItem> DragonVItems = new List<DragonVItem>();
        private Dictionary<Agent, UsingAction> AgentsUsingItem = new Dictionary<Agent, UsingAction>(); 
        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            this.AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
            foreach (ModuleInfo module in ModuleHelper.GetModules())
            {
                /*if (module.IsSelected || GameNetwork.IsServer)
                {*/
                this.LoadDragonVItems(module.Id);
                //}
            }
        }
        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();
            this.AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Remove);
            this.DragonVItems.Clear();
        }

        private void LoadDragonVItems(string moduleId)
        {
            string dragonvItemsPath = ModuleHelper.GetXmlPath(moduleId, "dragonvItems");
            if (File.Exists(dragonvItemsPath) == false) return;
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(dragonvItemsPath);
            foreach (XmlNode node in xmlDocument.SelectNodes("/DragonVItems/DragonVItem"))
            {
                string ItemId = node["ItemId"].InnerText;
                string animation = node["Animation"].InnerText;
                float duration = float.Parse(node["Duration"].InnerText);
                string usingSound = node["UsingSound"].InnerText;
                bool useInWater = bool.Parse(node["UseInWater"].InnerText);
                DragonVItem item = new DragonVItem(ItemId, animation, duration, usingSound, useInWater);
                this.DragonVItems.Add(item);
            }
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (GameNetwork.IsServer)
            {
                return;
            }
        }
         

        public bool RequestStartUsing()
        {
            Agent myAgent = GameNetwork.MyPeer.ControlledAgent;
            if (myAgent == null) return false;

            EquipmentIndex wieldedIndex = myAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            if (wieldedIndex == EquipmentIndex.None) return false;

            MissionWeapon equipment = myAgent.Equipment[wieldedIndex];
            if (equipment.IsEmpty) return false;

            DragonVItem item = this.DragonVItems.FirstOrDefault(f => f.Item != null && f.Item.StringId == equipment.Item.StringId);
            if (item.Item == null) return false;

            if (myAgent.HasMount) return false;

            if (item.UsingSound != "")
            {
                base.Mission.MakeSound(SoundEvent.GetEventIdFromString(item.UsingSound), myAgent.Position, false, true, -1, -1);
            }
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new RequestStartUsingDragonVItem());
            GameNetwork.EndModuleEventAsClient();
           // myAgent.SetActionChannel(0, item.Animation, true, 0UL, 0.0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
            return true;
        }

        public void RequestStopUsing()
        { 
            Agent myAgent = GameNetwork.MyPeer.ControlledAgent;
            if (myAgent == null) return;

            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new RequestStopUsingDragonVItem());
            GameNetwork.EndModuleEventAsClient();

          //  myAgent.SetActionChannel(0, ActionIndexCache.act_none, true, 0UL, 0.0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
          

        }

        private void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode mode)
        {
            GameNetwork.NetworkMessageHandlerRegisterer networkMessageHandlerRegisterer = new GameNetwork.NetworkMessageHandlerRegisterer(mode);
            if (GameNetwork.IsClient)
            {
                networkMessageHandlerRegisterer.Register<AgentUsingDragonVItem>(this.HandleAgentUsingDragonVItemFromServer);
            }
            else if (GameNetwork.IsServer)
            {
                networkMessageHandlerRegisterer.Register<RequestStartUsingDragonVItem>(this.HandleRequestStartUsingFromClient);
                networkMessageHandlerRegisterer.Register<RequestStopUsingDragonVItem>(this.HandleRequestStopUsingFromClient);
            }

        }
        private void StopAgentUsing(Agent agent)
        {
            Agent myAgent = GameNetwork.MyPeer.ControlledAgent;
            if (myAgent == null) return;

            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new RequestStopUsingDragonVItem());
            GameNetwork.EndModuleEventAsClient();

            myAgent.SetActionChannel(0, ActionIndexCache.act_none, true, 0UL, 0.0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
        }
        private void SpawnItem(Agent userAgent, DragonVItem dragonItem)
        {
            EquipmentIndex equipmentIndex = userAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            userAgent.RemoveEquippedWeapon(equipmentIndex); 
            Vec3 vecspawnFrame = new Vec3(userAgent.Position.X + 1, userAgent.Position.Y + 1, userAgent.Position.Z + 1);

            MatrixFrame adjSpawnFrame = userAgent.Frame;

            MissionObject mObject = Mission.Current.CreateMissionObjectFromPrefab(dragonItem.Item.StringId, adjSpawnFrame);
              
            LoggerHelper.LogAnAction(userAgent.MissionPeer.GetNetworkPeer(), "Spawn Dragon V Item", null, new object[] {
                dragonItem.Item.Name
            });

           
        }
        private void HandleAgentUsingDragonVItemFromServer(AgentUsingDragonVItem message)
        {
            if (message.PlayerAgent == null || message.PlayerAgent.IsActive() == false) return; 
            //this.StopAgentUsing(message.PlayerAgent);
            if (this.DragonVItems.Count > message.UsingDragonVItemIndex)
            {
                this.SpawnItem(message.PlayerAgent, this.DragonVItems[message.UsingDragonVItemIndex]);
            } 
        }

        private bool HandleRequestStopUsingFromClient(NetworkCommunicator peer, RequestStopUsingDragonVItem message)
        {
            if (peer.ControlledAgent == null) return false;
            if (this.AgentsUsingItem.ContainsKey(peer.ControlledAgent))
            {
                peer.ControlledAgent.SetActionChannel(0, ActionIndexCache.act_none, true, 0UL, 0.0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
            }

            return true;
        }
        private bool HandleRequestStartUsingFromClient(NetworkCommunicator peer, RequestStartUsingDragonVItem message)
        { 
            if (peer.ControlledAgent == null) return false;
            PersistentEmpireRepresentative persistentEmpireRepresentative = peer.GetComponent<PersistentEmpireRepresentative>();
            if (persistentEmpireRepresentative == null) return false;

            EquipmentIndex index = peer.ControlledAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            if (index == EquipmentIndex.None) return false;
            MissionWeapon equipmentElement = peer.ControlledAgent.Equipment[index];
             
            DragonVItem item = this.DragonVItems.FirstOrDefault(f => f.Item.StringId == equipmentElement.Item.StringId);
            //

            if (item.Item == null) return false;

            float waterLevelAdj = ConfigManager.GetFloatConfig("WaterLevelAdj", 0.2f);
            if (peer.ControlledAgent.Position.Z >= Mission.Current.Scene.GetWaterLevel() - waterLevelAdj)
            {
                InformationComponent.Instance.SendMessage("Cannot use this item here, please use it in the water.", new Color(1f, 0, 0).ToUnsignedInteger(), peer);
                return false;
            }
            else
            { 
                UsingAction usingAction = new UsingAction(peer.ControlledAgent, item);
                // this.AgentsUsingItem[peer.ControlledAgent] = usingAction;
                peer.ControlledAgent.SetActionChannel(0, item.Animation, true, 0UL, 0.0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
                this.SpawnItem(peer.ControlledAgent, item);
                peer.ControlledAgent.SetActionChannel(0, ActionIndexCache.act_none, true, 0UL, 0.0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
                
                return true;
            }
        }
    }
}
