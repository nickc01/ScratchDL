using Avalonia.Controls;

namespace ScratchDL.GUI.Options
{
    public class DownloadAllSharedProjectsFromUserView : ProgramOptionView<DownloadAllSharedProjectsFromUser>
    {
        public DownloadAllSharedProjectsFromUserView(DownloadAllSharedProjectsFromUser modeObject) : base(modeObject) { }

        public override void Setup(StackPanel controlsPanel)
        {
            CreateAndAddTextBox(nameof(ModeObject.Username),controlsPanel);
            CreateAndAddCheckbox(nameof(ModeObject.DownloadComments),controlsPanel);
        }
    }
}
