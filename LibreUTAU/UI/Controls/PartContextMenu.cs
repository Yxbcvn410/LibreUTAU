using System;
using System.Windows;
using System.Windows.Controls;
using LibreUtau.Core.Commands;
using LibreUtau.Core.USTx;
using LibreUtau.UI.Dialogs;

namespace LibreUtau.UI.Controls {
    public class PartContextMenu : ContextMenu {
        private readonly UPart _current;

        public PartContextMenu(UPart part) {
            _current = part;
            MenuItem renamePart = new MenuItem();
            renamePart.Header = Application.Current.Resources["midicontextmenu.rename"] as string;
            renamePart.Click += RenamePartOnClick;

            MenuItem exportPart = new MenuItem();
            exportPart.Header = Application.Current.Resources["midicontextmenu.exportas"] as string;
            exportPart.Click += ExportPartOnClick;

            this.Items.Add(renamePart);

            if (part is UVoicePart)
                this.Items.Add(exportPart);
        }

        public void Show(UIElement element) {
            IsOpen = true;
            PlacementTarget = element;
        }

        private void RenamePartOnClick(object sender, EventArgs e) {
            InputDialog x = new InputDialog(Application.Current.Resources["dialogs.enter.partname"] as string,
                _current.Name);
            x.ShowDialog();

            if (x.Text == null)
                return;

            CommandDispatcher.Inst.StartUndoGroup();
            CommandDispatcher.Inst.ExecuteCmd(new RenamePartCommand(CommandDispatcher.Inst.Project, _current,
                x.Text));
            CommandDispatcher.Inst.EndUndoGroup();
            CommandDispatcher.Inst.ExecuteCmd(new RedrawNotesNotification());
        }

        private void ExportPartOnClick(object sender, EventArgs e) {
            // todo
        }
    }
}
