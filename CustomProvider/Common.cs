using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CustomProvider
{
    public static class Common
    {

        private static HttpClient Client = new HttpClient();
        public static async Task<HttpResponseMessage> SendWebRequestAsync(string requesturl, HttpMethod httpMethod, HttpContent content = null)
        {
            if (httpMethod == HttpMethod.Get)
            {
                return await Client.GetAsync(requesturl);
            }

            return null;

        }
    }
}
