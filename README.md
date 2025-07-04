# ClashTowerWithMonster（開発中）

## 概要
斜め上視点のリアルタイムオンライン対戦ゲームです。  
プレイヤーが自キャラを操作しつつ、相手の手札やデッキに対応した戦略を構築し、スキルを使ったり、カードでモンスターやスペルを召喚してタワーを破壊します。

※現時点では「相手の卵を破壊 → そこから出現するモンスターを倒すと勝利」という仕様を予定していますが、確定ではなく、実装にも着手していません。

現在は以下の機能を中心に開発中です：

- AIモンスターの行動ロジック
- プレイヤーキャラクターの挙動
- カードUI
- 召喚・移動・戦闘のコアシステム

---

## 技術的な見どころ
- ステートマシンベースのAI制御  
　└ 特に移動・攻撃時の「不快じゃないキャンセル処理」にこだわり
- ユニットの押し出し処理や楕円形の当たり判定
- DOTweenを活用したカードUIの拡大・選択エフェクト
- UniTaskによる非同期処理の導入

---

## 今後の予定
- モンスター種の追加とバランス調整
- オンライン対戦、デッキ編成機能の実装
- ビジュアル・演出面のブラッシュアップ

---
##操作方法
- **左クリック**：目標地点を指定して移動します。移動中でも再度左クリックで新しい地点に移動可能です（攻撃中も移動可能）。
- **A + 左クリック**：移動中でも敵を見つけると停止して攻撃します。
- **カード上で右クリック → フィールド上で右クリック**：カードを選択してモンスター/スペルを召喚します。
- **カード変更**：新たに別のカード上で右クリックすることで変更できます。
- **手札枠内での召喚**：通常は不可ですが、**Eキー**を押すと可能になります。
- **Wキー**：選択中のカードを解除します。
- **Qキー**：手札の表示/非表示を切り替えます。
## 注意点（開発中）

- オンライン化は未実装のため、`.exe` ビルドでは敵キャラやデッキは固定です  
- 開発中のため、スペースキーを押すとエラーを吐いたり、手札からモンスターを出す前に召喚タイマーが動いてしまうことがあります  
- カードデータの設定が必要です（Unityプロジェクトを使用する場合）

### Unityプロジェクトで試す場合：

1. `CardData` の `CardPrefab` に、モンスターまたはスペルのプレファブを設定してください  
2. `CardType` を正しく設定してください（モンスターなら `Monster`、スペルなら `Spell`）  
3. `Energy` は最大10以下で設定してください（超えると召喚できません）

---

## 実行方法

- `ClashTowerWithMonster.exe` を実行してください。  
（ビルド済みファイルはギガファイル便で配布予定）

---

## GitHub（コード確認用）

[https://github.com/TK220107miyatayuuki/ClashTowerWithMonster](https://github.com/TK220107miyatayuuki/ClashTowerWithMonster)
