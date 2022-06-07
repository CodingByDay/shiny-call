using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ShinyCall.Services
{
    public static class APIAccess
    {
        public static string url = ConfigurationManager.AppSettings["APIaddress"];
        public static string user = ConfigurationManager.AppSettings["SIPPhoneNumber"];
        public static string call = ConfigurationManager.AppSettings["CallData"];


        public static async Task<string> GetPageAsync(string caller)
        {
            //using (var client = new HttpClient())
            //{
            //    client.BaseAddress = new Uri(url);
            //    client.DefaultRequestHeaders.Accept.Clear();
            //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //    // GET Method
            //    HttpResponseMessage response = await client.GetAsync(@$"roltekapi/popup?user={user}&call={caller}");
            //    if (response.IsSuccessStatusCode)
            //    {
            //        string link = await response.Content.ReadAsStringAsync();
            //        //return link;
            //        return "https://www.roltek.si/";
            //    }
            //    else
            //    {
            //        return "Internal server Error";
            //    }
            //}


            return "https://www.roltek.si/";

        }
    }
}
