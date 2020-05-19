using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace DiscordRoleBot
{
    public class Student
    {
        
        public int StudentId { get; private set; }
        [Key]
        public ulong DiscordSnowflake { get; private set; }

        public Student(int studentId, ulong discordSnowflake)
        {
            StudentId = studentId;
            DiscordSnowflake = discordSnowflake;
        }
        
        public void Save(StreamWriter streamWriter)
        {
            streamWriter.WriteLine(DiscordSnowflake + "," + StudentId);
        }
    }
    
}
