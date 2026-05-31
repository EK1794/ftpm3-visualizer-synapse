# Phase 4: テスト仕様書 (Test Specifications & Acceptance Criteria)

**Project Name:** LiveEvent Visualizer "Synapse"
**Version:** 2.0 (Tauri Architecture)

### 1. ユニットテスト要件 (AI実装対象: C# Backend)
* [ ] **T-101 (IPC Routing):** `CommandEnvelope` として渡されたJSON文字列が正しく対応するHandlerメソッドへルーティングされること。
* [ ] **T-102 (Safety Mode):** `IsImageForced` フラグを True にした際、Output用のデータがテキストから画像へ切り替わること。
* [ ] **T-103 (History Export):** `CommitTrack` コマンドを受信した際、指定パスにJSON/Textファイルが正しく書き出されること。

### 2. 統合・ハードウェアテスト (実機確認対象)
* [ ] **H-201 (Sidecar Lifecycle):** Tauriアプリの起動・終了に連動して、C#バックエンドのコンソールプロセスがゾンビ化せずに正しく起動・終了すること。
* [ ] **H-202 (Base64 Stream):** カメラのフレームデータが設定したFPS（1〜5）の頻度で標準出力からJSONとして送出され、Web UI側で遅延なく描画されること。
* [ ] **H-203 (MIDI Trigger):** MIDIコントローラーのパッド押下時、C#側から `MidiMessageEvent` が即座に発行され、フロントエンドの [GO] ボタンと連動すること。