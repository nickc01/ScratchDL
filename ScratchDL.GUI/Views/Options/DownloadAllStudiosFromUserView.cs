using Avalonia.Controls;

namespace ScratchDL.GUI.Options
{
    public class DownloadAllStudiosFromUserView : ProgramOptionView<DownloadAllStudiosFromUser>
    {
        public DownloadAllStudiosFromUserView(DownloadAllStudiosFromUser modeObject) : base(modeObject) { }

        public override void Setup(StackPanel controlsPanel)
        {
            CreateAndAddTextBox(nameof(ModeObject.Username), controlsPanel);
            CreateAndAddCheckbox(nameof(ModeObject.DownloadComments), controlsPanel);
        }
    }
}
