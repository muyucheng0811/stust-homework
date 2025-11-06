using System;
using System.Text;
using System.Windows.Forms;

namespace 視窗應用程式作業
{
    public partial class Form1 : Form
    {
        // 批次處理陣列（與 Designer 六列一一對應）
        private CheckBox[] _checks;
        private TrackBar[] _tracks;
        private Label[] _qtyLabels;

        // 品名與單價
        private readonly string[] _names = { "紅茶大杯", "紅茶小杯", "綠茶大杯", "綠茶小杯", "可樂大杯", "可樂小杯" };
        private readonly int[] _prices = { 60, 30, 60, 30, 40, 20 };

        // 外帶手續費（不需要就設 0）
        private const int TakeOutFee = 0;

        public Form1()
        {
            InitializeComponent();

            // 收集控制項
            _checks = new[] { chkRedL, chkRedS, chkGreenL, chkGreenS, chkColaL, chkColaS };
            _tracks = new[] { tbRedL, tbRedS, tbGreenL, tbGreenS, tbColaL, tbColaS };
            _qtyLabels = new[] { lblQtyRedL, lblQtyRedS, lblQtyGreenL, lblQtyGreenS, lblQtyColaL, lblQtyColaS };

            // 統一事件與初始狀態
            for (int i = 0; i < _tracks.Length; i++)
            {
                _checks[i].Tag = i;
                _tracks[i].Tag = i;

                _checks[i].CheckedChanged += Check_CheckedChanged;
                _tracks[i].Scroll += Track_Scroll;

                _tracks[i].Enabled = _checks[i].Checked;
                _tracks[i].Value = 0;
                _qtyLabels[i].Text = "0";
            }
        }

        // 勾選/取消 → 啟用/停用滑桿、清零
        private void Check_CheckedChanged(object sender, EventArgs e)
        {
            var chk = (CheckBox)sender;
            int i = (int)chk.Tag;

            _tracks[i].Enabled = chk.Checked;
            if (!chk.Checked)
            {
                _tracks[i].Value = 0;
                _qtyLabels[i].Text = "0";
            }
        }

        // 滑桿移動 → 數量同步
        private void Track_Scroll(object sender, EventArgs e)
        {
            var tb = (TrackBar)sender;
            int i = (int)tb.Tag;
            _qtyLabels[i].Text = tb.Value.ToString();
        }

        // 折扣階梯：>=500 → 8折；>=300 → 85折；>=200 → 9折；否則不折
        private decimal GetDiscountRate(int itemsSubtotal)
        {
            if (itemsSubtotal >= 500) return 0.80m;
            if (itemsSubtotal >= 300) return 0.85m;
            if (itemsSubtotal >= 200) return 0.90m;
            return 1.00m;
        }

        // 訂購
        private void btnOrder_Click(object sender, EventArgs e)
        {
            var sb = new StringBuilder();

            string mode = rbDineIn.Checked ? "內用" : "外帶";
            sb.AppendLine($"訂購清單（{mode}）");
            sb.AppendLine(new string('－', 28));

            int itemsSubtotal = 0;
            int line = 1;

            for (int i = 0; i < _names.Length; i++)
            {
                if (!_checks[i].Checked) continue;
                int qty = _tracks[i].Value;
                if (qty <= 0) continue;

                int sub = qty * _prices[i];
                itemsSubtotal += sub;
                sb.AppendLine($"{line}. {_names[i]}：{_prices[i]}元 × {qty}杯 = {sub}元");
                line++;
            }

            if (line == 1)
            {
                sb.AppendLine("尚未選擇任何品項。");
            }

            // 折扣計算（對「品項小計」折扣）
            decimal rate = GetDiscountRate(itemsSubtotal);
            int discountMoney = Convert.ToInt32(Math.Round(itemsSubtotal * (1 - rate)));
            int afterDiscount = itemsSubtotal - discountMoney;

            // 外帶手續費（若有）
            int fee = rbTakeOut.Checked ? TakeOutFee : 0;

            // 最終應付
            int finalTotal = afterDiscount + fee;

            sb.AppendLine();
            sb.AppendLine($"原價小計：{itemsSubtotal} 元");
            if (rate < 1.0m)
            {
                int percent = (int)((1 - rate) * 100);
                sb.AppendLine($"折扣：{100 - percent}%（折抵 {discountMoney} 元）");
            }
            else
            {
                sb.AppendLine("折扣：無");
            }
            if (fee > 0) sb.AppendLine($"外帶手續費：{fee} 元");
            sb.AppendLine($"應付金額：{finalTotal} 元");

            txtSummary.Text = sb.ToString();
        }
    }
}
