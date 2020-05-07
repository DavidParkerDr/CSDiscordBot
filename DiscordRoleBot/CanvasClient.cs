using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DiscordRoleBot
{
    class CanvasClient
    {
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
            client = new HttpClient();
            var canvasAuthentication = Program._config.GetValue(Type.GetType("System.String"), "CanvasAuthentication").ToString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", canvasAuthentication);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        }
        public async Task<StudentLookupResult> GetCanvasUserFrom9DigitId(int uni9DigitId)
        {
            int tryCount = 0;
            var canvasUser = Program._config.GetValue(Type.GetType("System.String"), "CanvasUser").ToString();
            string path = "https://canvas.hull.ac.uk/api/v1/courses/17835/users/" + "?as_user_id=sis_user_id:" + canvasUser + " & search_term=" + uni9DigitId.ToString();
            while (tryCount < 3)
            {
                (string response, string nextPagePath) = await GetStringAsync(path);
                try
                {
                    StudentLookupResult studentLookupResult = new StudentLookupResult(response);
                    return studentLookupResult;
                }
                catch
                {
                    tryCount++;
                }
            }
            return null;
        }
        public async Task<StudentLookupResult> GetCanvasUserFrom6DigitId(string userId)
        {
            int tryCount = 0;
            var canvasUser = Program._config.GetValue(Type.GetType("System.String"), "CanvasUser").ToString();
            string path = "https://canvas.hull.ac.uk/api/v1/courses/17835/users/" + "?as_user_id=sis_user_id:" + canvasUser + "&per_page=100";
            while (tryCount < 3)
            {
                string response = "";
                JArray totalResponse = null;
                do
                {
                    (response, path) = await GetStringAsync(path);
                    JArray jArray = JArray.Parse(response);
                    if (totalResponse == null)
                    {
                        totalResponse = jArray;
                    }
                    else
                    {
                        totalResponse.Merge(jArray);
                    }
                }
                while (path != null);
                try
                {
                    for(int i = 0; i < totalResponse.Count; i++)
                    {
                        JObject jObject = totalResponse[i] as JObject;
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
    }
}
