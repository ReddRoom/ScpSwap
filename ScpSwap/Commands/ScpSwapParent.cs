﻿// -----------------------------------------------------------------------
// <copyright file="ScpSwapParent.cs" company="Build">
// Copyright (c) Build. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace ScpSwap.Commands
{
    using System;
    using System.Linq;
    using CommandSystem;
    using Exiled.API.Extensions;
    using Exiled.API.Features;
    using ScpSwap.Models;

    /// <summary>
    /// The base command for ScpSwapParent.
    /// </summary>
    [CommandHandler(typeof(ClientCommandHandler))]
    public class ScpSwapParent : ParentCommand, IUsageProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScpSwapParent"/> class.
        /// </summary>
        public ScpSwapParent() => LoadGeneratedCommands();

        /// <inheritdoc />
        public override string Command { get; } = "scpswap";

        /// <inheritdoc />
        public override string[] Aliases { get; } = Array.Empty<string>();

        /// <inheritdoc />
        public override string Description { get; } = "Base command for ScpSwapParent.";

        /// <inheritdoc />
        public string[] Usage { get; } = { "ScpNumber" };

        /// <inheritdoc />
        public sealed override void LoadGeneratedCommands()
        {
            RegisterCommand(new Accept());
            RegisterCommand(new Cancel());
            RegisterCommand(new Decline());
            RegisterCommand(new List());
        }

        /// <inheritdoc />
        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Round.IsStarted)
            {
                response = "The round has not yet started.";
                return false;
            }

            if (Round.ElapsedTime.TotalSeconds > Plugin.Instance.Config.SwapTimeout)
            {
                response = "The swap period has ended.";
                return false;
            }

            if (arguments.IsEmpty())
            {
                response = $"Usage: .{Command} {string.Join(" ", Usage)}";
                return false;
            }

            Player playerSender = Player.Get(sender as CommandSender);
            if (!playerSender.IsScp)
            {
                response = "Take a good, long look at yourself then try to swap again.";
                return false;
            }

            if (Swap.FromSender(playerSender) != null)
            {
                response = "You already have a pending swap request!";
                return false;
            }

            Player receiver = GetReceiver(arguments.At(0), out Action<Player> spawnMethod);
            if (receiver != null)
            {
                if (playerSender == receiver)
                {
                    response = "You can't swap with yourself, idiot.";
                    return false;
                }

                Swap.Send(playerSender, receiver);
                response = "Request sent!";
                return true;
            }

            if (spawnMethod == null)
            {
                response = "Unable to find the specified role. Please refer to the list command for available roles.";
                return false;
            }

            if (Plugin.Instance.Config.AllowNewScps)
            {
                spawnMethod(playerSender);
                response = "Swap successful.";
                return true;
            }

            response = "Unable to locate a player with the requested role.";
            return false;
        }

        private Player GetReceiver(string request, out Action<Player> spawnMethod)
        {
            CustomSwap customSwap = ValidSwaps.GetCustom(request);
            if (customSwap != null)
            {
                spawnMethod = customSwap.SpawnMethod;
                return Player.List.FirstOrDefault(player => customSwap.VerificationMethod(player));
            }

            RoleType roleSwap = ValidSwaps.Get(request);
            if (Enum.IsDefined(typeof(RoleType), roleSwap))
            {
                spawnMethod = player => player.Role = roleSwap;
                return Player.List.FirstOrDefault(player => player.Role == roleSwap);
            }

            spawnMethod = null;
            return null;
        }
    }
}