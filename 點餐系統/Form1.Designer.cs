using System.Drawing;
using System.Windows.Forms;

namespace 視窗應用程式作業
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // ===== 頂部：內用/外帶 =====
        private GroupBox grpService;
        private RadioButton rbDineIn;
        private RadioButton rbTakeOut;

        // ===== 左側清單 =====
        private Panel pnlItems;
        private Label lblHeadItem, lblHeadPrice, lblHeadQty;

        // 每列：CheckBox + 文字Label + 價格Label + TrackBar + 數量Label
        private CheckBox chkRedL, chkRedS, chkGreenL, chkGreenS, chkColaL, chkColaS;
        private Label lblNameRedL, lblNameRedS, lblNameGreenL, lblNameGreenS, lblNameColaL, lblNameColaS;
        private Label lblPriceRedL, lblPriceRedS, lblPriceGreenL, lblPriceGreenS, lblPriceColaL, lblPriceColaS;
        private TrackBar tbRedL, tbRedS, tbGreenL, tbGreenS, tbColaL, tbColaS;
        private Label lblQtyRedL, lblQtyRedS, lblQtyGreenL, lblQtyGreenS, lblQtyColaL, lblQtyColaS;

        // ===== 右側按鈕 =====
        private Button btnOrder;

        // ===== 底部輸出（WinForms 用 TextBox 代替 TextBlock）=====
        private TextBox txtSummary;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // ------- 視窗 -------
            this.Text = "飲料點餐系統 ver2";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(1000, 680);
            this.Font = new Font("Microsoft JhengHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            // ------- 內用/外帶 -------
            grpService = new GroupBox();
            grpService.Text = "內用/外帶";
            grpService.Location = new Point(14, 12);
            grpService.Size = new Size(972, 64);
            grpService.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            rbDineIn = new RadioButton();
            rbDineIn.Text = "內用";
            rbDineIn.AutoSize = true;
            rbDineIn.Location = new Point(20, 28);
            rbDineIn.Checked = true;

            rbTakeOut = new RadioButton();
            rbTakeOut.Text = "外帶";
            rbTakeOut.AutoSize = true;
            rbTakeOut.Location = new Point(100, 28);

            grpService.Controls.Add(rbDineIn);
            grpService.Controls.Add(rbTakeOut);
            this.Controls.Add(grpService);

            // ------- 左側清單面板 -------
            pnlItems = new Panel();
            pnlItems.Location = new Point(14, 92);
            pnlItems.Size = new Size(710, 440);
            pnlItems.BorderStyle = BorderStyle.FixedSingle;
            pnlItems.BackColor = Color.FromArgb(255, 246, 240); // 淡粉
            pnlItems.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(pnlItems);

            // 表頭
            lblHeadItem = MakeHeaderLabel("品項", new Point(20, 14));
            lblHeadPrice = MakeHeaderLabel("單價（元）", new Point(190, 14));
            lblHeadQty = MakeHeaderLabel("數量", new Point(650, 14));
            pnlItems.Controls.Add(lblHeadItem);
            pnlItems.Controls.Add(lblHeadPrice);
            pnlItems.Controls.Add(lblHeadQty);

            // 位置參數
            int baseY = 48;   // 第一列 Y
            int stepY = 58;   // 列距
            int colChk = 20;
            int colName = 44;
            int colPrice = 190;
            int colBar = 260;
            int colQty = 650;

            // ------- 列 1：紅茶大杯 60 -------
            chkRedL = MakeCheck(new Point(colChk, baseY + stepY * 0));
            lblNameRedL = MakeNameLabel("紅茶大杯", new Point(colName, baseY + stepY * 0));
            lblPriceRedL = MakePriceLabel("60元", new Point(colPrice, baseY + stepY * 0));
            tbRedL = MakeTrackBar(new Point(colBar, baseY - 8 + stepY * 0));
            lblQtyRedL = MakeQtyLabel(new Point(colQty, baseY + stepY * 0));

            // ------- 列 2：紅茶小杯 30 -------
            chkRedS = MakeCheck(new Point(colChk, baseY + stepY * 1));
            lblNameRedS = MakeNameLabel("紅茶小杯", new Point(colName, baseY + stepY * 1));
            lblPriceRedS = MakePriceLabel("30元", new Point(colPrice, baseY + stepY * 1));
            tbRedS = MakeTrackBar(new Point(colBar, baseY - 8 + stepY * 1));
            lblQtyRedS = MakeQtyLabel(new Point(colQty, baseY + stepY * 1));

            // ------- 列 3：綠茶大杯 60 -------
            chkGreenL = MakeCheck(new Point(colChk, baseY + stepY * 2));
            lblNameGreenL = MakeNameLabel("綠茶大杯", new Point(colName, baseY + stepY * 2));
            lblPriceGreenL = MakePriceLabel("60元", new Point(colPrice, baseY + stepY * 2));
            tbGreenL = MakeTrackBar(new Point(colBar, baseY - 8 + stepY * 2));
            lblQtyGreenL = MakeQtyLabel(new Point(colQty, baseY + stepY * 2));

            // ------- 列 4：綠茶小杯 30 -------
            chkGreenS = MakeCheck(new Point(colChk, baseY + stepY * 3));
            lblNameGreenS = MakeNameLabel("綠茶小杯", new Point(colName, baseY + stepY * 3));
            lblPriceGreenS = MakePriceLabel("30元", new Point(colPrice, baseY + stepY * 3));
            tbGreenS = MakeTrackBar(new Point(colBar, baseY - 8 + stepY * 3));
            lblQtyGreenS = MakeQtyLabel(new Point(colQty, baseY + stepY * 3));

            // ------- 列 5：可樂大杯 40 -------
            chkColaL = MakeCheck(new Point(colChk, baseY + stepY * 4));
            lblNameColaL = MakeNameLabel("可樂大杯", new Point(colName, baseY + stepY * 4));
            lblPriceColaL = MakePriceLabel("40元", new Point(colPrice, baseY + stepY * 4));
            tbColaL = MakeTrackBar(new Point(colBar, baseY - 8 + stepY * 4));
            lblQtyColaL = MakeQtyLabel(new Point(colQty, baseY + stepY * 4));

            // ------- 列 6：可樂小杯 20 -------
            chkColaS = MakeCheck(new Point(colChk, baseY + stepY * 5));
            lblNameColaS = MakeNameLabel("可樂小杯", new Point(colName, baseY + stepY * 5));
            lblPriceColaS = MakePriceLabel("20元", new Point(colPrice, baseY + stepY * 5));
            tbColaS = MakeTrackBar(new Point(colBar, baseY - 8 + stepY * 5));
            lblQtyColaS = MakeQtyLabel(new Point(colQty, baseY + stepY * 5));

            // 全部加到面板（維持整齊的 Z-Order）
            pnlItems.Controls.AddRange(new Control[]
            {
                chkRedL, lblNameRedL, lblPriceRedL, tbRedL, lblQtyRedL,
                chkRedS, lblNameRedS, lblPriceRedS, tbRedS, lblQtyRedS,
                chkGreenL, lblNameGreenL, lblPriceGreenL, tbGreenL, lblQtyGreenL,
                chkGreenS, lblNameGreenS, lblPriceGreenS, tbGreenS, lblQtyGreenS,
                chkColaL, lblNameColaL, lblPriceColaL, tbColaL, lblQtyColaL,
                chkColaS, lblNameColaS, lblPriceColaS, tbColaS, lblQtyColaS
            });

            // ------- 右側：訂購按鈕 -------
            btnOrder = new Button();
            btnOrder.Text = "訂購";
            btnOrder.Size = new Size(160, 72);
            btnOrder.Location = new Point(748, 230);
            btnOrder.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOrder.Click += new System.EventHandler(this.btnOrder_Click);
            this.Controls.Add(btnOrder);

            // ------- 底部：摘要輸出 -------
            txtSummary = new TextBox();
            txtSummary.Multiline = true;
            txtSummary.ReadOnly = true;
            txtSummary.ScrollBars = ScrollBars.Vertical;
            txtSummary.Location = new Point(14, 548);
            txtSummary.Size = new Size(972, 116);
            txtSummary.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            txtSummary.BackColor = Color.FromArgb(235, 243, 229); // 淡綠
            txtSummary.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(txtSummary);
        }

        // ===== 產生一致樣式的小工具 =====
        private Label MakeHeaderLabel(string text, Point p)
        {
            var lb = new Label();
            lb.Text = text;
            lb.Location = p;
            lb.AutoSize = true;
            lb.Font = new Font(this.Font, FontStyle.Bold);
            return lb;
        }

        private CheckBox MakeCheck(Point p)
        {
            var cb = new CheckBox();
            cb.AutoSize = true; // 只顯示小方框，避免蓋住品名
            cb.Text = "";
            cb.Location = p;
            return cb;
        }

        private Label MakeNameLabel(string text, Point p)
        {
            var lb = new Label();
            lb.Text = text;
            lb.Location = p;
            lb.AutoSize = true;
            return lb;
        }

        private Label MakePriceLabel(string text, Point p)
        {
            var lb = new Label();
            lb.Text = text;
            lb.Location = p;
            lb.AutoSize = true;
            lb.ForeColor = Color.FromArgb(0, 90, 200);
            lb.Font = new Font(this.Font, FontStyle.Bold);
            return lb;
        }

        private TrackBar MakeTrackBar(Point p)
        {
            var tb = new TrackBar();
            tb.Location = p;
            tb.Size = new Size(360, 45);
            tb.Minimum = 0;
            tb.Maximum = 50;
            tb.TickStyle = TickStyle.None;
            tb.SmallChange = 1;
            tb.LargeChange = 5;
            tb.Enabled = false; // 預設未勾選 → 關閉
            return tb;
        }

        private Label MakeQtyLabel(Point p)
        {
            var lb = new Label();
            lb.Location = p;
            lb.AutoSize = true;
            lb.Text = "0";
            lb.Font = new Font(this.Font, FontStyle.Bold);
            return lb;
        }

        #endregion
    }
}
