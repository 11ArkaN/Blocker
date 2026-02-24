using System.Drawing;
using System.Drawing.Drawing2D;
using Forms = System.Windows.Forms;

namespace Blocker.App.Services;

public sealed class TrayService : IDisposable
{
    private const int MenuIconSize = 16;

    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _contextMenu;
    private readonly Forms.ToolStripMenuItem _openItem;
    private readonly Forms.ToolStripMenuItem _enableItem;
    private readonly Forms.ToolStripMenuItem _disableItem;
    private readonly Forms.ToolStripMenuItem _exitItem;

    public TrayService()
    {
        _openItem = CreateMenuItem(
            "Pokaz okno",
            (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty),
            CreateIcon(TrayMenuIcon.Open));
        _enableItem = CreateMenuItem(
            "Wlacz blokade",
            (_, _) => EnableRequested?.Invoke(this, EventArgs.Empty),
            CreateIcon(TrayMenuIcon.Enable));
        _disableItem = CreateMenuItem(
            "Wylacz blokade",
            (_, _) => DisableRequested?.Invoke(this, EventArgs.Empty),
            CreateIcon(TrayMenuIcon.Disable));
        _exitItem = CreateMenuItem(
            "Zamknij",
            (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty),
            CreateIcon(TrayMenuIcon.Exit));

        _contextMenu = new Forms.ContextMenuStrip
        {
            ShowImageMargin = true,
            ShowCheckMargin = false,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            BackColor = Color.FromArgb(18, 26, 42),
            ForeColor = Color.FromArgb(230, 238, 255),
            Padding = new Padding(6),
            Renderer = new TrayMenuRenderer(),
            RenderMode = Forms.ToolStripRenderMode.Professional
        };

        _contextMenu.Items.Add(_openItem);
        _contextMenu.Items.Add(CreateSeparator());
        _contextMenu.Items.Add(_enableItem);
        _contextMenu.Items.Add(_disableItem);
        _contextMenu.Items.Add(CreateSeparator());
        _contextMenu.Items.Add(_exitItem);

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Shield,
            Text = "Blocker",
            Visible = true,
            ContextMenuStrip = _contextMenu
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
        _contextMenu.Dispose();
        DisposeMenuItem(_openItem);
        DisposeMenuItem(_enableItem);
        DisposeMenuItem(_disableItem);
        DisposeMenuItem(_exitItem);
    }

    private static void DisposeMenuItem(Forms.ToolStripMenuItem item)
    {
        item.Image?.Dispose();
        item.Dispose();
    }

    private static Forms.ToolStripMenuItem CreateMenuItem(string text, EventHandler onClick, Image icon)
    {
        var item = new Forms.ToolStripMenuItem(text)
        {
            AutoSize = false,
            Size = new Size(220, 34),
            Height = 34,
            Padding = new Padding(8, 0, 8, 0),
            Margin = new Padding(1),
            TextAlign = ContentAlignment.MiddleLeft,
            Image = icon,
            ImageScaling = Forms.ToolStripItemImageScaling.None,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextImageRelation = Forms.TextImageRelation.ImageBeforeText
        };
        item.Click += onClick;
        return item;
    }

    private static Forms.ToolStripSeparator CreateSeparator()
    {
        return new Forms.ToolStripSeparator
        {
            AutoSize = false,
            Height = 10,
            Margin = new Padding(10, 4, 10, 4)
        };
    }

    private static Image CreateIcon(TrayMenuIcon kind)
    {
        var bitmap = new Bitmap(MenuIconSize, MenuIconSize);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);

        switch (kind)
        {
            case TrayMenuIcon.Open:
                DrawOpenIcon(graphics);
                break;
            case TrayMenuIcon.Enable:
                DrawEnableIcon(graphics);
                break;
            case TrayMenuIcon.Disable:
                DrawDisableIcon(graphics);
                break;
            case TrayMenuIcon.Exit:
                DrawExitIcon(graphics);
                break;
        }

