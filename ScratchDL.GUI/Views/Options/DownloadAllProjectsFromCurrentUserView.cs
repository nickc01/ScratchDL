using Avalonia.Controls;

namespace ScratchDL.GUI.Options
{
    public class DownloadAllProjectsFromCurrentUserView : ProgramOptionView<DownloadAllProjectsFromCurrentUser>
    {
        public DownloadAllProjectsFromCurrentUserView(DownloadAllProjectsFromCurrentUser modeObject) : base(modeObject) { }

        public override void Setup(StackPanel controlsPanel)
        {
            CreateAndAddCheckbox(nameof(ModeObject.DownloadComments), controlsPanel);
        }
    }
}
