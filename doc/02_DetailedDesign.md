# Phase 2: ソフトウェア基本・詳細設計書 (Architecture & Detailed Design)

**Project Name:** LiveEvent Visualizer "Synapse"
**Version:** 2.1 (Tauri Architecture & Workflow Update)
**Tech Stack:** TypeScript / React (Web UI) / Tauri v2 (Rust Core) / C# .NET 8 Console (Sidecar)

> **[📝 AI-Driven Development Note]**
> 本ドキュメントは、AIエディタ（Antigravity等）による自律的なコーディングを最適化するため、意図的に「基本設計（外部仕様・What）」と「詳細設計（内部仕様・How）」を単一のファイルに統合しています。AIに対し、UIの目的とバックグラウンドの処理機構を分断させず、連続したコンテキストとして理解させることを目的とします。
> 
> **【🚨 暫定仕様変更に関するルール】**
> 実装中にライブラリの制約等で本仕様書通りの実装が困難になった場合、AIは独断で設計を根本から覆してはなりません。ただし、軽微な技術的迂回が必要な場合は、「なぜ変更したか」「どのように迂回したか」を明記した `CHANGELOG_DRAFT.md` をルートディレクトリに生成した上で実装を進めてください。これらは後日、人間の設計者によって再評価されます。
> 
> **【重要】** 本プロジェクトはTauriのSidecarパターンを採用しています。UI（Web）とバックエンド（C#）は別プロセスで動作し、標準入出力（stdio）を用いたJSONメッセージで通信を行います。C#側はHeadlessなコンソールアプリとして実装し、System.Windows系の参照は含めないでください。

### 1. アーキテクチャ方針
* **Tauri Sidecar Pattern:** * `Frontend (Web)`: ユーザーインターフェースと状態のバインディング（React等）。
  * `Core (Rust)`: ウィンドウ管理とC#プロセスの起動・ライフサイクル管理。
  * `Backend (C#)`: カメラキャプチャ、OCR、MIDI制御、ステートマシン。
* **プロセス間通信 (IPC):** C#バックエンドは `Console.ReadLine()` でコマンド（JSON）を受信し、`Console.WriteLine()` でイベント（JSON）をフロントエンドへ送出する。
* **映像ストリーミング:** C#で取得したフレーム（1〜5fps）はBase64エンコードし、JSONイベントとしてUIへ送信する。共有メモリ等は使用しない。

### 2. 画面ワイヤーフレーム (Main Console)
AIはWeb UI (React / CSS等) 実装時、以下のレイアウト構造を基準とすること。OBSで切り抜くための「Output Display Area」を独立して設ける。

```text
【Main Operation Console (Dark Theme)】
+-----------------------+-------------------------+-------------------------+
| [Dev: ComboBox ▼  ]   | [Staging Area] (プレビュー)| [Output Display Area]   |
| [Camera 1 Preview]    | Title:  [ Editable  ]   | (※OBSキャプチャ対象)      |
|   [ Capture (Cam1) ]  | Artist: [ Editable  ]   |                         |
+-----------------------+ Album:  [ Editable  ]   | Title / Artist / Album  |
| [Dev: ComboBox ▼  ]   |                         | + Artwork Image         |
| [Camera 2 Preview]    | [ ] Force Image Mode    |                         |
|   [ Capture (Cam2) ]  | Image Filter Sliders... +-------------------------+
+-----------------------+-------------------------+ [History Panel]         |
| [Dev: ComboBox ▼  ]   |   ==================    | 1. Track A (Double-click|
| [Camera 3 Preview]    |   [    MIDI GO     ]    |    to inline edit)      |
|   [ Capture (Cam3) ]  |   ==================    | 2. Track B              |
+-----------------------+                         | 3. Track C              |
| [Camera 4 Preview]... |                         |                         |
+-----------------------+-------------------------+-------------------------+
```

### 3. MIDIマッピング仕様 (C# NAudio)
コントローラーの16個のPad（Note）を以下の規則で処理し、JSONでフロントエンドへルーティングする。
* **計算式:** `slotIndex = Note / 4` , `action = Note % 4`

| Note % 4 | 実行アクション | IPC イベント (C# -> UI) |
|:---|:---|:---|
| **0** | **確定・送出 (GO)** | `{"event":"MidiAction", "payload":{"slotIndex": x, "action":"go"}}` |
| **1** | **一つ戻す (Undo)** | `{"event":"MidiAction", "payload":{"slotIndex": x, "action":"undo"}}` |
| **2** | **一つ進む (Redo)** | `{"event":"MidiAction", "payload":{"slotIndex": x, "action":"redo"}}` |
| **3** | **モード切替 (Txt/Img)**| `{"event":"MidiAction", "payload":{"slotIndex": x, "action":"toggle"}}` |