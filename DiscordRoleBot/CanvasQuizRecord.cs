﻿using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordRoleBot
{
    class CanvasQuizRecord
    {
        private string user;
        public int StudentId { get; set; }
        public string User
        {
            get => user; set
            {
                user = GenerateUser(value);
            }
        }
        public string RecordTimeStampString { get; private set; }

        private string GenerateUser(string delimitedUser)
        {
            if (delimitedUser.Contains(','))
            {
                int commaPos = delimitedUser.LastIndexOf(',');
                string username = delimitedUser.Substring(0, commaPos).Trim();
                string discriminator = delimitedUser.Substring(commaPos+1).Trim();
                return username + "#" + discriminator;
            }
            else
            {
                return delimitedUser;
            }
        }

        public DateTime GenerateDateTime()
        {
            return DateTime.Now;
        }
    }

    internal sealed class CanvasQuizRecordMap : ClassMap<CanvasQuizRecord>
    {
        public CanvasQuizRecordMap()
        {
            Map(m => m.StudentId).Name("sis_id");
            Map(m => m.User).Index(8);
            Map(m => m.RecordTimeStampString).Name("submitted");
        }
    }
}