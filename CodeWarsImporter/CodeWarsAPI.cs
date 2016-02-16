using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;

namespace CodeWarsImporter
{
    internal class CodeWarsAPI
    {
        private string user;
        private string pass;
        private string token;

        private HttpClient client;

        public CodeWarsAPI(string user, string pass, string token)
        {
            this.user = user;
            this.pass = pass;
            this.token = token;

            this.client = new HttpClient();
            this.client.BaseAddress = new Uri("http://www.codewars.com");
            this.client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));
            this.client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        }

        public async Task LoginAsync()
        {
            string result = await this.client.GetStringAsync("https://www.codewars.com/users/sign_in");

            var document = new HtmlDocument();
            document.LoadHtml(result);

            string authenticityToken = document.DocumentNode.SelectSingleNode("//meta[@name='csrf-token']").Attributes["content"].Value;

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("utf8", "✓"),
                new KeyValuePair<string, string>("authenticity_token", authenticityToken),
                new KeyValuePair<string, string>("user[email]", this.user),
                new KeyValuePair<string, string>("user[password]", this.pass),
                new KeyValuePair<string, string>("user[remember_me]", "true"),
            });

            await this.client.PostAsync("/users/sign_in", content);
        }

        public void Login()
        {
            Task.WaitAll(Task.Run(LoginAsync));
        }

        public async Task<IEnumerable<string>> FetchKatasAsync(int page)
        {
            var url = $"/users/brunolm?page={page}";
            var result = await client.GetStringAsync(url);

            var document = new HtmlDocument();
            document.LoadHtml(result);

            var katas = document.DocumentNode.Descendants()
                .Where(o => o.Attributes["class"] != null
                    && o.Attributes["class"].Value == "is-loud"
                    && o.Attributes["href"].Value.Contains("/train/javascript"))
                .Select(o => o.Attributes["href"].Value.Replace("/train/", "/solutions/") + "/me");

            return katas;
        }

        public IEnumerable<string> FetchKatas(int page)
        {
            return FetchKatasAsync(page).Result;
        }

        public async Task<KataInfo> FetchSolutionAsync(string url)
        {
            var result = await client.GetStringAsync(url);

            var document = new HtmlDocument();
            document.LoadHtml(result);

            var solutionList = document.DocumentNode.SelectSingleNode("//*[@id='solutions_list']");
            if (solutionList == null)
            {
                throw new Exception("Couldn't find solutions");
            }

            var solutions = solutionList.Descendants("code")
                .Where(o => o.ParentNode.ParentNode.Attributes["id"] != null)
                .Select(o =>
                {
                    return System.Net.WebUtility.HtmlDecode(o.InnerText);
                });

            var kyu = document.DocumentNode.SelectSingleNode("//*[@id='shell_content']").Descendants("span").First().InnerText;

            return new KataInfo
            {
                Kyu = kyu,
                Solutions = solutions,
            };
        }

        public KataInfo FetchSolution(string url)
        {
            return FetchSolutionAsync(url).Result;
        }
    }
}