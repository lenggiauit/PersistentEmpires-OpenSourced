using PersistentEmpiresHarmony;
using PersistentEmpiresHarmony.Patches;
using PersistentEmpiresLib;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.GameModes;
using PersistentEmpiresLib.PersistentEmpiresGameModels;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresSave.Database.Repositories;
using PersistentEmpiresServer.ChatCommands.Commands;
using PersistentEmpiresServer.ServerMissions;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Debug = TaleWorlds.Library.Debug;

namespace PersistentEmpiresServer
{
    public class PersistentEmpires : MBSubModuleBase
    {
        public Dictionary<string, Command> commands;
        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            base.InitializeGameStarter(game, starterObject);
            CompressionBasic.MissionObjectIDCompressionInfo = new CompressionInfo.Integer(-1, 1000000, true);
            CompressionBasic.RoundGoldAmountCompressionInfo = new CompressionInfo.Integer(0, Int32.MaxValue, true);
            CompressionBasic.MapTimeLimitCompressionInfo = new CompressionInfo.Integer(1, int.MaxValue, true);
            CompressionBasic.BigRangeLowResLocalPositionCompressionInfo = new CompressionInfo.Float(-50000f, 50000f, 16);
            CompressionBasic.PositionCompressionInfo = new CompressionInfo.Float(-200f, 10385f, 22);
            CompressionBasic.PlayerCompressionInfo = new CompressionInfo.Integer(-1, 1048576, true);




            starterObject.AddModel(new PEAgentStatCalculateModel());
            starterObject.AddModel(new DefaultItemValueModel());
            starterObject.AddModel(new PEAgentApplyDamageModel()); 

            PersistentEmpireSkills.Initialize(game);
            InitializeChatCommand();
        }

        public override void OnMultiplayerGameStart(Game game, object starterObject)
        {
            InformationManager.DisplayMessage(new InformationMessage("** Persistent Empires, Multiplayer Game Start Loading..."));
            Debug.Print("** Persistent Empires, Multiplayer Game Start Loading...");

            PersistentEmpiresGameMode.OnStartMultiplayerGame += MissionManager.OpenPersistentEmpires;

            PatchGlobalChat.OnClientEventPlayerMessageAll += PatchGlobalChat_OnClientEventPlayerMessageAll;

            PatchGlobalChat.OnClientEventPlayerMessageTeam += FactionsBehavior.PatchGlobalChat_OnClientEventPlayerMessageTeam;

            PersistentEmpiresHarmonySubModule.OnRglExceptionThrown += SaveSystemBehavior.RglExceptionThrown;
            PatchGameNetwork.OnAddNewPlayerOnServer += SaveSystemBehavior.OnAddNewPlayerOnServer;

            AdminServerBehavior.OnIsPlayerBanned += DBBanRecordRepository.IsPlayerBanned;
            AdminServerBehavior.OnBanPlayer += DBBanRecordRepository.AdminServerBehavior_OnBanPlayer; 

            TaleWorlds.MountAndBlade.Module.CurrentModule.AddMultiplayerGameMode(new PersistentEmpiresGameMode(ConfigManager.ModuleId));
        }
         
        private bool PatchGlobalChat_OnClientEventPlayerMessageAll(NetworkCommunicator networkPeer, NetworkMessages.FromClient.PlayerMessageAll message)
        {
            PersistentEmpireRepresentative persistentEmpireRepresentative = networkPeer.GetComponent<PersistentEmpireRepresentative>();
            if (message.Message.StartsWith("!"))
            {
                string[] argsWithCommand = message.Message.Split(' ');
                string command = argsWithCommand[0];
                string[] args = argsWithCommand.Skip(1).ToArray();
                this.Execute(networkPeer, command, args);
                return false;
            }
            else if (persistentEmpireRepresentative != null && persistentEmpireRepresentative.IsAdmin)
            {
                InformationComponent.Instance.BroadcastMessage("(Admin) " + networkPeer.GetComponent<MissionPeer>().DisplayedName + ": " + message.Message, Color.ConvertStringToColor("#FDD835FF").ToUnsignedInteger());
                return false;
            }
            
            return true;
        }

        public bool Execute(NetworkCommunicator networkPeer, string command, string[] args)
        {
            Command executableCommand;
            bool exists = commands.TryGetValue(command, out executableCommand);
            if (!exists)
            {
                InformationComponent.Instance.SendMessage("This command is not exists", Colors.Red.ToUnsignedInteger(), networkPeer);
                return false;
            }
            if (!executableCommand.CanUse(networkPeer))
            {
                InformationComponent.Instance.SendMessage("You are not authorized to run this command", Colors.Red.ToUnsignedInteger(), networkPeer);
                return false;
            }
            return executableCommand.Execute(networkPeer, args);
        }

        private void InitializeChatCommand()
        {
            this.commands = new Dictionary<string, Command>();
            foreach (Type mytype in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                 .Where(mytype => mytype.GetInterfaces().Contains(typeof(Command))))
            {
                Command command = (Command)Activator.CreateInstance(mytype);
                if (!commands.ContainsKey(command.Command()))
                {
                    Debug.Print("** Chat Command " + command.Command() + " have been initiated !", 0, Debug.DebugColor.Green);
                    commands.Add(command.Command(), command);
                }
            }
        }


    }
}
