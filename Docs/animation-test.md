# Unity MCP 開発指示書：Idle → Run → Jump の確認用モック作成

## 目的

Unity MCPを使って、2D横スクロールアクションゲームの最小プロトタイプを作成してください。

まずは完成版ゲームではなく、以下を確認できる状態にしてください。

* キャラクターがIdle状態で待機する
* 左右移動でRunモーションに切り替わる
* ジャンプでJumpモーションに切り替わる
* 簡単なモック用ステージ上で操作できる
* Unity Editor上でPlayして確認できる

## 前提

* Unityプロジェクトは2Dで作成済み
* Unity MCPが使用可能な状態
* 使用言語はC#
* 入力はキーボードで確認する
* まずはアート品質よりも、動作確認を優先する
* スプライトが未設定の場合は、仮の四角形や簡易スプライトで代用してよい

## 作成してほしい構成

### Scene

以下のシーンを作成してください。

* `Assets/Scenes/PrototypeScene.unity`

シーン内には以下を配置してください。

* Player
* Ground
* Main Camera
* Directional Lightまたは2D用ライト
* 簡単な背景色

## Player仕様

### GameObject名

`Player`

### 必要コンポーネント

* SpriteRenderer
* Rigidbody2D
* CapsuleCollider2D または BoxCollider2D
* Animator
* PlayerController.cs

### 操作

* A / ←：左移動
* D / →：右移動
* Space：ジャンプ

### 挙動

* 地上で停止中：Idle
* 地上で左右移動中：Run
* 空中：Jump
* 左右移動時にSpriteRendererのflipXで向きを反転

## アニメーション

以下のAnimation Clipを作成してください。

* `Idle.anim`
* `Run.anim`
* `Jump.anim`

配置場所：

`Assets/Animations/Player/`

スプライト素材がない場合は、仮アニメーションで構いません。

例：

* Idle：少し上下するだけ
* Run：左右に少し揺れるだけ
* Jump：空中姿勢用の1枚

## Animator Controller

以下を作成してください。

`Assets/Animations/Player/PlayerAnimator.controller`

Animator Parameters：

* `Speed` / float
* `IsGrounded` / bool
* `VerticalVelocity` / float

State：

* Idle
* Run
* Jump

遷移条件：

### Idle → Run

* `Speed > 0.1`
* `IsGrounded == true`

### Run → Idle

* `Speed <= 0.1`
* `IsGrounded == true`

### Idle / Run → Jump

* `IsGrounded == false`

### Jump → Idle

* `IsGrounded == true`
* `Speed <= 0.1`

### Jump → Run

* `IsGrounded == true`
* `Speed > 0.1`

Has Exit Timeは基本OFFにしてください。

## PlayerController.cs

以下の仕様で作成してください。

配置場所：

`Assets/Scripts/Player/PlayerController.cs`

### 変数

* moveSpeed
* jumpForce
* groundCheck
* groundCheckRadius
* groundLayer
* Rigidbody2D
* Animator
* SpriteRenderer

### 処理

* Updateで入力取得
* FixedUpdateで移動処理
* Space押下時、地上ならジャンプ
* 地面判定はOverlapCircleまたはRaycastで行う
* Animatorに以下を渡す

  * Speed：Mathf.Abs(horizontalInput)
  * IsGrounded：接地判定
  * VerticalVelocity：Rigidbody2D.velocity.y

## ステージモック

以下を作成してください。

### Ground

* 横長のBoxCollider2Dつき床
* 位置：`(0, -3, 0)`
* サイズ：横20、縦1程度
* Layer：Ground

### 足場

可能であれば追加してください。

* 小さな足場を2〜3個
* ジャンプ確認用
* Layer：Ground

## カメラ

Main Cameraを2D用に設定してください。

* Orthographic
* Size：5〜6程度
* Playerが見える位置
* 背景色は暗めのグレー

可能であれば、簡単なCameraFollow.csを作成してPlayerを追従してください。

ただし、最優先はIdle / Run / Jumpの確認です。

## デバッグ表示

Unity Editor上で確認しやすいように、以下のどちらかを実装してください。

* Consoleに現在の状態を出す
* 画面上に簡単なDebug UIを出す

表示内容：

* Speed
* IsGrounded
* VerticalVelocity
* Current Animation State

## 完了条件

以下が確認できたら完了です。

1. UnityでPrototypeSceneを開ける
2. PlayするとPlayerが地面に立っている
3. 何も押さないとIdleになる
4. 左右移動でRunになる
5. SpaceでJumpになる
6. 着地後、停止していればIdleに戻る
7. 着地後、移動入力中ならRunに戻る
8. エラーがConsoleに出ていない

## 注意点

* まずは動くことを最優先にしてください
* 見た目は仮で問題ありません
* 複雑な攻撃、HP、敵、エフェクトはまだ不要です
* Input Systemではなく、まずは旧Input Managerの `Input.GetAxisRaw` と `Input.GetButtonDown` で構いません
* 後で本番スプライトに差し替えやすい構成にしてください

## 最終的に報告してほしい内容

作業後、以下を報告してください。

* 作成したファイル一覧
* 作成したシーン名
* 操作方法
* Animator Parameters
* 未対応・仮対応の箇所
* 次に追加すべき作業
