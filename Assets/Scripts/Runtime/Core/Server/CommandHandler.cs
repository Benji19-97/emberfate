using System;
using System.Linq;
using Mirror;
using Newtonsoft.Json;
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
                Console.WriteLine($"| {"Idx",3} | {"Connection",-15} | {"Nickname",-32} | {"Chars",-5} | {"Playing?",-8} |");
                Console.WriteLine($"| {new string('-', 3),3} | {new string('-', 15),-15} | {new string('-', 32),-32} | {new string('-', 5),-5} | {new string('-', 8),-8} |");
                var idx = 0;
                foreach (var connectionInfo in ProfileService.Instance.ConnectionInfos)
                {
                    Console.WriteLine(
                        $"| {idx,3} " +
                        $"| {connectionInfo.Key,-15} " +
                        $"| {connectionInfo.Value.name,-32} " +
                        $"| {connectionInfo.Value.characters.Length,-5} " +
                        $"| {connectionInfo.Value.PlayingCharacter != null,-8} |");
                    idx++;
                }
                Console.WriteLine($"| {new string('-', 3),3} | {new string('-', 15),-15} | {new string('-', 32),-32} | {new string('-', 5),-5} | {new string('-', 8),-8} |");

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

            if (GiveCurrencyCommandHandler(command))
            {
                return;
            }
            
            if (GetCharacterCommandHandler(command))
            {
                return;
            }
            
            if (GiveLevelCommandHandler(command))
            {
                return;
            }


            Console.WriteLine("Unknown command: " + command);
        }

        private bool GiveCurrencyCommandHandler(string command)
        {
            if (command.StartsWith("/give currency"))
            {
                try
                {
                    var commandParams = command.Substring(("/give currency ").Length);
                    var tokens = commandParams.Split(' ');

                    if (tokens.Length == 2)
                    {
                        var idx = Convert.ToInt32(tokens[0]);
                        var amount = Convert.ToInt32(tokens[1]);

                        var conn = ProfileService.Instance.ConnectionInfos.ElementAt(idx).Key;
                        ProfileService.Instance.ConnectionInfos[conn].currencyAmount += amount;
                        Console.WriteLine($"Gave {amount} currency to {conn}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return false;
                }
                
                return true;
            }

            return false;

        }
        
        private bool GetCharacterCommandHandler(string command)
        {
            if (command.StartsWith("/get character "))
            {
                try
                {
                    var commandParams = command.Substring(("/get character ").Length);
                    var tokens = commandParams.Split(' ');
                    if (tokens.Length == 1)
                    {
                        var idx = Convert.ToInt32(tokens[0]);
                        var conn = ProfileService.Instance.ConnectionInfos.ElementAt(idx).Key;
                        var character = ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter;

                        if (character != null)
                        {
                            Console.WriteLine(conn + " is currently playing " + JsonConvert.SerializeObject(character.data));
                        }
                        else
                        {
                            Console.WriteLine(conn + " is currently not playing any character");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return false;
                }
                
                return true;
            }

            return false;
        }
        
        private bool GiveLevelCommandHandler(string command)
        {
            if (command.StartsWith("/give levels "))
            {
                try
                {
                    var commandParams = command.Substring(("/give levels ").Length);
                    var tokens = commandParams.Split(' ');

                    if (tokens.Length == 2)
                    {
                        var idx = Convert.ToInt32(tokens[0]);
                        var amount = Convert.ToByte(tokens[1]);

                        var conn = ProfileService.Instance.ConnectionInfos.ElementAt(idx).Key;
                        ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter.data.level += amount;
                        Console.WriteLine($"Gave {amount} levels to {ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter.id}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return false;
                }
                
                return true;
            }

            return false;

        }
    }
}