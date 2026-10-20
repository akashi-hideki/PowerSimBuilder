using System.Collections.Generic;
using System.Windows.Forms;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.ComponentEdit
{
    public sealed class MockMessageBoxService : IMessageBoxService
    {
        public List<(string Message, string Title)> InfoCalls { get; } = new();
        public List<(string Message, string Title)> ErrorCalls { get; } = new();
        public List<(string Message, string Title)> WarningCalls { get; } = new();

        public void ShowInfo(IWin32Window owner, string message, string title)
            => InfoCalls.Add((message, title));

        public void ShowError(IWin32Window owner, string message, string title)
            => ErrorCalls.Add((message, title));

        public void ShowWarning(IWin32Window owner, string message, string title)
            => WarningCalls.Add((message, title));

        public bool ConfirmResult { get; set; } = true;
        public List<(string Message, string Title)> ConfirmCalls { get; } = new();

        public bool ShowConfirm(IWin32Window owner, string message, string title)
        {
            ConfirmCalls.Add((message, title));
            return ConfirmResult;
        }
    }

    public sealed class MockFileDialogService : IFileDialogService
    {
        public string SaveResult { get; set; }
        public string OpenResult { get; set; }
        public List<(string Filter, string Title, string DefaultFileName)> SaveCalls { get; } = new();
        public List<(string Filter, string Title)> OpenCalls { get; } = new();

        public string PickSavePath(IWin32Window owner, string filter, string title, string defaultFileName)
        {
            SaveCalls.Add((filter, title, defaultFileName));
            return SaveResult;
        }

        public string PickOpenPath(IWin32Window owner, string filter, string title)
        {
            OpenCalls.Add((filter, title));
            return OpenResult;
        }
    }

    [CollectionDefinition("GlobalServices", DisableParallelization = true)]
    public class GlobalServicesCollection { }
}