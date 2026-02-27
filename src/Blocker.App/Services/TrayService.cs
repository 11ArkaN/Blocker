using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Blocker.App.Contracts;
using Forms = System.Windows.Forms;

namespace Blocker.App.Services;

public sealed class TrayService : IDisposable
{
    private const int MenuIconSize = 16;

    // ?? Fluent Dark palette ??
    private static readonly Color MenuBackground = Color.FromArgb(44, 44, 44);
    private static readonly Color MenuBorder = Color.FromArgb(60, 60, 60);
    private static readonly Color HoverFill = Color.FromArgb(55, 55, 55);
    private static readonly Color PressedFill = Color.FromArgb(40, 40, 40);
    private static readonly Color TextPrimary = Color.FromArgb(255, 255, 255);
    private static readonly Color TextSecondary = Color.FromArgb(157, 157, 157);
    private static readonly Color TextDisabled = Color.FromArgb(90, 90, 90);
    private static readonly Color SeparatorLine = Color.FromArgb(56, 56, 56);
    private static readonly Color IconDefault = Color.FromArgb(200, 200, 200);
    private static readonly Color IconGreen = Color.FromArgb(108, 203, 95);
    private static readonly Color IconAmber = Color.FromArgb(252, 185, 0);
    private static readonly Color IconRed = Color.FromArgb(232, 80, 80);
    private static readonly Color AccentBlue = Color.FromArgb(96, 205, 255);
    private static readonly Color StatusActive = Color.FromArgb(108, 203, 95);
    private static readonly Color StatusInactive = Color.FromArgb(120, 120, 120);

    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _contextMenu;
    private readonly ILocalizationService _localizationService;
    private readonly Forms.ToolStripLabel _headerLabel;
    private readonly Forms.ToolStripLabel _statusLabel;
    private readonly Forms.ToolStripMenuItem _openItem;
    private readonly Forms.ToolStripMenuItem _enableItem;
    private readonly Forms.ToolStripMenuItem _disableItem;
    private readonly Forms.ToolStripMenuItem _exitItem;
    private bool _isBlockActive;

    public TrayService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;

