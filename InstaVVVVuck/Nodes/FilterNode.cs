#region usings
using InstaVVVVuck;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
#endregion usings

namespace VVVV.Nodes.InstaVVVVuck
{
    #region PluginInfo
    [PluginInfo(Name = "Filter",
                Category = "Instagram",
                Help = "",
                Tags = "ContentInfo",
                Author = "vnm")]
    #endregion PluginInfo

    public class FilterNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        [Input("Model")]
        public IDiffSpread<ContentInfo> FInModel;

        [Input("Username")]
        public IDiffSpread<string> FInUserName;

        [Input("Filter by Username", IsSingle = true)]
        public ISpread<bool> FInFilterByUsername;

        [Input("Hashtag")]
        public IDiffSpread<string> FInHashtag;

        [Input("Filter by Hashtag", IsSingle = true)]
        public ISpread<bool> FInFilterByHashtag;

        [Input("Video", IsSingle = true)]
        public ISpread<bool> FInVideo;

        [Input("Images", IsSingle = true)]
        public ISpread<bool> FInImages;

        [Input("Sidecars", IsSingle = true)]
        public ISpread<bool> FInSidecars;

        [Output("FOutMedia")]
        public ISpread<ContentInfo> FOutMedia;

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        { 
            if (FInModel.IsChanged || FInUserName.IsChanged || FInFilterByUsername.IsChanged ||
                FInHashtag.IsChanged || FInFilterByHashtag.IsChanged ||
                FInVideo.IsChanged || FInImages.IsChanged || FInSidecars.IsChanged)
            {
                FOutMedia.SliceCount = 0;

                for (int i=0; i<FInModel.SliceCount; i++)
                {
                    var ci = FInModel[i];
                    if (ci != null)
                    {
                        bool username = true;
                        if (FInFilterByUsername[0])
                        {
                            foreach (string un in FInUserName)
                            {
                                if (!ci.Username.ToLower().Contains(un.ToLower()))
                                {
                                    username = false;
                                }
                            }
                        }

                        bool hashtag = true;
                        if (FInFilterByHashtag[0])
                        {
                            foreach (string ht in FInHashtag)
                            {
                                if (!ci.Hashtag.ToLower().Contains(ht.ToLower()))
                                {
                                    hashtag = false;
                                }
                            }
                        }

                        bool image = (ci.ContentType == ContentTypes.Image) == FInImages[0];
                        bool video = (ci.ContentType == ContentTypes.Video) == FInVideo[0];
                        bool sidecar = (ci.MediaType == IGMediaTypes.GraphSidecar) == FInSidecars[0];

                        bool filter = username && hashtag && image && video && sidecar;

                        if (filter)
                            FOutMedia.Add(ci);
                    }
                }
            }
            //FLogger.Log(LogType.Debug, "hi tty!");
        }

        public void OnImportsSatisfied()
        {
            FOutMedia.SliceCount = 0;
        }
    }
}
