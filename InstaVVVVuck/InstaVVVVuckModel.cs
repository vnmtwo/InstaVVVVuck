using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstaVVVVuck
{
    public enum IGMediaTypes
    {
        GraphImage,
        GraphVideo,
        GraphSidecar
    }

    public enum ContentTypes
    {
        Image,
        Video
    }

    public class ContentInfo
    {
        public string Hashtag;
        public string Username;
        public string UserFullName;
        public string Shortcode;
        public IGMediaTypes MediaType;
        public ContentTypes ContentType;
        public int Timestamp;
        public string ImageURL;
        public string VideoURL;
        public string ImagePath;
        public string VideoPath;
    }
}
