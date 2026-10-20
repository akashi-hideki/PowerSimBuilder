using System.Windows.Forms;

namespace PowerSimBuilder
{
    public sealed class DefaultMessageBoxService : IMessageBoxService
    {
        public void ShowInfo(IWin32Window owner, string message, string title)
        {
            if (owner != null)
                MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ShowError(IWin32Window owner, string message, string title)
        {
            if (owner != null)
                MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void ShowWarning(IWin32Window owner, string message, string title)
        {
            if (owner != null)
                MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}