        return bitmap;
    }

    private static void DrawOpenIcon(Graphics graphics)
    {
        using var borderPen = new Pen(Color.FromArgb(124, 168, 224), 1.2F);
        using var fillBrush = new SolidBrush(Color.FromArgb(30, 66, 106, 166));
        using var topBrush = new SolidBrush(Color.FromArgb(64, 110, 170, 236));
        using var path = CreateRoundedRectanglePath(1F, 2F, 14F, 12F, 3F);
        graphics.FillPath(fillBrush, path);
        graphics.DrawPath(borderPen, path);
        graphics.FillRectangle(topBrush, 2, 3, 12, 3);
    }

    private static void DrawEnableIcon(Graphics graphics)
    {
        using var circleBrush = new SolidBrush(Color.FromArgb(38, 143, 97));
        using var circlePen = new Pen(Color.FromArgb(95, 214, 157), 1.2F);
        using var triangleBrush = new SolidBrush(Color.FromArgb(232, 255, 244));
        graphics.FillEllipse(circleBrush, 1, 1, 14, 14);
        graphics.DrawEllipse(circlePen, 1, 1, 14, 14);
        var points = new[]
        {
            new PointF(6.2F, 4.8F),
            new PointF(11.2F, 8F),
            new PointF(6.2F, 11.2F)
        };
        graphics.FillPolygon(triangleBrush, points);
    }

    private static void DrawDisableIcon(Graphics graphics)
    {
        using var circleBrush = new SolidBrush(Color.FromArgb(133, 101, 28));
        using var circlePen = new Pen(Color.FromArgb(231, 193, 96), 1.2F);
        using var linePen = new Pen(Color.FromArgb(255, 244, 203), 2F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.FillEllipse(circleBrush, 1, 1, 14, 14);
        graphics.DrawEllipse(circlePen, 1, 1, 14, 14);
        graphics.DrawLine(linePen, 4.8F, 8F, 11.2F, 8F);
    }

    private static void DrawExitIcon(Graphics graphics)
    {
        using var circleBrush = new SolidBrush(Color.FromArgb(143, 56, 71));
        using var circlePen = new Pen(Color.FromArgb(237, 115, 135), 1.2F);
        using var linePen = new Pen(Color.FromArgb(255, 230, 236), 1.8F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.FillEllipse(circleBrush, 1, 1, 14, 14);
        graphics.DrawEllipse(circlePen, 1, 1, 14, 14);
        graphics.DrawLine(linePen, 5.2F, 5.2F, 10.8F, 10.8F);
        graphics.DrawLine(linePen, 10.8F, 5.2F, 5.2F, 10.8F);
    }

    private static GraphicsPath CreateRoundedRectanglePath(float x, float y, float width, float height, float radius)
    {
        var diameter = radius * 2F;
        var path = new GraphicsPath();

        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    private sealed class TrayMenuRenderer : Forms.ToolStripProfessionalRenderer
    {
        public TrayMenuRenderer() : base(new TrayMenuColorTable())
        {
            RoundedEdges = true;
        }

        protected override void OnRenderToolStripBorder(Forms.ToolStripRenderEventArgs e)
        {
            var bounds = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            using var pen = new Pen(Color.FromArgb(52, 80, 123));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(pen, bounds);
        }

        protected override void OnRenderSeparator(Forms.ToolStripSeparatorRenderEventArgs e)
        {
            var y = e.Item.Bounds.Top + (e.Item.Height / 2);
            using var pen = new Pen(Color.FromArgb(43, 67, 102));
            var x1 = e.Item.Bounds.Left + 6;
            var x2 = e.Item.Bounds.Right - 6;
            e.Graphics.DrawLine(pen, x1, y, x2, y);
        }

        protected override void OnRenderItemText(Forms.ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled
                ? Color.FromArgb(230, 238, 255)
                : Color.FromArgb(130, 147, 175);
            base.OnRenderItemText(e);
        }
    }

    private enum TrayMenuIcon
    {
        Open,
        Enable,
        Disable,
        Exit
    }

    private sealed class TrayMenuColorTable : Forms.ProfessionalColorTable
    {
        public TrayMenuColorTable()
        {
            UseSystemColors = false;
        }

        public override Color ToolStripDropDownBackground => Color.FromArgb(18, 26, 42);
        public override Color ImageMarginGradientBegin => Color.FromArgb(18, 26, 42);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(18, 26, 42);
        public override Color ImageMarginGradientEnd => Color.FromArgb(18, 26, 42);
        public override Color ImageMarginRevealedGradientBegin => Color.FromArgb(14, 22, 36);
        public override Color ImageMarginRevealedGradientMiddle => Color.FromArgb(14, 22, 36);
        public override Color ImageMarginRevealedGradientEnd => Color.FromArgb(14, 22, 36);
        public override Color MenuItemBorder => Color.FromArgb(52, 80, 123);
        public override Color MenuItemSelected => Color.FromArgb(26, 47, 78);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(26, 47, 78);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(26, 47, 78);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(20, 36, 61);
        public override Color MenuItemPressedGradientMiddle => Color.FromArgb(20, 36, 61);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(20, 36, 61);
        public override Color SeparatorDark => Color.FromArgb(43, 67, 102);
        public override Color SeparatorLight => Color.FromArgb(43, 67, 102);
    }
}
