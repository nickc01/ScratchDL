using Avalonia.Controls;

namespace ScratchDL.GUI.Options
{
    public class DownloadAllFollowingUsersFromUserView : ProgramOptionView<DownloadAllFollowingUsersFromUser>
    {
        public DownloadAllFollowingUsersFromUserView(DownloadAllFollowingUsersFromUser modeObject) : base(modeObject) { }

        public override string Column3 => string.Empty;

        public override void Setup(StackPanel controlsPanel)
        {
            CreateAndAddTextBox(nameof(ModeObject.Username), controlsPanel);
        }
    }
}
