using CommandBooks.Items;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace CommandBooks
{
    public class CommandBooksModSystem : ModSystem
    {

        private ICoreServerAPI sapi;

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass(Mod.Info.ModID + ".ItemCommandBook", typeof(ItemCommandBook));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            sapi = api;

            RegisterCommands(api, "commandbook");
        }

        private void RegisterCommands(ICoreAPI api, string commandName)
        {
            api.ChatCommands.Create(commandName).WithDescription("Configure held Command Books").RequiresPrivilege(Privilege.controlserver)
                .BeginSubCommand("commands")
                .WithDescription("Add, remove, or list commands of the held Command Book")
                .WithArgs([api.ChatCommands.Parsers.OptionalWord("action"), api.ChatCommands.Parsers.OptionalAll("args")])
                .HandleWith(HandleCommandsCommand)
                .EndSubCommand()
                .BeginSubCommand("onetimeuse").RequiresPrivilege(Privilege.controlserver)
                .WithDescription("Set whether or not the held Command Book is one time use")
                .WithArgs([api.ChatCommands.Parsers.Bool("true|false", "true")])
                .HandleWith(HandleOneTimeUseCommand)
                .EndSubCommand()
                .BeginSubCommand("elevated").RequiresPrivilege(Privilege.controlserver)
                .WithDescription("Set whether or not the commands will be run with the highest permissions possible.")
                .WithArgs([api.ChatCommands.Parsers.Bool("true|false", "true")])
                .HandleWith(HandleElevatedCommand)
                .EndSubCommand()
                .BeginSubCommand("secret").RequiresPrivilege(Privilege.controlserver)
                .WithDescription("Set whether or not the item description should list the book's commands.")
                .WithArgs([api.ChatCommands.Parsers.Bool("true|false", "true")])
                .HandleWith(HandleSecretCommand)
                .EndSubCommand();
        }

        private TextCommandResult HandleCommandsCommand(TextCommandCallingArgs args)
        {
            IPlayer player = args.Caller.Player;
            ItemStack itemStack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
            if (itemStack == null || !itemStack.Item.Code.ToString().Contains("commandbook"))
            {
                return TextCommandResult.Error("You're not holding a Command Book!");
            }

            string text = (args.Parsers[0].GetValue() as string) ?? "";
            string text2 = (args.Parsers[1].GetValue() as string) ?? "";

            List<string> parsedCommands = new List<string>();
            if (itemStack.Attributes?["commands"] != null)
            {
                sapi.Logger.Debug(itemStack.Attributes?["commands"].ToString());

                foreach (string command in JsonUtil.ToObject<List<string>>(itemStack.Attributes?["commands"].ToString(), ""))
                {
                    parsedCommands.Add(command);
                }
            }

            switch (text.ToLowerInvariant())
            {
                case "list":
                    string formatted = "";
                    int i = 0;
                    foreach (string command in parsedCommands)
                    {
                        formatted += "\n" + i + ". " + command;
                        i++;
                    }
                    return TextCommandResult.Success("This book's commands:" + formatted);

                case "add":
                    parsedCommands.Add(text2);
                    itemStack.Attributes.SetString("commands", JsonConvert.SerializeObject(parsedCommands));
                    player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                    return TextCommandResult.Success("New command added to book");

                case "remove":
                    if (int.TryParse(text2, out var result))
                    {
                        parsedCommands.RemoveAt(result);
                        itemStack.Attributes.SetString("commands", JsonConvert.SerializeObject(parsedCommands));
                        player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                        return TextCommandResult.Success("Command removed from book");
                    }
                    return TextCommandResult.Error("Unable to remove command at index. Did you provide a valid integer?");


            }

            return TextCommandResult.Success();
        }

        private TextCommandResult HandleOneTimeUseCommand(TextCommandCallingArgs args)
        {
            IPlayer player = args.Caller.Player;
            ItemStack itemStack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
            if (itemStack == null || !itemStack.Item.Code.ToString().Contains("commandbook"))
            {
                return TextCommandResult.Error("You're not holding a Command Book!");
            }

            switch (args.Parsers[0].GetValue())
            {
                case true:
                    itemStack.Attributes.SetBool("oneTimeUse", true);
                    player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                    return TextCommandResult.Success("One time use enabled on this book");
                case false:
                    itemStack.Attributes.SetBool("oneTimeUse", false);
                    player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                    return TextCommandResult.Success("One time use disabled on this book");
                default:
                    return TextCommandResult.Success("This book's one time use value: " + itemStack.Attributes?["oneTimeUse"].ToString());
            }
        }

        private TextCommandResult HandleElevatedCommand(TextCommandCallingArgs args)
        {
            IPlayer player = args.Caller.Player;
            ItemStack itemStack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
            if (itemStack == null || !itemStack.Item.Code.ToString().Contains("commandbook"))
            {
                return TextCommandResult.Error("You're not holding a Command Book!");
            }

            switch (args.Parsers[0].GetValue())
            {
                case true:
                    itemStack.Attributes.SetBool("elevated", true);
                    player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                    return TextCommandResult.Success("Elevated permissions enabled on this book");
                case false:
                    itemStack.Attributes.SetBool("elevated", false);
                    player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                    return TextCommandResult.Success("Elevated permissions disabled on this book");
                default:
                    return TextCommandResult.Success("This book's elevated permissions value: " + itemStack.Attributes?["elevated"].ToString());
            }
        }

        private TextCommandResult HandleSecretCommand(TextCommandCallingArgs args)
        {
            IPlayer player = args.Caller.Player;
            ItemStack itemStack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
            if (itemStack == null || !itemStack.Item.Code.ToString().Contains("commandbook"))
            {
                return TextCommandResult.Error("You're not holding a Command Book!");
            }

            switch (args.Parsers[0].GetValue())
            {
                case true:
                    itemStack.Attributes.SetBool("secret", true);
                    player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                    return TextCommandResult.Success("Secret commands enabled on this book");
                case false:
                    itemStack.Attributes.SetBool("secret", false);
                    player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                    return TextCommandResult.Success("Secret commands disabled on this book");
                default:
                    return TextCommandResult.Success("This book's secret commands value: " + itemStack.Attributes?["secret"].ToString());
            }
        }
    }
}
