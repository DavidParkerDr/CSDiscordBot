﻿using Castle.Core.Internal;
using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DiscordRoleBot
{
    public class ApplicantsFile
    {
        private Dictionary<int, Applicant> applicants;
        private static ApplicantsFile instance;

        public static ApplicantsFile Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new ApplicantsFile();
                }
                return instance;
            }
        }

        public bool TryGetApplicant(int applicantId, out Applicant applicant)
        {
            bool success = applicants.TryGetValue(applicantId, out applicant);
            return success;
        }

        public ApplicantsFile(string path = "applicants.txt")
        {
            applicants = new Dictionary<int, Applicant>();
            Load(path);
        }
        private void Load(string fileName)
        {

            try
            {
                //Pass the file path and file name to the StreamReader constructor
                StreamReader streamReader = new StreamReader(fileName);
                Load(streamReader);
                //close the file
                streamReader.Close();
            }
            catch(FileNotFoundException e)
            {
                Program.Log(new LogMessage(LogSeverity.Warning, "Bot", "Could not find Applicants File with name: " + fileName));
            }
            catch (Exception e)
            {
                Program.Log(new LogMessage(LogSeverity.Warning, "Bot", "Could not load Applicants File with name: " + fileName));
            }

        }
        private void Load(StreamReader streamReader)
        {
            //Read the first line of text
            String line = streamReader.ReadLine();
            do
            {
                string[] tokens = line.Split(',');
                string applicantIdString = tokens[0].Trim();
                string discordSnowflakeString = tokens[1].Trim();
                int applicantId = int.Parse(applicantIdString);
                bool discordConnected = false;
                ulong discordSnowflake = 0;
                if (discordSnowflakeString.IsNullOrEmpty() || discordSnowflakeString == "false")
                {
                    discordConnected = false;
                }
                else
                {
                    discordConnected = ulong.TryParse(discordSnowflakeString, out discordSnowflake);
                }
                Applicant applicant = new Applicant(applicantId, discordSnowflake, discordConnected);
                applicants.Add(applicant.ApplicantId, applicant);
                line = streamReader.ReadLine();
            }
            //Continue to read until you reach end of file
            while (line != null);


        }
        public void Save(string fileName = "applicants.txt")
        {
            try
            {
                StreamWriter streamWriter = new StreamWriter(fileName);
                Save(streamWriter);
                streamWriter.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
        }
        public void Save(StreamWriter streamWriter)
        {
            foreach (KeyValuePair<int, Applicant> applicantRecord in applicants)
            {
                Applicant applicant = applicantRecord.Value;
                applicant.Save(streamWriter);
            }
        }
    }
}
