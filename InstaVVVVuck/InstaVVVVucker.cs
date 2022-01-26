using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace InstaVVVVuck
{
    class InstaVVVVucker
    {
        const string INSTAGRAM_EXPLORE_TAG = @"https://www.instagram.com/explore/tags/%hashtag%/?__a=1%nextpage%";
        const string NEXT_PAGE = @"&max_id=%endcursor%";
        const string INSTAGRAM_MEDIAPAGE = @"https://www.instagram.com/p/%shortcode%/?__a=1";

        public int TotalMediaCount = 0;
        public int MediaProcessed = 0;

        const string VIDEODIR = "videos";
        const string IMAGEDIR = "images";
        const string JSONPath = "db.js";

        private string VideoDirectory;
        private string ImageDirectory;
        private string DBDirectory;
        private string DBPath;

        private Random rand = new Random();

        private int SleepPhotoMin = 300;
        private int SleepPhotoMax = 1200;

        private int SleepVideoMin = 1500;
        private int SleepVideoMax = 5000;

        public List<ContentInfo> ContentList
        {
            get; private set;
        }
        public delegate void AddToContentListHandler(ContentInfo info);
        public event AddToContentListHandler AddToContentListEvent;

        public InstaVVVVucker(string DBDirectory, bool LoadIfExist)
        {
            this.DBDirectory = DBDirectory;
            this.DBPath = Path.Combine(DBDirectory, JSONPath);
            this.VideoDirectory = Path.Combine(DBDirectory, VIDEODIR);
            this.ImageDirectory = Path.Combine(DBDirectory, IMAGEDIR);

            if (LoadIfExist && File.Exists(DBPath))
            {
                try
                {
                    string JSONString = File.ReadAllText(DBPath);
                    ContentList = JsonConvert.DeserializeObject<List<ContentInfo>>(JSONString);
                }
                catch (Exception e)
                {

                }
            }
            else
            {
                ContentList = new List<ContentInfo>();
            }
        }

        internal void VVVVuck(IEnumerable<string> Hashtags, bool DownloadImage, bool DownloadVideo)
        {
            MediaProcessed = 0;
            foreach (string hashtag in Hashtags)
            {
                string videoDir = Path.Combine(this.VideoDirectory, hashtag);
                if (!Directory.Exists(videoDir))
                    Directory.CreateDirectory(videoDir);

                string imageDir = Path.Combine(this.ImageDirectory, hashtag);
                if (!Directory.Exists(imageDir))
                    Directory.CreateDirectory(imageDir);

                string IGExploreTagURL = INSTAGRAM_EXPLORE_TAG.Replace("%hashtag%", hashtag);
                string IGExploreTagPageURL = IGExploreTagURL.Replace("%nextpage%", "");

                bool hasNextPage = true;
                while (hasNextPage)
                {
                    using (var webClient = new WebClient())
                    {
                        string response = webClient.DownloadString(IGExploreTagPageURL);
                        JObject instaPage = JObject.Parse(response);

                        TotalMediaCount = (int)instaPage.SelectToken("graphql.hashtag.edge_hashtag_to_media.count");
                        hasNextPage = (bool)instaPage.SelectToken("graphql.hashtag.edge_hashtag_to_media.page_info.has_next_page");

                        var Edges = instaPage.SelectToken("graphql.hashtag.edge_hashtag_to_media.edges");

                        foreach (var _node in Edges)
                        {
                            var Node = _node.SelectToken("node");
                            string ShortCode = (string)Node["shortcode"];
                            if (!MediaListContains(ShortCode))
                            {
                                OpenMediaPage(ShortCode, hashtag, DownloadImage, DownloadVideo);
                            }
                        }

                        if (hasNextPage)
                        {
                            string endcursor = (string)instaPage.SelectToken("graphql.hashtag.edge_hashtag_to_media.page_info.end_cursor");
                            string nextpage = NEXT_PAGE.Replace("%endcursor%", endcursor);
                            IGExploreTagPageURL = IGExploreTagURL.Replace("%nextpage%", nextpage);
                        }
                    }
                    Save();
                }
            }
        }

        private bool MediaListContains(string shortCode)
        {
            for (int i = 0; i < ContentList.Count; i++)
            {
                if (ContentList[i].Shortcode == shortCode)
                    return true;
            }
            return false;
        }

        private void OpenMediaPage(string shortCode, string hashtag, bool downloadImages, bool downloadVideos)
        {
            string MediaPageURL = INSTAGRAM_MEDIAPAGE.Replace("%shortcode%", shortCode);
            using (var webClient = new WebClient())
            {
                string response = webClient.DownloadString(MediaPageURL);
                JObject mediaPage = JObject.Parse(response);

                string Type = (string)mediaPage.SelectToken("graphql.shortcode_media.__typename");
                string ImageURL = (string)mediaPage.SelectToken("graphql.shortcode_media.display_url"); 

                switch (Type)
                {
                    case "GraphImage":
                        {
                            var path = "";
                            if (downloadImages)
                            {
                                path = DownloadImage(ImageURL, shortCode, hashtag);
                            }
                            var info = GetContentInfo(hashtag, mediaPage, path);
                            AddToContentList(info);
                            break;
                        }
                    case "GraphVideo":
                        {
                            var imagepath = "";
                            if (downloadImages)
                                imagepath = DownloadImage(ImageURL, shortCode, hashtag);
                            string VideoURL = (string)mediaPage.SelectToken("graphql.shortcode_media.video_url");
                            var videopath = "";
                            if (downloadVideos)
                                videopath = DownloadVideo(VideoURL, shortCode, hashtag);
                            var info = GetContentInfo(hashtag, mediaPage, imagepath, videopath);
                            AddToContentList(info);
                            break;
                        }
                    case "GraphSidecar":
                        {
                            var Edges = mediaPage.SelectToken("graphql.shortcode_media.edge_sidecar_to_children.edges");
                            foreach (var _node in Edges)
                            {
                                var Node = _node["node"];
                                string TypeOfSidecar = (string)Node["__typename"];
                                string SidecarShortcode = (string)Node["shortcode"];
                                string SidecarImageURL = (string)Node["display_url"];
                                switch (TypeOfSidecar)
                                {
                                    case "GraphImage":
                                        {
                                            string imagepath = "";
                                            if (downloadImages)
                                                imagepath = DownloadImage(SidecarImageURL, SidecarShortcode, hashtag);
                                            var info = GetContentInfo(hashtag, mediaPage, Node, imagepath);
                                            AddToContentList(info);
                                            break;
                                        }
                                    case "GraphVideo":
                                        {
                                            var imagepath = "";
                                            if (downloadImages)
                                                imagepath = DownloadImage(SidecarImageURL, SidecarShortcode, hashtag);
                                            string SidecarVideoURL = (string)Node["video_url"];
                                            var videopath = "";
                                            if (downloadVideos)
                                                videopath = DownloadVideo(SidecarVideoURL, SidecarShortcode, hashtag);
                                            var info = GetContentInfo(hashtag, mediaPage, Node, imagepath, videopath);
                                            AddToContentList(info);
                                            break;
                                        }
                                }
                            }
                            break;
                        }
                }
            }
            MediaProcessed++;
        }

        private void AddToContentList(ContentInfo info)
        {
            ContentList.Add(info);
            AddToContentListEvent?.Invoke(info);
        }

        private ContentInfo GetContentInfo(string hashtag, JObject mediaPage, string imagepath, string videopath="")
        {
            IGMediaTypes MT = IGMediaTypes.GraphImage;
            ContentTypes CT = ContentTypes.Image;

            switch ((string)mediaPage.SelectToken("graphql.shortcode_media.__typename"))
            {
                case "GraphImage":
                    MT = IGMediaTypes.GraphImage;
                    CT = ContentTypes.Image;
                    break;
                case "GraphVideo":
                    MT = IGMediaTypes.GraphVideo;
                    CT = ContentTypes.Video;
                    break;
            }

            string videourl = "";
            try
            {
                videourl = (string)mediaPage.SelectToken("graphql.shortcode_media.video_url");
            }
            catch (Exception e) { }

            return new ContentInfo()
            {
                Hashtag = hashtag,
                Username = (string)mediaPage.SelectToken("graphql.shortcode_media.owner.username"),
                UserFullName = (string)mediaPage.SelectToken("graphql.shortcode_media.owner.full_name"),
                Shortcode = (string)mediaPage.SelectToken("graphql.shortcode_media.shortcode"),
                MediaType = MT,
                ContentType = CT,
                Timestamp = (int)mediaPage.SelectToken("graphql.shortcode_media.taken_at_timestamp"),
                ImageURL = (string)mediaPage.SelectToken("graphql.shortcode_media.display_url"),
                ImagePath = imagepath,
                VideoURL = videourl,
                VideoPath = videopath
            };
        }

        private ContentInfo GetContentInfo(string hashtag, JObject MediaPage, JToken Node, string imagepath, string videopath = "")
        {
            ContentTypes CT = ContentTypes.Image;
            switch ((string)Node["__typename"])
            {
                case "GraphImage":
                    CT = ContentTypes.Image;
                    break;
                case "GraphVideo":
                    CT = ContentTypes.Video;
                    break;
            }

            string videourl = "";
            try
            {
                videourl = (string)Node["video_url"];
            }
            catch (Exception e) { }

            return new ContentInfo()
            {
                Hashtag = hashtag,
                Username = (string)MediaPage.SelectToken("graphql.shortcode_media.owner.username"),
                UserFullName = (string)MediaPage.SelectToken("graphql.shortcode_media.owner.full_name"),
                Shortcode = (string)MediaPage.SelectToken("graphql.shortcode_media.shortcode"),
                MediaType = IGMediaTypes.GraphSidecar,
                ContentType = CT,
                Timestamp = (int)MediaPage.SelectToken("graphql.shortcode_media.taken_at_timestamp"),
                ImageURL = (string)Node["display_url"],
                VideoURL = videourl,
                ImagePath = imagepath,
                VideoPath = videopath
            };
        }

        private string DownloadVideo(string videoURL, string shortCode, string hashtag)
        {
            string filePath = Path.Combine(VideoDirectory, hashtag, shortCode + ".mp4");
            using (var webClient = new WebClient())
            {
                webClient.DownloadFile(videoURL, filePath);
            }
            Thread.Sleep(rand.Next(SleepVideoMin, SleepVideoMax));
            return Extensions.GetRelativePath(DBDirectory, filePath);
        }

        private string DownloadImage(string imageURL, string shortCode, string hashtag)
        {
            string filePath = Path.Combine(ImageDirectory, hashtag, shortCode+".jpg");
            using(var webClient = new WebClient())
            {
                webClient.DownloadFile(imageURL, filePath);
            }
            Thread.Sleep(rand.Next(SleepPhotoMin, SleepPhotoMax));
            return Extensions.GetRelativePath(DBDirectory, filePath);
        }

        public void Save()
        {
            string JSONstring = JsonConvert.SerializeObject(ContentList);
            File.WriteAllText(DBPath, JSONstring);
        }


    }
}
