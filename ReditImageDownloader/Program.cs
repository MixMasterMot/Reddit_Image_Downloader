using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            string saveLocation = prog.GetSaveDir();
            if (saveLocation == null)
            {
                return;
            }

            prog.RemoveOldImage(saveLocation);
            List<string> subReddits = prog.GetSubReddit();

            Reddit reddit = new Reddit();
            
            foreach(string sub in subReddits)
            {
                Console.WriteLine("Downloading from " + sub);
                int count = 15;
                var subreddit = reddit.GetSubreddit(sub);

                foreach (var post in subreddit.Hot.Take(count))
                {
                    if (post.IsStickied || post.IsSelfPost || Convert.ToString(post.Url).Contains("reddituploads")) continue;
                    string postURL = Convert.ToString(post.Url);
                    string postName = Convert.ToString(post.Title);
                    postName = prog.MakeNameSafe(postName);
                    if (!prog.ValidImgUrl(postURL))
                    {
                        continue;
                    }
                    string img = prog.DownloadImages(postURL, saveLocation, postName);
                    if (img != null)
                    {
                        prog.AddToDownloadList(img);
                    }
                }
            }
            Console.WriteLine("Reddit Downloader Completed");
        }

        private string MakeNameSafe(string postName)
        {
            List<string> illegal = new List<string>();
            illegal.Add("/");
            illegal.Add(@"\");
            illegal.Add(":");
            illegal.Add(";");

            foreach(string str in illegal)
            {
                if (postName.Contains(str))
                {
                    postName = postName.Replace(str, " ");
                }
            }

            string[] splitName = postName.Split(' ');
            if (splitName.Length > 6)
            {
                string tmp = null;
                for(int i = 0;i<6;i++)
                {
                    tmp = tmp + splitName[0] + " ";
                }
                postName = tmp;
            }
            return postName.TrimEnd();
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

            //List<int> rndms = new List<int>();
            //Random rnd = new Random();
            //for(int i = 0; i < 3; i++)
            //{
            //    int tmpRnd = rnd.Next(0, subs.Count - 1);
            //    if (!rndms.Contains(tmpRnd))
            //    {
            //        rndms.Add(tmpRnd);
            //    }
            //}

            //List<string> ret = new List<string>();
            //foreach(int i in rndms)
            //{
            //    ret.Add(subs[i]);
            //}
            return subs;
        }
        private string GetSaveDir()
        {
            string json = File.ReadAllText("SubReddits/SaveLocation.json");
            List<string> tstLocation = JsonConvert.DeserializeObject<List<string>>(json);
            if (!Directory.Exists(tstLocation[0]))
            {
                using (StreamWriter file = new StreamWriter("SubReddits/Errors.txt", true))
                {
                    string error = "File location " + tstLocation[0] + " does not exist";
                    file.WriteLine(error);
                }
                return null;
            }
            return tstLocation[0];
        }
        public string DownloadImages(string imageURL, string userDir,string postName)
        {
            Console.WriteLine("Downloading {0}", imageURL);
            string[] splitURL = imageURL.Split('.');
            int index = splitURL.Length - 1;
            string fileExt = "."+splitURL[index];


            bool download = true;
            try
            {
                using(WebClient client = new WebClient())
                {
                    client.DownloadFile(imageURL, Path.Combine(userDir, postName + fileExt));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[INFO] ERROR DOWNLOADING FILE: {ex}");
                using (StreamWriter file = new StreamWriter("SubReddits/Errors.txt", true))
                {
                    file.WriteLine("Download Error:");
                    file.WriteLine(ex);
                }
                download = false;
            }
            if (download == false)
            {
                return null;
            }
            else
            {
                return imageURL;
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
        public void AddToDownloadList(string img)
        {
            using (StreamWriter file = new StreamWriter("SubReddits/DownloadedImg.txt", true))
            {
                file.WriteLine(img);
            }
        }

    }
}
