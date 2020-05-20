using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordRoleBot
{
    class FileLogger
    {
        internal static SemaphoreSlim mut = new SemaphoreSlim(1, 1);
        private string filePath = "logfile.txt";

        private static FileLogger instance;

        public static FileLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new FileLogger();
                }
                return instance;
            }
        }

        public Task Log(LogMessage message)
        {
            mut.Wait();
            using (StreamWriter streamWriter = new StreamWriter(filePath, true))
            {
                string date = DateTime.Now.ToShortDateString() + " ";
                streamWriter.Write(date);
                streamWriter.WriteLine(message.ToString());
                Console.Write(date);
                Console.WriteLine(message.ToString());
                streamWriter.Close();
            }
            mut.Release();
            return Task.CompletedTask;
        }
    }
}
