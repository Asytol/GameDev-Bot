// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;


//Netcord
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord;
using NetCord.Hosting.Services;
using NetCord.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;
using NetCord.Services.Commands;

//Json
using Json;
using System.Text.Json.Serialization;
using System.Text.Json;
using NetCord.Services;
using System.ComponentModel;
using System.Net;
using System.Text.RegularExpressions;
using NetCord.Hosting.Rest;

namespace GameDevBot
{
    public delegate string InitDelegator();
    internal class MainProgram
    {
        static async Task Main(string[] args){
            SettingsHandler.LoadEnviorment();

            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddDiscordGateway(options =>
            {
                options.Intents = GatewayIntents.GuildMessages 
                | GatewayIntents.DirectMessages 
                | GatewayIntents.MessageContent
                | GatewayIntents.GuildMessageReactions
                | GatewayIntents.DirectMessageReactions
                | GatewayIntents.Guilds
                | GatewayIntents.GuildMessages
                | GatewayIntents.GuildUsers
                | GatewayIntents.GuildPresences;
            }).AddGatewayHandlers(typeof(MessageCreateHandler).Assembly)
            .AddApplicationCommands()
            .AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>()
            .AddComponentInteractions<RoleMenuInteraction, RoleMenuInteractionContext>();

            IHost host = builder.Build();

            host.AddModules(typeof(MainProgram).Assembly);

            await host.RunAsync();

            Console.WriteLine("discord bot closed");
        }
    }

