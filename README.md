# Nagara（ながら）

**作業しながら、動画見ながら、ゲームしながら — でも音楽の操作はいつも同じ場所へ。**

[English README](README.en.md)

Nagara は、キーボードのメディアキー（再生／停止・次へ・前へ）の送り先を固定できる、macOS 向けの常駐フリーソフトです（Windows 版は開発中）。

## 解決する問題

メディアキーは通常「最後にメディアを掴んだアプリ」に流れるため、Spotify で音楽を聴きながらブラウザで動画を開いていると、止めたい方と違うアプリが操作されてしまいます。Nagara はメディアキーの送り先を自分で選んで固定し、この取り合いをなくします。

## ダウンロードとインストール（macOS）

1. [Releases](https://github.com/Ren9618/Nagara/releases) から最新の `Nagara-vX.Y.Z-mac.zip` をダウンロードし、解凍して `Nagara.app` を「アプリケーション」フォルダに移動します。
2. 初回起動：開発者署名のないアプリのため、そのままでは開けない場合があります。その場合は `Nagara.app` を右クリック →「開く」を選ぶか、システム設定 → プライバシーとセキュリティ で「このまま開く」を許可してください。
3. メディアキーの横取りには「アクセシビリティ」の許可が必要です。アプリの案内に沿って、システム設定 → プライバシーとセキュリティ → アクセシビリティ で Nagara を ON にしてください。
4. Spotify / Apple Music の操作には「オートメーション」の許可が必要です。初めて操作するときに macOS が確認するので、許可してください。

## 主な機能

- メディアキーの送り先を固定：Spotify / Apple Music / 自動（再生中のプレイヤーへ）
- メニューバーからワンクリックで送り先を切替
- 送り先アプリが起動していない場合、再生キーで起動して再生を開始
- ログイン時起動の ON / OFF
- 多言語対応：日本語 / 英語（OS 言語に追従）
- 通信なし・テレメトリなし。設定はローカルにのみ保存されます

## 対応環境

- macOS 14.6 以降（Universal Binary：Apple Silicon / Intel）
  - 動作確認済み：macOS 27.0.1
- Windows：開発中

## 既知の制約

- 「自動」モードの送り先判定は、Nagara が最後に送った先とキー押下時点の再生状態をもとにした近似です。Nagara を介さず手動で再生・停止した場合は、判定が古くなることがあります。
- アプリを差し替えると、macOS の仕様によりアクセシビリティ許可の再設定が必要になる場合があります。

## ビルド方法（macOS）

Xcode が必要です。

```bash
cd macos
DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer \
  xcodebuild -project Nagara.xcodeproj -scheme Nagara \
  -configuration Release build
```

## 仕様

仕様ドラフトは [docs/spec.md](docs/spec.md) を参照してください。

## ライセンス

MIT License

## 作者

レイノワークス（Rayno Works）
- Megrab Studio（動画ダウンローダー）: https://megrab.mikumiku.online
- X: https://x.com/Rayno_works

※ 個人開発のため、Issue は読みますが対応は保証しません。
