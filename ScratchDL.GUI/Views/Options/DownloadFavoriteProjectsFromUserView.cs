using Avalonia.Controls;

namespace ScratchDL.GUI.Options
{
    public class DownloadFavoriteProjectsFromUserView : ProgramOptionView<DownloadFavoriteProjectsFromUser>
    {
        public DownloadFavoriteProjectsFromUserView(DownloadFavoriteProjectsFromUser modeObject) : base(modeObject) { }

        public override void Setup(StackPanel controlsPanel)
        {
            CreateAndAddTextBox(nameof(ModeObject.Username), controlsPanel);
            CreateAndAddCheckbox(nameof(ModeObject.DownloadComments), controlsPanel);
        }
    }
}
