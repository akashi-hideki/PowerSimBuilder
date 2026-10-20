using System.Windows.Forms;

namespace PowerSimBuilder
{
    public sealed class DefaultFileDialogService : IFileDialogService
    {
        public string PickSavePath(IWin32Window owner, string filter, string title, string defaultFileName)
        {
            using (var dlg = new SaveFileDialog
            {
                Filter = filter,
                Title = title,
                FileName = defaultFileName,
            })
            {
                DialogResult result = owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog();
                return result == DialogResult.OK ? dlg.FileName : null;
            }
        }

        public string PickOpenPath(IWin32Window owner, string filter, string title)
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = filter,
                Title = title,
            })
            {
                DialogResult result = owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog();
                return result == DialogResult.OK ? dlg.FileName : null;
            }
        }
    }
}