﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SHC
{
    class StateMachine
    {
        static Dictionary<String, State> stateList = new Dictionary<string, State>();
        static State currentState;
        static List<int> ActivePlayers { get; }

        static Random gen = new Random();
        static int GameID = gen.Next();

        static StateMachine()
        {
            State lobby = new State("Lobby", () => Reader.TestZero(0x024BA938, 4));
            State game = new State("Game", () => !Reader.TestZero(0x024BA938, 4) && !Reader.IsStatic(0x024BAEC0, 4));
            State stats = new State("Stats", () => !Reader.TestZero(0x24BA938,4) && Reader.IsStatic(0x024BAEC0, 4));

            stateList["Lobby"] = lobby;
            stateList["Game"] = game;
            stateList["Stats"] = stats;

            ActivePlayers = new List<int>();
            currentState = lobby;
        }

        public static void Reset()
        {
            currentState = stateList["Lobby"];
        }

        static State Next()
        {
            if (currentState == stateList["Lobby"])
            {
                return stateList["Game"];
            } else if (currentState == stateList["Game"])
            {
                return stateList["Stats"];
            } else if (currentState == stateList["Stats"])
            {
                if (Reader.TestZero(0x024BA938, 4))
                {
                    return stateList["Lobby"];
                } else
                {
                    return stateList["Game"];
                }
            }
            return stateList["Lobby"];
        }

        public static bool Lobby() => currentState == stateList["Lobby"];
        public static bool Game() => currentState == stateList["Game"];
        public static bool Stats() => currentState == stateList["Stats"];


        public static void Update()
        {
            if (!currentState.isActive())
            {
                currentState = StateMachine.Next();

                if (Stats())
                {
                    String filename = "greatestLord " + GameID.ToString() + ".txt";
                    File.WriteAllText(filename, Newtonsoft.Json.JsonConvert.SerializeObject(GreatestLord.Update()));
                } else if (Lobby())
                {
                    GameID = gen.Next();
                }
            }

            if (Game())
            {
                Dictionary<String, String> gameData = new Dictionary<string, string>();
                for (int i = 0; i < PlayerFactory.PlayerList.Count; i++)
                {
                    Player player = PlayerFactory.PlayerList[i];
                    bool active = Reader.ReadBool(Convert.ToInt32(player.Data["Active"]["address"].ToString(), 16), 1);
                    if (!active)
                    {
                        continue;
                    }
                    gameData["Player" + (i + 1).ToString()] = player.Update();
                }
                System.IO.File.WriteAllText("playerStats.txt", Newtonsoft.Json.JsonConvert.SerializeObject(gameData));
            }
        }

        public static String CurrentState()
        {
            return currentState.ToString();
        }

    }
}