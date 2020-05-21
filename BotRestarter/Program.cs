using System;
using System.Diagnostics;
using System.Threading;

namespace BotRestarter
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                var runningProcessByName = Process.GetProcessesByName("DiscordRoleBot"); // Change this process name to the exe that you want to run
                if (runningProcessByName.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("BotRestarter: The bot went down. I am restarting it.");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    Process p = new Process();

                    p.StartInfo.FileName = "DiscordRoleBot.exe"; // Change this process name to the exe that you want to run

                    // redirect the output
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;

                    // hookup the eventhandlers to capture the data that is received
                    p.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                    p.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

                    // direct start
                    p.StartInfo.UseShellExecute = false;

                    if (p.Start())
                    {
                        p.BeginOutputReadLine();
                        p.BeginErrorReadLine();

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("BotRestarter: Successful restart.");
                        Console.ForegroundColor = ConsoleColor.Gray;

                        p.WaitForExit();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("BotRestarter: Restart failed. Trying again in 10 sec...");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Thread.Sleep(new TimeSpan(0, 0, 10));
                    }

                }
            }
        }
    }
}
