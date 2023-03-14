using ReactiveUI;
using System.Runtime.CompilerServices;
using System.Text;

namespace ScratchDL.GUI
{
    public class ProjectEntry : ReactiveObject
    {
        bool _selected = true;
        public bool Selected
        {
            get => _selected;
            set => this.RaiseAndSetIfChanged(ref _selected, value);
        }
        public long ID { get; init; }
        public string Name { get; init; }
        public string Creator { get; init; }

        public ProjectEntry(bool selected, long id, string name, string creator)
        {
            Selected = selected;
            ID = id;
            Name = name;
            Creator = creator;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is ProjectEntry otherEntry && ID == otherEntry.ID;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("ProjectEntry");
            stringBuilder.Append(" { ");
            if (PrintMembers(stringBuilder))
            {
                stringBuilder.Append(' ');
            }
            stringBuilder.Append('}');
            return stringBuilder.ToString();
        }

        bool PrintMembers(StringBuilder builder)
        {
            builder.Append("Selected = ");
            builder.Append(Selected.ToString());
            builder.Append(", ID = ");
            builder.Append(ID.ToString());
            builder.Append(", Name = ");
            builder.Append((object)Name);
            builder.Append(", Creator = ");
            builder.Append((object)Creator);
            return true;
        }
    }
}
