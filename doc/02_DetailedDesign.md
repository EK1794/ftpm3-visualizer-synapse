# Phase 2: ソフトウェア基本・詳細設計書 (Architecture & Detailed Design)

**Project Name:** LiveEvent Visualizer "Synapse"
**Version:** 2.0 (Tauri Architecture)
**Tech Stack:** TypeScript / React (Web UI) / Tauri v2 (Rust Core) / C# .NET 8 Console (Sidecar)

> **[📝 AI-Driven Development Note]**
> 本ドキュメントは、AIエディタ（Antigravity等）による自律的なコーディングを最適化するため、意図的に「基本設計（外部仕様・What）」と「詳細設計（内部仕様・How）」を単一のファイルに統合しています。AIに対し、UIの目的とバックグラウンドの処理機構を分断させず、連続したコンテキストとして理解させることを目的とします。
> 
> **【重要】** 本プロジェクトはTauriのSidecarパターンを採用しています。UI（Web）とバックエンド（C#）は別プロセスで動作し、標準入出力（stdio）を用いたJSONメッセージで通信を行います。C#側はHeadlessなコンソールアプリとして実装し、System.Windows系の参照は含めないでください。

### 1. アーキテクチャ方針
* **Tauri Sidecar Pattern:** * `Frontend (Web)`: ユーザーインターフェースと状態のバインディング（React等）。
  * `Core (Rust)`: ウィンドウ管理とC#プロセスの起動・ライフサイクル管理。
  * `Backend (C#)`: カメラキャプチャ、OCR、MIDI制御、ステートマシン。
* **プロセス間通信 (IPC):** C#バックエンドは `Console.ReadLine()` でコマンド（JSON）を受信し、`Console.WriteLine()` でイベント（JSON）をフロントエンドへ送出する。
* **映像ストリーミング:** C#で取得したフレーム（1〜5fps）はBase64エンコードし、JSONイベントとしてUIへ送信する。共有メモリ等は使用しない。

### 2. 画面ワイヤーフレーム (Main Console)
AIはWeb UI (React / CSS等) 実装時、以下のレイアウト構造を基準とすること。MIDI機器の有無に関わらず、全ての基本操作がUI上で完結する。各カメラプレビューの上部にはデバイス選択用のComboBoxを配置する。

```text
【Main Operation Console (Dark Theme)】
+-----------------------+-------------------------+-----------------------+
| [Dev: ComboBox ▼  ]   | [Staging Area] (次曲)   | [History Panel]       |
| [Camera 1 Preview]    |                         |                       |
|   [ Capture (Cam1) ]  | Title:  [ Editable  ]   | 1. Track A [Img/Txt]  |
+-----------------------+ Artist: [ Editable  ]   | 2. Track B [Img/Txt]  |
| [Dev: ComboBox ▼  ]   | Album:  [ Editable  ]   | 3. Track C [Img/Txt]  |
| [Camera 2 Preview]    |                         |                       |
|   [ Capture (Cam2) ]  | [ ] Force Image Mode    |                       |
+-----------------------+ Image Filter Sliders... |                       |
| [Dev: ComboBox ▼  ]   |-------------------------+-----------------------+
| [Camera 3 Preview]    |   ==================    | [Settings] [Layout]   |
|   [ Capture (Cam3) ]  |   [    MIDI GO     ]    |                       |
+-----------------------+   ==================    |                       |
| [Dev: ComboBox ▼  ]...|                         |                       |
+-----------------------+-------------------------+-----------------------+