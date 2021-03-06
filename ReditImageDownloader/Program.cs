﻿using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using RedditSharp;
using RedditSharp.Things;
using System.Diagnostics;
using System.Threading;

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
            List<int> targetResolution = prog.GetTargetResolution();

            Reddit reddit = new Reddit();
            
            foreach(string sub in subReddits)
            {
                string text = "Downloading from " + sub;
                ConsoleColor colorBack = ConsoleColor.Black;
                ConsoleColor colorFront = ConsoleColor.Green;
                PrintConsoleColour(text, colorBack, colorFront);
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

                    var func = new Func<bool>(() =>
                    {
                        return prog.CheckImageSize(targetResolution[0], targetResolution[1], postURL);
                    });
                    bool goodSize;
                    TryExecute(func,500, out goodSize);

                    //bool goodSize = prog.CheckImageSize(targetResolution[0], targetResolution[1], postURL);

                    if (goodSize == false)
                    {
                        continue;
                    }

                    string img = prog.DownloadImages(postURL, saveLocation, postName);
                    if (img != null)
                    {
                        prog.AddToDownloadList(img, postName);
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
                    tmp = tmp + splitName[i] + " ";
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
            return subs;
        }
        private List<int> GetTargetResolution()
        {
            Dictionary<string, string> cmds;
            string json = File.ReadAllText("SubReddits/Resolution.json");
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            cmds = data.ToObject<Dictionary<string, string>>();

            List<int> values = new List<int>();
            foreach (KeyValuePair<string, string> pair in cmds)
            {
                values.Add(Convert.ToInt32(pair.Value));
            }
            return values;
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
        public void AddToDownloadList(string img, string imgName)
        {
            using (StreamWriter file = new StreamWriter("SubReddits/DownloadedImg.txt", true))
            {
                string line = Convert.ToString(DateTime.Now) + "     " + imgName + "     " + img;
                file.WriteLine(line);
            }
        }
        public static void PrintConsoleColour(string text, ConsoleColor colorBack,ConsoleColor colorFront)
        {
            Console.BackgroundColor = colorBack;
            Console.ForegroundColor = colorFront;
            Console.WriteLine(text); 
            Console.ResetColor();
        }

        public bool CheckImageSize(int targetX,int targetY,string targetUrl)
        {
            bool retTuple = true;
            try
            {
                Uri uri = new Uri(targetUrl);
                Size size = GetImageResolution.GetWebDimensions(uri);
                if (size.xSize < targetX && size.ySize < targetY)
                {
                    retTuple = false;
                }
            }
            catch
            {
                retTuple = false;
            }
            return retTuple;
        }
        public static T Execute<T>(Func<T> func, int timeout)
        {
            T result;
            TryExecute(func, timeout, out result);
            return result;
        }

        public static bool TryExecute<T>(Func<T> func, int timeout, out T result)
        {
            var t = default(T);
            var thread = new Thread(() => t = func());
            thread.Start();
            var completed = thread.Join(timeout);
            if (!completed)
            {
                thread.Abort();
            } 
            result = t;
            return completed;
        }

    }
}
