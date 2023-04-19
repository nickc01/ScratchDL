using Avalonia.Controls;

namespace ScratchDL.GUI.Options
{
    public class DownloadSingleProjectView : ProgramOptionView<DownloadSingleProject>
    {
        public DownloadSingleProjectView(DownloadSingleProject modeObject) : base(modeObject) { }

        public override void Setup(StackPanel controlsPanel)
        {
            CreateAndAddTextBox(nameof(ModeObject.ProjectURL), controlsPanel);
            CreateAndAddCheckbox(nameof(ModeObject.DownloadComments), controlsPanel);
        }
    }
}
