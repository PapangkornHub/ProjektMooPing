namespace ProjektMooPing.Models
{
    public class StoryCutScene
    {
        public int    Id          { get; set; }
        public string Title       { get; set; }
        public string TitleTh     { get; set; }
        public string Text        { get; set; }
        public string TextTh      { get; set; }
        public string StoryImagePath { get; set; }

        private static Services.LocalizationService Loc => Services.LocalizationService.Instance;

        public string DisplayTitle =>
            Loc.IsThai && !string.IsNullOrEmpty(TitleTh) ? TitleTh : Title;
        public string DisplayText =>
            Loc.IsThai && !string.IsNullOrEmpty(TextTh) ? TextTh : Text;
        public string DisplayImagePath =>
            string.IsNullOrEmpty(StoryImagePath) ? null
            : StoryImagePath.EndsWith(".png") ? StoryImagePath : StoryImagePath + ".png";
    }
}
