using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

#nullable disable

namespace CommandBooks.Items
{
    internal class ItemCommandBook : Item
    {
        public SimpleParticleProperties particlesHeld;

        public bool recentlyUsed;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            recentlyUsed = false;

            particlesHeld = new SimpleParticleProperties(
                50, 50,
                ColorUtil.ToRgba(50, 220, 220, 220),
                new Vec3d(),
                new Vec3d(),
                new Vec3f(-0.4f, 0.45f, -0.4f),
                new Vec3f(0.4f, 1.0f, 0.4f),
                1.5f,
                0,
                0.5f,
                0.75f,
                EnumParticleModel.Cube
            );
        }

        public void UseCheck(float dt)
        {
            recentlyUsed = false;
            return;
        }

        delegate void OnInteractUsed();
        
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (!firstEvent) return;
            if (byEntity.Controls.ShiftKey)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }
            if (recentlyUsed)
            {
                if (api is ICoreClientAPI capi)
                {
                    capi.TriggerIngameError(this, "error-cooldown", "Please wait a moment before using this book again.");
                    return;
                }
            }
            recentlyUsed = true;
            api.Event.RegisterCallback(UseCheck, 1000);
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
                if (api is ICoreClientAPI capi)
                {
                    capi.ShowChatMessage("You activate the contents of the book...");

                    if (slot.Itemstack.Attributes.GetBool("oneTimeUse") == true)
                    {
                        capi.ShowChatMessage("The book crumbles to dust in your hands.");
                    }
                }
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

                    api.World.PlaySoundAt(new AssetLocation("game", "sounds/held/bookturn3"), byEntity);
                    SpawnParticles(api.World, byEntity.Pos.XYZ, true);
                    
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

                if (inSlot.Itemstack.Attributes.GetAsBool("secret") == true)
                {
                    dsc.AppendLine("<i><font color=\"#919090\">The commands on this book are hidden.</font></i>");
                }
                else
                {
                    if (parsedCommands.Count == 0)
                    {
                        dsc.AppendLine("This book currently has no commands.");
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

                    if (inSlot.Itemstack.Attributes?["elevated"] != null)
                    {
                        if (inSlot.Itemstack.Attributes.GetAsBool("elevated") == true)
                        {
                            dsc.AppendLine("<strong><font color=\"#34ebe8\">Will run with elevated permissions.</font></strong>");
                        }
                    }
                }

                if (inSlot.Itemstack.Attributes?["oneTimeUse"] != null)
                {
                    if (inSlot.Itemstack.Attributes.GetAsBool("oneTimeUse") == true)
                    {
                        dsc.AppendLine("<strong><font color=\"#eb3434\">Will be destroyed on use.</font></strong>");
                    }
                }
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to set commandbook item info");
                api.Logger.Error(ex.ToString());
            }
        }

        void SpawnParticles(IWorldAccessor world, Vec3d pos, bool final)
        {
            if (final || world.Rand.NextDouble() > 0.8)
            {
                int h = 110 + world.Rand.Next(15);
                int v = 100 + world.Rand.Next(50);
                particlesHeld.MinPos = pos;
                particlesHeld.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(h, 180, v));

                particlesHeld.MinSize = 0.2f;
                particlesHeld.ParticleModel = EnumParticleModel.Quad;
                particlesHeld.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -150);
                particlesHeld.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(h, 180, v, 150));

                world.SpawnParticles(particlesHeld);
            }
        }
    }
}
