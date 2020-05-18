using Castle.Core.Internal;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DiscordRoleBot
{
    public class StudentsFile
    {
        private Dictionary<int, Student> students;
        private Dictionary<ulong, Student> studentsDiscordLookup;
        private static StudentsFile instance;

        public static StudentsFile Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new StudentsFile();
                }
                return instance;
            }
        }

        public bool TryGetStudent(int applicantId, out Student student)
        {
            bool success = students.TryGetValue(applicantId, out student);
            return success;
        }
        public bool TryGetDiscordStudent(ulong applicantDiscordSnowflake, out Student student)
        {
            bool success = studentsDiscordLookup.TryGetValue(applicantDiscordSnowflake, out student);
            return success;
        }

        public void AddStudent(Student student)
        {
            studentsDiscordLookup.Add(student.DiscordSnowflake, student);
            students.Add(student.StudentId, student);
            //save updated list
            Save();
        }

        public StudentsFile(string path = "students.txt")
        {
            students = new Dictionary<int, Student>();
            studentsDiscordLookup = new Dictionary<ulong, Student>();
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
            catch (FileNotFoundException e)
            {
                Program.Log(new LogMessage(LogSeverity.Warning, "Bot", "Could not find Students File with name: " + fileName));
            }
            catch (Exception e)
            {
                Program.Log(new LogMessage(LogSeverity.Warning, "Bot", "Could not load Students File with name: " + fileName + "because of: " + e.Message));
            }

        }
        private void Load(StreamReader streamReader)
        {
            //Read the first line of text
            String line = streamReader.ReadLine();
            do
            {
                string[] tokens = line.Split(',');
                string discordSnowflakeString = tokens[0].Trim();
                string studentIdString = tokens[1].Trim();
                int studentId = int.Parse(studentIdString);
                ulong discordSnowflake = ulong.Parse(discordSnowflakeString);
                Student student = new Student(studentId, discordSnowflake);
                students.Add(student.StudentId, student);
                studentsDiscordLookup.Add(student.DiscordSnowflake, student);
                line = streamReader.ReadLine();
            }
            //Continue to read until you reach end of file
            while (line != null);


        }
        public void LoadOldDiscordList(string fileName)
        {

            try
            {
                //Pass the file path and file name to the StreamReader constructor
                StreamReader streamReader = new StreamReader(fileName);
                LoadOldDiscordList(streamReader);
                //close the file
                streamReader.Close();
            }
            catch (FileNotFoundException e)
            {
                Program.Log(new LogMessage(LogSeverity.Warning, "Bot", "Could not find Students File with name: " + fileName));
            }
            catch (Exception e)
            {
                Program.Log(new LogMessage(LogSeverity.Warning, "Bot", "Could not load Students File with name: " + fileName + "because of: " + e.Message));
            }

        }
        private void LoadOldDiscordList(StreamReader streamReader)
        {
            //Read the first line of text
            String line = streamReader.ReadLine();
            do
            {
                string[] tokens = line.Split(',');
                string discordUsernamePlusDiscriminatorString = tokens[1].Trim();
                string studentIdString = tokens[0].Trim();
                int studentId = int.Parse(studentIdString);
                SocketGuildUser discordUser = Program.GetSocketGuildUser(discordUsernamePlusDiscriminatorString);
                if(discordUser != null)
                {
                    ulong discordSnowflake = discordUser.Id;
                    Student student = new Student(studentId, discordSnowflake);
                    students.Add(student.StudentId, student);
                    studentsDiscordLookup.Add(student.DiscordSnowflake, student);
                }
                else
                {
                    // that username and discriminator combo may have changed since the original list was made.
                    Console.WriteLine(line + ", is no longer a valid record.");
                }
                
                line = streamReader.ReadLine();
            }
            //Continue to read until you reach end of file
            while (line != null);


        }
        public void Save(string fileName = "students.txt")
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
            foreach (KeyValuePair<ulong, Student> studentRecord in studentsDiscordLookup)
            {
                Student student = studentRecord.Value;
                student.Save(streamWriter);
            }
        }
    }
}