        var headerFont = CreateFont("Segoe UI Variable Display", 10F, FontStyle.Bold)
                         ?? new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);

        _headerLabel = new Forms.ToolStripLabel("Blocker")
        {
            Font = headerFont,
            ForeColor = TextPrimary,
            Padding = new Padding(6, 6, 6, 0),
            Margin = new Padding(4, 2, 4, 0)
        };
        _statusLabel = new Forms.ToolStripLabel(string.Empty)
        {
            Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = TextSecondary,
            Padding = new Padding(6, 0, 6, 4),
            Margin = new Padding(4, 0, 4, 0)
        };

        _openItem = CreateMenuItem(
            string.Empty,
            (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty),
            CreateIcon(TrayMenuIcon.Open));
        _enableItem = CreateMenuItem(
            string.Empty,
            (_, _) => EnableRequested?.Invoke(this, EventArgs.Empty),
            CreateIcon(TrayMenuIcon.Enable));
        _disableItem = CreateMenuItem(
            string.Empty,
            (_, _) => DisableRequested?.Invoke(this, EventArgs.Empty),
            CreateIcon(TrayMenuIcon.Disable));
        _exitItem = CreateMenuItem(
            string.Empty,
            (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty),
            CreateIcon(TrayMenuIcon.Exit));

        _contextMenu = new Forms.ContextMenuStrip
        {
            ShowImageMargin = true,
            ShowCheckMargin = false,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            BackColor = MenuBackground,
            ForeColor = TextPrimary,
            Padding = new Padding(2, 4, 2, 4),
            Renderer = new TrayMenuRenderer(),
            RenderMode = Forms.ToolStripRenderMode.Professional,
            AutoSize = true,
            DropShadowEnabled = true
        };

        _contextMenu.Items.Add(_headerLabel);
        _contextMenu.Items.Add(_statusLabel);
        _contextMenu.Items.Add(CreateSeparator());
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
        _localizationService.LanguageChanged += HandleLanguageChanged;
        ApplyLocalization();
    }

    public event EventHandler? OpenRequested;
    public event EventHandler? EnableRequested;
    public event EventHandler? DisableRequested;
    public event EventHandler? ExitRequested;

    public void SetBlockState(bool isActive)
    {
        _isBlockActive = isActive;
        _enableItem.Enabled = !isActive;
        _disableItem.Enabled = isActive;
        _exitItem.Enabled = !isActive;
        ApplyStatusText();
    }

    public void Dispose()
    {
        _localizationService.LanguageChanged -= HandleLanguageChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _headerLabel.Font?.Dispose();
        _headerLabel.Dispose();
        _statusLabel.Dispose();
        DisposeMenuItem(_openItem);
        DisposeMenuItem(_enableItem);
        DisposeMenuItem(_disableItem);
        DisposeMenuItem(_exitItem);
    }

    private void HandleLanguageChanged(object? sender, EventArgs e)
    {
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        _openItem.Text = _localizationService["Tray.OpenWindow"];
        _enableItem.Text = _localizationService["Tray.EnableBlock"];
        _disableItem.Text = _localizationService["Tray.DisableBlock"];
        _exitItem.Text = _localizationService["Tray.Exit"];
        ApplyStatusText();
    }

    private void ApplyStatusText()
    {
        var statusText = _isBlockActive
            ? _localizationService["Tray.StatusOn"]
            : _localizationService["Tray.StatusOff"];
        _notifyIcon.Text = statusText;
        _statusLabel.Text = statusText;
        _statusLabel.ForeColor = _isBlockActive ? StatusActive : StatusInactive;
    }

    private static Font? CreateFont(string familyName, float size, FontStyle style)
    {
        try
        {
            var font = new Font(familyName, size, style, GraphicsUnit.Point);
            if (font.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase))
                return font;
            font.Dispose();
        }
        catch { }
        return null;
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
            Size = new Size(220, 32),
            Height = 32,
            Padding = new Padding(4, 0, 4, 0),
            Margin = new Padding(4, 1, 4, 1),
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
            Height = 9,
            Margin = new Padding(8, 2, 8, 2)
        };
    }

    private static Image CreateIcon(TrayMenuIcon kind)
    {
        var bitmap = new Bitmap(MenuIconSize, MenuIconSize);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
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
        using var pen = new Pen(AccentBlue, 1.3F)
        {
            LineJoin = LineJoin.Round,
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        // Window outline
        using var path = CreateRoundedRectanglePath(1.5F, 1.5F, 13F, 11F, 2F);
        graphics.DrawPath(pen, path);

        // Title bar line
        graphics.DrawLine(pen, 2F, 4.5F, 14F, 4.5F);

        // Arrow (open/launch)
        using var arrowPen = new Pen(AccentBlue, 1.2F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.DrawLine(arrowPen, 9F, 8F, 12F, 8F);
        graphics.DrawLine(arrowPen, 10.5F, 6.5F, 12F, 8F);
        graphics.DrawLine(arrowPen, 10.5F, 9.5F, 12F, 8F);
    }

    private static void DrawEnableIcon(Graphics graphics)
    {
        using var pen = new Pen(IconGreen, 1.4F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        // Shield outline
        var shieldPoints = new[]
        {
            new PointF(8F, 1.5F),
            new PointF(13.5F, 3.5F),
            new PointF(13.5F, 8F),
            new PointF(8F, 14F),
            new PointF(2.5F, 8F),
            new PointF(2.5F, 3.5F),
            new PointF(8F, 1.5F)
        };
        graphics.DrawPolygon(pen, shieldPoints);

        // Checkmark
        using var checkPen = new Pen(IconGreen, 1.6F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        graphics.DrawLine(checkPen, 5.5F, 8F, 7.2F, 10F);
        graphics.DrawLine(checkPen, 7.2F, 10F, 11F, 5.5F);
    }

    private static void DrawDisableIcon(Graphics graphics)
    {
        using var pen = new Pen(IconAmber, 1.4F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        // Shield outline
        var shieldPoints = new[]
        {
            new PointF(8F, 1.5F),
            new PointF(13.5F, 3.5F),
            new PointF(13.5F, 8F),
            new PointF(8F, 14F),
            new PointF(2.5F, 8F),
            new PointF(2.5F, 3.5F),
            new PointF(8F, 1.5F)
        };
        graphics.DrawPolygon(pen, shieldPoints);

        // Horizontal line (pause/disable)
        using var linePen = new Pen(IconAmber, 1.6F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.DrawLine(linePen, 5.5F, 8F, 10.5F, 8F);
    }

    private static void DrawExitIcon(Graphics graphics)
    {
        using var pen = new Pen(IconRed, 1.4F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        // Door frame
        graphics.DrawLine(pen, 7F, 2F, 3F, 2F);
        using var doorPath = CreateRoundedRectanglePath(2F, 2F, 6F, 12F, 1.5F);
        graphics.DrawPath(pen, doorPath);

        // Arrow pointing right (exit)
        using var arrowPen = new Pen(IconRed, 1.5F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.DrawLine(arrowPen, 8F, 8F, 13.5F, 8F);
        graphics.DrawLine(arrowPen, 11.5F, 5.8F, 13.5F, 8F);
        graphics.DrawLine(arrowPen, 11.5F, 10.2F, 13.5F, 8F);
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
        private const int Radius = 8;

        public TrayMenuRenderer() : base(new TrayMenuColorTable())
        {
            RoundedEdges = true;
        }

        protected override void OnRenderToolStripBackground(Forms.ToolStripRenderEventArgs e)
        {
            var rect = new Rectangle(0, 0, e.ToolStrip.Width, e.ToolStrip.Height);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var bgBrush = new SolidBrush(MenuBackground);
            using var bgPath = CreateRoundedRectanglePath(0, 0, rect.Width, rect.Height, Radius);
            e.Graphics.FillPath(bgBrush, bgPath);
        }

        protected override void OnRenderToolStripBorder(Forms.ToolStripRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = CreateRoundedRectanglePath(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1, Radius);
            using var pen = new Pen(MenuBorder, 1F);
            e.Graphics.DrawPath(pen, path);
        }

        protected override void OnRenderMenuItemBackground(Forms.ToolStripItemRenderEventArgs e)
        {
            if (e.Item is not Forms.ToolStripMenuItem)
            {
                base.OnRenderMenuItemBackground(e);
                return;
            }

            var rect = new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (e.Item.Pressed && e.Item.Enabled)
            {
                using var path = CreateRoundedRectanglePath(rect.X, rect.Y, rect.Width, rect.Height, 4);
                using var brush = new SolidBrush(PressedFill);
                e.Graphics.FillPath(brush, path);
            }
            else if (e.Item.Selected && e.Item.Enabled)
            {
                using var path = CreateRoundedRectanglePath(rect.X, rect.Y, rect.Width, rect.Height, 4);
                using var brush = new SolidBrush(HoverFill);
                e.Graphics.FillPath(brush, path);
            }
        }

        protected override void OnRenderSeparator(Forms.ToolStripSeparatorRenderEventArgs e)
        {
            var y = e.Item.Height / 2;
            using var pen = new Pen(SeparatorLine, 1F);
            e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
        }

        protected override void OnRenderItemText(Forms.ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item is Forms.ToolStripLabel)
            {
                base.OnRenderItemText(e);
                return;
            }

            e.TextColor = e.Item.Enabled ? TextPrimary : TextDisabled;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderImageMargin(Forms.ToolStripRenderEventArgs e)
        {
            // suppress default image-margin background
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

        public override Color ToolStripDropDownBackground => MenuBackground;
        public override Color ImageMarginGradientBegin => MenuBackground;
        public override Color ImageMarginGradientMiddle => MenuBackground;
        public override Color ImageMarginGradientEnd => MenuBackground;
        public override Color ImageMarginRevealedGradientBegin => MenuBackground;
        public override Color ImageMarginRevealedGradientMiddle => MenuBackground;
        public override Color ImageMarginRevealedGradientEnd => MenuBackground;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => HoverFill;
        public override Color MenuItemSelectedGradientBegin => HoverFill;
        public override Color MenuItemSelectedGradientEnd => HoverFill;
        public override Color MenuItemPressedGradientBegin => PressedFill;
        public override Color MenuItemPressedGradientMiddle => PressedFill;
        public override Color MenuItemPressedGradientEnd => PressedFill;
        public override Color SeparatorDark => SeparatorLine;
        public override Color SeparatorLight => SeparatorLine;
    }
}
