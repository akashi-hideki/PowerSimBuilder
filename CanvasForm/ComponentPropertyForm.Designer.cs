namespace PowerSimBuilder
{
    partial class ComponentPropertyForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ComponentPropertyForm
            // 
            this.ClientSize = new System.Drawing.Size(384, 261);
            this.Name = "ComponentPropertyForm";
            this.ResumeLayout(false);
        }
    }
}