    public class RolesHandler(RestClient client) : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SlashCommand("initialize", "initialize's role picking and other user choices.",DefaultGuildPermissions = Permissions.Administrator)]
        public Task<string> initialize(Channel channel) => InitializeRolePicking(Context,client,channel);

        private static async Task<string> InitializeRolePicking(ApplicationCommandContext context, RestClient client, Channel channel)
        {
            if (SettingsHandler.GlobalBotSettings == null)
            {
                return "Error: BotSettings are null or improperly set";
            }
            
            IList<SettingsHandler.RoleMenu> LoadedRoleMenu = SettingsHandler.GlobalBotSettings.RoleMenus;
            foreach (SettingsHandler.RoleMenu menu in LoadedRoleMenu)
            {
                ActionRowProperties ActionProperties = new ActionRowProperties();
                IList<ButtonProperties> ButtonList = [];

                foreach (SettingsHandler.RoleOption option in menu.RoleOptions){
                    string CustomId = "assign_role";

                    CustomId = string.Join(":",[CustomId,option.RoleId,menu.Exclusive,2]);
                    Console.WriteLine(CustomId);   
                    ButtonList.Add(new ButtonProperties("assign_role",option.Name,ButtonStyle.Primary).WithCustomId(CustomId));
                };
                ActionProperties = ConstructButton(ButtonList);
                MessageProperties message = SendingMessages.CreateMessage<MessageProperties>(menu.Name,null,ActionProperties);
                await client.SendMessageAsync(channel.Id,message);
            }

            return "sucess";
        }

        public static ActionRowProperties ConstructButton(IList<ButtonProperties> ButtonProperties)
        {
            int Length = ButtonProperties.Count;
            if (Length == 5){ return new ActionRowProperties().AddComponents(ButtonProperties[0],ButtonProperties[1],ButtonProperties[2],ButtonProperties[3],ButtonProperties[4]);}  
            if (Length == 4){ return new ActionRowProperties().AddComponents(ButtonProperties[0],ButtonProperties[1],ButtonProperties[2],ButtonProperties[3]);} 
            if (Length == 3){ return new ActionRowProperties().AddComponents(ButtonProperties[0],ButtonProperties[1],ButtonProperties[2]);}
            if (Length == 2){ return new ActionRowProperties().AddComponents(ButtonProperties[0],ButtonProperties[1]);}
            if (Length == 1){ return new ActionRowProperties().AddComponents(ButtonProperties[0]);}
            return null;
        }

        [SlashCommand("chicanery", "You think this is bad? this- this chicanery?")]
        public string chicanery()
        {
            return "*\\*Defecates through sunroof\\**";
        }

        [SlashCommand("SetGameJamChannel", "Sets the channel where the bot announces upcoming game jams",DefaultGuildPermissions = Permissions.BanUsers)]
        public string SetGameJamChannel(Channel channel)
        {
            try{
                SettingsHandler.GlobalBotSettings.UpcomingGamesChannel = channel.Id;
                string JsonString = JsonSerializer.Serialize(SettingsHandler.GlobalBotSettings);
                File.WriteAllText(SettingsHandler.SavePath,JsonString);
                BackgroundTaskHandler.Client = client;
                return "Sucess";
            }
            catch
            {
                return "Failed";
            }

        }
        [SlashCommand("AssignClient", "Command for fixing the error: Client is null",DefaultGuildPermissions = Permissions.BanUsers)]
        public string AssignClient()
        {
            BackgroundTaskHandler.Client = client;
            return "re-Assigned client";
        }

    }
    public class MessageCreateHandler(ILogger<MessageCreateHandler> logger, RestClient client) : IMessageCreateGatewayHandler
    {
        public ValueTask HandleAsync(Message message)
        {
            logger.LogInformation("{}",message.Content);
            //client.SendMessageAsync(message.ChannelId,"test");
            if (message.GuildId != null)
            {
                Console.Write(message.GuildId);
                ulong GuildId = (ulong)message.GuildId;
                client.AddGuildUserRoleAsync(GuildId,message.Author.Id,1540143349003059210);   
            }
            //client.CreateGuildRoleAsync(message.GuildId,);
            return default;
        }
    }

    //public class Test(RestClient client) : ICreat

    //Note to future me, this class doesn't work: Idk why, It's the exact same as the docs?
    public class MessageReactionAddHandler(RestClient client) : IMessageReactionAddGatewayHandler
    {
        public async ValueTask HandleAsync(MessageReactionAddEventArgs args)
        {
            Console.WriteLine("Recieved reaction");
        }
    }

    public class MemberJoinHandler() : IGuildUserAddGatewayHandler
    {
        public async ValueTask HandleAsync(GuildUser arg)
        {
            Console.WriteLine("Member joined");
            if (SettingsHandler.GlobalBotSettings.JoinAssignRoles != null)
            {
                foreach(ulong id in SettingsHandler.GlobalBotSettings.JoinAssignRoles)
                {
                    Console.WriteLine($"Gave new member: {arg.Id} join role: {id}");
                    await arg.AddRoleAsync(id);
                }
            }  
        }
    }

    public class SendingMessages()
    {
        

        public static T CreateMessage<T>(string content, EmbedProperties ?embed, IMessageComponentProperties? properties) where T : IMessageProperties, new()
        {
            T message = new();

            message.Content = content;
            message.Components = [];
            if (properties != null)
            {
                message.AddComponents(properties);   
            }
            if (embed != null)
            {
                message.AddEmbeds(embed);   
            }

            return message;
        }
    }

    public class ButtonModule() : ComponentInteractionModule<ButtonInteractionContext>
    {
        [ComponentInteraction("button")]
        public string Button() => $"Guild: {Context.Guild.Id}";

        [ComponentInteraction("assign_role")]
        public async Task assign_role(ulong RoleId,bool Exclusive,params ulong[] RoleIdList) => GiveRole(Context, RoleId,Exclusive,RoleIdList);

        private async static Task GiveRole(ButtonInteractionContext context,ulong RoleId,bool Exclusive,IList<ulong> RoleIdList)
        {
            if (context.Guild != null)
            {
                if (Exclusive == true)
                {
                    foreach(ulong id in RoleIdList)
                    {
                        context.Guild.RemoveUserRoleAsync(context.User.Id,id);
                    }
                }
                GuildUser guilduser = await context.Guild.GetUserAsync(context.User.Id);
                bool AlreadyHaveRole = false;
                foreach(ulong id in guilduser.RoleIds)
                {
                    if (id == RoleId)
                    {
                        AlreadyHaveRole = true;
                    }
                }

                if (AlreadyHaveRole)
                {  //1539887719076073562
                    await guilduser.RemoveRoleAsync(RoleId);
                    Console.WriteLine($"Removed role: {RoleId} to user {context.User.Id}");
                }
                else
                {
                    await context.Guild.AddUserRoleAsync(context.User.Id,RoleId);   
                    Console.WriteLine($"Gave role: {RoleId} to user {context.User.Id}");
                }

                await context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());
                await context.Interaction.DeleteResponseAsync();
                //Response.Dispose();
            }
        }
    }

    public class RoleMenuModule : ComponentInteractionModule<RoleMenuInteractionContext>
    {
        [ComponentInteraction("rolemenu")]
        public string Menu() => $"You selected: {string.Join(", ", Context.SelectedValues)}";
    }

    public class BackgroundTaskHandler
    {
        public static RestClient Client;
        public static string ItchAddres = "https://itch.io/jams";
        public async static Task StartEveryDayMonitoring()
        {
            DateTime date = DateTime.Now;

            await Task.Run(() =>
            {
                while (true)
                {
                    if (date.Hour == 0)
                    {
                        Console.WriteLine("Date synchronized");
                        break;
                    }   
                    Thread.Sleep(120000);   
                }
                ReportUpcomingGameJams(null);
            });
        }

        public static async void ReportUpcomingGameJams(Object? obj)
        {
            Console.WriteLine("New day started");

            if (Client == null)
            {
                Console.WriteLine("Error: Client is null  (Run \"/AssignClient\" or get Adam's lazy ass to fix the GameJamReport function)");
                goto NullClient;
            }

            if (SettingsHandler.GlobalBotSettings.UpcomingGamesChannel != null && SettingsHandler.GlobalBotSettings.UpcomingGamesChannel != 0)
            {
                string itchHtml = HtmlSearcher.MainProgram.GetPageSource(ItchAddres);
                IList<HtmlSearcher.MainProgram.GameJamElement> gameJamElements = HtmlSearcher.MainProgram.GetGameJams(itchHtml);
                gameJamElements = HtmlSearcher.MainProgram.SortByDates(gameJamElements,[1,3,7]);
                
                foreach(HtmlSearcher.MainProgram.GameJamElement element in gameJamElements)
                {
                    EmbedProperties embed = new EmbedProperties();
                    embed.Url = element.url;
                    MessageProperties properties = SendingMessages.CreateMessage<MessageProperties>($"Upcoming game jam: \n Title: {element.title} | Joined:{element.PlayerAmount} | Starts in: {element.DaysLeft}",embed,null);

                    await Client.SendMessageAsync((ulong)SettingsHandler.GlobalBotSettings.UpcomingGamesChannel,properties);   
                }
            }
            else
            {
                Console.WriteLine("Invalid channel");
            }

            NullClient:

            DateTime today = DateTime.Now;
            DateTime tomorrow = DateTime.Today.AddDays(1);

            int TimeDelay = (today - tomorrow).Milliseconds;
            Console.WriteLine(TimeDelay);
            Timer timer = new Timer(new TimerCallback(ReportUpcomingGameJams),null,TimeDelay,Timeout.Infinite);
        }
    }

    public class SettingsHandler()
    {
        public static SettingsClass? GlobalBotSettings = null;
        public static readonly string SavePath = "BotSettings.json";
        public static void LoadEnviorment()
        {
            string json = System.IO.File.ReadAllText(SavePath);

            SettingsClass? settings = JsonSerializer.Deserialize<SettingsClass>(json);

            SettingsHandler.GlobalBotSettings = settings;

            //Console.Write("settings: " + settings);
        }

        public class SettingsClass
        {
            public IList<ulong>? JoinAssignRoles {get; set;}

            public IList<RoleMenu>? RoleMenus {get; set;}
            public ulong? UpcomingGamesChannel {get; set;}
        }

        public class RoleMenu
        {
            public string? Name {get; set;}
            public bool Exclusive {get; set;}
            public IList<RoleOption>? RoleOptions {get; set;}
        }

        public class RoleOption
        {
            public string? Name {get; set;}
            public ulong RoleId {get; set;}
            public string? Emoji {get; set;}
        } 

        public static void SerializationHelper()
        {
            //On github this function will be almost empty, this is just used to serialize the settings object if 
            // you have difficulty writing json yourself.
        }
    }
}


