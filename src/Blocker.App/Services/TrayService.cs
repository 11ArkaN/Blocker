using System.Drawing;
using Forms = System.Windows.Forms;

namespace Blocker.App.Services;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _enableItem;
    private readonly Forms.ToolStripMenuItem _disableItem;
    private readonly Forms.ToolStripMenuItem _exitItem;

    public TrayService()
    {
        _enableItem = new Forms.ToolStripMenuItem("Enable block");
        _disableItem = new Forms.ToolStripMenuItem("Disable block");
        var openItem = new Forms.ToolStripMenuItem("Open window");
        _exitItem = new Forms.ToolStripMenuItem("Exit");

        _enableItem.Click += (_, _) => EnableRequested?.Invoke(this, EventArgs.Empty);
        _disableItem.Click += (_, _) => DisableRequested?.Invoke(this, EventArgs.Empty);
        openItem.Click += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
        _exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(_enableItem);
        contextMenu.Items.Add(_disableItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(_exitItem);

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Shield,
            Text = "Blocker",
            Visible = true,
            ContextMenuStrip = contextMenu
        };

        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OpenRequested;
    public event EventHandler? EnableRequested;
    public event EventHandler? DisableRequested;
    public event EventHandler? ExitRequested;

    public void SetBlockState(bool isActive)
    {
        _enableItem.Enabled = !isActive;
        _disableItem.Enabled = isActive;
        _exitItem.Enabled = !isActive;
        _notifyIcon.Text = isActive ? "Blocker (ON)" : "Blocker (OFF)";
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _enableItem.Dispose();
        _disableItem.Dispose();
        _exitItem.Dispose();
    }
}
