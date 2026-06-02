# Phase 1: 要件定義書 (Requirement Definition)

**Project Name:** LiveEvent Visualizer "Synapse"
**Version:** 2.1 (Tauri Architecture & Workflow Update)

### 1. プロジェクト背景と目的
LiveEventにおける、「今かかっている曲の情報をフロアと共有する」という演出を技術的に支援する。

### 2. コア要件 (Core Requirements)
1. **入力の柔軟性:** Webカメラでの盤面撮影（画像・OCRテキスト抽出）に対応。HDMIキャプチャ等も許容する。
2. **OBS連携の最適化:** 従来のテキストファイル出力による連携を廃止。Tauri UI内に「送出用ディスプレイエリア」を構築し、OBSのウィンドウキャプチャで情報を取得する方式に統一する。
3. **MIDIマルチコントロール連携:** 4台のカメラ×4つのアクション（GO, Undo, Redo, Toggle Mode）を、16個の物理MIDI Pad（DDJ-SP1等）にマッピングし、即時操作可能とする。
4. **安全設計と履歴管理:** OCR誤認識に備え、一度送出した楽曲を後からUI上でインライン修正できる「セットリスト履歴編集機能」を備える。
5. **アーキテクチャの分離:** 非同期UIバインディングの安定性確保と描画パフォーマンス向上のため、UI層はWebフロントエンド（Tauri v2）で構築し、ハードウェア制御・ロジック層はC#（.NET 8 コンソール）のSidecarプロセスとして完全に分離する。