namespace HtmlSearcher
{

    internal class MainProgram
    {
        public static string Url = "https://itch.io/jams";

        public static IList<GameJamElement> SortByDates(IList<GameJamElement> elements, IList<int> DaysLeftArgs)
        {
            Console.WriteLine("Day intervals:");
            DateTime today = DateTime.Now;
            IList<GameJamElement> SortedElements = [];
            foreach (GameJamElement element in elements)
            {
                int Distance = (element.start_date - today).Days;
                foreach (int day in DaysLeftArgs)
                {
                    if (Distance == day)
                    {
                        element.DaysLeft = Distance;
                        SortedElements.Add(element);
                        Console.WriteLine("Element is in appropriate time range");
                    }
                }
            }

            return SortedElements;
        }


        public static string GetPageSource(string url)
        {
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(url);
            webrequest.Method = "GET";
            HttpWebResponse webresponse = (HttpWebResponse)webrequest.GetResponse();
            string ResponseHtml;
            using (StreamReader responseStream = new StreamReader(webresponse.GetResponseStream()))
            {
                ResponseHtml = responseStream.ReadToEnd();
            }

            return ResponseHtml;
        }

        public static IList<GameJamElement> GetGameJams(string ItchHtml)
        {
            IList<GameJamElement> JamList = [];
            int i = 0;
            int u = 0;
            string CompareFilterString = "R.Jam.FilteredJamCalendar";
            while (i < ItchHtml.Length)
            {
                if (ItchHtml[i] == '(')
                {
                    i++;
                    for(u = 0; u < CompareFilterString.Length; u++)
                    {
                        //Console.Write(ItchHtml[i]);
                        if (ItchHtml[i] != CompareFilterString[u])
                        {
                            goto WrongString1;
                        }
                        i++;
                    }
                    goto CorrectString1;
                }
                WrongString1:
                i++;
            }   
            CorrectString1:
            i += 10; //jump to the game jam list (goes past: '({"jams":['
            while (true)
            {
                if (ItchHtml[i] == '{')
                {
                    GameJamElementReturnElement returnElement =  GetJamInformation(ItchHtml,i);
                    i = returnElement.index;
                    JamList.Add(returnElement.element);
                }
                //Console.Write(ItchHtml[i]);
                i++;

                if (ItchHtml[i] == ';')
                {
                    break;
                }
            }
            return JamList;
        }


