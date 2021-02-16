using System;
using System.Linq;
using Mirror;
using Runtime.Helpers;
using Runtime.Services;
using UnityEngine;

namespace Runtime.Core.Server
{
    /* //Notes
     * This class is where you will process commands typed into your console.
     * Commands are issued into the Run method below. You may organized and sort
     * them however you please. */
    public class CommandHandler
    {
        public void Run(string command)
        {
            command = "/" + command.Substring(1, command.Length - 1).ToLower();

            Console.ForegroundColor = ConsoleColor.White;

            if (command == "/list connections")
            {
                Console.WriteLine($"| {"Idx", 3} | {"Connection",-15} | {"Nickname",-32} | {"Chars",-5} | {"Playing?",-8} |");
                Console.WriteLine($"| {"---", 3} {"---------------",-15} | {"--------------------------------",-32} | {"-----",-5} | {"--------",-8} |");
                var idx = 0;
                foreach (var connectionInfo in ProfileService.Instance.ConnectionInfos)
                {
                    Console.WriteLine(
                        $"| {idx, 3}" +
                        $"| {connectionInfo.Key,-15} " +
                        $"| {connectionInfo.Value.name,-32} " +
                        $"| {connectionInfo.Value.characters.Length,-5} " +
                        $"| {connectionInfo.Value.PlayingCharacter != null,-8} |");
                    idx++;
                }

                Console.WriteLine($"| {"---", 3} | {"---------------",-15} | {"--------------------------------",-32} | {"-----",-5} | {"--------",-8} |");
                return;
            }

            if (command.StartsWith("/get profile "))
            {
                var connectionIdx = Convert.ToInt32(command.Substring(("/get profile ").Length));
                var conn = ProfileService.Instance.ConnectionInfos.ElementAt(connectionIdx).Key;
                var profile = ProfileService.Instance.ConnectionInfos[conn];

                Console.WriteLine("connection: " + conn);
                Console.WriteLine("name: " + profile.name);
                Console.WriteLine("steamId: " + profile.steamId);
                Console.WriteLine("currency amount: " + profile.currencyAmount);
                Console.WriteLine("max character count: " + profile.maxCharacterCount);
                Console.WriteLine("characters: " + profile.characters.Length);
                Console.WriteLine("playing character: " + (profile.PlayingCharacter == null ? "none" : profile.PlayingCharacter.id));
                return;
            }

            // if (command.StartsWith("/get character"))
            // {
            //     var characterId = command.Substring(("/get character").Length);
            // }


            Console.WriteLine("Unknown command: " + command);
        }
    }
}