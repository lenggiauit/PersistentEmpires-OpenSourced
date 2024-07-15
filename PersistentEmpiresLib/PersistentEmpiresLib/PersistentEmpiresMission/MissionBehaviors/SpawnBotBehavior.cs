using PersistentEmpiresLib.NetworkMessages.Server;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using PersistentEmpiresLib.Factions;
using TaleWorlds.ObjectSystem;
using static TaleWorlds.MountAndBlade.Agent;
using static TaleWorlds.MountAndBlade.HumanAIComponent;
using PersistentEmpiresLib.Database.DBEntities;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.Engine;  
using TaleWorlds.CampaignSystem.Settlements;
using System.ComponentModel;
using static TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultPerks;
using TaleWorlds.ModuleManager;
using System.IO;
using Newtonsoft.Json;

namespace PersistentEmpiresMission.MissionBehaviors
{

    public class BotConfig
    { 
        public int Id { get; set; } 
        public float[] Position { get; set; }
        public float Range { get; set; }  
    }

    public class BotAgent
    {
        public Agent Agent { get; set; }
        public BotConfig Config {  get; set; }     
    }



    public class SpawnBotBehavior : MissionLogic
    {
        List<BotConfig> BotConfigList { get; set; } 
        Dictionary<int,long> WillBeSpawnAts = new Dictionary<int, long>();
        Dictionary<int, long> WillBeIdleAts = new Dictionary<int, long>(); 
        private int SpawnDuration = 40;
        private int IdleDuration = 10;
        Vec3 botInitialPosition = new Vec3(1374.61f, 1101.65f, 1.41434f);
        List<string> botIndexs = new List<string>()
        {
            {"event_theoden" },
            {"event_rohan_officer" },
            {"event_rohan_archer" }, 
            {"event_rohan_infantry" },
            {"event_galadhrim_warrior" },
            {"event_uruk_commander" },
            {"event_uruk_berserker" },
            {"event_uruk_crossbow" },
            {"event_uruk_pikeman" },
            {"event_uruk_infantry" }, 
            {"event_legolas"} 
        };
         
        List<BotAgent> botAgents = new List<BotAgent>();  
         
        protected void MissionSpawnBot(BotConfig botConfig)
        {
            var rnd = new Random(DateTime.Now.Millisecond);
            int botCharIndex = rnd.Next(0, botIndexs.Count()); 
            FactionsBehavior factionBehavior = Mission.Current.GetMissionBehavior<FactionsBehavior>();
            Banner banner = new Banner("11.116.116.1408.1101.764.764.1.0.0.309.40.116.735.435.764.764.1.0.0.309.40.116.735.435.764.764.1.1.0.347.40.116.250.250.764.524.1.0.0");
            Faction outlaws = factionBehavior.Factions[1]; 
            BasicCharacterObject basicCharacterObject = MBObjectManager.Instance.GetObject<BasicCharacterObject>(botIndexs[botCharIndex]); 
            uint color =  banner.GetPrimaryColor();
            uint color2 = banner.GetFirstIconColor();
             
            AgentBuildData agentBuildData = new AgentBuildData(basicCharacterObject)
                .Team(outlaws.team) 
                .TroopOrigin(new BasicBattleAgentOrigin(basicCharacterObject)) 
                .ClothingColor1(color)
                .ClothingColor2(color2)
                .IsFemale(basicCharacterObject.IsFemale)
                .VisualsIndex(0);
            agentBuildData.Equipment(Equipment.GetRandomEquipmentElements(basicCharacterObject, !GameNetwork.IsMultiplayer, isCivilianEquipment: false, agentBuildData.AgentEquipmentSeed));
            agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, basicCharacterObject.GetBodyPropertiesMin(), basicCharacterObject.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, basicCharacterObject.HairTags, basicCharacterObject.BeardTags, basicCharacterObject.TattooTags));
            

            MatrixFrame spawnFrame = base.Mission.GetMissionBehavior<SpawnFrameSelectionBehavior>().DefaultSpawnFrames[0].GameEntity.GetGlobalFrame();

            Vec2 v;
            if (!(spawnFrame.origin != agentBuildData.AgentInitialPosition))
            {
                v = spawnFrame.rotation.f.AsVec2.Normalized();
                Vec2? agentInitialDirection = agentBuildData.AgentInitialDirection;
                if (!(v != agentInitialDirection))
                {
                    Debug.FailedAssert("PE Spawn frame could not be found.", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\SpawnBehaviors\\SpawningBehaviors\\SpawningBehaviorBase.cs", "OnTick", 194);
                }
            }

            Vec3 initialPosition = new Vec3(botConfig.Position[0], botConfig.Position[1], botConfig.Position[2]); 
            agentBuildData.InitialPosition(initialPosition); 
            v = spawnFrame.rotation.f.AsVec2; 
            agentBuildData.InitialDirection(v); 
             
            Agent agentBot = Mission.SpawnAgent(agentBuildData, true); 

            agentBot.SetBehaviorValueSet(BehaviorValueSet.Charge);
            agentBot.AgentVisuals.SetVoiceDefinitionIndex(-1, 0f); 
            agentBot.AIStateFlags |= Agent.AIStateFlag.Alarmed; 
             
            agentBot.SetTargetPosition(new Vec2(botConfig.Position[0] , botConfig.Position[1])); 
            Equipment equipment = basicCharacterObject.Equipment.Clone(false);
            agentBuildData.Equipment(equipment); 
            
            //agentBot.SetTeam(outlaws.team, true); 
            
            agentBot.BaseHealthLimit = ConfigManager.GetFloatConfig("BotHealth", 800f);
            agentBot.HealthLimit = agentBot.BaseHealthLimit;
            agentBot.Health = agentBot.HealthLimit;

            agentBot.OnAgentHealthChanged += AgentBot_OnAgentHealthChanged; 
            
            BotAgent  bot = new BotAgent();
            bot.Agent = agentBot;
            bot.Config = botConfig;
            botAgents.Add(bot);   
        }

