using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace POSales
{
    internal static class ModernUI
    {
        private static readonly Color AppBackground = Color.FromArgb(246, 244, 238);
        private static readonly Color Surface = Color.White;
        private static readonly Color Sidebar = Color.FromArgb(18, 21, 18);
        private static readonly Color SidebarLight = Color.FromArgb(34, 41, 34);
        private static readonly Color Header = Color.FromArgb(255, 255, 255);
        private static readonly Color Primary = Color.FromArgb(9, 112, 78);
        private static readonly Color PrimaryDark = Color.FromArgb(5, 72, 54);
        private static readonly Color Accent = Color.FromArgb(210, 160, 62);
        private static readonly Color Danger = Color.FromArgb(202, 55, 45);
        private static readonly Color Violet = Color.FromArgb(110, 86, 45);
        private static readonly Color Info = Color.FromArgb(70, 128, 94);
        private static readonly Color Text = Color.FromArgb(31, 31, 27);
        private static readonly Color MutedText = Color.FromArgb(100, 96, 84);
        private static readonly Color Border = Color.FromArgb(226, 219, 204);
        private static readonly Font DefaultFont = new Font("Segoe UI", 10F, FontStyle.Regular);
        private static readonly Font TitleFont = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
        private static readonly Font SmallFont = new Font("Segoe UI", 9F, FontStyle.Regular);
        private static readonly HashSet<Control> StyledControls = new HashSet<Control>();
        private static readonly HashSet<Form> EnhancedForms = new HashSet<Form>();

        private static Image _cachedLogo = null;
        private static string _cachedStoreName = null;

        public static Image StoreLogo
        {
            get
            {
                if (_cachedLogo == null)
                {
                    _cachedLogo = GetStoreLogoImage();
                }
                return _cachedLogo;
            }
            set
            {
                _cachedLogo = value;
            }
        }

        public static string StoreName
        {
            get
            {
                if (_cachedStoreName == null)
                {
                    DBConnect db = new DBConnect();
                    string dbStoreName = db.getStoreName();
                    _cachedStoreName = !string.IsNullOrEmpty(dbStoreName) ? dbStoreName : "IRAS SPOT";
                }
                return _cachedStoreName;
            }
            set
            {
                _cachedStoreName = value;
            }
        }

        public static Image GetStoreLogoImage()
        {
            try
            {
                DBConnect db = new DBConnect();
                byte[] imgBytes = db.getStoreLogo();
                if (imgBytes != null && imgBytes.Length > 0)
                {
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream(imgBytes))
                    {
                        return Image.FromStream(ms);
                    }
                }
            }
            catch
            {
            }
            return Properties.Resources.WhatsApp_Image_2026_06_01_at_2_12_01_PM;
        }

        public static void ResetCache()
        {
            _cachedLogo = null;
            _cachedStoreName = null;
        }

        public static void RefreshBranding()
        {
            ResetCache();
            foreach (Form form in Application.OpenForms)
            {
                PictureBox logo = FindControl<PictureBox>(form, "pictureBox1");
                if (logo != null && (IsSplashLogo(logo) || IsLoginLogo(logo) || IsSidebarLogo(logo)))
                {
                    logo.Image = StoreLogo;
                }

                if (form.Name == "MainForm")
                {
                    form.Text = StoreName;
                }
                else if (form.Name == "Cashier")
                {
                    form.Text = StoreName;
                }
                else if (form.Name == "Login")
                {
                    Label brand = FindControl<Label>(form, "label1");
                    if (brand != null)
                    {
                        brand.Text = StoreName;
                    }
                }
                else if (form.Name == "Home")
                {
                    Label title = FindControl<Label>(form, "label4");
                    if (title != null)
                    {
                        title.Text = StoreName;
                    }
                    Label welcome = FindControl<Label>(form, "label2");
                    if (welcome != null)
                    {
                        welcome.Text = "Welcome to " + StoreName;
                    }
                }
            }
        }

        public static void LoadReportWithCustomLogo(Microsoft.Reporting.WinForms.LocalReport localReport, string rdlcPath)
        {
            try
            {
                if (System.IO.File.Exists(rdlcPath))
                {
                    string rdlcContent = System.IO.File.ReadAllText(rdlcPath);
                    Image logoImg = StoreLogo;
                    if (logoImg != null)
                    {
                        byte[] logoBytes;
                        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                        {
                            using (Bitmap bmp = new Bitmap(logoImg))
                            {
                                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            }
                            logoBytes = ms.ToArray();
                        }
                        string base64Logo = Convert.ToBase64String(logoBytes);
                        
                        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(
                            @"<EmbeddedImage\s+Name=""Logo"">.*?<ImageData>.*?</ImageData>.*?</EmbeddedImage>", 
                            System.Text.RegularExpressions.RegexOptions.Singleline);
                            
                        if (regex.IsMatch(rdlcContent))
                        {
                            string newEmbeddedImage = string.Format(
                                @"<EmbeddedImage Name=""Logo""><MIMEType>image/png</MIMEType><ImageData>{0}</ImageData></EmbeddedImage>", 
                                base64Logo);
                            rdlcContent = regex.Replace(rdlcContent, newEmbeddedImage);
                        }
                    }
                    
                    using (System.IO.StringReader sr = new System.IO.StringReader(rdlcContent))
                    {
                        localReport.LoadReportDefinition(sr);
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error customizing report logo: " + ex.Message);
            }
            localReport.ReportPath = rdlcPath;
        }

        public static void ApplyOpenForms()
        {
            foreach (Form form in Application.OpenForms)
            {
                Apply(form);
            }
        }

        public static void Apply(Form form)
        {
            if (form == null)
                return;

            StyleForm(form);
            EnhanceForm(form);
            StyleControlTree(form);
        }

        private static void StyleControlTree(Control root)
        {
            StyleControl(root);
            root.ControlAdded -= ControlAdded;
            root.ControlAdded += ControlAdded;

            foreach (Control child in root.Controls)
            {
                StyleControlTree(child);
            }
        }

        private static void ControlAdded(object sender, ControlEventArgs e)
        {
            StyleControlTree(e.Control);
        }

        private static void StyleForm(Form form)
        {
            form.Font = DefaultFont;
            form.BackColor = AppBackground;
            form.ForeColor = Text;
            form.StartPosition = form.StartPosition == FormStartPosition.WindowsDefaultLocation
                ? FormStartPosition.CenterScreen
                : form.StartPosition;
        }

        private static void EnhanceForm(Form form)
        {
            if (EnhancedForms.Contains(form))
                return;

            EnhancedForms.Add(form);

            if (form.Name == "MainForm")
                EnhanceMainForm(form);
            else if (form.Name == "Login")
                EnhanceLogin(form);
            else if (form.Name == "Home")
                EnhanceSplash(form);
            else if (form.Name == "Dashboard")
                EnhanceDashboard(form);
            else if (form.Name == "UserAccount")
                EnhanceUserAccount(form);
            else if (form.Name == "Supplier")
                EnhanceSupplier(form);
            else if (form.Name == "Product")
                EnhanceProduct(form);
            else if (form.Name == "ProductModule")
                EnhanceProductModule(form);
            else if (form.Name == "BrandModule")
                EnhanceModuleTitleBar(form, "Brand Module");
            else if (form.Name == "CategoryModule")
                EnhanceModuleTitleBar(form, "Category Module");
            else if (form.Name == "SupplierModule")
                EnhanceModuleTitleBar(form, "Supplier Module");
            else if (form.Name == "Discount")
                EnhanceModuleTitleBar(form, "Discount");
            else if (form.Name == "Adjustments")
                EnhanceAdjustments(form);
            else if (form.Name == "StockIn")
                EnhanceStockIn(form);
            else if (form.Name == "Cashier")
                EnhanceCashier(form);
            else if (form.Name == "LookUpProduct")
                EnhanceLookUpProduct(form);
            else if (form.Name == "DailySale")
                EnhanceDailySale(form);
            else if (form.Name == "ChangePassword")
                EnhanceNativeWindow(form, new Size(520, 360), new Size(440, 300));
            else if (form.Name == "Settle")
                EnhanceSettle(form);
            else if (form.Name == "Qty")
                EnhanceQty(form);
            else if (form.Name == "Void")
                EnhanceNativeWindow(form, new Size(430, 300), new Size(410, 270));
            else if (form.Name == "CancelOrder")
                EnhanceNativeWindow(form, new Size(920, 560), new Size(760, 480));
        }

        private static void StyleControl(Control control)
        {
            if (control == null || StyledControls.Contains(control))
                return;

            if (IsCaptionButton(control))
                return;

            StyledControls.Add(control);

            if (control is DataGridView grid)
            {
                StyleGrid(grid);
                return;
            }

            if (control is Button button)
            {
                StyleButton(button);
                return;
            }

            if (control is TextBox textBox)
            {
                StyleTextBox(textBox);
                return;
            }

            if (control.GetType().FullName == "MetroFramework.Controls.MetroTextBox")
            {
                StyleMetroTextBox(control);
                return;
            }

            if (control is ComboBox comboBox)
            {
                StyleComboBox(comboBox);
                return;
            }

            if (control is Label label)
            {
                StyleLabel(label);
                return;
            }

            if (control is Panel panel)
            {
                StylePanel(panel);
                return;
            }

            if (control is GroupBox groupBox)
            {
                groupBox.Font = TitleFont;
                groupBox.ForeColor = Text;
                groupBox.BackColor = Surface;
                return;
            }

            if (control is PictureBox pictureBox)
            {
                StylePictureBox(pictureBox);
            }
        }

        private static void StylePanel(Panel panel)
        {
            string name = panel.Name.ToLowerInvariant();

            if (IsLoginHeader(panel))
            {
                SetBackColor(panel, Color.Transparent);
                panel.Paint -= PaintHeader;
                return;
            }

            if (IsDashboardCard(panel))
            {
                StyleDashboardCard(panel);
                return;
            }

            if (name.Contains("slide") || name.Contains("side") || name.Contains("menu"))
            {
                panel.BackColor = Sidebar;
                panel.Paint -= PaintSidebar;
                panel.Paint += PaintSidebar;
                return;
            }

            if (name.Contains("sub"))
            {
                panel.BackColor = SidebarLight;
                AdjustSubmenuHeight(panel);
                return;
            }

            if (name.Contains("logo"))
            {
                SetBackColor(panel, Color.Transparent);
                panel.Paint -= PaintLogoPanel;
                panel.Paint += PaintLogoPanel;
                return;
            }

            if (panel.Dock == DockStyle.Top)
            {
                panel.BackColor = Header;
                panel.Paint -= PaintHeader;
                panel.Paint += PaintHeader;
                return;
            }

            if (panel.Dock == DockStyle.Bottom)
            {
                panel.BackColor = PrimaryDark;
                panel.Resize -= BottomActionPanel_Resize;
                panel.Resize += BottomActionPanel_Resize;
                AdjustBottomActionPanel(panel);
                return;
            }

            if (panel.BackColor == SystemColors.Control || panel.BackColor == Color.White)
                panel.BackColor = Surface;
        }

        private static void StyleLabel(Label label)
        {
            label.Image = null;
            label.BackgroundImage = null;
            label.Font = IsTitleLabel(label) ? TitleFont : DefaultFont;

            if (IsInsideDarkSurface(label))
            {
                label.ForeColor = Color.White;
            }
            else if (label.ForeColor == SystemColors.ControlText || label.ForeColor == Color.Black)
            {
                label.ForeColor = IsTitleLabel(label) ? Text : MutedText;
            }

            SetBackColor(label, Color.Transparent);
        }

        private static void StyleButton(Button button)
        {
            bool menuButton = IsInsideSidebar(button);

            button.Image = null;
            button.BackgroundImage = null;
            NormalizeIconOnlyButton(button);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Segoe UI Semibold", menuButton ? 10F : 9.5F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;

            Color color = GetButtonColor(button);
            button.BackColor = color;
            button.ForeColor = Color.White;
            button.FlatAppearance.MouseOverBackColor = ThemeColor.ChangeColorBrightness(color, 0.12);
            button.FlatAppearance.MouseDownBackColor = ThemeColor.ChangeColorBrightness(color, -0.12);
            button.TextAlign = menuButton ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
            button.Padding = menuButton ? new Padding(button.Padding.Left + 8, 0, 0, 0) : new Padding(8, 0, 8, 0);

            if (button.Height < 38)
                button.Height = 38;

            if (!menuButton && button.Width < 88 && button.FindForm()?.Name != "Settle")
                button.Width = 88;

            AdjustSubmenuHeight(button.Parent);
            AdjustBottomActionPanel(button.Parent);
            button.Paint -= PaintButtonGlow;
            button.Paint += PaintButtonGlow;
            button.Resize -= RoundButton;
            button.Resize += RoundButton;
            ApplyRoundedRegion(button, menuButton ? 0 : 8);
        }

        private static void StyleTextBox(TextBox textBox)
        {
            textBox.Font = DefaultFont;
            textBox.BackColor = Surface;
            textBox.ForeColor = Text;
            textBox.BorderStyle = BorderStyle.FixedSingle;

            if (textBox.Height < 30 && !textBox.Multiline)
                textBox.Height = 30;
        }

        private static void StyleMetroTextBox(Control textBox)
        {
            textBox.Font = DefaultFont;
            textBox.BackColor = Surface;
            textBox.ForeColor = Text;
            textBox.Height = Math.Max(textBox.Height, 36);
            SetProperty(textBox, "BorderStyle", BorderStyle.FixedSingle);
            SetProperty(textBox, "Style", MetroFramework.MetroColorStyle.Teal);
            SetProperty(textBox, "Theme", MetroFramework.MetroThemeStyle.Light);
            SetProperty(textBox, "DisplayIcon", false);
            SetProperty(textBox, "Icon", null);
            SetProperty(textBox, "WaterMarkColor", MutedText);
            SetProperty(textBox, "WaterMarkFont", new Font("Segoe UI", 10F, FontStyle.Regular));
        }

        private static void StyleComboBox(ComboBox comboBox)
        {
            comboBox.Font = DefaultFont;
            comboBox.BackColor = Surface;
            comboBox.ForeColor = Text;
            comboBox.FlatStyle = FlatStyle.Flat;

            if (comboBox.Height < 30)
                comboBox.Height = 30;
        }

        private static void StyleGrid(DataGridView grid)
        {
            ReplaceImageColumns(grid);
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Border;
            grid.RowHeadersVisible = false;
            grid.AllowUserToResizeRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.RowTemplate.Height = Math.Max(grid.RowTemplate.Height, 34);
            grid.ColumnHeadersHeight = Math.Max(grid.ColumnHeadersHeight, 38);

            grid.DefaultCellStyle.BackColor = Surface;
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 247, 244);
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.DefaultCellStyle.Font = SmallFont;
            grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);

            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 253);
            grid.AlternatingRowsDefaultCellStyle.ForeColor = Text;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 247, 244);
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Text;

            grid.ColumnHeadersDefaultCellStyle.BackColor = Header;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        }

        private static void StylePictureBox(PictureBox pictureBox)
        {
            pictureBox.BackgroundImage = null;

            if (IsSplashLogo(pictureBox) || IsLoginLogo(pictureBox) || IsSidebarLogo(pictureBox))
            {
                SetBackColor(pictureBox, Color.Transparent);
                pictureBox.Image = StoreLogo;
                pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox.Visible = true;
                return;
            }

            if (pictureBox.Name.Equals("picLogo", StringComparison.OrdinalIgnoreCase))
            {
                pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox.Visible = true;
                return;
            }

            if (pictureBox.Name.Equals("picClose", StringComparison.OrdinalIgnoreCase))
            {
                pictureBox.Image = null;
                SetBackColor(pictureBox, Color.Transparent);
                pictureBox.Cursor = Cursors.Hand;
                pictureBox.Paint -= PaintCloseGlyph;
                pictureBox.Paint += PaintCloseGlyph;
                pictureBox.Visible = true;
                return;
            }

            pictureBox.Image = null;
            pictureBox.Visible = false;
        }

        private static Color GetButtonColor(Button button)
        {
            string nameAndText = (button.Name + " " + button.Text).ToLowerInvariant();

            if (nameAndText.Contains("delete") || nameAndText.Contains("remove") ||
                nameAndText.Contains("void") || nameAndText.Contains("cancel") ||
                nameAndText.Contains("logout") || nameAndText.Contains("close"))
                return Danger;

            if (nameAndText.Contains("search") || nameAndText.Contains("print") ||
                nameAndText.Contains("report") || nameAndText.Contains("update"))
                return Accent;

            if (IsInsideSidebar(button))
            {
                if (nameAndText.Contains("logout"))
                return Color.FromArgb(164, 45, 38);

                return nameAndText.Contains("stock") || nameAndText.Contains("record")
                    ? Color.FromArgb(45, 56, 45)
                    : Sidebar;
            }

            return Primary;
        }

        private static bool IsTitleLabel(Label label)
        {
            string name = label.Name.ToLowerInvariant();
            return name.Contains("title") || label.Font.Size >= 12F;
        }

        private static bool IsInsideDarkSurface(Control control)
        {
            Control parent = control.Parent;

            while (parent != null)
            {
                Color backColor = parent.BackColor;
                if (backColor == Sidebar || backColor == SidebarLight || backColor == PrimaryDark)
                    return true;

                parent = parent.Parent;
            }

            return false;
        }

        private static bool IsInsideSidebar(Control control)
        {
            Control parent = control.Parent;

            while (parent != null)
            {
                if (parent.Name.ToLowerInvariant().Contains("slide"))
                    return true;

                parent = parent.Parent;
            }

            return false;
        }

        private static bool IsLoginHeader(Panel panel)
        {
            Form form = panel.FindForm();
            return form != null &&
                   form.Name == "Login" &&
                   panel.Name.Equals("panel1", StringComparison.OrdinalIgnoreCase);
        }

        private static void AdjustSubmenuHeight(Control control)
        {
            Panel panel = control as Panel;
            if (panel == null || !panel.Name.ToLowerInvariant().Contains("sub"))
                return;

            int height = 0;
            Form form = panel.FindForm();
            bool isFormVisible = form != null && form.Visible;

            foreach (Control child in panel.Controls)
            {
                if (isFormVisible)
                {
                    if (child.Visible)
                        height += child.Height;
                }
                else
                {
                    if (child is Button)
                        height += child.Height;
                }
            }

            if (height > 0)
                panel.Height = height;
        }

        private static void AdjustBottomActionPanel(Control control)
        {
            Panel panel = control as Panel;
            if (panel == null || panel.Dock != DockStyle.Bottom)
                return;

            int tallestButton = 0;
            foreach (Control child in panel.Controls)
            {
                if (child is Button)
                    tallestButton = Math.Max(tallestButton, child.Height);
            }

            if (tallestButton == 0)
                return;

            panel.Height = Math.Max(panel.Height, tallestButton + 20);

            foreach (Control child in panel.Controls)
            {
                if (child is Button && child.Dock == DockStyle.None)
                    child.Top = Math.Max(10, (panel.Height - child.Height) / 2);

                if (child is Label label && label.Name.ToLowerInvariant().Contains("total"))
                {
                    label.ForeColor = Color.White;
                    label.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
                    label.TextAlign = ContentAlignment.MiddleRight;
                    label.Width = Math.Max(label.Width, 160);
                    label.Height = Math.Max(label.Height, 30);
                    label.Left = Math.Max(8, panel.Width - label.Width - 14);
                    label.Top = Math.Max(10, (panel.Height - label.Height) / 2);
                }
            }
        }

        private static void BottomActionPanel_Resize(object sender, EventArgs e)
        {
            AdjustBottomActionPanel(sender as Control);
        }

        private static void NormalizeIconOnlyButton(Button button)
        {
            if (!string.IsNullOrWhiteSpace(button.Text))
                return;

            string name = button.Name.ToLowerInvariant();

            if (name.Contains("print"))
                button.Text = "Print";
            else if (name.Contains("load"))
                button.Text = "Load";
            else if (name.Contains("save"))
                button.Text = "Save";
            else if (name.Contains("cancel"))
                button.Text = "Cancel";
            else if (name.Contains("delete") || name.Contains("remove"))
                button.Text = "Remove";
            else if (name.Contains("edit") || name.Contains("update"))
                button.Text = "Edit";
            else if (name.Contains("add") || name.Contains("new"))
                button.Text = "Add";
            else if (name.Contains("search"))
                button.Text = "Search";
            else
                button.Text = "Action";
        }

        private static bool IsDashboardCard(Panel panel)
        {
            if (panel.Width < 150 || panel.Height < 80 || panel.Height > 170)
                return false;

            bool hasPicture = false;
            bool hasLabel = false;

            foreach (Control child in panel.Controls)
            {
                hasPicture = hasPicture || child is PictureBox;
                hasLabel = hasLabel || child is Label;
            }

            return hasPicture && hasLabel;
        }

        private static bool IsSplashLogo(PictureBox pictureBox)
        {
            Form form = pictureBox.FindForm();
            return form != null &&
                   form.Name == "Home" &&
                   pictureBox.Name.Equals("pictureBox1", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLoginLogo(PictureBox pictureBox)
        {
            Form form = pictureBox.FindForm();
            return form != null &&
                   form.Name == "Login" &&
                   pictureBox.Name.Equals("pictureBox1", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSidebarLogo(PictureBox pictureBox)
        {
            Form form = pictureBox.FindForm();
            return form != null &&
                   (form.Name == "MainForm" || form.Name == "Cashier") &&
                   pictureBox.Name.Equals("pictureBox1", StringComparison.OrdinalIgnoreCase);
        }

        private static void StyleDashboardCard(Panel panel)
        {
            panel.BackColor = Surface;
            panel.Padding = new Padding(10);
            panel.Paint -= PaintCard;
            panel.Paint += PaintCard;

            foreach (Control child in panel.Controls)
            {
                if (child is Label label)
                {
                    bool valueLabel = label.Name.StartsWith("lbl", StringComparison.OrdinalIgnoreCase);
                    label.ForeColor = valueLabel ? Text : MutedText;
                    label.Font = valueLabel
                        ? new Font("Segoe UI Semibold", 16F, FontStyle.Bold)
                        : new Font("Segoe UI", 9.25F, FontStyle.Regular);
                }

                if (child is PictureBox picture)
                {
                    picture.SizeMode = PictureBoxSizeMode.Zoom;
                    SetBackColor(picture, Surface);
                }
            }

            LayoutDashboardCard(panel);
        }

        private static void LayoutDashboardCard(Panel panel)
        {
            Label value = null;
            Label title = null;
            Label caption = null;

            foreach (Control child in panel.Controls)
            {
                Label label = child as Label;
                if (label == null)
                    continue;

                if (label.Name.StartsWith("lbl", StringComparison.OrdinalIgnoreCase))
                    value = label;
                else if (title == null)
                    title = label;
                else
                    caption = label;
            }

            int left = 28;
            int width = Math.Max(80, panel.Width - left - 12);

            if (value != null)
            {
                value.SetBounds(left, 10, width, 26);
                value.TextAlign = ContentAlignment.MiddleRight;
                value.AutoSize = false;
            }

            if (title != null)
            {
                title.SetBounds(left, 42, width, 22);
                title.TextAlign = ContentAlignment.MiddleCenter;
                title.AutoSize = false;
            }

            if (caption != null)
            {
                caption.SetBounds(left, 64, width, Math.Max(36, panel.Height - 70));
                caption.TextAlign = ContentAlignment.TopCenter;
                caption.AutoSize = false;
            }
        }

        private static void EnhanceMainForm(Form form)
        {
            Panel slide = FindControl<Panel>(form, "panelSlide");
            Panel logoPanel = FindControl<Panel>(form, "panelLogo");
            Panel title = FindControl<Panel>(form, "panelTitle");
            Panel main = FindControl<Panel>(form, "panelMain");
            Label titleLabel = FindControl<Label>(form, "lblTitle");
            Label username = FindControl<Label>(form, "lblUsername");
            Label role = FindControl<Label>(form, "lblRole");
            PictureBox logo = FindControl<PictureBox>(form, "pictureBox1");

            form.FormBorderStyle = FormBorderStyle.FixedSingle;
            form.BackColor = AppBackground;
            form.MinimumSize = new Size(1200, 720);

            if (slide != null)
            {
                slide.Width = 238;
                slide.Padding = new Padding(0, 0, 0, 12);
            }

            if (logoPanel != null)
            {
                logoPanel.Height = 176;
                logoPanel.BackColor = Color.Transparent;
                logoPanel.Paint -= PaintLogoPanel;
                logoPanel.Paint += PaintLogoPanel;
            }

            if (logo != null)
            {
                logo.Visible = true;
                logo.SizeMode = PictureBoxSizeMode.Zoom;
                logo.SetBounds(38, 18, 162, 104);
                logo.BringToFront();
            }

            if (username != null)
            {
                username.AutoSize = false;
                username.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
                username.ForeColor = Color.White;
                username.TextAlign = ContentAlignment.MiddleCenter;
                username.SetBounds(18, 124, 202, 24);
            }

            if (role != null)
            {
                role.AutoSize = false;
                role.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                role.ForeColor = Color.FromArgb(220, 232, 222);
                role.TextAlign = ContentAlignment.MiddleCenter;
                role.SetBounds(18, 148, 202, 22);
            }

            if (title != null)
            {
                title.Height = 64;
                title.Padding = new Padding(24, 0, 24, 0);
            }

            if (main != null)
            {
                main.BackColor = AppBackground;
                main.Padding = new Padding(14);
            }

            if (titleLabel != null)
            {
                titleLabel.Dock = DockStyle.Fill;
                titleLabel.TextAlign = ContentAlignment.MiddleLeft;
                titleLabel.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
                titleLabel.ForeColor = Text;
            }

            foreach (Button button in GetControls<Button>(form))
            {
                if (!IsInsideSidebar(button))
                    continue;

                if (button.Height < 48)
                    button.Height = 48;

                button.Text = GetMenuText(button.Text);
            }
        }

        private static void EnhanceLogin(Form form)
        {
            form.BackColor = AppBackground;
            form.ClientSize = new Size(950, 650);
            form.Paint -= PaintLoginBackground;
            form.Paint += PaintLoginBackground;

            Panel header = FindControl<Panel>(form, "panel1");
            Label brand = FindControl<Label>(form, "label1");
            Label prompt = FindControl<Label>(form, "label2");
            Label copyright = FindControl<Label>(form, "label3");
            Control user = FindControl<Control>(form, "txtName");
            Control pass = FindControl<Control>(form, "txtPass");
            Button login = FindControl<Button>(form, "btnLogin");
            Button cancel = FindControl<Button>(form, "btnCancel");
            Button dbConfig = FindControl<Button>(form, "btnDbConfig");
            PictureBox close = FindControl<PictureBox>(form, "picClose");
            PictureBox logo = FindControl<PictureBox>(form, "pictureBox1");

            if (header != null)
            {
                header.Height = 96;
                SetBackColor(header, Color.Transparent);
                header.BringToFront();
            }

            if (brand != null)
            {
                brand.Text = StoreName;
                brand.Font = new Font("Segoe UI Semibold", 25F, FontStyle.Bold);
                brand.ForeColor = Accent;
                brand.AutoSize = true;
                brand.Location = new Point(54, 32);
            }

            if (logo != null)
            {
                SetBackColor(logo, Color.Transparent);
                logo.Image = StoreLogo;
                logo.SizeMode = PictureBoxSizeMode.Zoom;
                logo.SetBounds(380, 58, 190, 190);
                logo.Visible = logo.Image != null;
                logo.BringToFront();
            }

            if (close != null)
            {
                close.Location = new Point(form.ClientSize.Width - 42, 16);
                close.Size = new Size(26, 26);
                close.Visible = true;
            }

            int cardX = 292;
            int top = 276;

            if (prompt != null)
            {
                prompt.Text = "Welcome back";
                prompt.Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold);
                prompt.ForeColor = Text;
                prompt.AutoSize = true;
                prompt.Location = new Point(cardX + 42, top + 34);
            }

            if (user != null)
            {
                user.SetBounds(cardX + 40, top + 104, 326, 42);
                StyleLoginInput(user);
            }

            if (pass != null)
            {
                pass.SetBounds(cardX + 40, top + 160, 326, 42);
                StyleLoginInput(pass);
            }

            if (login != null)
                login.SetBounds(cardX + 40, top + 232, 326, 44);

            if (cancel != null)
                cancel.SetBounds(cardX + 40, top + 286, 326, 44);

            if (dbConfig != null)
                dbConfig.SetBounds(40, form.ClientSize.Height - 54, 180, 36);

            if (copyright != null)
            {
                copyright.Text = "Secure sales workspace";
                copyright.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
                copyright.ForeColor = MutedText;
                copyright.AutoSize = true;
                copyright.Location = new Point(form.ClientSize.Width - 206, form.ClientSize.Height - 36);
            }
        }

        private static void EnhanceSplash(Form form)
        {
            form.BackColor = Sidebar;
            form.Paint -= PaintSplashBackground;
            form.Paint += PaintSplashBackground;

            Label loading = FindControl<Label>(form, "label1");
            Label welcome = FindControl<Label>(form, "label2");
            Label studio = FindControl<Label>(form, "label3");
            Label title = FindControl<Label>(form, "label4");
            Panel progress = FindControl<Panel>(form, "panel1");
            Panel progressTrack = FindControl<Panel>(form, "panel2");
            PictureBox logo = FindControl<PictureBox>(form, "pictureBox1");
            if (logo != null)
            {
                SetBackColor(logo, Color.Transparent);
                logo.Image = StoreLogo;
                logo.SizeMode = PictureBoxSizeMode.Zoom;
                logo.SetBounds(302, 74, 238, 210);
                logo.Visible = true;
            }

            if (title != null)
            {
                title.Text = StoreName;
                title.Font = new Font("Segoe UI Semibold", 25F, FontStyle.Bold);
                title.ForeColor = Accent;
                title.AutoSize = true;
                title.Location = new Point(Math.Max(24, (form.ClientSize.Width - title.Width) / 2), 334);
            }

            if (welcome != null)
            {
                welcome.Text = "Welcome to " + StoreName;
                welcome.Font = new Font("Segoe UI", 13F, FontStyle.Regular);
                welcome.ForeColor = Color.FromArgb(214, 224, 235);
                welcome.AutoSize = true;
                welcome.Location = new Point(Math.Max(24, (form.ClientSize.Width - welcome.Width) / 2), 392);
            }

            if (loading != null)
            {
                loading.Text = "LOADING";
                loading.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
                loading.ForeColor = Primary;
                loading.Location = new Point(24, 456);
            }

            if (studio != null)
            {
                studio.Text = "Restaurant and Bar";
                studio.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
                studio.ForeColor = Color.FromArgb(175, 189, 203);
                studio.AutoSize = true;
                studio.Location = new Point(Math.Max(24, form.ClientSize.Width - studio.Width - 46), 460);
            }

            if (progressTrack != null)
            {
                progressTrack.SetBounds(0, 492, form.ClientSize.Width, 16);
                progressTrack.BackColor = Color.FromArgb(31, 45, 60);
            }

            if (progress != null)
                progress.BackColor = Primary;
        }

        private static void StyleLoginInput(Control input)
        {
            input.BackColor = Color.White;
            input.Padding = new Padding(0);
            SetProperty(input, "BorderStyle", BorderStyle.FixedSingle);
            SetProperty(input, "UseStyleColors", false);
            SetProperty(input, "DisplayIcon", false);
            SetProperty(input, "Icon", null);
        }

        private static void EnhanceDashboard(Form form)
        {
            form.BackColor = AppBackground;
            PictureBox hero = FindControl<PictureBox>(form, "pictureBox5");

            if (hero != null)
            {
                hero.Visible = false;
            }

            form.Paint -= PaintDashboardCanvas;
            form.Paint += PaintDashboardCanvas;
        }

        private static void EnhanceUserAccount(Form form)
        {
            Panel footer = FindControl<Panel>(form, "panel1");
            Label footerTitle = FindControl<Label>(form, "label1");
            MetroFramework.Controls.MetroTabControl tabs = FindControl<MetroFramework.Controls.MetroTabControl>(form, "metroTabControl1");
            DataGridView grid = FindControl<DataGridView>(form, "dgvUser");
            GroupBox passwordBox = FindControl<GroupBox>(form, "gbUser");

            form.BackColor = AppBackground;

            if (tabs != null)
            {
                tabs.BackColor = AppBackground;
                tabs.UseSelectable = true;
                tabs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

                foreach (Control page in tabs.Controls)
                    page.BackColor = Surface;
            }

            if (grid != null)
            {
                grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                grid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                grid.Columns[0].Width = 58;
                grid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                grid.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                grid.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                grid.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            if (passwordBox != null)
            {
                passwordBox.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
                passwordBox.ForeColor = Text;
                passwordBox.BackColor = Surface;
            }

            if (footer != null)
            {
                footer.Height = 68;
                footer.BackColor = PrimaryDark;
            }

            if (footerTitle != null)
            {
                footerTitle.Image = null;
                footerTitle.Text = "User";
                footerTitle.AutoSize = false;
                footerTitle.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
                footerTitle.ForeColor = Color.White;
                footerTitle.TextAlign = ContentAlignment.MiddleLeft;
            }

            LayoutUserAccount(form);
            form.Resize -= UserAccount_Resize;
            form.Resize += UserAccount_Resize;
        }

        private static void LayoutUserAccount(Form form)
        {
            Panel footer = FindControl<Panel>(form, "panel1");
            Label footerTitle = FindControl<Label>(form, "label1");
            MetroFramework.Controls.MetroTabControl tabs = FindControl<MetroFramework.Controls.MetroTabControl>(form, "metroTabControl1");

            if (footer != null)
                footer.Height = 68;

            if (footerTitle != null)
                footerTitle.SetBounds(32, 16, 180, 34);

            int margin = 38;
            int bottomGap = footer == null ? 26 : footer.Height + 26;
            if (tabs != null)
            {
                tabs.SetBounds(margin, 24, Math.Max(520, form.ClientSize.Width - (margin * 2)),
                    Math.Max(360, form.ClientSize.Height - bottomGap - 24));
            }

            LayoutUserCreateTab(form);
            LayoutUserPasswordTab(form);
            LayoutUserActivationTab(form);
        }

        private static void LayoutUserCreateTab(Form form)
        {
            Control page = FindControl<Control>(form, "metroTabPage1");
            if (page == null)
                return;

            int width = Math.Max(620, page.ClientSize.Width);
            int fieldWidth = Math.Min(560, Math.Max(360, width - 360));
            int labelWidth = 152;
            int startX = Math.Max(48, (width - labelWidth - fieldWidth - 18) / 2);
            int inputX = startX + labelWidth + 18;
            int y = 58;
            int gap = 52;

            LayoutLabel(FindControl<Label>(form, "label2"), startX, y, labelWidth, "Username :");
            LayoutInput(FindControl<Control>(form, "txtUsername"), inputX, y - 4, fieldWidth);
            LayoutLabel(FindControl<Label>(form, "label3"), startX, y + gap, labelWidth, "Password :");
            LayoutInput(FindControl<Control>(form, "txtPass"), inputX, y + gap - 4, fieldWidth);
            LayoutLabel(FindControl<Label>(form, "label4"), startX, y + (gap * 2), labelWidth, "Re-type Password :");
            LayoutInput(FindControl<Control>(form, "txtRePass"), inputX, y + (gap * 2) - 4, fieldWidth);
            LayoutLabel(FindControl<Label>(form, "label5"), startX, y + (gap * 3), labelWidth, "Role :");
            LayoutInput(FindControl<Control>(form, "cbRole"), inputX, y + (gap * 3) - 4, fieldWidth);
            LayoutLabel(FindControl<Label>(form, "label6"), startX, y + (gap * 4), labelWidth, "Full Name :");
            LayoutInput(FindControl<Control>(form, "txtName"), inputX, y + (gap * 4) - 4, fieldWidth);

            Button save = FindControl<Button>(form, "btnAccSave");
            Button cancel = FindControl<Button>(form, "btnAccCancel");
            int buttonY = Math.Max(y + (gap * 5) + 8, page.ClientSize.Height - 60);
            if (cancel != null)
                cancel.SetBounds(inputX + fieldWidth - 106, buttonY, 106, 40);
            if (save != null)
                save.SetBounds(inputX + fieldWidth - 224, buttonY, 106, 40);
        }

        private static void LayoutUserPasswordTab(Form form)
        {
            Control page = FindControl<Control>(form, "metroTabPage2");
            if (page == null)
                return;

            int width = Math.Max(620, page.ClientSize.Width);
            int fieldWidth = Math.Min(560, Math.Max(360, width - 360));
            int labelWidth = 164;
            int startX = Math.Max(48, (width - labelWidth - fieldWidth - 18) / 2);
            int inputX = startX + labelWidth + 18;
            int y = 118;
            int gap = 58;

            PictureBox userIcon = FindControl<PictureBox>(form, "pictureBox1");
            Label userName = FindControl<Label>(form, "lblUsername");
            if (userIcon != null)
            {
                userIcon.SetBounds(startX, 38, 52, 52);
                userIcon.SizeMode = PictureBoxSizeMode.Zoom;
            }
            if (userName != null)
            {
                userName.AutoSize = false;
                userName.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
                userName.ForeColor = Text;
                userName.SetBounds(startX + 68, 50, 260, 32);
            }

            LayoutLabel(FindControl<Label>(form, "label8"), startX, y, labelWidth, "Current Password :");
            LayoutInput(FindControl<Control>(form, "txtCurPass"), inputX, y - 4, fieldWidth);
            LayoutLabel(FindControl<Label>(form, "label9"), startX, y + gap, labelWidth, "New Password :");
            LayoutInput(FindControl<Control>(form, "txtNPass"), inputX, y + gap - 4, fieldWidth);
            LayoutLabel(FindControl<Label>(form, "label10"), startX, y + (gap * 2), labelWidth, "Re-type Password :");
            LayoutInput(FindControl<Control>(form, "txtRePass2"), inputX, y + (gap * 2) - 4, fieldWidth);

            Button save = FindControl<Button>(form, "btnPassSave");
            Button cancel = FindControl<Button>(form, "btnPassCancel");
            int buttonY = Math.Max(y + (gap * 3), page.ClientSize.Height - 60);
            if (cancel != null)
                cancel.SetBounds(inputX + fieldWidth - 106, buttonY, 106, 40);
            if (save != null)
                save.SetBounds(inputX + fieldWidth - 224, buttonY, 106, 40);
        }

        private static void LayoutUserActivationTab(Form form)
        {
            Control page = FindControl<Control>(form, "metroTabPage3");
            DataGridView grid = FindControl<DataGridView>(form, "dgvUser");
            Button remove = FindControl<Button>(form, "btnRemove");
            Button properties = FindControl<Button>(form, "btnProperties");
            GroupBox passwordBox = FindControl<GroupBox>(form, "gbUser");
            PictureBox icon = FindControl<PictureBox>(form, "pictureBox2");
            Label note = FindControl<Label>(form, "lblAccNote");
            Button reset = FindControl<Button>(form, "btnResetPass");

            if (page == null)
                return;

            int margin = 16;
            int pageWidth = Math.Max(620, page.ClientSize.Width);
            int pageHeight = Math.Max(360, page.ClientSize.Height);
            int groupHeight = 108;
            int groupY = Math.Max(250, pageHeight - groupHeight - 14);
            int buttonY = groupY - 54;
            int gridHeight = Math.Max(170, buttonY - 18);

            if (grid != null)
                grid.SetBounds(margin, 8, pageWidth - (margin * 2), gridHeight);

            if (properties != null)
                properties.SetBounds(pageWidth - margin - 116, buttonY, 116, 40);
            if (remove != null)
                remove.SetBounds(pageWidth - margin - 238, buttonY, 116, 40);

            if (passwordBox != null)
                passwordBox.SetBounds(margin, groupY, pageWidth - (margin * 2), groupHeight);

            int groupWidth = passwordBox == null ? pageWidth - (margin * 2) : passwordBox.ClientSize.Width;
            if (icon != null)
                icon.SetBounds(22, 38, 42, 42);
            if (note != null)
            {
                note.AutoSize = false;
                note.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
                note.ForeColor = Text;
                note.TextAlign = ContentAlignment.MiddleLeft;
                note.SetBounds(86, 30, Math.Max(260, groupWidth - 340), 54);
            }
            if (reset != null)
                reset.SetBounds(Math.Max(86, groupWidth - 240), 54, 220, 40);
        }

        private static void LayoutInput(Control input, int x, int y, int width)
        {
            if (input == null)
                return;

            input.SetBounds(x, y, width, 34);
            input.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        }

        private static void UserAccount_Resize(object sender, EventArgs e)
        {
            Form form = sender as Form;
            if (form != null)
                LayoutUserAccount(form);
        }

        private static void EnhanceSupplier(Form form)
        {
            Panel footer = FindControl<Panel>(form, "panel1");
            Label footerTitle = FindControl<Label>(form, "label1");
            Button add = FindControl<Button>(form, "btnAdd");
            DataGridView grid = FindControl<DataGridView>(form, "dgvSupplier");

            form.BackColor = AppBackground;

            if (grid != null)
            {
                grid.Dock = DockStyle.None;
                grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

                SetColumnWidth(grid, "Column1", 58);
                SetColumnWidth(grid, "Column2", 142);
                SetColumnWidth(grid, "Column4", 150);
                SetColumnWidth(grid, "Column5", 116);
                SetColumnWidth(grid, "Column6", 158);
                SetColumnWidth(grid, "Column7", 116);
                SetColumnWidth(grid, "Edit", 74);
                SetColumnWidth(grid, "Delete", 86);

                if (grid.Columns.Contains("Column3"))
                    grid.Columns["Column3"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                if (grid.Columns.Contains("Edit"))
                    grid.Columns["Edit"].HeaderText = "Edit";
                if (grid.Columns.Contains("Delete"))
                    grid.Columns["Delete"].HeaderText = "Remove";
            }

            if (footer != null)
            {
                footer.Height = 68;
                footer.BackColor = PrimaryDark;
            }

            if (footerTitle != null)
            {
                footerTitle.Image = null;
                footerTitle.Text = "Manage Supplier";
                footerTitle.AutoSize = false;
                footerTitle.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
                footerTitle.ForeColor = Color.White;
                footerTitle.TextAlign = ContentAlignment.MiddleLeft;
            }

            if (add != null)
            {
                add.Text = "Add Supplier";
                add.Image = null;
                add.AutoSize = false;
                add.TextAlign = ContentAlignment.MiddleCenter;
            }

            LayoutSupplier(form);
            form.Resize -= Supplier_Resize;
            form.Resize += Supplier_Resize;
        }

        private static void LayoutSupplier(Form form)
        {
            Panel footer = FindControl<Panel>(form, "panel1");
            Label footerTitle = FindControl<Label>(form, "label1");
            Button add = FindControl<Button>(form, "btnAdd");
            DataGridView grid = FindControl<DataGridView>(form, "dgvSupplier");

            int margin = 20;
            int footerHeight = footer == null ? 0 : 68;

            if (footer != null)
                footer.Height = footerHeight;

            if (grid != null)
                grid.SetBounds(margin, 14, Math.Max(520, form.ClientSize.Width - (margin * 2)),
                    Math.Max(260, form.ClientSize.Height - footerHeight - 28));

            if (footerTitle != null)
                footerTitle.SetBounds(32, 16, 260, 34);

            if (add != null)
                add.SetBounds(Math.Max(32, form.ClientSize.Width - 172), 14, 140, 40);
        }

        private static void Supplier_Resize(object sender, EventArgs e)
        {
            Form form = sender as Form;
            if (form != null)
                LayoutSupplier(form);
        }

        private static void SetColumnWidth(DataGridView grid, string name, int width)
        {
            if (grid == null || !grid.Columns.Contains(name))
                return;

            grid.Columns[name].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            grid.Columns[name].Width = width;
        }

        private static void EnhanceProduct(Form form)
        {
            Panel footer = FindControl<Panel>(form, "panel1");
            Label footerTitle = FindControl<Label>(form, "label1");
            Label sample = FindControl<Label>(form, "sample_file_download");
            Button add = FindControl<Button>(form, "btnAdd");
            Button addMultiple = FindControl<Button>(form, "btn_add_multiple");
            DataGridView grid = FindControl<DataGridView>(form, "dgvProduct");

            form.BackColor = AppBackground;

            if (grid != null)
            {
                grid.Dock = DockStyle.None;
                grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

                SetColumnWidth(grid, "Column1", 58);
                SetColumnWidth(grid, "Column2", 82);
                SetColumnWidth(grid, "Column3", 126);
                SetColumnWidth(grid, "Column5", 110);
                SetColumnWidth(grid, "Column6", 120);
                SetColumnWidth(grid, "Column7", 100);
                SetColumnWidth(grid, "Column8", 96);
                SetColumnWidth(grid, "Edit", 74);
                SetColumnWidth(grid, "Delete", 86);

                if (grid.Columns.Contains("Column4"))
                    grid.Columns["Column4"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                if (grid.Columns.Contains("Edit"))
                    grid.Columns["Edit"].HeaderText = "Edit";
                if (grid.Columns.Contains("Delete"))
                    grid.Columns["Delete"].HeaderText = "Remove";
            }

            if (footer != null)
            {
                footer.Height = 86;
                footer.BackColor = PrimaryDark;
            }

            if (footerTitle != null)
            {
                footerTitle.Image = null;
                footerTitle.Text = "Manage Product";
                footerTitle.AutoSize = false;
                footerTitle.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
                footerTitle.ForeColor = Color.White;
                footerTitle.TextAlign = ContentAlignment.MiddleLeft;
            }

            if (sample != null)
            {
                sample.AutoSize = false;
                sample.Text = "Download sample CSV";
                sample.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold | FontStyle.Underline);
                sample.ForeColor = Color.FromArgb(244, 214, 142);
                sample.TextAlign = ContentAlignment.MiddleLeft;
            }

            if (add != null)
            {
                add.Image = null;
                add.Text = "Add Product";
                add.AutoSize = false;
            }

            if (addMultiple != null)
            {
                addMultiple.Image = null;
                addMultiple.Text = "Add Multiple";
                addMultiple.AutoSize = false;
            }

            LayoutProduct(form);
            form.Resize -= Product_Resize;
            form.Resize += Product_Resize;
        }

        private static void LayoutProduct(Form form)
        {
            Panel footer = FindControl<Panel>(form, "panel1");
            Label footerTitle = FindControl<Label>(form, "label1");
            Label sample = FindControl<Label>(form, "sample_file_download");
            Button add = FindControl<Button>(form, "btnAdd");
            Button addMultiple = FindControl<Button>(form, "btn_add_multiple");
            Control search = FindControl<Control>(form, "txtSearch");
            DataGridView grid = FindControl<DataGridView>(form, "dgvProduct");

            int margin = 20;
            int footerHeight = footer == null ? 0 : 86;

            if (footer != null)
                footer.Height = footerHeight;

            if (grid != null)
                grid.SetBounds(margin, 14, Math.Max(520, form.ClientSize.Width - (margin * 2)),
                    Math.Max(260, form.ClientSize.Height - footerHeight - 28));

            if (footerTitle != null)
                footerTitle.SetBounds(32, 16, 230, 30);

            if (sample != null)
                sample.SetBounds(32, 52, 220, 24);

            int buttonWidth = 130;
            int gap = 14;
            int right = form.ClientSize.Width - 32;

            if (add != null)
                add.SetBounds(Math.Max(32, right - buttonWidth), 24, buttonWidth, 40);

            if (addMultiple != null)
                addMultiple.SetBounds(Math.Max(32, right - (buttonWidth * 2) - gap), 24, buttonWidth, 40);

            if (search != null)
            {
                int leftLimit = 280;
                int rightLimit = Math.Max(leftLimit + 260, right - (buttonWidth * 2) - (gap * 2) - 18);
                int searchWidth = Math.Min(360, Math.Max(260, rightLimit - leftLimit));
                search.SetBounds(leftLimit, 25, searchWidth, 36);
            }
        }

        private static void Product_Resize(object sender, EventArgs e)
        {
            Form form = sender as Form;
            if (form != null)
                LayoutProduct(form);
        }

        private static void EnhanceProductModule(Form form)
        {
            EnhanceModuleTitleBar(form, "Product Module");
        }

        private static void EnhanceModuleTitleBar(Form form, string titleText)
        {
            Panel header = FindControl<Panel>(form, "panel1");
            Label title = FindControl<Label>(form, "label1");
            PictureBox close = FindControl<PictureBox>(form, "picClose");
            Button minimize = EnsureCaptionButton(form, "modernMinimizeButton", "-", CaptionMinimize_Click);
            Button maximize = EnsureCaptionButton(form, "modernMaximizeButton", "□", CaptionMaximize_Click);
            Button closeButton = EnsureCaptionButton(form, "modernCloseButton", "X", CaptionClose_Click);

            form.BackColor = AppBackground;
            form.Padding = new Padding(1);

            if (header != null)
            {
                header.Visible = true;
                header.Dock = DockStyle.Top;
                header.Height = 46;
                header.BackColor = PrimaryDark;
                header.Paint -= PaintHeader;
            }

            if (title != null)
            {
                title.Visible = true;
                title.AutoSize = false;
                title.Text = titleText;
                title.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
                title.ForeColor = Color.White;
                title.TextAlign = ContentAlignment.MiddleLeft;
            }

            if (close != null)
                close.Visible = false;

            StyleCaptionButton(minimize, PrimaryDark, Color.White);
            StyleCaptionButton(maximize, PrimaryDark, Color.White);
            StyleCaptionButton(closeButton, Danger, Color.White);

            LayoutProductModule(form);
            form.Resize -= ProductModule_Resize;
            form.Resize += ProductModule_Resize;
        }

        private static void LayoutProductModule(Form form)
        {
            Panel header = FindControl<Panel>(form, "panel1");
            Label title = FindControl<Label>(form, "label1");
            PictureBox close = FindControl<PictureBox>(form, "picClose");
            Button minimize = FindControl<Button>(form, "modernMinimizeButton");
            Button maximize = FindControl<Button>(form, "modernMaximizeButton");
            Button closeButton = FindControl<Button>(form, "modernCloseButton");

            if (header != null)
            {
                header.Height = 46;
                header.BackColor = PrimaryDark;
                header.BringToFront();
            }

            if (title != null)
                title.SetBounds(20, 8, Math.Max(160, form.ClientSize.Width - 168), 30);

            if (close != null)
                close.Visible = false;

            int right = form.ClientSize.Width - 1;
            LayoutCaptionButton(closeButton, right - 44, Danger);
            LayoutCaptionButton(maximize, right - 88, PrimaryDark);
            LayoutCaptionButton(minimize, right - 132, PrimaryDark);
        }

        private static Button EnsureCaptionButton(Form form, string name, string text, EventHandler click)
        {
            Button button = FindControl<Button>(form, name);
            if (button == null)
            {
                button = new Button
                {
                    Name = name,
                    Text = text
                };
                form.Controls.Add(button);
            }

            button.Click -= click;
            button.Click += click;
            return button;
        }

        private static bool IsCaptionButton(Control control)
        {
            if (control == null || string.IsNullOrEmpty(control.Name))
                return false;

            return control.Name.Equals("modernMinimizeButton", StringComparison.OrdinalIgnoreCase) ||
                control.Name.Equals("modernMaximizeButton", StringComparison.OrdinalIgnoreCase) ||
                control.Name.Equals("modernCloseButton", StringComparison.OrdinalIgnoreCase);
        }

        private static void StyleCaptionButton(Button button, Color backColor, Color foreColor)
        {
            if (button == null)
                return;

            button.Visible = true;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.18F);
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.12F);
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.Cursor = Cursors.Hand;
            button.TabStop = false;
            button.BringToFront();
        }

        private static void LayoutCaptionButton(Button button, int x, Color backColor)
        {
            if (button == null)
                return;

            button.SetBounds(Math.Max(0, x), 0, 44, 46);
            button.BackColor = backColor;
            button.BringToFront();
        }

        private static void CaptionMinimize_Click(object sender, EventArgs e)
        {
            Form form = (sender as Control)?.FindForm();
            if (form != null)
                form.WindowState = FormWindowState.Minimized;
        }

        private static void CaptionMaximize_Click(object sender, EventArgs e)
        {
            Form form = (sender as Control)?.FindForm();
            if (form == null)
                return;

            form.WindowState = form.WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal
                : FormWindowState.Maximized;
        }

        private static void CaptionClose_Click(object sender, EventArgs e)
        {
            Form form = (sender as Control)?.FindForm();
            if (form != null)
                form.Close();
        }

        private static void ProductModule_Resize(object sender, EventArgs e)
        {
            Form form = sender as Form;
            if (form != null)
                LayoutProductModule(form);
        }

        private static void EnhanceAdjustments(Form form)
        {
            Panel details = FindControl<Panel>(form, "panel2");
            Panel bottom = FindControl<Panel>(form, "panel1");
            DataGridView grid = FindControl<DataGridView>(form, "dgvAdjustment");

            if (details != null)
            {
                details.Height = 172;
                details.Padding = new Padding(22, 18, 22, 18);
                details.BackColor = Color.FromArgb(253, 251, 244);
                details.Paint -= PaintCard;
                details.Paint -= PaintAdjustmentCard;
                details.Paint += PaintAdjustmentCard;
            }

            if (grid != null)
            {
                grid.Location = new Point(0, details == null ? 0 : details.Height);
                grid.Dock = DockStyle.Fill;
            }

            LayoutAdjustments(form);
            form.Resize -= AdjustmentsForm_Resize;
            form.Resize += AdjustmentsForm_Resize;
        }

        private static void LayoutAdjustments(Form form)
        {
            Panel details = FindControl<Panel>(form, "panel2");
            Panel bottom = FindControl<Panel>(form, "panel1");
            Button save = FindControl<Button>(form, "btnSave");
            Control search = FindControl<Control>(form, "txtSearch");
            Label footerTitle = FindControl<Label>(form, "label1");
            Label lblUsername = FindControl<Label>(form, "lblUsername");
            Label label2 = FindControl<Label>(form, "label2");
            Label label3 = FindControl<Label>(form, "label3");
            Label label4 = FindControl<Label>(form, "label4");
            Label label5 = FindControl<Label>(form, "label5");
            Label label6 = FindControl<Label>(form, "label6");
            Label label7 = FindControl<Label>(form, "label7");
            Label lblRefNo = FindControl<Label>(form, "lblRefNo");
            Label lblDesc = FindControl<Label>(form, "lblDesc");
            Label lblPcode = FindControl<Label>(form, "lblPcode");
            ComboBox cbAction = FindControl<ComboBox>(form, "cbAction");
            TextBox txtQty = FindControl<TextBox>(form, "txtQty");
            TextBox txtRemark = FindControl<TextBox>(form, "txtRemark");

            if (lblUsername != null)
            {
                lblUsername.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
                lblUsername.ForeColor = Text;
                lblUsername.SetBounds(24, 22, 240, 28);
                lblUsername.AutoSize = false;
            }

            int contentWidth = Math.Max(760, details == null ? form.ClientSize.Width : details.ClientSize.Width);
            int leftLabel = 24;
            int leftValue = 134;
            int rightLabel = Math.Max(510, contentWidth - 392);
            int rightValue = rightLabel + 94;
            int inputWidth = Math.Max(230, contentWidth - rightValue - 34);
            int leftValueWidth = Math.Max(260, rightLabel - leftValue - 28);

            LayoutLabel(label2, leftLabel, 72, 104, "Reference No :");
            LayoutValue(lblRefNo, leftValue, 72, leftValueWidth);
            LayoutLabel(label4, leftLabel, 104, 104, "Pcode :");
            LayoutValue(lblPcode, leftValue, 104, leftValueWidth);
            LayoutLabel(label3, leftLabel, 136, 104, "Description :");
            LayoutValue(lblDesc, leftValue, 136, leftValueWidth);

            LayoutLabel(label5, rightLabel, 32, 86, "Action :");
            LayoutLabel(label6, rightLabel, 76, 86, "Qty :");
            LayoutLabel(label7, rightLabel, 120, 86, "Remarks :");

            if (cbAction != null)
            {
                cbAction.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                cbAction.BackColor = Color.FromArgb(255, 253, 248);
                cbAction.ForeColor = Text;
                cbAction.FlatStyle = FlatStyle.Standard;
                cbAction.SetBounds(rightValue, 28, inputWidth, 34);
            }
            if (txtQty != null)
            {
                txtQty.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                txtQty.BackColor = Color.FromArgb(255, 253, 248);
                txtQty.ForeColor = Text;
                txtQty.BorderStyle = BorderStyle.FixedSingle;
                txtQty.SetBounds(rightValue, 72, inputWidth, 34);
            }
            if (txtRemark != null)
            {
                txtRemark.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                txtRemark.BackColor = Color.FromArgb(255, 253, 248);
                txtRemark.ForeColor = Text;
                txtRemark.BorderStyle = BorderStyle.FixedSingle;
                txtRemark.SetBounds(rightValue, 116, inputWidth, 34);
            }

            if (bottom != null)
            {
                bottom.Height = 64;
                bottom.BackColor = PrimaryDark;
                bottom.Resize -= AdjustmentsBottom_Resize;
                bottom.Resize += AdjustmentsBottom_Resize;
            }

            if (footerTitle != null)
            {
                footerTitle.Text = "Stock";
                footerTitle.SetBounds(24, 14, 150, 34);
                footerTitle.AutoSize = false;
            }

            if (search != null)
            {
                search.Anchor = AnchorStyles.Bottom;
                search.SetBounds(Math.Max(190, (form.ClientSize.Width - 330) / 2), 14, 330, 36);
            }

            if (save != null)
            {
                save.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                save.SetBounds(Math.Max(0, form.ClientSize.Width - 118), 12, 96, 40);
            }
        }

        private static void AdjustmentsForm_Resize(object sender, EventArgs e)
        {
            Form form = sender as Form;
            if (form != null)
                LayoutAdjustments(form);
        }

        private static void AdjustmentsBottom_Resize(object sender, EventArgs e)
        {
            Panel panel = sender as Panel;
            if (panel == null)
                return;

            Control form = panel.FindForm();
            Control search = FindControl<Control>(panel, "txtSearch");
            Button save = FindControl<Button>(panel, "btnSave");

            if (form != null && search != null)
                search.SetBounds(Math.Max(190, (form.ClientSize.Width - 330) / 2), 14, 330, 36);

            if (form != null && save != null)
                save.SetBounds(Math.Max(0, form.ClientSize.Width - 118), 12, 96, 40);
        }

        private static void EnhanceStockIn(Form form)
        {
            Panel footer = FindControl<Panel>(form, "panel1");
            MetroFramework.Controls.MetroTabControl tabs = FindControl<MetroFramework.Controls.MetroTabControl>(form, "metroTabControl1");
            Panel entryPanel = FindControl<Panel>(form, "panel2");
            Panel historyPanel = FindControl<Panel>(form, "panel3");
            DataGridView entryGrid = FindControl<DataGridView>(form, "dgvStockIn");
            DataGridView historyGrid = FindControl<DataGridView>(form, "dgvInStockHistory");

            form.BackColor = AppBackground;

            if (tabs != null)
            {
                tabs.BackColor = AppBackground;
                tabs.Padding = new Point(12, 8);
                tabs.UseSelectable = true;
            }

            if (entryPanel != null)
            {
                entryPanel.Height = 190;
                entryPanel.Padding = new Padding(22, 18, 22, 18);
                entryPanel.BackColor = Surface;
                entryPanel.Paint -= PaintCard;
                entryPanel.Paint += PaintCard;
            }

            if (historyPanel != null)
            {
                historyPanel.Height = 76;
                historyPanel.BackColor = Surface;
            }

            if (entryGrid != null)
                entryGrid.Dock = DockStyle.Fill;

            if (historyGrid != null)
                historyGrid.Dock = DockStyle.Fill;

            if (footer != null)
            {
                footer.Height = 64;
                footer.BackColor = PrimaryDark;
            }

            LayoutStockIn(form);
            form.Resize -= StockInForm_Resize;
            form.Resize += StockInForm_Resize;
        }

        private static void LayoutStockIn(Form form)
        {
            Panel entryPanel = FindControl<Panel>(form, "panel2");
            Panel historyPanel = FindControl<Panel>(form, "panel3");
            Panel footer = FindControl<Panel>(form, "panel1");
            Button entryButton = FindControl<Button>(form, "btnEntry");
            Button loadButton = FindControl<Button>(form, "btnLoad");
            Label footerTitle = FindControl<Label>(form, "label1");

            Label refLabel = FindControl<Label>(form, "label2");
            Label byLabel = FindControl<Label>(form, "label3");
            Label dateLabel = FindControl<Label>(form, "label4");
            Label supplierLabel = FindControl<Label>(form, "label5");
            Label contactLabel = FindControl<Label>(form, "label6");
            Label addressLabel = FindControl<Label>(form, "label7");
            Label historyFilterLabel = FindControl<Label>(form, "label8");
            Label historyToLabel = FindControl<Label>(form, "label9");
            Label supplierId = FindControl<Label>(form, "lblId");

            TextBox refText = FindControl<TextBox>(form, "txtRefNo");
            TextBox stockInBy = FindControl<TextBox>(form, "txtStockInBy");
            DateTimePicker stockInDate = FindControl<DateTimePicker>(form, "dtStockIn");
            ComboBox supplier = FindControl<ComboBox>(form, "cbSupplier");
            TextBox contact = FindControl<TextBox>(form, "txtConPerson");
            TextBox address = FindControl<TextBox>(form, "txtAddress");
            LinkLabel generate = FindControl<LinkLabel>(form, "LinGenerate");
            LinkLabel browse = FindControl<LinkLabel>(form, "LinProduct");
            DateTimePicker from = FindControl<DateTimePicker>(form, "dtFrom");
            DateTimePicker to = FindControl<DateTimePicker>(form, "dtTo");

            int width = Math.Max(760, entryPanel == null ? form.ClientSize.Width : entryPanel.ClientSize.Width);
            int leftLabel = 28;
            int leftInput = 150;
            int rightLabel = Math.Max(466, width - 476);
            int rightInput = rightLabel + 116;
            int leftInputWidth = Math.Max(250, rightLabel - leftInput - 34);
            int rightInputWidth = Math.Max(260, width - rightInput - 34);

            LayoutLabel(refLabel, leftLabel, 30, 118, "Reference No :");
            LayoutLabel(byLabel, leftLabel, 74, 118, "Stock In By :");
            LayoutLabel(dateLabel, leftLabel, 118, 118, "Stock In Date :");
            LayoutLabel(supplierLabel, rightLabel, 30, 108, "Supplier :");
            LayoutLabel(contactLabel, rightLabel, 74, 108, "Contact :");
            LayoutLabel(addressLabel, rightLabel, 118, 108, "Address :");

            if (refText != null)
                refText.SetBounds(leftInput, 28, Math.Min(170, leftInputWidth - 96), 34);
            if (generate != null)
            {
                generate.Text = "Generate";
                generate.AutoSize = false;
                generate.LinkColor = Primary;
                generate.ActiveLinkColor = PrimaryDark;
                generate.VisitedLinkColor = Primary;
                generate.SetBounds(leftInput + 184, 33, 86, 26);
            }
            if (stockInBy != null)
                stockInBy.SetBounds(leftInput, 72, leftInputWidth, 34);
            if (stockInDate != null)
                stockInDate.SetBounds(leftInput, 116, leftInputWidth, 34);

            if (supplier != null)
                supplier.SetBounds(rightInput, 28, rightInputWidth, 34);
            if (contact != null)
                contact.SetBounds(rightInput, 72, rightInputWidth, 34);
            if (address != null)
                address.SetBounds(rightInput, 116, rightInputWidth, 34);

            if (browse != null)
            {
                browse.Text = "Browse Product";
                browse.AutoSize = false;
                browse.LinkColor = Primary;
                browse.ActiveLinkColor = PrimaryDark;
                browse.VisitedLinkColor = Primary;
                browse.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
                browse.SetBounds(leftInput, 154, 180, 26);
            }

            if (supplierId != null)
                supplierId.Visible = false;

            if (entryButton != null)
            {
                entryButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                Control parent = entryButton.Parent ?? form;
                entryButton.SetBounds(Math.Max(0, parent.ClientSize.Width - 124), Math.Max(0, parent.ClientSize.Height - 54), 104, 42);
                entryButton.BringToFront();
            }

            if (footerTitle != null)
            {
                footerTitle.Text = "Stock In";
                footerTitle.SetBounds(24, 14, 180, 34);
                footerTitle.AutoSize = false;
                footerTitle.TextAlign = ContentAlignment.MiddleLeft;
            }

            if (footer != null)
                footer.Height = 64;

            if (historyPanel != null)
            {
                LayoutLabel(historyFilterLabel, 24, 24, 150, "Filter By Date : From");
                if (from != null)
                    from.SetBounds(178, 20, 116, 34);
                LayoutLabel(historyToLabel, 310, 24, 28, "To");
                if (to != null)
                    to.SetBounds(342, 20, 116, 34);
                if (loadButton != null)
                {
                    loadButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    loadButton.Text = "Load Data";
                    loadButton.SetBounds(480, 16, 128, 40);
                }
            }
        }

        private static void StockInForm_Resize(object sender, EventArgs e)
        {
            Form form = sender as Form;
            if (form != null)
                LayoutStockIn(form);
        }

        private static void EnhanceCashier(Form form)
        {
            Panel sidebar = FindControl<Panel>(form, "panel1");
            Panel userPanel = FindControl<Panel>(form, "panel2");
            Panel divider = FindControl<Panel>(form, "panel3");
            Panel header = FindControl<Panel>(form, "panel4");
            Panel summary = FindControl<Panel>(form, "panel5");
            Panel activeBar = FindControl<Panel>(form, "panelSlide");
            DataGridView cart = FindControl<DataGridView>(form, "dgvCash");
            PictureBox logo = FindControl<PictureBox>(form, "pictureBox1");

            form.BackColor = AppBackground;

            if (sidebar != null)
            {
                sidebar.Width = 184;
                sidebar.BackColor = Sidebar;
                sidebar.Padding = new Padding(0, 8, 0, 8);
                sidebar.Paint -= PaintSidebar;
                sidebar.Paint += PaintSidebar;
            }

            if (userPanel != null)
            {
                userPanel.Height = 146;
                userPanel.BackColor = Sidebar;
            }

            if (logo != null)
            {
                logo.Image = StoreLogo;
                logo.Visible = true;
                logo.SizeMode = PictureBoxSizeMode.Zoom;
                SetBackColor(logo, Color.Transparent);
                logo.BringToFront();
            }

            if (divider != null)
            {
                divider.Width = 1;
                divider.BackColor = Border;
            }

            if (header != null)
            {
                header.Height = 58;
                header.BackColor = Header;
                header.Paint -= PaintHeader;
                header.Paint += PaintHeader;
            }

            if (summary != null)
            {
                summary.Width = 232;
                summary.BackColor = Surface;
                summary.Padding = new Padding(14, 18, 14, 18);
                summary.Paint -= PaintCard;
                summary.Paint += PaintCard;
            }

            if (activeBar != null)
            {
                activeBar.Width = 4;
                activeBar.BackColor = Primary;
            }

            if (cart != null)
            {
                cart.Dock = DockStyle.None;
                cart.Columns["colAdd"].HeaderText = "Add";
                cart.Columns["colReduce"].HeaderText = "Reduce";
                cart.Columns["Delete"].HeaderText = "Remove";
            }

            LayoutCashier(form);
            form.Resize -= CashierForm_Resize;
            form.Resize += CashierForm_Resize;
        }

        private static void LayoutCashier(Form form)
        {
            Panel sidebar = FindControl<Panel>(form, "panel1");
            Panel userPanel = FindControl<Panel>(form, "panel2");
            Panel divider = FindControl<Panel>(form, "panel3");
            Panel header = FindControl<Panel>(form, "panel4");
            Panel summary = FindControl<Panel>(form, "panel5");
            Panel activeBar = FindControl<Panel>(form, "panelSlide");
            DataGridView cart = FindControl<DataGridView>(form, "dgvCash");
            Label username = FindControl<Label>(form, "lblUsername");
            PictureBox logo = FindControl<PictureBox>(form, "pictureBox1");
            Label title = FindControl<Label>(form, "lblname");
            Label total = FindControl<Label>(form, "lblDisplayTotal");
            Label tranLabel = FindControl<Label>(form, "label2");
            Label dateLabel = FindControl<Label>(form, "label5");
            Label barcodeLabel = FindControl<Label>(form, "label6");
            Label saleLabel = FindControl<Label>(form, "label7");
            Label discountLabel = FindControl<Label>(form, "label8");
            Label vatLabel = FindControl<Label>(form, "label9");
            Label vatableLabel = FindControl<Label>(form, "label10");
            Label tran = FindControl<Label>(form, "lblTranNo");
            Label date = FindControl<Label>(form, "lblDate");
            Label sale = FindControl<Label>(form, "lblSaleTotal");
            Label discount = FindControl<Label>(form, "lblDiscount");
            Label vat = FindControl<Label>(form, "lblVat");
            Label vatable = FindControl<Label>(form, "lblVatable");
            Label timer = FindControl<Label>(form, "lblTimer");
            TextBox barcode = FindControl<TextBox>(form, "txtBarcode");
            TextBox qty = FindControl<TextBox>(form, "txtQty");

            if (userPanel != null && username != null)
            {
                if (logo != null)
                {
                    int logoWidth = Math.Max(112, Math.Min(150, userPanel.ClientSize.Width - 36));
                    logo.SetBounds((userPanel.ClientSize.Width - logoWidth) / 2, 12, logoWidth, 82);
                    logo.BringToFront();
                }

                username.AutoSize = false;
                username.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
                username.ForeColor = Color.White;
                username.TextAlign = ContentAlignment.MiddleCenter;
                username.SetBounds(16, 98, Math.Max(120, userPanel.ClientSize.Width - 32), 30);
            }

            if (title != null)
            {
                title.AutoSize = false;
                title.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
                title.ForeColor = Text;
                title.SetBounds(24, 14, 460, 30);
            }

            if (summary != null)
            {
                int x = 18;
                int valueX = 120;
                int inputWidth = Math.Max(120, summary.ClientSize.Width - 72);
                int valueWidth = Math.Max(94, summary.ClientSize.Width - valueX - 22);

                if (total != null)
                {
                    total.Dock = DockStyle.None;
                    total.BackColor = PrimaryDark;
                    total.ForeColor = Color.White;
                    total.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
                    total.SetBounds(0, 0, summary.ClientSize.Width, 58);
                }

                LayoutCashierSummaryLabel(tranLabel, x, 92, "Transaction No");
                LayoutCashierSummaryValue(tran, x, 120, inputWidth);
                LayoutCashierSummaryLabel(dateLabel, x, 162, "Date");
                LayoutCashierSummaryValue(date, x, 190, inputWidth);
                LayoutCashierSummaryLabel(barcodeLabel, x, 232, "Barcode");

                if (barcode != null)
                    barcode.SetBounds(x, 260, Math.Max(120, summary.ClientSize.Width - 86), 34);
                if (qty != null)
                    qty.SetBounds(summary.ClientSize.Width - 52, 260, 34, 34);

                LayoutCashierAmountRow(saleLabel, sale, x, valueX, 342, valueWidth, "Sales Total :");
                LayoutCashierAmountRow(discountLabel, discount, x, valueX, 376, valueWidth, "Discount :");
                LayoutCashierAmountRow(vatLabel, vat, x, valueX, 410, valueWidth, "VAT :");
                LayoutCashierAmountRow(vatableLabel, vatable, x, valueX, 444, valueWidth, "VATable :");

                if (timer != null)
                {
                    timer.Dock = DockStyle.None;
                    timer.BackColor = PrimaryDark;
                    timer.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
                    timer.SetBounds(0, Math.Max(0, summary.ClientSize.Height - 58), summary.ClientSize.Width, 58);
                }
            }

            if (sidebar != null)
            {
                foreach (Button button in GetControls<Button>(sidebar))
                {
                    if (button.Parent != sidebar)
                        continue;

                    button.Height = 46;
                    button.Padding = new Padding(22, 0, 8, 0);
                    button.TextAlign = ContentAlignment.MiddleLeft;
                }
            }

            if (activeBar != null && activeBar.Parent != null)
                activeBar.BringToFront();

            if (cart != null)
            {
                int left = divider == null ? (sidebar == null ? 0 : sidebar.Right) : divider.Right;
                int top = header == null ? 0 : header.Bottom;
                int right = summary == null ? form.ClientSize.Width : summary.Left;
                cart.Dock = DockStyle.None;
                cart.SetBounds(left, top, Math.Max(320, right - left), Math.Max(260, form.ClientSize.Height - top));
            }
        }

        private static void CashierForm_Resize(object sender, EventArgs e)
        {
            Form form = sender as Form;
            if (form != null)
                LayoutCashier(form);
        }

        private static void EnhanceSettle(Form form)
        {
            DBConnect dbcon = new DBConnect();
            bool noteEnabled = dbcon.GetSpecialNoteEnabled();

            form.FormBorderStyle = FormBorderStyle.FixedSingle;
            form.StartPosition = FormStartPosition.CenterParent;

            int formHeight = noteEnabled ? 640 : 580;
            form.ClientSize = new Size(330, formHeight);
            form.MinimumSize = new Size(330, formHeight);
            form.MaximumSize = new Size(330, formHeight);
            form.BackColor = AppBackground;

            TextBox sale = FindControl<TextBox>(form, "txtSale");
            TextBox vatable = FindControl<TextBox>(form, "txtVatable");
            TextBox vat = FindControl<TextBox>(form, "txtVat");
            TextBox cash = FindControl<TextBox>(form, "txtCash");
            TextBox change = FindControl<TextBox>(form, "txtChange");
            TextBox note = FindControl<TextBox>(form, "txtNote");
            Label noteLabel = FindControl<Label>(form, "lblNote");
            Label lblTotalAmount = FindControl<Label>(form, "lblTotalAmount");
            Label lblAmount = FindControl<Label>(form, "lblAmount");
            Label lblVat = FindControl<Label>(form, "lblVat");
            Label lblCash = FindControl<Label>(form, "lblCash");
            Label lblChange = FindControl<Label>(form, "lblChange");
            Label paymentLabel = FindControl<Label>(form, "lblPaymentType");
            ComboBox paymentType = FindControl<ComboBox>(form, "cboPaymentType");
            Button seven = FindControl<Button>(form, "btnSeven");
            Button eight = FindControl<Button>(form, "btnEight");
            Button nine = FindControl<Button>(form, "btnNine");
            Button clear = FindControl<Button>(form, "btnClear");
            Button four = FindControl<Button>(form, "btnFour");
            Button five = FindControl<Button>(form, "btnFive");
            Button six = FindControl<Button>(form, "btnSix");
            Button zero = FindControl<Button>(form, "btnZero");
            Button one = FindControl<Button>(form, "btnOne");
            Button two = FindControl<Button>(form, "btnTwo");
            Button three = FindControl<Button>(form, "btnThree");
            Button dZero = FindControl<Button>(form, "btnDZero");
            Button enter = FindControl<Button>(form, "btnEnter");

            int margin = 18;
            int width = form.ClientSize.Width - margin * 2;
            int inputHeight = 32;
            int gap = 8;
            int key = 64;
            int top = 18;

            int labelWidth = 110;
            int inputX = 130;
            int inputWidth = 182;

            LayoutSettleField(lblTotalAmount, sale, margin, top, labelWidth, inputX, inputWidth, inputHeight);
            LayoutSettleField(lblAmount, vatable, margin, top + 40, labelWidth, inputX, inputWidth, inputHeight);
            LayoutSettleField(lblVat, vat, margin, top + 80, labelWidth, inputX, inputWidth, inputHeight);
            LayoutSettleField(lblCash, cash, margin, top + 120, labelWidth, inputX, inputWidth, inputHeight);
            LayoutSettleField(lblChange, change, margin, top + 160, labelWidth, inputX, inputWidth, inputHeight);

            if (vatable != null)
            {
                vatable.ReadOnly = true;
                vatable.Enabled = false;
            }
            if (vat != null)
            {
                vat.ReadOnly = true;
                vat.Enabled = false;
            }

            if (paymentLabel == null)
            {
                paymentLabel = new Label();
                paymentLabel.Name = "lblPaymentType";
                form.Controls.Add(paymentLabel);
            }

            paymentLabel.Text = "Payment Type";
            paymentLabel.AutoSize = false;
            paymentLabel.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            paymentLabel.ForeColor = MutedText;
            paymentLabel.SetBounds(margin, top + 198, width, 22);

            if (paymentType == null)
            {
                paymentType = new ComboBox();
                paymentType.Name = "cboPaymentType";
                form.Controls.Add(paymentType);
            }

            paymentType.DropDownStyle = ComboBoxStyle.DropDownList;
            paymentType.FlatStyle = FlatStyle.Flat;
            paymentType.Items.Clear();
            paymentType.Items.AddRange(new object[] { "Cash", "Transfer", "POS Card Payment" });
            if (paymentType.SelectedIndex < 0)
                paymentType.SelectedIndex = 0;
            paymentType.SetBounds(margin, top + 222, width, 34);

            if (noteEnabled)
            {
                if (noteLabel != null)
                {
                    noteLabel.Text = "Special Note";
                    noteLabel.AutoSize = false;
                    noteLabel.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
                    noteLabel.ForeColor = MutedText;
                    noteLabel.SetBounds(margin, top + 262, width, 22);
                    noteLabel.Visible = true;
                }

                if (note != null)
                {
                    note.Font = DefaultFont;
                    note.BackColor = Surface;
                    note.ForeColor = Text;
                    note.BorderStyle = BorderStyle.FixedSingle;
                    note.SetBounds(margin, top + 286, width, 34);
                    note.Visible = true;
                }
            }
            else
            {
                if (noteLabel != null) noteLabel.Visible = false;
                if (note != null) note.Visible = false;
            }

            int keypadTop = noteEnabled ? top + 336 : top + 272;
            int[] xs = { margin, margin + key + gap, margin + (key + gap) * 2, margin + (key + gap) * 3 };
            LayoutSettleButton(seven, xs[0], keypadTop, key, "7");
            LayoutSettleButton(eight, xs[1], keypadTop, key, "8");
            LayoutSettleButton(nine, xs[2], keypadTop, key, "9");
            LayoutSettleButton(clear, xs[3], keypadTop, key, "C");
            LayoutSettleButton(four, xs[0], keypadTop + 58, key, "4");
            LayoutSettleButton(five, xs[1], keypadTop + 58, key, "5");
            LayoutSettleButton(six, xs[2], keypadTop + 58, key, "6");
            LayoutSettleButton(zero, xs[3], keypadTop + 58, key, "0");
            LayoutSettleButton(one, xs[0], keypadTop + 116, key, "1");
            LayoutSettleButton(two, xs[1], keypadTop + 116, key, "2");
            LayoutSettleButton(three, xs[2], keypadTop + 116, key, "3");
            LayoutSettleButton(dZero, xs[3], keypadTop + 116, key, ".");

            if (enter != null)
            {
                enter.Text = "Enter";
                enter.SetBounds(margin, keypadTop + 178, width, 52);
            }
        }

        private static void EnhanceQty(Form form)
        {
            TextBox qty = FindControl<TextBox>(form, "txtQty");

            form.FormBorderStyle = FormBorderStyle.FixedSingle;
            form.StartPosition = FormStartPosition.CenterParent;
            form.ClientSize = new Size(240, 120);
            form.MinimumSize = new Size(240, 120);
            form.MaximumSize = new Size(240, 120);
            form.BackColor = AppBackground;

            if (qty != null)
            {
                qty.Dock = DockStyle.None;
                qty.Multiline = false;
                qty.Font = new Font("Segoe UI Semibold", 28F, FontStyle.Bold);
                qty.TextAlign = HorizontalAlignment.Center;
                qty.BorderStyle = BorderStyle.FixedSingle;
                qty.SetBounds(20, 24, 198, 58);
                qty.BackColor = Color.FromArgb(255, 253, 248);
                qty.ForeColor = Text;
            }
        }

        private static void EnhanceLookUpProduct(Form form)
        {
            Panel footer = FindControl<Panel>(form, "panel1");
            DataGridView grid = FindControl<DataGridView>(form, "dgvProduct");
            bool embedded = !form.TopLevel;

            if (embedded)
            {
                form.BackColor = Surface;
                form.Padding = new Padding(0);
            }
            else
            {
                Rectangle area = Screen.FromControl(form.Owner ?? form).WorkingArea;
                Size lookupSize = new Size(Math.Min(820, area.Width - 120), Math.Min(540, area.Height - 120));
                Point lookupLocation = Point.Empty;
                EnhanceNativeWindow(form, lookupSize, new Size(720, 460));

                if (form.Owner != null && form.Owner.Name == "Cashier")
                {
                    Panel cashierSidebar = FindControl<Panel>(form.Owner, "panel1");
                    Panel cashierSummary = FindControl<Panel>(form.Owner, "panel5");
                    Panel cashierHeader = FindControl<Panel>(form.Owner, "panel4");

                    int left = (cashierSidebar == null ? 0 : cashierSidebar.Right) + 16;
                    int right = form.Owner.ClientSize.Width - (cashierSummary == null ? 0 : cashierSummary.Width) - 16;
                    int top = (cashierHeader == null ? 50 : cashierHeader.Bottom) + 14;
                    int bottom = form.Owner.ClientSize.Height - 26;
                    Rectangle centerLane = new Rectangle(left, top, Math.Max(720, right - left), Math.Max(420, bottom - top));

                    lookupSize = new Size(Math.Min(lookupSize.Width, centerLane.Width), Math.Min(lookupSize.Height, centerLane.Height));
                    Point screenLane = form.Owner.PointToScreen(centerLane.Location);
                    lookupLocation = new Point(
                        screenLane.X + Math.Max(0, (centerLane.Width - lookupSize.Width) / 2),
                        screenLane.Y + Math.Max(0, (centerLane.Height - lookupSize.Height) / 2));
                    form.StartPosition = FormStartPosition.Manual;
                }
                else
                {
                    form.StartPosition = FormStartPosition.CenterParent;
                }

                form.Size = lookupSize;
                if (form.StartPosition == FormStartPosition.Manual)
                    form.Location = lookupLocation;
            }

            if (grid != null)
            {
                grid.Dock = DockStyle.Fill;
                grid.BackgroundColor = Color.FromArgb(248, 251, 252);
                if (grid.Columns.Contains("Select"))
                {
                    grid.Columns["Select"].HeaderText = "Select";
                    grid.Columns["Select"].Width = 76;
                    grid.Columns["Select"].MinimumWidth = 70;
                }
            }

            if (footer != null)
            {
                footer.Height = 68;
                footer.BackColor = PrimaryDark;
                footer.Resize -= LookUpFooter_Resize;
                footer.Resize += LookUpFooter_Resize;
            }

            LayoutLookUpProduct(form);
            form.Resize -= LookUpProduct_Resize;
            form.Resize += LookUpProduct_Resize;
        }

        private static void EnhanceDailySale(Form form)
        {
            Panel header = FindControl<Panel>(form, "panel1");
            Panel footer = FindControl<Panel>(form, "panel2");
            DataGridView grid = FindControl<DataGridView>(form, "dgvSold");
            PictureBox close = FindControl<PictureBox>(form, "picClose");

            Rectangle area = Screen.FromControl(form.Owner ?? form).WorkingArea;
            EnhanceNativeWindow(form, new Size(Math.Min(920, area.Width - 120), Math.Min(560, area.Height - 120)), new Size(760, 460));
            form.StartPosition = FormStartPosition.CenterParent;

            if (header != null)
            {
                header.Height = 54;
                header.BackColor = PrimaryDark;
                header.Paint -= PaintHeader;
            }

            if (close != null)
                close.Visible = false;

            if (grid != null)
            {
                grid.Dock = DockStyle.Fill;
                grid.BackgroundColor = Color.FromArgb(248, 251, 252);
                if (grid.Columns.Contains("Cancel"))
                {
                    grid.Columns["Cancel"].HeaderText = "Cancel";
                    grid.Columns["Cancel"].Width = 76;
                    grid.Columns["Cancel"].MinimumWidth = 70;
                }
            }

            if (footer != null)
            {
                footer.Height = 68;
                footer.BackColor = PrimaryDark;
                footer.Resize -= DailySaleFooter_Resize;
                footer.Resize += DailySaleFooter_Resize;
            }

            LayoutDailySale(form);
            form.Resize -= DailySale_Resize;
            form.Resize += DailySale_Resize;
        }

        private static void LayoutDailySale(Form form)
        {
            Panel footer = FindControl<Panel>(form, "panel2");
            Label title = FindControl<Label>(form, "lblTitle");
            PictureBox close = FindControl<PictureBox>(form, "picClose");
            DateTimePicker from = FindControl<DateTimePicker>(form, "dtFrom");
            DateTimePicker to = FindControl<DateTimePicker>(form, "dtTo");
            ComboBox cashier = FindControl<ComboBox>(form, "cboCashier");
            Button print = FindControl<Button>(form, "btnPrint");
            Label fromLabel = FindControl<Label>(form, "label2");
            Label toLabel = FindControl<Label>(form, "label3");
            Label total = FindControl<Label>(form, "lblTotal");

            if (title != null)
            {
                title.Visible = true;
                title.Text = "Sale History";
                title.AutoSize = false;
                title.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
                title.ForeColor = Color.White;
                title.SetBounds(22, 13, 220, 30);
            }

            if (close != null)
            {
                close.Visible = false;
                close.SetBounds(Math.Max(0, form.ClientSize.Width - 42), 10, 30, 30);
            }

            if (footer == null)
                return;

            LayoutLabel(fromLabel, 22, 22, 150, "Filter By Date: From");
            if (from != null)
                from.SetBounds(174, 18, 116, 34);
            LayoutLabel(toLabel, 304, 22, 26, "To");
            if (to != null)
                to.SetBounds(334, 18, 116, 34);
            if (cashier != null)
                cashier.SetBounds(466, 18, 130, 34);
            if (print != null)
            {
                print.Text = "Print";
                print.SetBounds(620, 14, 96, 40);
            }
            if (total != null)
            {
                total.ForeColor = Color.White;
                total.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
                total.TextAlign = ContentAlignment.MiddleRight;
                total.SetBounds(Math.Max(730, footer.ClientSize.Width - 170), 20, 142, 28);
            }
        }

        private static void DailySale_Resize(object sender, EventArgs e)
        {
            Form form = sender as Form;
            if (form != null)
                LayoutDailySale(form);
        }

        private static void DailySaleFooter_Resize(object sender, EventArgs e)
        {
            Panel panel = sender as Panel;
            Form form = panel == null ? null : panel.FindForm();
            if (form != null)
                LayoutDailySale(form);
        }

        private static void LayoutLookUpProduct(Form form)
        {
            Panel footer = FindControl<Panel>(form, "panel1");
            Control search = FindControl<Control>(form, "txtSearch");
            Button close = FindControl<Button>(form, "btnClose");
            Label title = FindControl<Label>(form, "label1");

            if (footer == null)
                return;

            int panelWidth = footer.ClientSize.Width;

            if (title != null)
            {
                title.Text = "Search";
                title.AutoSize = false;
                title.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
                title.ForeColor = Color.White;
                title.TextAlign = ContentAlignment.MiddleLeft;
                title.SetBounds(24, 16, 180, 34);
            }

            if (search != null)
            {
                int searchWidth = Math.Min(330, Math.Max(220, panelWidth - 360));
                search.Anchor = AnchorStyles.Bottom;
                search.SetBounds(Math.Max(200, (panelWidth - searchWidth) / 2), 16, searchWidth, 36);
            }

            if (close != null)
            {
                if (form.TopLevel)
                {
                    close.Visible = false;
                    return;
                }

                close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                close.SetBounds(Math.Max(0, panelWidth - 118), 14, 96, 40);
            }
        }

        private static void LookUpProduct_Resize(object sender, EventArgs e)
        {
            Form form = sender as Form;
            if (form != null)
                LayoutLookUpProduct(form);
        }

        private static void LookUpFooter_Resize(object sender, EventArgs e)
        {
            Panel panel = sender as Panel;
            Form form = panel == null ? null : panel.FindForm();
            if (form != null)
                LayoutLookUpProduct(form);
        }

        private static void EnhanceNativeWindow(Form form, Size size, Size minimumSize)
        {
            PictureBox close = FindControl<PictureBox>(form, "picClose");

            form.FormBorderStyle = FormBorderStyle.Sizable;
            form.ControlBox = true;
            form.MinimizeBox = true;
            form.MaximizeBox = true;
            form.ShowIcon = true;
            form.BackColor = AppBackground;
            form.Padding = new Padding(0);
            form.Size = size;
            form.MinimumSize = minimumSize;
            ApplyDarkTitleBar(form);

            if (close != null)
                close.Visible = false;
        }

        private static void ApplyDarkTitleBar(Form form)
        {
            if (form == null)
                return;

            form.HandleCreated -= NativeWindow_HandleCreated;
            form.HandleCreated += NativeWindow_HandleCreated;

            if (form.IsHandleCreated)
                SetDarkTitleBar(form);
        }

        private static void NativeWindow_HandleCreated(object sender, EventArgs e)
        {
            SetDarkTitleBar(sender as Form);
        }

        private static void SetDarkTitleBar(Form form)
        {
            if (form == null || !form.IsHandleCreated)
                return;

            try
            {
                int enabled = 1;
                int result = DwmSetWindowAttribute(form.Handle, 20, ref enabled, Marshal.SizeOf(typeof(int)));
                if (result != 0)
                    DwmSetWindowAttribute(form.Handle, 19, ref enabled, Marshal.SizeOf(typeof(int)));
            }
            catch
            {
                // Older Windows builds ignore this; the standard window buttons still work.
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private static void ApplyDialogBoundary(Form form)
        {
            form.BackColor = PrimaryDark;
            form.Padding = new Padding(4);
        }

        private static void EnsureDialogAccent(Form form, string name)
        {
            Panel accent = FindControl<Panel>(form, name);
            if (accent == null)
            {
                accent = new Panel
                {
                    Name = name,
                    Dock = DockStyle.Top,
                    Height = 10,
                    BackColor = PrimaryDark
                };
                form.Controls.Add(accent);
                accent.BringToFront();
            }
            else
            {
                accent.Height = 10;
                accent.BackColor = PrimaryDark;
            }
        }

        private static void LayoutCashierSummaryLabel(Label label, int x, int y, string text)
        {
            if (label == null)
                return;

            label.Text = text;
            label.AutoSize = false;
            label.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            label.ForeColor = MutedText;
            label.SetBounds(x, y, 180, 24);
        }

        private static void LayoutCashierSummaryValue(Label label, int x, int y, int width)
        {
            if (label == null)
                return;

            label.AutoSize = false;
            label.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            label.ForeColor = Text;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.SetBounds(x, y, width, 26);
        }

        private static void LayoutCashierAmountRow(Label label, Label value, int labelX, int valueX, int y, int valueWidth, string text)
        {
            if (label != null)
            {
                label.Text = text;
                label.AutoSize = false;
                label.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
                label.ForeColor = MutedText;
                label.SetBounds(labelX, y, 110, 24);
            }

            if (value != null)
            {
                value.AutoSize = false;
                value.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
                value.ForeColor = Text;
                value.TextAlign = ContentAlignment.MiddleRight;
                value.SetBounds(valueX, y, valueWidth, 24);
            }
        }

        private static void LayoutSettleTextBox(TextBox textBox, int x, int y, int width, int height)
        {
            if (textBox == null)
                return;

            textBox.SetBounds(x, y, width, height);
            textBox.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            textBox.TextAlign = HorizontalAlignment.Right;
            textBox.Multiline = false;
        }

        private static void LayoutSettleField(Label label, TextBox textBox, int labelX, int y, int labelWidth, int inputX, int inputWidth, int height)
        {
            if (label != null)
            {
                label.AutoSize = false;
                label.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
                label.ForeColor = MutedText;
                label.TextAlign = ContentAlignment.MiddleLeft;
                label.SetBounds(labelX, y, labelWidth, height);
            }
            if (textBox != null)
            {
                LayoutSettleTextBox(textBox, inputX, y, inputWidth, height);
            }
        }

        private static void LayoutSettleButton(Button button, int x, int y, int size, string text)
        {
            if (button == null)
                return;

            button.Text = text;
            button.SetBounds(x, y, size, 52);
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.Padding = new Padding(0);
            button.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
        }

        private static void LayoutLabel(Label label, int x, int y, int width, string text)
        {
            if (label == null)
                return;

            label.Text = text;
            label.SetBounds(x, y, width, 24);
            label.AutoSize = false;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = MutedText;
            label.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        }

        private static void LayoutValue(Label label, int x, int y, int width)
        {
            if (label == null)
                return;

            label.SetBounds(x, y, width, 28);
            label.AutoSize = false;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = Text;
            label.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
        }

        private static string GetMenuText(string text)
        {
            string trimmed = text.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith(">"))
                return trimmed;

            return ">  " + trimmed;
        }

        private static T FindControl<T>(Control root, string name) where T : Control
        {
            if (root == null)
                return null;

            foreach (Control control in root.Controls)
            {
                if (control.Name == name && control is T match)
                    return match;

                T nested = FindControl<T>(control, name);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static IEnumerable<T> GetControls<T>(Control root) where T : Control
        {
            foreach (Control control in root.Controls)
            {
                if (control is T match)
                    yield return match;

                foreach (T nested in GetControls<T>(control))
                    yield return nested;
            }
        }

        private static void ReplaceImageColumns(DataGridView grid)
        {
            for (int i = 0; i < grid.Columns.Count; i++)
            {
                DataGridViewImageColumn imageColumn = grid.Columns[i] as DataGridViewImageColumn;
                if (imageColumn == null)
                    continue;

                string actionText = CleanActionText(string.IsNullOrWhiteSpace(imageColumn.HeaderText)
                    ? imageColumn.Name
                    : imageColumn.HeaderText);

                DataGridViewButtonColumn buttonColumn = new DataGridViewButtonColumn
                {
                    Name = imageColumn.Name,
                    HeaderText = actionText,
                    Text = actionText,
                    UseColumnTextForButtonValue = true,
                    Width = Math.Max(imageColumn.Width, 76),
                    MinimumWidth = 64,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    FlatStyle = FlatStyle.Flat,
                    DisplayIndex = imageColumn.DisplayIndex,
                    ReadOnly = imageColumn.ReadOnly,
                    Visible = imageColumn.Visible
                };

                grid.Columns.RemoveAt(i);
                grid.Columns.Insert(i, buttonColumn);
            }
        }

        private static string CleanActionText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Action";

            value = value.Replace("Column", "").Trim();
            if (value.Equals("colAdd", StringComparison.OrdinalIgnoreCase))
                return "Add";
            if (value.Equals("colReduce", StringComparison.OrdinalIgnoreCase))
                return "Reduce";
            if (value.Equals("Delete", StringComparison.OrdinalIgnoreCase))
                return "Remove";

            return string.IsNullOrWhiteSpace(value) ? "Action" : value;
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName);
            if (property == null || !property.CanWrite)
                return;

            property.SetValue(target, value, null);
        }

        private static void SetBackColor(Control control, Color color)
        {
            try
            {
                control.BackColor = color;
            }
            catch (ArgumentException)
            {
                control.BackColor = Surface;
            }
        }

        private static void PaintSidebar(object sender, PaintEventArgs e)
        {
            Control control = (Control)sender;
            using (LinearGradientBrush brush = new LinearGradientBrush(
                control.ClientRectangle,
                Color.FromArgb(18, 28, 40),
                Color.FromArgb(37, 50, 64),
                LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(brush, control.ClientRectangle);
            }
        }

        private static void PaintLogoPanel(object sender, PaintEventArgs e)
        {
            Control control = (Control)sender;
            Rectangle rect = control.ClientRectangle;

            using (LinearGradientBrush brush = new LinearGradientBrush(
                rect,
                Color.FromArgb(0, 168, 150),
                Color.FromArgb(52, 152, 219),
                LinearGradientMode.ForwardDiagonal))
            {
                e.Graphics.FillRectangle(brush, rect);
            }
        }

        private static void PaintHeader(object sender, PaintEventArgs e)
        {
            Control control = (Control)sender;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(Border))
            {
                e.Graphics.DrawLine(pen, 0, control.Height - 1, control.Width, control.Height - 1);
            }
        }

        private static void PaintCard(object sender, PaintEventArgs e)
        {
            Panel panel = (Panel)sender;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle shadow = new Rectangle(3, 5, panel.Width - 8, panel.Height - 9);
            Rectangle card = new Rectangle(0, 0, panel.Width - 5, panel.Height - 7);

            using (GraphicsPath shadowPath = RoundedRectangle(shadow, 12))
            using (GraphicsPath cardPath = RoundedRectangle(card, 12))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(28, 12, 20, 30)))
            using (SolidBrush cardBrush = new SolidBrush(Surface))
            using (Pen borderPen = new Pen(Color.FromArgb(232, 237, 243)))
            using (SolidBrush accentBrush = new SolidBrush(GetCardAccent(panel.Name)))
            {
                e.Graphics.FillPath(shadowBrush, shadowPath);
                e.Graphics.FillPath(cardBrush, cardPath);
                e.Graphics.DrawPath(borderPen, cardPath);
                e.Graphics.FillRectangle(accentBrush, new Rectangle(0, 18, 5, panel.Height - 36));
            }
        }

        private static void PaintAdjustmentCard(object sender, PaintEventArgs e)
        {
            Panel panel = (Panel)sender;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle shadow = new Rectangle(3, 5, panel.Width - 8, panel.Height - 9);
            Rectangle card = new Rectangle(0, 0, panel.Width - 5, panel.Height - 7);
            Rectangle inputZone = new Rectangle(Math.Max(560, panel.Width - 405), 16, 380, Math.Max(120, panel.Height - 34));

            using (GraphicsPath shadowPath = RoundedRectangle(shadow, 12))
            using (GraphicsPath cardPath = RoundedRectangle(card, 12))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(26, 42, 34, 22)))
            using (SolidBrush cardBrush = new SolidBrush(Color.FromArgb(253, 251, 244)))
            using (SolidBrush zoneBrush = new SolidBrush(Color.FromArgb(248, 244, 232)))
            using (Pen borderPen = new Pen(Color.FromArgb(218, 201, 162)))
            using (SolidBrush accentBrush = new SolidBrush(Primary))
            {
                e.Graphics.FillPath(shadowBrush, shadowPath);
                e.Graphics.FillPath(cardBrush, cardPath);
                e.Graphics.DrawPath(borderPen, cardPath);

                if (inputZone.Right < panel.Width - 12)
                    e.Graphics.FillRectangle(zoneBrush, inputZone);

                e.Graphics.FillRectangle(accentBrush, new Rectangle(0, 18, 5, panel.Height - 36));
            }
        }

        private static Color GetCardAccent(string name)
        {
            string value = name.ToLowerInvariant();
            if (value.Contains("2"))
                return Primary;
            if (value.Contains("4"))
                return Accent;
            if (value.Contains("6"))
                return Danger;
            return Info;
        }

        private static void PaintButtonGlow(object sender, PaintEventArgs e)
        {
            Button button = (Button)sender;
            if (button.ClientRectangle.Width <= 0 || button.ClientRectangle.Height <= 0)
                return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (IsInsideSidebar(button))
            {
                using (Pen pen = new Pen(Color.FromArgb(30, Color.White)))
                {
                    e.Graphics.DrawLine(pen, 16, button.Height - 1, button.Width - 16, button.Height - 1);
                }

                return;
            }

            using (GraphicsPath path = RoundedRectangle(new Rectangle(0, 0, button.Width - 1, button.Height - 1), 8))
            using (Pen pen = new Pen(Color.FromArgb(65, Color.White)))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        private static void PaintLoginBackground(object sender, PaintEventArgs e)
        {
            Form form = (Form)sender;
            Rectangle rect = form.ClientRectangle;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (LinearGradientBrush brush = new LinearGradientBrush(
                rect,
                Color.FromArgb(10, 13, 10),
                Color.FromArgb(7, 88, 63),
                LinearGradientMode.ForwardDiagonal))
            {
                e.Graphics.FillRectangle(brush, new Rectangle(0, 0, rect.Width, 276));
            }

            using (SolidBrush soft = new SolidBrush(Color.FromArgb(30, Accent)))
            {
                e.Graphics.FillEllipse(soft, new Rectangle(690, -145, 340, 340));
                e.Graphics.FillEllipse(soft, new Rectangle(-130, 138, 280, 280));
            }

            using (SolidBrush page = new SolidBrush(AppBackground))
            {
                e.Graphics.FillRectangle(page, new Rectangle(0, 276, rect.Width, rect.Height - 276));
            }

            Rectangle card = new Rectangle(292, 276, 406, 358);
            using (GraphicsPath shadowPath = RoundedRectangle(new Rectangle(card.X + 8, card.Y + 10, card.Width, card.Height), 20))
            using (GraphicsPath cardPath = RoundedRectangle(card, 20))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(42, 13, 12, 8)))
            using (SolidBrush cardBrush = new SolidBrush(Color.FromArgb(255, 254, 250)))
            using (Pen borderPen = new Pen(Color.FromArgb(222, 207, 170)))
            using (SolidBrush accent = new SolidBrush(Accent))
            {
                e.Graphics.FillPath(shadowBrush, shadowPath);
                e.Graphics.FillPath(cardBrush, cardPath);
                e.Graphics.DrawPath(borderPen, cardPath);
                e.Graphics.FillRectangle(accent, new Rectangle(card.X, card.Y + 44, 5, card.Height - 88));
            }

            using (Font font = new Font("Segoe UI", 10.5F, FontStyle.Regular))
            using (SolidBrush brush = new SolidBrush(MutedText))
            {
                e.Graphics.DrawString("Sign in to manage sales, stock, and reports.", font, brush, card.X + 42, card.Y + 78);
            }
        }

        private static void PaintSplashBackground(object sender, PaintEventArgs e)
        {
            Form form = (Form)sender;
            Rectangle rect = form.ClientRectangle;

            using (LinearGradientBrush brush = new LinearGradientBrush(
                rect,
                Color.FromArgb(12, 15, 12),
                Color.FromArgb(6, 78, 58),
                LinearGradientMode.ForwardDiagonal))
            {
                e.Graphics.FillRectangle(brush, rect);
            }

            Rectangle badge = new Rectangle((rect.Width - 150) / 2, 82, 150, 150);
            using (GraphicsPath badgePath = RoundedRectangle(badge, 28))
            using (SolidBrush badgeBrush = new SolidBrush(Color.FromArgb(21, 27, 21)))
            using (Pen badgePen = new Pen(Color.FromArgb(145, Accent)))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(badgeBrush, badgePath);
                e.Graphics.DrawPath(badgePen, badgePath);
            }

            using (Font font = new Font("Segoe UI Semibold", 42F, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(Primary))
            {
                string text = "POS";
                SizeF size = e.Graphics.MeasureString(text, font);
                e.Graphics.DrawString(text, font, brush, (rect.Width - size.Width) / 2, 128);
            }
        }

        private static void PaintDashboardCanvas(object sender, PaintEventArgs e)
        {
            Form form = (Form)sender;
            Rectangle area = new Rectangle(44, 170, Math.Max(200, form.ClientSize.Width - 88), Math.Max(220, form.ClientSize.Height - 214));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = RoundedRectangle(area, 18))
            using (SolidBrush brush = new SolidBrush(Color.White))
            using (Pen pen = new Pen(Color.FromArgb(230, 236, 242)))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            using (Font title = new Font("Segoe UI Semibold", 22F, FontStyle.Bold))
            using (Font body = new Font("Segoe UI", 11F, FontStyle.Regular))
            using (SolidBrush titleBrush = new SolidBrush(Text))
            using (SolidBrush bodyBrush = new SolidBrush(MutedText))
            {
                e.Graphics.DrawString("Sales overview", title, titleBrush, area.X + 34, area.Y + 34);
                e.Graphics.DrawString("Track sales, stock levels, product counts, and critical inventory from one clean workspace.", body, bodyBrush, area.X + 36, area.Y + 82);
            }

            Rectangle lineArea = new Rectangle(area.X + 44, area.Y + 145, area.Width - 88, 120);
            using (Pen gridPen = new Pen(Color.FromArgb(238, 242, 246)))
            using (Pen linePen = new Pen(Primary, 4))
            {
                for (int i = 0; i < 4; i++)
                {
                    int y = lineArea.Y + i * 34;
                    e.Graphics.DrawLine(gridPen, lineArea.X, y, lineArea.Right, y);
                }

                Point[] points =
                {
                    new Point(lineArea.X, lineArea.Bottom - 18),
                    new Point(lineArea.X + lineArea.Width / 5, lineArea.Bottom - 56),
                    new Point(lineArea.X + lineArea.Width * 2 / 5, lineArea.Bottom - 34),
                    new Point(lineArea.X + lineArea.Width * 3 / 5, lineArea.Bottom - 92),
                    new Point(lineArea.X + lineArea.Width * 4 / 5, lineArea.Bottom - 62),
                    new Point(lineArea.Right, lineArea.Bottom - 106)
                };
                e.Graphics.DrawLines(linePen, points);
            }
        }

        private static void PaintCloseGlyph(object sender, PaintEventArgs e)
        {
            PictureBox pictureBox = (PictureBox)sender;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (Pen pen = new Pen(Color.White, 2))
            {
                e.Graphics.DrawLine(pen, 9, 9, pictureBox.Width - 9, pictureBox.Height - 9);
                e.Graphics.DrawLine(pen, pictureBox.Width - 9, 9, 9, pictureBox.Height - 9);
            }
        }

        private static void RoundButton(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            ApplyRoundedRegion(button, IsInsideSidebar(button) ? 0 : 8);
        }

        private static void ApplyRoundedRegion(Control control, int radius)
        {
            if (radius <= 0 || control.Width <= 0 || control.Height <= 0)
            {
                control.Region = null;
                return;
            }

            using (GraphicsPath path = RoundedRectangle(new Rectangle(0, 0, control.Width, control.Height), radius))
            {
                control.Region = new Region(path);
            }
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
