# Audio Carousel

[English](README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md)

設定したデバイスリストをグローバルホットキー1つで巡回し、システムの既定の音声出力デバイスを切り替える、軽量な Windows タスクトレイユーティリティです。

[PeekDesktop](https://github.com/shanselman/PeekDesktop) からインスパイアされた設計：インストーラ不要・単一実行ファイル・設定はポータブル。

## 使い方

1. [Releases](https://github.com/kai-rin/audio-carousel/releases) ページから `AudioCarousel.exe` をダウンロードし、任意のフォルダに配置します。
2. ダブルクリックで起動します。

   > 初回起動時の注意： バイナリは署名されていないため、Windows SmartScreen が「PC が保護されました」と表示することがあります。詳細情報 → 実行 をクリックしてください。あるいは起動前に .exe を右クリック → プロパティ → ブロックの解除 にチェック → OK でも構いません。

3. タスクトレイにアイコンが表示され、初回起動時は自動的に設定ウィンドウが開きます。
4. 巡回したい音声出力デバイスを追加し、ホットキー（例：`F16`、`Ctrl+Alt+A`）を設定して OK をクリックします。
5. ホットキーを押すとデバイスが順に切り替わります。アクティブモニタの右下にトーストが表示され、新しいデバイス名がわかります。
6. 必要に応じて、トレイメニューまたは設定から「Windows 起動時に開始」を有効にできます。

設定は実行ファイルと同じフォルダの `audio-carousel.json` に保存されます。レジストリへの書き込みは「Windows 起動時に開始」を有効にしたときの `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` だけで、影響を受けるのは現在のユーザーのみです。管理者権限は不要です。

## ビルド

Windows 上で .NET 9 SDK が必要です。

```bash
dotnet build
dotnet test
```

## 単一実行ファイルの発行

```powershell
pwsh ./scripts/publish.ps1
```

出力：`publish/AudioCarousel.exe`。

> バイナリサイズについて： Windows Forms は NativeAOT と非互換（`NETSDK1175`）であり、トリミングは `NAudio.CoreAudioApi` が依存する COM 相互運用機構を取り除いてしまいます。そのため本スクリプトは トリミング無効・自己完結型・JIT・単一ファイル発行 を採用しており、約 108 MB の実行ファイルが生成されます。このサイズは「.exe をどこに置いても、.NET ランタイム不要で動く」という利便性の対価です。

## 動作環境

- Windows 10 1809 以降、または Windows 11
- x64
- 管理者権限不要

## 免責事項

Audio Carousel は既定の音声エンドポイントを切り替えるために、非公開の Windows COM インターフェース である `IPolicyConfig` を呼び出します。これは SoundSwitch / EarTrumpet / NirCmd など多くの類似ツールが採用している手法で、Windows Vista から Windows 11 まで安定して動作してきました。ただし非公式 API のため、Microsoft が将来の Windows アップデートで予告なく変更・削除する可能性はあります。将来の Windows アップデートで Audio Carousel が動かなくなった場合は、Issue を立ててください。

リリースバイナリにはコード署名を行っていません（コード署名証明書には継続的な費用がかかるため、無償配布の本プロジェクトでは現状導入を見送っています）。バイナリのレピュテーションが蓄積されるまでは、初回起動時に Windows SmartScreen の警告が出ます。使い方 の *初回起動時の注意* を参照してください。

## セキュリティ

脅威モデルとセキュリティ問題の報告方法については [SECURITY.md](SECURITY.md) を参照してください。

## ライセンス

[MIT](LICENSE) — © 2026 kai-rin
