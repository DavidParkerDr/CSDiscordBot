using Discord;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordRoleBot
{
    class CanvasClient
    {
        private Dictionary<Guid, JArray> lookupResults;
        private HttpClient client;
        private static CanvasClient instance;
        public static CanvasClient Instance { 
            get
            {
                if(instance == null)
                {
                    instance = new CanvasClient();
                }
                return instance;
            }
        }
        private CanvasClient()
        {
            lookupResults = new Dictionary<Guid, JArray>();
            client = new HttpClient();
            var canvasAuthentication = Bot._config.GetValue(Type.GetType("System.String"), "CanvasAuthentication").ToString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", canvasAuthentication);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        }
        public async Task<StudentLookupResult> GetCanvasUserFrom9DigitId(int uni9DigitId)
        {
            int tryCount = 0;
            var canvasUser = Bot._config.GetValue(Type.GetType("System.String"), "CanvasUser").ToString();
            //string path = "https://canvas.hull.ac.uk/api/v1/courses/17835/users/" + uni9DigitId.ToString() + "/?as_user_id=sis_user_id:" + canvasUser;
            string path = "https://canvas.hull.ac.uk/api/v1/courses/17835/users/sis_user_id:" + uni9DigitId.ToString() + "/?as_user_id=sis_user_id:" + canvasUser;
            while (tryCount < 3)
            {
                (string response, string nextPagePath) = await GetStringAsync(path);
                try
                {
                    JObject responseObject = JObject.Parse(response);
                    if(responseObject.ContainsKey("errors"))
                    {
                        return null;
                    }
                    StudentLookupResult studentLookupResult = new StudentLookupResult(responseObject);
                    return studentLookupResult;
                }
                catch
                {
                    tryCount++;
                }
            }
            return null;
        }
        /// <summary>
        /// When querying Canvas it will likely generate a paginated response.
        /// That is it will pass you the response to your query and if there is 
        /// more to come then it will give you a path to the next set of results.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        public async Task<JArray> ProcessResponseAsJArray(Task<(string,string)> task, object arg2)
        {
            Guid lookupId = (Guid)arg2;
            string response = task.Result.Item1;
            string path = task.Result.Item2;
            JArray responseArray = null;
            JArray result = null;
            if(response!= null)
            {
                responseArray = JArray.Parse(response);
            }
            
            JArray combinedLookups = null;
            if(lookupResults.TryGetValue(lookupId, out combinedLookups))
            {
                combinedLookups.Merge(responseArray);
                lookupResults[lookupId] = combinedLookups;
            }
            else
            {
                lookupResults.Add(lookupId, responseArray);
            }

            //Console.WriteLine(lookupId.ToString() + " - " + path);
            if (path != null)
            {
                result = await GetStringAsync(path).ContinueWith(ProcessResponseAsJArray, lookupId).Result;
            }
            else
            {
                // we should have everything now...
                lookupResults.TryGetValue(lookupId, out result);                
            }
            return result;
        }
        public async Task<JArray> GetCompleteCanvasUserList()
        {
            Guid lookupId = Guid.NewGuid();
            var canvasUser = Bot._config.GetValue(Type.GetType("System.String"), "CanvasUser").ToString();
            string path = "https://canvas.hull.ac.uk/api/v1/courses/17835/users/" + "?as_user_id=sis_user_id:" + canvasUser + "&per_page=100";
            JArray result = await GetStringAsync(path).ContinueWith(ProcessResponseAsJArray, lookupId).Result;
            return result;
        }
        public async Task<StudentLookupResult> GetCanvasUserFrom6DigitId(string userId)
        {
            int tryCount = 0;
            while (tryCount < 3)
            {
                JArray result = GetCompleteCanvasUserList().Result;   
                try
                {
                    for(int i = 0; i < result.Count; i++)
                    {
                        JObject jObject = result[i] as JObject;
                        if (jObject.ContainsKey("login_id"))
                        {
                            string loginId = jObject["login_id"].ToString();
                            if (userId == loginId)
                            {
                                StudentLookupResult studentLookupResult = new StudentLookupResult(jObject);
                                return studentLookupResult;
                            }
                        }
                    }
                    
                }
                catch (Exception e)
                {
                    tryCount++;
                }
            }
            return null;
        }
        private async Task<(string, string)> GetStringAsync(string path)
        {
            string responsestring = "";
            string nextPagePath = "";
            HttpRequestMessage request = new HttpRequestMessage { Method = HttpMethod.Get, RequestUri = new Uri(path) };
            HttpResponseMessage response = await client.SendAsync(request);
            responsestring = await response.Content.ReadAsStringAsync();
            HttpHeaders headers = response.Headers;
            IEnumerable<string> values;
            if (headers.TryGetValues("Link", out values))
            {
                string linkHeaders = values.First();
                nextPagePath = GetNextPagePath(linkHeaders);                
               
            }
            return (responsestring, nextPagePath);
        }

        private string GetNextPagePath(string linkHeaders)
        {
            string nextPagePath = null;
            string[] linkHeadersArray = linkHeaders.Split(',');
            foreach (string linkHeaderString in linkHeadersArray)
            {
                if(linkHeaderString.Contains("rel=\"next\""))
                {
                    int closeChevron = linkHeaderString.IndexOf('>');
                    if(closeChevron != -1)
                    {
                        nextPagePath = linkHeaderString.Substring(1, closeChevron - 1);
                        return nextPagePath;
                    }
                }
            }
            return nextPagePath;
        }

        async Task<string> PostStringAsync(string path, string jsonString)
        {
            string responsestring = "";
            HttpContent content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            HttpRequestMessage request = new HttpRequestMessage { Method = HttpMethod.Post, RequestUri = new Uri(path), Content = content };
            HttpResponseMessage response = await client.SendAsync(request);
            responsestring = await response.Content.ReadAsStringAsync();
            return responsestring;
        }
        public void Go()
        {
            GetQuizResponses().Wait();
        }

        public async Task GetQuizResponses()
        {
            var canvasUser = Bot._config.GetValue(Type.GetType("System.String"), "CanvasUser").ToString();
            while (true)
            {
                try
                {
                    //First create a new report
                    _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "CanvasQuiz", "Starting quiz report build."));
                    string path = @"https://canvas.hull.ac.uk/api/v1/courses/17835/quizzes/20659/reports?as_user_id=sis_user_id:" + canvasUser;
                    int timeout = 1000;
                    var task = PostStringAsync(path, @"{""quiz_report"":{""report_type"": ""student_analysis"",""includes_all_versions"": true}}");
                    if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                    {
                        // task completed within timeout
                        string response = task.Result;
                        // string response = await PostStringAsync(path, @"{""quiz_report"":{""report_type"": ""student_analysis"",""includes_all_versions"": true}}");
                        var obj = JObject.Parse(response);
                        bool complete = false;
                        string nextPagePath = null;
                        path = (string)obj["progress_url"] + "?as_user_id=sis_user_id:" + canvasUser;
                        double progress = 0.0;
                        while (!complete)
                        {
                            Thread.Sleep(5000);

                            _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "CanvasQuiz", "Building quiz report progress: " + progress + "%"));
                            (response, nextPagePath) = await GetStringAsync(path);
                            obj = JObject.Parse(response);
                            progress = (double)obj["completion"];
                            if (progress == 100.0)
                            {
                                complete = true;
                                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "CanvasQuiz", "Quiz report complete: " + progress + "%"));
                            }
                        }
                        _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "CanvasQuiz", "Retrieving quiz report."));
                        path = "https://canvas.hull.ac.uk/api/v1/courses/17835/quizzes/20659/reports/" + (string)obj["context_id"] + "?as_user_id=sis_user_id:" + canvasUser;
                        (response, nextPagePath) = await GetStringAsync(path);
                        obj = JObject.Parse(response);
                        path = (string)obj["file"]["url"] + "&as_user_id=sis_user_id:" + canvasUser;
                        (response, nextPagePath) = await GetStringAsync(path);

                        // in theory we should now have a complete response that we can inspect for new responses and update the student role
                        // todo here
                        _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "CanvasQuiz", "Got the quiz responses!"));
                        StudentsFile.Instance.UnpackFromCanvas(response);
                    }
                    else
                    {
                        // timeout logic
                        //_ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Error, "CanvasQuiz", "Failed to get quiz report: timed out after " + timeout + " seconds."));
                        throw new TimeoutException("Failed to get quiz report: timed out after " + timeout + " seconds.");                        
                    }
                   
                }
                catch (Exception e)
                {
                    string message = e.Message + "\n";
                    while (e.InnerException != null)
                    {
                        message += e.InnerException.Message + "\n";
                        e = e.InnerException;
                    }
                    _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Error, "CanvasQuiz", "Failed to get quiz report: " + message));
                }

           

                Thread.Sleep(new TimeSpan(0, 10, 0));
            }
        }
    }
}
