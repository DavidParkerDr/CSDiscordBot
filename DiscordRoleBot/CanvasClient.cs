using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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
        public async Task<StudentLookupResult> GetCanvasUser(int uni9DigitId)
        {
            int tryCount = 0;
            var canvasUser = Program._config.GetValue(Type.GetType("System.String"), "CanvasUser").ToString();
            string path = "https://canvas.hull.ac.uk/api/v1/courses/17835/users/" + "?as_user_id=sis_user_id:" + canvasUser + " & search_term=" + uni9DigitId.ToString();
            while (tryCount < 3)
            {
                string response = await GetStringAsync(path);
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
        private async Task<string> GetStringAsync(string path)
        {
            string responsestring = "";
            HttpRequestMessage request = new HttpRequestMessage { Method = HttpMethod.Get, RequestUri = new Uri(path) };
            HttpResponseMessage response = await client.SendAsync(request);
            responsestring = await response.Content.ReadAsStringAsync();
            return responsestring;
        }
    }
}
