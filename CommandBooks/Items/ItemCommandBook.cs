using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

#nullable disable

namespace CommandBooks.Items
{
    internal class ItemCommandBook : Item
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }
        
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (!firstEvent) return;
            if (slot.Itemstack.Attributes?["commands"] == null)
            {
                slot.Itemstack.Attributes.SetString("commands", "[]");
            }
            if (slot.Itemstack.Attributes?["oneTimeUse"] == null)
            {
                slot.Itemstack.Attributes.SetString("oneTimeUse", "false");
            }
            if (slot.Itemstack.Attributes?["elevated"] == null)
            {
                slot.Itemstack.Attributes.SetString("elevated", "false");
            }
            if (slot.Itemstack.Attributes?["secret"] == null)
            {
                slot.Itemstack.Attributes.SetString("secret", "false");
            }

            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (byPlayer == null) return;

            if (byEntity.World.Side != EnumAppSide.Server)
            {
                handling = EnumHandHandling.PreventDefault;
                return;
            }

            try
            {
                List<string> commands = JsonUtil.ToObject<List<string>>(slot.Itemstack.Attributes?["commands"].ToString(), "");

                bool.TryParse(slot.Itemstack.Attributes?["oneTimeUse"].ToString(), out bool oneTimeUse);
                bool.TryParse(slot.Itemstack.Attributes?["elevated"].ToString(), out bool elevated);
                bool.TryParse(slot.Itemstack.Attributes?["secret"].ToString(), out bool secret);

                if (byEntity is EntityPlayer)
                {

                    foreach (string command in commands)
                    {
                        TextCommandCallingArgs args = new();
                        args.Caller = new();
                        args.Caller.Player = byPlayer;
                        if (elevated)
                        {
                            args.Caller.CallerPrivileges = Privilege.AllCodes();
                        }
                        else
                        {
                            args.Caller.CallerPrivileges = byPlayer.Privileges;
                        }
                            args.Command = byEntity.Api.ChatCommands[command.Split(" ")[0].Split("/").Last().Split(".").Last()];
                        args.RawArgs = new CmdArgs(byEntity.Api.ChatCommands.Parsers.All("filler").ToString());
                        byEntity.Api.ChatCommands.ExecuteUnparsed(command, args);
                    }

                    if (oneTimeUse)
                    {
                        slot.TakeOut(1);
                        slot.MarkDirty();
                    }
                }
            }
            catch (Exception ex)
            {
                byEntity.World.Logger.Error("Failed using command book");
                byEntity.World.Logger.Error(ex);
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            try
            {
                List<string> parsedCommands = new List<string>();
                if (inSlot.Itemstack.Attributes?["commands"] != null)
                {
                    //world.Logger.Debug(inSlot.Itemstack.Attributes?["commands"].ToString());

                    foreach (string command in JsonUtil.ToObject<List<string>>(inSlot.Itemstack.Attributes?["commands"].ToString(), ""))
                    {
                        parsedCommands.Add(command);
                    }
                }

                if (parsedCommands.Count == 0)
                {
                    dsc.AppendLine("This book currently has no commands.");
                }
                else
                {
                    if (inSlot.Itemstack.Attributes.GetAsBool("secret") == true)
                    {
                        dsc.AppendLine("<i><font color=\"#919090\">The commands on this book are hidden.</font></i>");
                    }
                    else
                    {
                        string formatted = "";
                        int i = 0;
                        foreach (string command in parsedCommands)
                        {
                            formatted += "\n" + i + ". " + command;
                            i++;
                        }
                        dsc.AppendLine("This book will run the following commands on use:" + formatted);
                    }

                }

                if (inSlot.Itemstack.Attributes?["oneTimeUse"] != null)
                {
                    if (inSlot.Itemstack.Attributes.GetAsBool("oneTimeUse") == true)
                    {
                        dsc.AppendLine("<strong><font color=\"#eb3434\">Will be destroyed on use.</font></strong>");
                    }
                }

                if (inSlot.Itemstack.Attributes?["elevated"] != null)
                {
                    if (inSlot.Itemstack.Attributes.GetAsBool("elevated") == true)
                    {
                        dsc.AppendLine("<strong><font color=\"#34ebe8\">Will run with elevated permissions.</font></strong>");
                    }
                }
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to set commandbook item info");
                api.Logger.Error(ex.ToString());
            }
        }
    }
}