        private void AgentBot_OnAgentHealthChanged(Agent agent, float oldHealth, float newHealth)
        {
            if (agent != null)
            {
                  
                if (newHealth < 50f)
                {
                   // agent.AIStateFlags |= Agent.AIStateFlag.Cautious;
                }

                if (newHealth <= 0f)
                { 
                    BotAgent botAgent = this.botAgents.Where(b => b.Agent.Equals(agent)).First();
                    if (botAgent != null)
                    {
                        WillBeSpawnAts[botAgent.Config.Id] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + SpawnDuration;
                        this.botAgents.Remove(botAgent);
                         
                    }
                }
            }
              

        }

        void BotIdleMoving( int id, Agent agentBot)
        {
            if (WillBeIdleAts[id] < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                var botAgent = this.botAgents.Where(ba => ba.Agent.Equals(agentBot)).First();
                if (botAgent != null)
                { 
                    Vec3 initPosition = new Vec3(botAgent.Config.Position[0], botAgent.Config.Position[1], botAgent.Config.Position[2]);
                    if (agentBot.Position.Distance(initPosition) > 10f)
                    {
                        agentBot.ResetEnemyCaches();
                        agentBot.SetTargetPosition(initPosition.AsVec2); 
                    }
                    else
                    {
                        var rnd = new Random(DateTime.Now.Millisecond);
                        int x = rnd.Next(-10, 10);
                        int y = rnd.Next(-10, 10);
                        agentBot.ResetEnemyCaches();
                        agentBot.SetTargetPosition(new Vec2(initPosition.X + x, initPosition.Y + y)); 
                    }
                    WillBeIdleAts[id] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + IdleDuration;
                }
                
            } 
        }

        public override void OnAfterMissionCreated()
        {
            base.OnAfterMissionCreated(); 
            string jsonPath = ModuleHelper.GetModuleFullPath(ConfigManager.ModuleId) + "ModuleData/"  + "botConfigs.json";
            if (File.Exists(jsonPath))
            {
                using (StreamReader r = new StreamReader(jsonPath))
                {
                    string json = r.ReadToEnd();
                    BotConfigList = JsonConvert.DeserializeObject<List<BotConfig>>(json);
                }
            }
        }

        public override void OnMissionTick(float dt)
        {

            try
            {
                base.OnMissionTick(dt); 

                foreach (BotConfig botConfig in BotConfigList)
                {
                    var botExist = this.botAgents.Where(b => b.Config.Id == botConfig.Id).FirstOrDefault();
                    if (botExist == null)
                    {
                        if (!WillBeSpawnAts.ContainsKey(botConfig.Id))
                        {
                            WillBeSpawnAts.Add(botConfig.Id, 0);
                        }

                        if (WillBeSpawnAts[botConfig.Id] < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                        {
                            WillBeSpawnAts[botConfig.Id] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + SpawnDuration;
                            WillBeIdleAts[botConfig.Id] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + IdleDuration;
                            MissionSpawnBot(botConfig);
                            Debug.Print("Spawn Bot");
                        }
                    }

                }


                foreach (var bot in botAgents)
                {
                    if (bot != null && !bot.Agent.IsPaused)
                    {

                        if (!bot.Agent.IsInBeingStruckAction)
                        {
                            // bot idle moving 
                            BotIdleMoving( bot.Config.Id,bot.Agent);
                        }

                        foreach (Agent agent in Mission.Current.AllAgents.Where(a => a.IsPlayerControlled && a.IsHuman && a.IsPlayerUnit && a.IsActive() && a.Controller != ControllerType.AI).ToList())
                        {
                            if (bot.Agent.Position.Distance(agent.Position) < bot.Config.Range)
                            {
                                if (bot.Agent.GetTargetAgent() == null)
                                {
                                    bot.Agent.SetTargetAgent(agent);
                                    //Debug.Print("SetTargetAgent ------------------------------------- " + agent.Name);
                                }
                                if (bot.Agent.AIStateFlags != Agent.AIStateFlag.Alarmed)
                                {
                                     bot.Agent.AIStateFlags |= Agent.AIStateFlag.Alarmed; 
                                }

                                if (bot.Agent.HasMeleeWeaponCached && bot.Agent.Position.Distance(agent.Position) <  bot.Config.Range)
                                {
                                     bot.Agent.SetTargetPosition(new Vec2(agent.Position.AsVec2.x + 1, agent.Position.AsVec2.y + 1));
                                }
                            }
                        }
                    } 
                }
            }
            catch (Exception ex) { Debug.PrintError("Error: " + ex.Message); }
        }

        public void OnMissionEnded(IMission mission)
        {
            this.botAgents.Clear(); 
        }

        
        

    }
}
