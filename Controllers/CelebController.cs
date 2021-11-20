using CelebAppServer.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace CelebAppServer.Controllers
{
    [RoutePrefix("api/Celeb")]
    public class CelebController : ApiController
    {
        string DB_PATH = System.Web.HttpContext.Current.Server.MapPath(@"~/App_Data/ImdbJsonDB.json");
        string ORIGINAL_DB_PATH = System.Web.HttpContext.Current.Server.MapPath(@"~/App_Data/ImdbJsonDB_ORIGINAL.json");

        /// <summary>
        /// stores everything inside server json file
        /// </summary>
        /// <returns></returns>
        [Route("RestoreAll")]
        [HttpGet]
        public async Task<Dictionary<int, CelebrityItem>> RestoreDataFromWeb()
        {
            const string BASE_URL = "https://www.imdb.com";
            string ALL_CELEB_URL = $"{BASE_URL}/list/ls052283250/"; // as given in the assignment
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(ALL_CELEB_URL);

            var htmlDocument = new HtmlAgilityPack.HtmlDocument();
            htmlDocument.LoadHtml(html);

            // we know (after investigation) that the list which holds the celebrites is a div with a class name of "lister-list"
            // we know it's the only 1 with that name
            var listHolder = htmlDocument.DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("lister-list")).ToList()[0];

            // each celebrity is held under div with class name of "lister-item mode-detail"
            var celebrityDivList = listHolder.Descendants("div")
               .Where(node => node.GetAttributeValue("class", "")
               .Equals("lister-item mode-detail")).ToList();

            var celebLinks = GetCelebritiesLinks(BASE_URL, celebrityDivList);

            Dictionary<int, CelebrityItem>  _celebrities = new Dictionary<int, CelebrityItem>();
            for (int i = 0; i < celebLinks.Count; i++)
            {
                _celebrities.Add(i + 1, await GetCelebrity(httpClient, i + 1, celebLinks[i]));
            }
            SaveCelebrites(_celebrities);
            return _celebrities;
        }

        /// <summary>
        /// saves celebrities in DB
        /// </summary>
        private void SaveCelebrites(Dictionary<int, CelebrityItem> _celebrities)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(_celebrities);
            File.WriteAllText(DB_PATH, json);
        }

        /// <summary>
        /// loads celebrites from json file and returns it
        /// </summary>
        /// <returns></returns>
        [Route("LoadFromLocal")]
        [HttpGet]
        public Dictionary<int, CelebrityItem> LoadCelebsFromJSON()
        {
            return LoadDB();
        }

        /// <summary>
        /// loads db into _celebrities
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, CelebrityItem> LoadDB()
        {
            using (StreamReader r = new StreamReader(DB_PATH))
            {
                string json = r.ReadToEnd();
                var result = JsonConvert.DeserializeObject<Dictionary<int, CelebrityItem>>(json);
                return result;
            }
        }

        /// <summary>
        /// loads celebrites from json file and returns it
        /// </summary>
        /// <returns></returns>
        [Route("LoadOriginal")]
        [HttpGet]
        public Dictionary<int, CelebrityItem> LoadCelebsFromOriginalJSON()
        {
            using (StreamReader r = new StreamReader(ORIGINAL_DB_PATH))
            {
                string json = r.ReadToEnd();
                var result = JsonConvert.DeserializeObject<Dictionary<int, CelebrityItem>>(json);
                SaveCelebrites(result);
                return result;
            }
        }

        /// <summary>
        /// returns celebrity by fetching his information from link
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="id"></param>
        /// <param name="link"></param>
        /// <returns></returns>
        private static async Task<CelebrityItem> GetCelebrity(HttpClient httpClient, int id, string link)
        {
            var httpClient2 = new HttpClient();
            var html2 = await httpClient.GetStringAsync(link);
            var htmlDocument2 = new HtmlAgilityPack.HtmlDocument();
            htmlDocument2.LoadHtml(html2);
            string name = GetCelebName(htmlDocument2);
            string role = GetCelebRole(htmlDocument2);
            string imageSource = GetCelebImageSrc(htmlDocument2);
            DateTime birthDate = GetCelebBirthDate(htmlDocument2);
            Gender gender = GetCelebGender(role, name);
            return new CelebrityItem(name, birthDate, gender, role, imageSource, id);
        }

        /// <summary>
        /// decides gender by name and role (wasn't able to find specified gender online)
        /// </summary>
        /// <param name="role"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Gender GetCelebGender(string role, string name)
        {
            if (role.ToLower().Contains("actress") || name.ToLower().Contains("drew barrymore"))
            {
                return Gender.Female;
            }
            else
            {
                return Gender.Male;
            }
        }
        
        /// <summary>
        /// scraping birth of date
        /// </summary>
        /// <param name="htmlDocument"></param>
        /// <returns></returns>
        private static DateTime GetCelebBirthDate(HtmlAgilityPack.HtmlDocument htmlDocument)
        {
            var wrapper = htmlDocument.DocumentNode.Descendants("div")
            .Where(node => node.GetAttributeValue("id", "")
            .Equals("name-born-info")).ToList()[0];

            string timeHtml = wrapper.Descendants("time").ToList()[0].OuterHtml;
            int start = timeHtml.IndexOf("\"") + 1;
            int length = timeHtml.IndexOf("\"", start) - start;
            DateTime retval = Convert.ToDateTime(timeHtml.Substring(start, length));
            return retval;
        }
        
        /// <summary>
        /// scraping image source
        /// </summary>
        /// <param name="htmlDocument"></param>
        /// <returns></returns>
        private static string GetCelebImageSrc(HtmlAgilityPack.HtmlDocument htmlDocument)
        {
            var imgHtml = htmlDocument.DocumentNode.Descendants("img")
            .Where(node => node.GetAttributeValue("id", "")
            .Equals("name-poster")).ToList()[0].OuterHtml;

            int start = imgHtml.IndexOf("src=\"") + 5;
            int length = imgHtml.IndexOf("\">", start) - start;
            var retval = imgHtml.Substring(start, length);
            return retval;
        }
        
         /// <summary>
         /// scraping Role of celeb
         /// </summary>
         /// <param name="htmlDocument"></param>
         /// <returns></returns>
        private static string GetCelebRole(HtmlAgilityPack.HtmlDocument htmlDocument)
        {
            var wrapper = htmlDocument.DocumentNode.Descendants("div")
            .Where(node => node.GetAttributeValue("class", "")
            .Equals("infobar")).ToList()[0];

            return wrapper.Descendants("span")
            .Where(node => node.GetAttributeValue("class", "")
            .Equals("itemprop")).ToList()[0].InnerHtml.Trim();
        }


        /// <summary>
        /// scraping celeb name
        /// </summary>
        /// <param name="htmlDocument"></param>
        /// <returns></returns>
        private static string GetCelebName(HtmlAgilityPack.HtmlDocument htmlDocument)
        {
            var wrapper = htmlDocument.DocumentNode.Descendants("h1")
            .Where(node => node.GetAttributeValue("class", "")
            .Equals("header")).ToList()[0];

            return wrapper.Descendants("span")
            .Where(node => node.GetAttributeValue("class", "")
            .Equals("itemprop")).ToList()[0].InnerHtml.Trim();
        }

        /// <summary>
        /// for every celeb, fetching out his link
        /// </summary>
        /// <param name="BASE_URL"></param>
        /// <param name="celebrityDivList"></param>
        /// <returns></returns>
        private static List<string> GetCelebritiesLinks(string BASE_URL, List<HtmlAgilityPack.HtmlNode> celebrityDivList)
        {

            // for each div i want to extract the url for each celebrity
            List<string> celebLinks = new List<string>();
            foreach (var div in celebrityDivList)
            {
                string aHref = div.Descendants("a")
                   .Where(node => node.GetAttributeValue("href", "")
                   .Contains("/name/nm")).ToList()[0].OuterHtml;

                int preStart = aHref.IndexOf("\"/") + 2;
                int preLength = aHref.IndexOf("\">", preStart) - preStart;

                string celebPrefix = aHref.Substring(preStart, preLength);

                string celebLink = $"{BASE_URL}/{celebPrefix}";
                celebLinks.Add(celebLink);
            }

            return celebLinks;
        }

        [Route("Delete")]
        [HttpPost]
        public int DeleteCeleb(DeleteCelebrityBindingModels model)
        {
            int retval = 0;
            Dictionary<int, CelebrityItem> celebrities = LoadDB();
            if (celebrities.ContainsKey(model.CelebrityId))
            {
                celebrities.Remove(model.CelebrityId);
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(celebrities);
                File.WriteAllText(DB_PATH, json);
                retval = model.CelebrityId;
            }
            return retval;
        }


    }
}
