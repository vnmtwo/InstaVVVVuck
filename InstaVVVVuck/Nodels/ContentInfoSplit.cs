#region usings
using InstaVVVVuck;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
#endregion usings

namespace VVVV.Nodes.InstaVVVVuck
{
    #region PluginInfo
    [PluginInfo(Name = "Split",
                Category = "Instagram",
                Help = "",
                Tags = "ContentInfo",
                Author = "vnm")]
    #endregion PluginInfo

    public class ContentInfoSplitNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Model")]
        public IDiffSpread<ContentInfo> FInModel;

        [Output("Hashtag")]
        public ISpread<string> FOutHashtag;

        [Output("Username")]
        public ISpread<string> FOutUsername;

        [Output("UserFullname")]
        public ISpread<string> FOutUserFullname;

        [Output("Shortcode", Visibility = PinVisibility.OnlyInspector)]
        public ISpread<string> FOutShortcode;

        [Output("MediaType", Visibility = PinVisibility.OnlyInspector)]
        public ISpread<string> FOutMediaType;

        [Output("ContentType")]
        public ISpread<string> FOutContentType;

        [Output("Timestamp")]
        public ISpread<int> FOutTimestamp;

        [Output("ImageURL", Visibility = PinVisibility.OnlyInspector)]
        public ISpread<string> FOutImageURL;

        [Output("VideoURL", Visibility = PinVisibility.OnlyInspector)]
        public ISpread<string> FOutVideoURL;

        [Output("ImagePath")]
        public ISpread<string> FOutImagePath;

        [Output("VideoPath")]
        public ISpread<string> FOutVideoPath;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        { 
            if (FInModel.IsChanged)
            {
                ResetOutputs();
                foreach (var ci in FInModel)
                {
                    if (ci != null)
                    {
                        FOutHashtag.Add(ci.Hashtag);
                        FOutUsername.Add(ci.Username);
                        FOutUserFullname.Add(ci.UserFullName);
                        FOutShortcode.Add(ci.Shortcode);
                        FOutMediaType.Add(ci.MediaType.ToString());
                        FOutContentType.Add(ci.ContentType.ToString());
                        FOutTimestamp.Add(ci.Timestamp);
                        FOutImageURL.Add(ci.ImageURL);
                        FOutVideoURL.Add(ci.VideoURL);
                        FOutImagePath.Add(ci.ImagePath);
                        FOutVideoPath.Add(ci.VideoPath);
                    }
                }
            }
        }

        private void ResetOutputs()
        {
            FOutHashtag.SliceCount = 0;
            FOutUsername.SliceCount = 0;
            FOutUserFullname.SliceCount = 0;
            FOutShortcode.SliceCount = 0;
            FOutMediaType.SliceCount = 0;
            FOutContentType.SliceCount = 0;
            FOutTimestamp.SliceCount = 0;
            FOutImageURL.SliceCount = 0;
            FOutVideoURL.SliceCount = 0;
            FOutImagePath.SliceCount = 0;
            FOutVideoPath.SliceCount = 0;
        }
    }
}
