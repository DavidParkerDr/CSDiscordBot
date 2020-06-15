using Castle.Core.Internal;
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
        private Dictionary<ulong, Applicant> applicantsDiscordLookup;
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

        public int NumberOfRegisteredApplicants()
        {
            return applicantsDiscordLookup.Count;
        }

        public bool TryGetApplicant(int applicantId, out Applicant applicant)
        {
            bool success = applicants.TryGetValue(applicantId, out applicant);
            return success;
        }
        public bool TryGetDiscordApplicant(ulong applicantDiscordSnowflake, out Applicant applicant)
        {
            bool success = applicantsDiscordLookup.TryGetValue(applicantDiscordSnowflake, out applicant);
            return success;
        }

        public void UpdateDiscordLookup(Applicant applicant)
        {
            applicantsDiscordLookup.Add(applicant.DiscordSnowflake, applicant);
            //save updated list
            Save();
        }

        public ApplicantsFile(string path = "applicants.txt")
        {
            applicants = new Dictionary<int, Applicant>();
            applicantsDiscordLookup = new Dictionary<ulong, Applicant>();
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
                FileLogger.Instance.Log(new LogMessage(LogSeverity.Warning, "Bot", "Could not find Applicants File with name: " + fileName));
            }
            catch (Exception e)
            {
                FileLogger.Instance.Log(new LogMessage(LogSeverity.Warning, "Bot", "Could not load Applicants File with name: " + fileName + " because of: " + e.Message));
            }

        }
        private void Load(StreamReader streamReader)
        {
            //Read the first line of text
            String line = streamReader.ReadLine();
            if (!line.IsNullOrEmpty())
            {
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
                    Applicant existingApplicant = null;
                    if (!applicants.TryGetValue(applicantId, out existingApplicant))
                    {
                        Applicant applicant = new Applicant(applicantId, discordSnowflake, discordConnected);
                        applicants.Add(applicant.ApplicantId, applicant);
                        if (applicant.DiscordConnected)
                        {
                            applicantsDiscordLookup.Add(applicant.DiscordSnowflake, applicant);
                        }
                    }
                    else
                    {
                        // if the current record has a discord id then we can potentially fill in the blank of an existing.
                        if (discordConnected)
                        {
                            // the line in the file has a discord snowflake, need to check if the existing one does
                            if (existingApplicant.DiscordConnected)
                            {
                                // this is a bit weird as there is a duplicate and both have a discord id
                                if(discordSnowflake == existingApplicant.DiscordSnowflake)
                                {
                                    // the snowflakes are a match, which is weird but ok
                                    string warningMessage = "The applicants file contains a duplicate with the same Discord ids: ApplicantID(" + applicantId.ToString() + ") DiscordId(" + discordSnowflake.ToString() + " - " + existingApplicant.DiscordSnowflake.ToString() + ").";
                                    _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Warning, "Bot", warningMessage));
                                }
                                else
                                {
                                    // the snowflakes are different... this should not happen
                                    string errorMessage = "The applicants file contains a duplicate with different Discord ids: ApplicantID(" + applicantId.ToString() + ") DiscordId(" + discordSnowflake.ToString() + " - " + existingApplicant.DiscordSnowflake.ToString() + ").";
                                    _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Error, "Bot", errorMessage));
                                }
                            }
                            else
                            {
                                string warningMessage = "The applicants file contains a duplicate but this one has a DiscordId so adding it: ApplicantID(" + applicantId.ToString() + ") DiscordId(" + discordSnowflake.ToString() + ").";
                                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Warning, "Bot", warningMessage));
                                existingApplicant.AddDiscordSnowflake(discordSnowflake);
                            }
                        }
                    }
                    line = streamReader.ReadLine();
                }
                //Continue to read until you reach end of file
                while (line != null);
            }
            else
            {
                FileLogger.Instance.Log(new LogMessage(LogSeverity.Warning, "Bot", "Could not load Applicants File because it is empty."));
            }

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
