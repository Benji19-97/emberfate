#if (UNITY_STANDALONE_WIN && UNITY_SERVER) //|| UNITY_EDITOR
using System;
using Runtime.Core.Server;
using FirstGearGames.Mirrors.ServerCommandConsole;
#endif
using UnityEngine;

namespace Packages.FirstGearGames.ServerCommandConsole.garryTV
{

    /* //Notes
     * Notice this is a MonoBehaviour. It's important you add this
     * to one of your scenes, most likely your loading scene. In my
     * example I have put it on the same object as the NetworkManager.
     * I've also added 'UNITY_SERVER' to ensure this code is only
     * present on the server builds. 
     * If you do not need the console in editor, feel free to remove the 
     * UNITY_EDITOR_WIN check. There's also a fun gotcha on this one.
     * When you are done in play mode, be sure to exit play mode using 
     * the stop button in your editor. Do not close the console manually; it
     * will in return close the editor as well. */
    public class ServerConsole : MonoBehaviour
    {
        //#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#if (UNITY_STANDALONE_WIN && UNITY_SERVER) //|| UNITY_EDITOR

        ConsoleWindow console = new ConsoleWindow();
        ConsoleInput input = new ConsoleInput();
        //Notes I've made my CommandHandler an instance instead of a static class.
        /// <summary>
        /// Handles commands received from the console.
        /// </summary>
        CommandHandler commandHandler = new CommandHandler();

        //
        // Create console window, register callbacks
        //
        void Awake()
        {
            DontDestroyOnLoad(gameObject);

            console.Initialize();
            /* //Notes you may set the title to your console
             * app here. */
            console.SetTitle("Emberfate Server Console");

            input.OnInputText += OnInputText;

            //Application.RegisterLogCallback(HandleLog);
            /* //Notes The website indicates to use Application.RegisterLogCallback, but
             * that method is however obsolete. Instead I am using the event
             * Application.logMessageReceived. */
            Application.logMessageReceived += Application_logMessageReceived;

            Console.WriteLine("Console Started");
        }

        /// <summary>
        /// Debug.Log callback.
        /// </summary>
        private void Application_logMessageReceived(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Warning)
                System.Console.ForegroundColor = ConsoleColor.Yellow;
            else if (type == LogType.Error)
                System.Console.ForegroundColor = ConsoleColor.Red;
            else
                System.Console.ForegroundColor = ConsoleColor.White;

            /* //Notes
             * The code below was causing text to display twice
             * so I have commented it out. */
            /*
            // We're half way through typing something, so clear this line ..
            if (Console.CursorLeft != 0)
                input.ClearLine();

            System.Console.WriteLine(message);

            // If we were typing something re-add it.
            input.RedrawInputLine();
            */
        }

        //
        // Text has been entered into the console
        // Run it as a console command
        //
        void OnInputText(string obj)
        {
            commandHandler.Run(obj);
        }

        //
        // Update the input every frame
        // This gets new key input and calls the OnInputText callback
        //
        void Update()
        {
            input.Update();
        }

        //
        // It's important to call console.ShutDown in OnDestroy
        // because compiling will error out in the editor if you don't
        // because we redirected output. This sets it back to normal.
        //
        void OnDestroy()
        {
            console.Shutdown();
        }

#endif
    }


}