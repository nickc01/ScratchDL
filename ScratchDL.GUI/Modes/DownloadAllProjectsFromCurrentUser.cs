using Avalonia.Controls;
using Avalonia.Data;
using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScratchDL.GUI.Modes
{
    public class DownloadAllProjectsFromCurrentUser : DownloadMode
    {
        public bool DownloadComments = true;

        public DownloadAllProjectsFromCurrentUser(MainWindowViewModel viewModel) : base(viewModel)
        {

        }

        public override string Name => "Download All Projects From Current User";

        public override Task Download(Action<ProjectEntry> addEntry)
        {
            return Task.CompletedTask;
        }

        public override Task Export(IEnumerable<long> selectedIDs)
        {
            return Task.CompletedTask;
        }
    }

    public class DownloadAllProjectsFromCurrentUserUI : DownloadModeUI<DownloadAllProjectsFromCurrentUser>
    {
        public DownloadAllProjectsFromCurrentUserUI(DownloadAllProjectsFromCurrentUser modeObject) : base(modeObject) { }

        public override void Setup(StackPanel controlsPanel)
        {

            var commentsCheckbox = CreateCheckbox("download_comments", "Download Comments", ModeObject.DownloadComments, b => ModeObject.DownloadComments = b);
            /*var testButton1 = new Button();
            testButton1.Content = "This is a test 1";

            var test2 = new TextBlock();
            test2.Text = "This is a text block 123";*/



            /*var testInput = new TextBox();
            testInput.Text = ModeObject.TestInput.ToString();
            testInput.KeyUp += GetNumberUpdateDelegate(testInput, n => ModeObject.TestInput = n);*/

            controlsPanel.Children.Add(commentsCheckbox);

            //controlsPanel.Children.Add(testButton1);
            //controlsPanel.Children.Add(test2);
            //controlsPanel.Children.Add(testInput);
        }

        /*private void TestInput_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            var textbox = (TextBox)e.Source!;
            ModeObject.TestInput = textbox.Text;
        }*/
    }
}
