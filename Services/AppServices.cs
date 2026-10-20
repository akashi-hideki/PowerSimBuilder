namespace PowerSimBuilder
{
    /// <summary>
    /// Application-wide service locator for modal dialogs.
    /// Tests can swap implementations to drive GUI logic headlessly.
    /// </summary>
    public static class AppServices
    {
        public static IMessageBoxService MessageBox { get; set; } = new DefaultMessageBoxService();
        public static IFileDialogService FileDialog { get; set; } = new DefaultFileDialogService();

        public static void Reset()
        {
            MessageBox = new DefaultMessageBoxService();
            FileDialog = new DefaultFileDialogService();
        }
    }
}