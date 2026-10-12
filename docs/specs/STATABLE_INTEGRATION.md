# StaTable 連携仕様書

## 目的
StaTable（独立ツール）で設計した状態機械 XML を
PowerSimBuilder に取り込み、Python 制御ボックスコードを自動生成する。

## ステータス
- [x] 設計完了
- [ ] 実装（将来対応）

## アーキテクチャ
StaTable (独立) → design.xml → PowerSimBuilder → controller_box.py

## 追加モジュール（予定）
- StaTableIntegration/StaTableModel.cs
- StaTableIntegration/StaTableProjectReader.cs
- StaTableIntegration/StaTableCodeGenerator.cs
- StaTableIntegration/PythonCodeTemplates.cs
- StaTableIntegration/StaTableImportForm.cs

## 詳細
（前回提示した設計内容をここに貼り付け）
