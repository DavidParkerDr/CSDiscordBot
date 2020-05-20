using Castle.Core.Internal;
using CsvHelper;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace DiscordRoleBot
{
    public class StudentsFile
    {
        internal static SemaphoreSlim mut = new SemaphoreSlim(1, 1);

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
                FileLogger.Instance.Log(new LogMessage(LogSeverity.Warning, "Bot", "Could not find Students File with name: " + fileName));
            }
            catch (Exception e)
            {
                FileLogger.Instance.Log(new LogMessage(LogSeverity.Warning, "Bot", "Could not load Students File with name: " + fileName + "because of: " + e.Message));
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
                FileLogger.Instance.Log(new LogMessage(LogSeverity.Warning, "Bot", "Could not find Students File with name: " + fileName));
            }
            catch (Exception e)
            {
                FileLogger.Instance.Log(new LogMessage(LogSeverity.Warning, "Bot", "Could not load Students File with name: " + fileName + "because of: " + e.Message));
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

        public bool UnpackFromCanvas(string csv)
        {
            bool success = false;
            using (TextReader reader = new StringReader(csv))
            using (CsvReader csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csvReader.Configuration.RegisterClassMap<CanvasQuizRecordMap>();
                IEnumerable<CanvasQuizRecord> canvasQuizRecords;
                try
                {
                    canvasQuizRecords = csvReader.GetRecords<CanvasQuizRecord>();
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Something went wrong with the CSV.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return false;
                }

                if (canvasQuizRecords is IEnumerable<CanvasQuizRecord>)
                {
                    string notification = "";
                    mut.Wait();
                    foreach (CanvasQuizRecord canvasQuizRecord in canvasQuizRecords)
                    {
                        Student student = null;
                        if(TryGetStudent(canvasQuizRecord.StudentId, out student))
                        {
                            //quiz record matches an existing student;
                        }
                        else
                        {
                            //this quiz entry is by a student that does not have a discord record
                            SocketGuildUser discordUser = Program.GetSocketGuildUser(canvasQuizRecord.User);
                            if(discordUser != null)
                            {
                                student = new Student(canvasQuizRecord.StudentId, discordUser.Id);
                                AddStudent(student);
                                SocketRole studentRole = Program.GetRole("student");
                                _ = Program.AddRoleToUser(discordUser, studentRole);
                                FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "CanvasQuiz", "Added a new user with id: " + canvasQuizRecord.User + " (" + canvasQuizRecord.StudentId +")"));
                                notification += "Added a new user with id: " + canvasQuizRecord.User + " (" + canvasQuizRecord.StudentId + ")\n";
                                
                            }
                            else
                            {
                                // this quiz entry did not specify a valid user on the discord server.
                            }
                        }
                    }
                    mut.Release();
                    if(notification.Length > 0)
                    {
                        Program.Notify(notification);
                    }
                }
            }
            return success;
        }
    }
}
