#!/usr/bin/env python3
"""
自動生成されるメインシミュレーションループ
VC#コード生成ツール用テンプレート
"""

import numpy as np
import json
from typing import Dict, Any

# コンポーネントインポート - VC#が自動的に挿入
{COMPONENT_IMPORTS}

# シミュレーションデータクラス
class SimulationData:
    """シミュレーションデータを保持するクラス"""
    def __init__(self):
        self.time_step = {TIME_STEP}
        self.logs = []
        self.parameters = {}
    
    def to_dict(self):
        """辞書形式に変換"""
        return {
            'time_step': self.time_step,
            'logs': self.logs,
            'parameters': self.parameters
        }

def main():
    """メインシミュレーションループ"""
    # シミュレーション設定
    dt = {TIME_STEP}
    t_end = {DURATION}
    sim_data = SimulationData()
    
    # コンポーネント初期化 - VC#が自動的に挿入
    components = {
        {COMPONENT_INITIALIZATIONS}
    }
    
    # 接続定義 - VC#が自動的に挿入
    connections = [
        {CONNECTIONS}
    ]
    
    print("シミュレーション開始")
    print(f"時間ステップ: {dt}秒, 終了時間: {t_end}秒")
    print(f"コンポーネント数: {len(components)}")
    print(f"接続数: {len(connections)}")
    print("-" * 50)
    
    # シミュレーションループ
    for t in np.arange(0, t_end, dt):
        # 1. すべてのコンポーネントを更新
        for comp in components.values():
            comp.update(t, dt, sim_data)
        
        # 2. 接続を更新（コンポーネント間のデータ伝搬）
        for conn in connections:
            from_comp, from_port = conn["from"].split(".")
            to_comp, to_port = conn["to"].split(".")
            
            if from_comp in components and to_comp in components:
                components[to_comp].inputs[to_port] = components[from_comp].outputs[from_port]
        
        # 3. ログ記録（指定間隔ごと）
        if t % {LOG_INTERVAL} < dt:
            sim_data.logs.append({
                "time": t,
                "components": {
                    cid: {
                        "inputs": comp.inputs.copy(),
                        "outputs": comp.outputs.copy(),
                        "parameters": comp.parameters.copy() if hasattr(comp, 'parameters') else {}
                    }
                    for cid, comp in components.items()
                }
            })
        
        # 4. 進行状況の表示（指定間隔ごと）
        if t % {DISPLAY_INTERVAL} < dt:
            {DISPLAY_CODE}
    
    print("シミュレーション完了")
    
    # 結果の保存
    save_results(sim_data, components)
    
    return sim_data, components

def save_results(sim_data: SimulationData, components: Dict[str, Any]):
    """シミュレーション結果を保存"""
    import pickle
    import datetime
    
    # タイムスタンプ付きのファイル名
    timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    filename = f"simulation_results_{timestamp}.pkl"
    
    # 結果データの準備
    results = {
        'simulation_data': sim_data.to_dict(),
        'components': {
            comp_id: comp.get_status() for comp_id, comp in components.items()
        },
        'timestamp': timestamp
    }
    
    # ピクル形式で保存
    with open(filename, 'wb') as f:
        pickle.dump(results, f)
    
    print(f"結果を {filename} に保存しました")
    
    # JSON形式でも保存（可読性のため）
    json_filename = f"simulation_results_{timestamp}.json"
    with open(json_filename, 'w') as f:
        json.dump(results, f, indent=2, default=str)
    
    print(f"JSON結果を {json_filename} に保存しました")

if __name__ == "__main__":
    # メインシミュレーションの実行
    sim_data, components = main()