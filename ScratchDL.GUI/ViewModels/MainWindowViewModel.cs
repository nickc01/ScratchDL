using Avalonia.Controls.Primitives;
using System.Collections.Generic;

namespace ScratchDL.GUI.ViewModels
{
    public record class ProjectEntry
    (
        bool Selected,
        long ID,
        string Name,
        string Creator
    );


    public class MainWindowViewModel : ViewModelBase
    {
        public string Greeting => "Welcome to Avalonia!";

        public List<ProjectEntry> ProjectEntries => new List<ProjectEntry>
        {
            new ProjectEntry(false,8062448201,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,8062448202,"Ropes","WingDingWarrior89"),
            new ProjectEntry(false,8062448203,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,8062448204,"Ropes","WingDingWarrior89"),new ProjectEntry(false,80624482011,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,8062448205,"Ropes","WingDingWarrior89"),new ProjectEntry(false,80624482012,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,8062448206,"Ropes","WingDingWarrior89"),new ProjectEntry(false,80624482013,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,8062448207,"Ropes","WingDingWarrior89"),new ProjectEntry(false,80624482014,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,8062448208,"Ropes","WingDingWarrior89"),new ProjectEntry(false,80624482015,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,8062448209,"Ropes","WingDingWarrior89"),new ProjectEntry(false,80624482016,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,80624482010,"Ropes","WingDingWarrior89")
        };

        public void DisplayLoginWindow()
        {

        }
    }
}