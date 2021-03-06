﻿using System;
using System.Linq;

namespace Insider
{
    class Program
    {
        static bool EncounteredError;

        static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: insider.exe [target] [save] [references]");
                Console.Error.WriteLine(" - target: Path to the assembly to process");
                Console.Error.WriteLine(" - save: Path to the file that'll be created");
                Console.Error.WriteLine(" - references: Semicolon-separated list of");
                Console.Error.WriteLine("               the assembly's references");
                return 1;
            }

            using (Outsider outsider = new Outsider(args[0], args[1], args[2].Split(';')))
            {
                outsider.Weaver.MessageLogged += MessageLogged;

                try
                {
                    outsider.Weaver.Process();
                }
                catch (Exception e)
                {
                    EncounteredError = true;
                }
            }

            return EncounteredError ? 1 : 0;
        }

        private static void MessageLogged(MessageLoggedEventArgs e)
        {
            string prefix = "";

            switch (e.Importance)
            {
                case MessageImportance.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    prefix = "[+] {0}";
                    break;
                case MessageImportance.Debug:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    prefix = "[*] {0}";
                    break;
                case MessageImportance.Warning:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    prefix = "[-] {0}";
                    break;
                case MessageImportance.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    prefix = "[!] {0}";
                    break;
            }

            Console.WriteLine(prefix, e.Message);
            Console.ResetColor();

            if (e.StoppedWeaving)
                EncounteredError = true;
        }
    }
}
