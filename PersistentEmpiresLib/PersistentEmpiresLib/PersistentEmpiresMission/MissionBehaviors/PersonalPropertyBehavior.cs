using Data;
using Database.DBEntities;
using PersistentEmpiresLib.Database.DBEntities;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.NetworkMessages.Server;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresLib.SceneScripts;
using SceneScripts;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresMission.MissionBehaviors
{
    public class PersonalPropertyBehavior : MissionNetwork
    { 
        public Dictionary<int, PE_PersonalProperty> Property = new Dictionary<int, PE_PersonalProperty>();
        public Dictionary<int, PersonalProperty> PropertyData { get; set; }

        public delegate void PersonalPropertyAddedHandler(int ppIndex, PE_PersonalProperty pp);  
        public event PersonalPropertyAddedHandler OnPersonalPropertyAdded;  

        public override void AfterStart()
        {
            base.AfterStart();


            List<GameEntity> gameEntities = new List<GameEntity>();
            base.Mission.Scene.GetAllEntitiesWithScriptComponent<PE_PersonalProperty>(ref gameEntities);

            IEnumerable<DBPersonalProperties> dbPersonalProperties = SaveSystemBehavior.HandleGetPersonalProperties();
            Dictionary<int, DBPersonalProperties> savedProperties = new Dictionary<int, DBPersonalProperties>();
            if (dbPersonalProperties != null)
            {
                foreach (DBPersonalProperties property in dbPersonalProperties)
                {
                    savedProperties[property.PropertyIndex] = property;
                    PropertyData[property.PropertyIndex] = new PersonalProperty(property.PropertyIndex, property.PropertyName, new TaleWorlds.Core.Banner(property.PropertyBanner) , property.OwnerId);
                }
            }
            foreach (GameEntity gameEntity in gameEntities)
            {
                PE_PersonalProperty pp = gameEntity.GetFirstScriptOfType<PE_PersonalProperty>();
                if (savedProperties.ContainsKey(pp.PropertyIndex))
                {
                    pp.PropertyIndex = savedProperties[pp.PropertyIndex].PropertyIndex;
                    pp.PropertyName = savedProperties[pp.PropertyIndex].PropertyName;
                    pp.PropertyBanner = savedProperties[pp.PropertyIndex].PropertyBanner;
                    pp.OnwerId = savedProperties[pp.PropertyIndex].OwnerId;
                }
                else
                {
                    this.AddPersonalProperty(pp);
                }

                PE_PersonalDoor pE_PersonalDoor = gameEntity.GetFirstScriptOfType<PE_PersonalDoor>();
                if(pE_PersonalDoor != null)
                {
                    pE_PersonalDoor.PropertyIndex = pp.PropertyIndex;
                }


            } 
        }
          
        public void AddPersonalProperty(PE_PersonalProperty pp)
        {
            Debug.Print("Adding new Personal Property " + pp.PropertyName); 
            if (this.OnPersonalPropertyAdded != null)
                this.OnPersonalPropertyAdded(pp.PropertyIndex, pp);

        } 
    }
}