        public class SearchParamaters
        {
            public SearchParamaters(string MatchCase,char Terminator = '"')
            {
                this.MatchCase = MatchCase;
                this.Terminator = Terminator;
            }
            public string MatchCase = "";
            public char Terminator = '"';
            public string Result = "";
        }

        public static GameJamElementReturnElement GetJamInformation(string ItchHtml, int index)
        {
            int BeginningIndex = index;
            int i = index;

            IList<SearchParamaters> paramaters = [
                new SearchParamaters("\"title\":"),
                new SearchParamaters("\"url\":"),
                new SearchParamaters("\"joined\":",','),
                new SearchParamaters("\"start_date\":"),
                new SearchParamaters("\"end_date\":")
            ];


            foreach (SearchParamaters paramater in paramaters)
            {
                i = BeginningIndex;
                string SearchParamater = paramater.MatchCase;
                while (true)
                {
                    if (SearchParamater[0] == ItchHtml[i])
                    {
                        i++;
                        for (int u = 1; u < SearchParamater.Length; u++)
                        {
                            if (SearchParamater[u] != ItchHtml[i])
                            {
                                goto WrongString;
                            }
                            i++;
                        }
                        goto CorrectString;
                    }
                    WrongString:
                    i++;
                }
                CorrectString:
                if (ItchHtml[i] == paramater.Terminator){i++;}

                int startIndex = i;
                int length = 0;
                while (ItchHtml[i] != paramater.Terminator)
                {
                    i++;
                    length++;                
                }

                paramater.Result = ItchHtml.Substring(startIndex, length);   
            }

            GameJamElement element = new GameJamElement(
                paramaters[0].Result,
                paramaters[1].Result,
                paramaters[2].Result,
                paramaters[3].Result,
                paramaters[4].Result
            );


            Console.WriteLine($"[ {element.end_date} | {element.PlayerAmount} | {element.title} | {element.url} | {element.start_date} ]");
            return new GameJamElementReturnElement(element,i);
        }

        public class GameJamElementReturnElement
        {
            public GameJamElement element;
            public int index;
            public GameJamElementReturnElement(GameJamElement element, int index)
            {
                this.element = element;
                this.index = index;
            }
        }

        public class GameJamElement
        {
            public string title = "";
            public string url = "";
            public string PlayerAmount = "";
            public DateTime start_date;
            public DateTime end_date;
            public int DaysLeft;
            public GameJamElement(string title, string url, string PlayerAmount, string start_date, string end_date)
            {
                this.title = title;
                this.url = url;
                this.PlayerAmount = PlayerAmount;
                try
                {
                    this.start_date = DateTime.Parse(start_date);   
                }
                catch
                {
                    this.start_date = DateTime.MaxValue;
                }
                
                try
                {
                    this.end_date = DateTime.Parse(end_date);   
                }
                catch
                {
                    this.end_date = DateTime.MaxValue;
                }
            }
        }
    };

    
}