using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace DiscordRoleBot
{
    public class Applicant
    {
        [Key]
        public int ApplicantId { get; private set; }
        public ulong DiscordSnowflake { get; private set; }
        public bool DiscordConnected { get; private set; }

        public Applicant(int applicantId, ulong discordSnowflake, bool discordConnected)
        {
            ApplicantId = applicantId;
            DiscordSnowflake = discordSnowflake;
            DiscordConnected = discordConnected;
        }
        public bool AddDiscordSnowflake(ulong discordSnowflake)
        {
            bool success = false;
            if (!DiscordConnected)
            {
                DiscordSnowflake = discordSnowflake;
                DiscordConnected = true;
                success = true;
            }
            return success;
        }
        public void Save(StreamWriter streamWriter)
        {
            streamWriter.Write(ApplicantId + ",");
            if(DiscordConnected)
            {
                streamWriter.WriteLine(DiscordSnowflake);
            }
            else
            {
                streamWriter.WriteLine("false");
            }
        }
    }
}
