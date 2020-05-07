using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordRoleBot
{
    class StudentLookupResult
    {
        public int UniId { get; private set; }
        public string LoginId { get; private set; }
        public string Email { get; private set; }
        public string Name { get; private set; }

        public StudentLookupResult(string canvasJsonString)
        {
            try
            {
                JArray jArray = JArray.Parse(canvasJsonString);
                UniId = int.Parse(jArray[0]["sis_user_id"].ToString());
                LoginId = jArray[0]["login_id"].ToString();
                Email = jArray[0]["email"].ToString();                
                Name = jArray[0]["name"].ToString();                
            }
            catch
            {
                throw new Exception("Bad JSON");
            }
        }
        public StudentLookupResult(JObject jObject)
        {
            try
            {
                UniId = int.Parse(jObject["sis_user_id"].ToString());
                LoginId = jObject["login_id"].ToString();
                Email = jObject["email"].ToString();
                Name = jObject["name"].ToString();
            }
            catch
            {
                throw new Exception("Bad JSON");
            }
        }
    }
}
