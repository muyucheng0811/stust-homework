# stust-homework
作者：穆昱丞 (學號 4B3G0905)

## 專案包含
- `order-system/` — 點餐系統（WinForms / WPF）  
- `painter/` — WPF 小畫家（Canvas）

## 快速執行 (WPF 小畫家)
1. 用 Visual Studio 打開 `painter.sln`
2. Build -> Run (或 F5)
3. 功能：矩形/橢圓/線/自由筆、顏色選擇、儲存 PNG、Undo/Redo、橡皮擦

## 點餐系統
- 若為 WPF 版本，會從 `drinks.csv` 讀取品項。若是 WinForms 版本直接執行 exe。
- 範例 CSV: `name,price`（紅茶大杯,60）

## 其他
- .gitignore 已設定（忽略 bin/obj 等）
- License: MIT
