# Phase 0: ユースケース定義書 (Use Case Definition)

**Project Name:** LiveEvent Visualizer "Synapse"
**Version:** 2.0 (Tauri Architecture)

### 1. アクター（システムの登場人物）
* **オペレーター (OP):** 本システムの主操作者。Web UIやMIDIコントローラーを操作し、表示の管理を行う。
* **ゲストDJ (DJ):** 楽曲をプレイし、情報ソースを提示する。本システムを直接は操作しない。
* **プロジェクター/観客:** 最終的なアウトプット（楽曲情報）を受け取る対象。

### 2. ユースケース図 (Mermaid)

```mermaid
flowchart LR
    OP(["👤 オペレーター (OP)"])
    DJ(["🎧 ゲストDJ"])
    Audience(["📽 プロジェクター / 観客"])

    subgraph Synapse ["LiveEvent Visualizer 'Synapse' (Tauri + C#)"]
        direction TB
        UC1("事前設定 (カメラ/レイアウト/MIDI)")
        UC2("映像の取得 (Capture)")
        UC3("画像補正・OCR解析 (Pre-Process & OCR)")
        UC4("次曲候補の確認・修正 (Staging Edit)")
        UC5("送出・確定トリガー (Manual/MIDI GO)")
        UC6("テキスト/画像モード切替 (Toggle Mode)")
        UC7("履歴の保存と画面更新 (Output & Save)")
    end

    OP --> UC1
    DJ -. "ジャケットを置く等" .-> UC2
    OP --> UC4
    OP --> UC5
    OP --> UC6
    UC5 --> UC7
    UC7 --> Audience