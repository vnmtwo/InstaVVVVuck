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
    [PluginInfo(Name = "SearchByHashtag",
                Category = "Instagram",
                Help = "",
                Tags = "Constructor",
                Author = "vnm")]
    #endregion PluginInfo

    public class SearchByHashtagNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        [Input("Hashtag", IsSingle = true)]
        public ISpread<string> FInHashtag;

        [Input("Content Directory", IsSingle = true, StringType = StringType.Directory)]
        public ISpread<string> FInContentDirectory;

        [Input("Download Photo", IsSingle = true)]
        public ISpread<bool> FInDownloadPhoto;

        [Input("Download Video", IsSingle = true)]
        public ISpread<bool> FInDownloadVideo;

        [Input("VVVVuck!", IsSingle = true, IsBang =true)]
        public ISpread<bool> FinVVVVuck;

        [Output("Model")]
        public ISpread<ContentInfo> FOutDB;

        [Output("Total Media Count")]
        public ISpread<int> FOutTotalMediaCount;

        [Output("Processed Media Count")]
        public ISpread<int> FOutProcessedMediaCount;

        [Output("Busy")]
        public ISpread<bool> FOutBusy;

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins

        InstaVVVVucker InstaVVVVucker;

        public void Evaluate(int SpreadMax)
        {
            if (FinVVVVuck[0] && !FOutBusy[0])
            {
                if (InstaVVVVucker == null)
                {
                    InstaVVVVucker = new InstaVVVVucker(FInContentDirectory[0], true);
                    InstaVVVVucker.AddToContentListEvent += InstaVVVVucker_AddToContentListEvent;
                }
                FOutDB.SliceCount = 0;
                FOutDB.AssignFrom(InstaVVVVucker.ContentList);

                VVVVuckAsync();
            }

            if (InstaVVVVucker != null)
            {
                FOutProcessedMediaCount[0] = InstaVVVVucker.MediaProcessed;
                FOutTotalMediaCount[0] = InstaVVVVucker.TotalMediaCount;
            }
            //FLogger.Log(LogType.Debug, "hi tty!");
        }

        public void OnImportsSatisfied()
        {
            FOutDB.SliceCount = 0;
        }

        private void InstaVVVVucker_AddToContentListEvent(ContentInfo info)
        {
            FOutDB.Add(info);
        }

        private async void VVVVuckAsync()
        {
            FOutBusy[0] = true;
            await Task.Run(()=>InstaVVVVucker.VVVVuck(FInHashtag, FInDownloadPhoto[0], FInDownloadVideo[0]));
            FOutBusy[0] = false;
        }
    }
}
