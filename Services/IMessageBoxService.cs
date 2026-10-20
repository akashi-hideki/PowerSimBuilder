namespace PowerSimBuilder
{
    public interface IMessageBoxService
    {
        void ShowInfo(System.Windows.Forms.IWin32Window owner, string message, string title);
        void ShowError(System.Windows.Forms.IWin32Window owner, string message, string title);
        void ShowWarning(System.Windows.Forms.IWin32Window owner, string message, string title);
    }
}