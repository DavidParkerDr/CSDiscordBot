using Castle.Core.Internal;
using CsvHelper;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

        private DateTime lastQuizEntry;

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
        public int NumberOfRegisteredStudents()
        {
            return studentsDiscordLookup.Count;
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
            String dateStampString = streamReader.ReadLine();
            lastQuizEntry = DateTime.Parse(dateStampString);
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
                SocketGuildUser discordUser = Bot.GetSocketGuildUser(discordUsernamePlusDiscriminatorString);
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
                streamWriter.WriteLine(lastQuizEntry.ToString());
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
                    List<CanvasQuizRecord> canvasQuizRecordsList = canvasQuizRecords.ToList<CanvasQuizRecord>();
                    if (canvasQuizRecordsList.Count > 0)
                    {
                        CanvasQuizRecord latestRecord = canvasQuizRecordsList[0];

                        DateTime latestRecordTimeStamp = latestRecord.GenerateDateTime();
                        if (latestRecordTimeStamp > lastQuizEntry)
                        {

                            foreach (CanvasQuizRecord canvasQuizRecord in canvasQuizRecordsList)
                            {
                                DateTime currentRecordDateStamp = canvasQuizRecord.GenerateDateTime();
                                if (currentRecordDateStamp > lastQuizEntry)
                                {
                                    Student student = null;
                                    if (TryGetStudent(canvasQuizRecord.StudentId, out student))
                                    {
                                        //quiz record matches an existing student;
                                        _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Error, "CanvasQuiz", "Quiz record: " + canvasQuizRecord.User + " matches existing student: " + canvasQuizRecord.Name + " (" + canvasQuizRecord.StudentId + ")."));
                                    }
                                    else
                                    {
                                        //this quiz entry is by a student that does not have a discord record
                                        SocketGuildUser discordUser = Bot.GetSocketGuildUser(canvasQuizRecord.User);
                                        if (discordUser != null)
                                        {
                                            student = new Student(canvasQuizRecord.StudentId, discordUser.Id);
                                            AddStudent(student);
                                            SocketRole studentRole = Bot.GetRole("student");
                                            SocketRole unassignedRole = Bot.GetRole("unassigned");
                                            _ = Bot.AddRoleToUser(discordUser, studentRole);
                                            _ = Bot.RemoveRole(discordUser, unassignedRole);
                                            FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "CanvasQuiz", "Added a new user " + canvasQuizRecord.Name + " (" + canvasQuizRecord.StudentId + ")" + " with Discord id: " + canvasQuizRecord.User));
                                            notification += "Added a new user " + canvasQuizRecord.Name + " (" + canvasQuizRecord.StudentId + ")" + " with Discord id: " + canvasQuizRecord.User + "\n";

                                        }
                                        else
                                        {
                                            // this quiz entry did not specify a valid user on the discord server.
                                            _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Error, "CanvasQuiz", "Quiz entry: " + canvasQuizRecord.User + " by student: " + canvasQuizRecord.Name + " (" + canvasQuizRecord.StudentId + ") does not match a valid Discord user on the server."));
                                        }
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                            lastQuizEntry = latestRecordTimeStamp;
                            Save();
                        }
                        else
                        {
                            _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "CanvasQuiz", "No new quiz responses."));
                        }
                    }
                    else
                    {
                        _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "CanvasQuiz", "No quiz responses."));
                    }
                    mut.Release();
                    while(notification.Length > 0)
                    {
                        if (notification.Length > 1000)
                        {
                            _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "Notification", "Notification is too long: " + notification.Length + " characters long."));
                            int lineFeedIndex = 1000;
                            char character = notification[lineFeedIndex];
                            while(character != '\n' && lineFeedIndex > 0)
                            {
                                lineFeedIndex--;
                                character = notification[lineFeedIndex];    
                            }
                            if(lineFeedIndex > 0)
                            {
                                string notificationPart = notification.Substring(0, lineFeedIndex);
                                notification = notification.Substring(lineFeedIndex);
                                Bot.Notify(notificationPart);
                            }
                            else
                            {
                                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "Error", "Couldn't find line feed"));
                            }
                            
                        }
                        else
                        {
                            Bot.Notify(notification);
                            break;
                        }
                    }
                }
            }
            return success;
        }
    }
}
