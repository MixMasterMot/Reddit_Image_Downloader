using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using RedditSharp;
using RedditSharp.Things;

namespace ReditImageDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            Program prog = new Program();
            Console.WriteLine("Started Reddit Downloader");
            string saveLocation = @"C:\Users\Administrator\Pictures\RedditWall\";
            prog.RemoveOldImage(saveLocation);
            List<string> subReddits = prog.GetSubReddit();

            Reddit reddit = new Reddit();
            
            foreach(string sub in subReddits)
            {
                Console.WriteLine("Downloading from " + sub);
                int count = 15;
                var subreddit = reddit.GetSubreddit(sub);

                foreach (var post in subreddit.GetTop(FromTime.All).Take(count))
                {
                    if (post.IsStickied || post.IsSelfPost || Convert.ToString(post.Url).Contains("reddituploads")) continue;
                    string postURL = Convert.ToString(post.Url);
                    if (!prog.ValidImgUrl(postURL))
                    {
                        continue;
                    }
                    prog.DownloadImages(postURL, saveLocation);
                }
            }
            Console.WriteLine("Reddit Downloader Completed");
            Console.ReadKey();
        }

        private List<string> GetSubReddit()
        {
            Dictionary<string, string> cmds;
            string json = File.ReadAllText("SubReddits/SubReddit.json");
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            cmds = data.ToObject<Dictionary<string, string>>();

            List<string> subs = new List<string>();
            foreach (KeyValuePair<string, string> pair in cmds)
            {
                subs.Add(pair.Value);
            }

            List<int> rndms = new List<int>();
            Random rnd = new Random();
            for(int i = 0; i < 3; i++)
            {
                int tmpRnd = rnd.Next(0, subs.Count - 1);
                if (!rndms.Contains(tmpRnd))
                {
                    rndms.Add(tmpRnd);
                }
            }

            List<string> ret = new List<string>();
            foreach(int i in rndms)
            {
                ret.Add(subs[i]);
            }
            return ret;
        }

        public void DownloadImages(string imageURL, string userDir)
        {
            Console.WriteLine("Downloading {0}", imageURL);
            string[] splitURL = imageURL.Split('/');
            int index = splitURL.Length - 1;
            string fileName = splitURL[index];
            
            try
            {
                using(WebClient client = new WebClient())
                {
                    client.DownloadFile(imageURL, Path.Combine(userDir, fileName));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[INFO] ERROR DOWNLOADING FILE: {ex}");
            }
        }
        public bool ValidImgUrl(string imgUrl)
        {
            switch (imgUrl)
            {
                case string url when url.Contains("gfycat.com"):
                    return false;

                case string url when url.Contains(".gifv"):
                    return false;

                default: return true;
            }
        }
        public void RemoveOldImage(string saveLocation)
        {
            List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
            var files = Directory.GetFiles(saveLocation);
            foreach(var f in files)
            {
                if (!ImageExtensions.Contains(Path.GetExtension(f).ToUpperInvariant()))
                {
                    try
                    {
                        File.Delete(f);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Delete of file " + f + " failed");
                        Console.WriteLine(ex);
                    }
                }
                else
                {
                    DateTime now = DateTime.Now;
                    DateTime creation = File.GetCreationTime(f);

                    if ((now - creation).TotalDays > 21)
                    {
                        try
                        {
                            File.Delete(f);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Delete of file " + f + " failed");
                            Console.WriteLine(ex);
                        }
                    }
                }
            }
            
        }

    }
}
