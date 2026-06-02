# Phase 4: テスト仕様書 (Test Specifications & Acceptance Criteria)

**Project Name:** LiveEvent Visualizer "Synapse"
**Version:** 2.1 (Tauri Architecture & Workflow Update)

### 1. ユニットテスト要件 (AI実装対象: C# Backend)
* [ ] **T-101 (IPC Routing):** `CommandEnvelope` として渡されたJSON文字列が正しく対応するHandlerメソッドへルーティングされること。
* [ ] **T-102 (Safety Mode):** `IsImageForced` フラグを True にした際、Output用のデータがテキストから画像へ切り替わること。
* [ ] **T-103 (Midi Mapping Logic):** Note Number 0〜15 を受け取った際、正しく `slotIndex` (0-3) と `action` (go/undo/redo/toggle) にマッピングされ `MidiActionEvent` が発行されること。（※ファイル出力テストは廃止）

### 2. 統合・ハードウェアテスト (実機確認対象)
* [ ] **H-201 (Sidecar Lifecycle):** Tauriアプリの起動・終了に連動して、C#バックエンドのコンソールプロセスがゾンビ化せずに正しく起動・終了すること。
* [ ] **H-202 (Base64 Stream):** カメラのフレームデータが設定したFPS（1〜5）の頻度で標準出力からJSONとして送出され、Web UI側で遅延なく描画されること。
* [ ] **H-203 (Handshake Connection):** フロントエンド側からの `getStatus` コマンドに対し、C#側が正しく `StatusResponse` を返し、レースコンディションなくデバイス初期化が完了すること。

### 3. UI/UX シナリオテスト (人間確認対象)
* [ ] **U-301 (UI Capture Area):** 「送出用ディスプレイエリア」が独立して綺麗に描画され、OBSからウィンドウキャプチャで支障なく取り込めること。
* [ ] **U-302 (History Edit & Undo/Redo):** * 履歴パネルでテキストをインライン編集した際、送出用エリアの文字が即座に変更されること。
  * MIDI Padの Undo/Redo ボタンを押下した際、フロントエンドの表示ポインタが前後に移動し、送出エリアの表示が過去/未来の楽曲に切り替わること。