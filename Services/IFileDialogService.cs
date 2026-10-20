namespace PowerSimBuilder
{
    public interface IFileDialogService
    {
        string PickSavePath(
            System.Windows.Forms.IWin32Window owner,
            string filter,
            string title,
            string defaultFileName);

        string PickOpenPath(
            System.Windows.Forms.IWin32Window owner,
            string filter,
            string title);
    }
}