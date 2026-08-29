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
        

        public static T CreateMessage<T>(string content, EmbedProperties ?embed, IMessageComponentProperties properties) where T : IMessageProperties, new()
        {
            T message = new();

            message.Content = content;
            message.Components = [];
            message.AddComponents(properties);
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

    public class SettingsHandler()
    {
        public static SettingsClass? GlobalBotSettings = null;
        public static void LoadEnviorment()
        {
            string json = System.IO.File.ReadAllText("BotSettings.json");

            SettingsClass? settings = JsonSerializer.Deserialize<SettingsClass>(json);

            SettingsHandler.GlobalBotSettings = settings;

            //Console.Write("settings: " + settings);
        }

        public class SettingsClass
        {
            public IList<ulong>? JoinAssignRoles {get; set;}

            public IList<RoleMenu>? RoleMenus {get; set;}
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

        /*
        public static void SortByDates(IList<GameJamElement> elements, IList<int> DaysLeftArgs)
        {
            DateTime today = new DateTime();
            IList<GameJamElement> SortedElements;
            foreach (GameJamElement element in elements)
            {
                if (today.Month - element.start_date.Month > 2)
                {
                    return;
                }

                foreach (int Day in DaysLeftArgs)
                {
                    int Distance = today.Day - element.start_date.Day; 
                }
            }
        }*/


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
            //Console.Write(element.url);
            //Console.Write(element.PlayerAmount);
            //Console.Write(element.start_date);
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
            public string start_date;
            public string end_date;
            public GameJamElement(string title, string url, string PlayerAmount, string start_date, string end_date)
            {
                this.title = title;
                this.url = url;
                this.PlayerAmount = PlayerAmount;
                this.start_date = start_date;
                this.end_date = end_date;
            }
        }
    };

    
}