from __future__ import annotations

import numpy as np
import matplotlib.pyplot as plt

from python_runtime import SimulationData
from python_runtime.components import DigitalOutput, Sum


def main():
    dt = 0.001
    t_end = 5.0
    sim_data = SimulationData(time_step=dt)
    # コンポーネント辞書の定義
    components = {
        "digital output": DigitalOutput("digital output"),
        "sum": Sum("sum"),
    }

    # Digital Output のパラメータ設定
    components["digital output"].parameters["active_low"] = False
    components["digital output"].parameters["on_current_ma"] = 20

    # Sum のパラメータ設定
    components["sum"].parameters["input_count"] = 3

    # 接続リストの定義
    connections = [
        {"from":"digital output.outputs.current_draw","to":"sum.inputs.input_1"},
    ]

    n_steps = int(round(t_end / dt))
    for i in range(n_steps + 1):
        t = i * dt
        for comp in components.values():
            comp.update(t, dt, sim_data)

        for conn in connections:
            src_id, src_sec, src_port = conn["from"].split(".")
            dst_id, dst_sec, dst_port = conn["to"].split(".")
            components[dst_id].inputs[dst_port] = components[src_id].outputs[src_port]

        sim_data.logs.append({
            "time": t,
            "components": {cid: {"inputs": comp.inputs.copy(),
                                 "outputs": comp.outputs.copy(),
                                 "parameters": comp.parameters.copy()}
                           for cid, comp in components.items()}
        })

    # シミュレーション実行後のプロット
    plot_signals(sim_data, plot_config)
    print("シミュレーション完了")


# プロット設定（ベースデータ）
# 接続情報から自動生成された信号設定
plot_config = {
    'signals': [
        {'name': 'digital output_current_draw', 'component': 'digital output', 'port': 'current_draw', 'enabled': True, 'color': '#FF5733', 'line_style': '-'},
    ],
    'global_settings': {
        'figure_size': (12, 8),
        'title': 'シミュレーション信号',
        'xlabel': '時間 (秒)',
        'ylabel': '値',
        'grid': True,
        'legend_location': 'upper right',
        'save_format': 'png',
        'dpi': 300
    }
}


# プロット関数（コード）
def plot_signals(sim_data, plot_config):
    """
    設定に基づいて信号をプロットする

    Parameters:
    sim_data: SimulationData
        シミュレーションデータオブジェクト
    plot_config: dict
        プロット設定
    """

    # グローバル設定の適用
    settings = plot_config['global_settings']
    plt.figure(figsize=settings['figure_size'])
    plt.title(settings['title'])
    plt.xlabel(settings['xlabel'])
    plt.ylabel(settings['ylabel'])

    if settings['grid']:
        plt.grid(True, linestyle='--', alpha=0.7)

    # 有効な信号のみをフィルタリング
    enabled_signals = [s for s in plot_config['signals'] if s['enabled']]

    # 各信号のデータを収集してプロット
    for signal in enabled_signals:
        comp_name = signal['component']
        port_name = signal['port']

        # 時間と値を格納するリスト
        times = []
        values = []

        # 各時間ステップのデータを収集
        for log in sim_data.logs:
            time = log['time']
            comp_data = log['components'].get(comp_name, {})
            outputs = comp_data.get('outputs', {})
            value = outputs.get(port_name, 0)

            times.append(time)
            values.append(value)

        # データをプロット
        if times and values:
            color = signal.get('color', None)
            line_style = signal.get('line_style', '-')

            plt.plot(times, values, label=signal['name'], color=color, linestyle=line_style, linewidth=2)

    # 凡例を追加
    plt.legend(loc=settings['legend_location'])

    # レイアウト調整
    plt.tight_layout()

    # グラフを表示
    plt.show()

    # グラフを保存
    save_format = settings['save_format']
    dpi = settings['dpi']
    plt.savefig(f'simulation_signals.{save_format}', bbox_inches='tight', dpi=dpi)
    print(f"グラフを 'simulation_signals.{save_format}' に保存しました")


def toggle_signal(plot_config, signal_name, enabled=None):
    """
    信号の有効/無効を切り替える

    Parameters:
    plot_config: dict
        プロット設定
    signal_name: str
        信号名
    enabled: bool, optional
        設定する状態（Noneの場合はトグル）

    Returns:
    bool: 新しい状態
    """
    for signal in plot_config['signals']:
        if signal['name'] == signal_name:
            if enabled is None:
                # トグル
                signal['enabled'] = not signal['enabled']
            else:
                signal['enabled'] = enabled

            status = '有効' if signal['enabled'] else '無効'
            print(f"信号 '{signal_name}' を {status} に設定しました")
            return signal['enabled']

    print(f"エラー: 信号 '{signal_name}' が見つかりませんでした")
    return False


def list_signals(plot_config):
    """
    すべての信号とその状態をリスト表示する

    Parameters:
    plot_config: dict
        プロット設定
    """
    print("利用可能な信号:")
    print("-" * 50)
    for signal in plot_config['signals']:
        status = "有効" if signal['enabled'] else "無効"
        print(f"{signal['name']}: {status} (コンポーネント: {signal['component']}, ポート: {signal['port']})")


if __name__ == "__main__":
    main()
