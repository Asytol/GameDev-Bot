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
                | GatewayIntents.GuildMessages;
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
            //if (args.MessageAuthorId)

            /*
            EmbedProperties test = new EmbedProperties()
            {
                Title = "I'm just testing how embeds work",
                Url = "https://netcord.dev/guides/basic-concepts/sending-messages.html?tabs=classic-syntax",   
            };
            ActionRowProperties ActionProperties = new ActionRowProperties
            {
                new ButtonProperties("assign_role","RoleAssignTest",ButtonStyle.Primary),
                new ButtonProperties("button","test",ButtonStyle.Primary),
            };

            MessageProperties message = SendingMessages.CreateMessage<MessageProperties>("test",test,ActionProperties);
            await client.SendMessageAsync(args.ChannelId, message);
            await client.SendMessageAsync(args.ChannelId, $"<@{args.UserId}> reacted with {args.Emoji.Name}!");
            */
             ActionRowProperties ActionProperties = new ActionRowProperties
            {
                new ButtonProperties("button","test",ButtonStyle.Primary),
            };

            MessageProperties message = SendingMessages.CreateMessage<MessageProperties>("test",null,ActionProperties);
            await client.SendMessageAsync(args.ChannelId, message);
        }
    }

    public class MemberJoinHandler() : IGuildUserAddGatewayHandler
    {
        public async ValueTask HandleAsync(GuildUser arg)
        {
            /*
            if (SettingsHandler.SettingsClass.JoinAssignRoles != null)
            {
                foreach(ulong id in SettingsHandler.SettingsClass.JoinAssignRoles)
                {
                    await arg.AddRoleAsync(id);
                }
            }*/
            //arg.   
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
                context.Guild.AddUserRoleAsync(context.User.Id,RoleId);

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
            /*
            SettingsClass TempSettings = new SettingsClass()
            {
                JoinAssignRoles = [1540833515996708906],
                RoleMenus = [
                    new RoleMenu{
                        Name = "Pick a role between these 2:",
                        Exclusive = false,
                        RoleOptions = [
                            new RoleOption(){
                                Name = "role1",
                                RoleId = 1540833705092980837,
                                Emoji = null
                            },
                            new RoleOption(){
                                Name = "role2",
                                RoleId = 1540833781353553982,
                                Emoji = null
                            } 
                        ]
                    },
                    new RoleMenu{
                        Name = "Pick a exclusive role between these 2:",
                        Exclusive = true,
                        RoleOptions = [
                            new RoleOption(){
                                Name = "Exclusive1",
                                RoleId = 1540833823816945824,
                                Emoji = null
                            },
                            new RoleOption(){
                                Name = "Exclusive2",
                                RoleId = 1540833863188742194,
                                Emoji = null
                            } 
                        ]
                    }
                ]
            };

            string FileName = "BotSettings.json";
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string JsonString = JsonSerializer.Serialize(TempSettings,options);
            File.WriteAllText(FileName,JsonString);

            Console.WriteLine(File.ReadAllText(FileName));
            */
        }
    }
}



