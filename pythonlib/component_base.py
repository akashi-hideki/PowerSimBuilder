from typing import Dict, Any
from abc import ABC, abstractmethod
from enum import Enum

class Component(ABC):
    """コンポーネントの共通ベースクラス"""
    
    def __init__(self, comp_id: str, comp_type: str):
        self.id = comp_id
        self.type = comp_type
        self.inputs: Dict[str, Any] = {}
        self.outputs: Dict[str, Any] = {}
        self.parameters: Dict[str, Any] = {}
        
    @abstractmethod
    def update(self, t: float, dt: float, sim_data: Any) -> None:
        """コンポーネントの状態を更新する抽象メソッド"""
        pass
        
    def get_status(self) -> Dict[str, Any]:
        """コンポーネントの現在の状態を取得"""
        return {
            'id': self.id,
            'type': self.type,
            'inputs': self.inputs,
            'outputs': self.outputs,
            'parameters': self.parameters
        }

class GeneratorType(Enum):
    """発電機の種類"""
    SALIENT_POLE = "salient_pole"  # 突極形
    CYLINDRICAL = "cylindrical"    # 円筒形

class ExcitationType(Enum):
    """励磁方式"""
    PERMANENT_MAGNET = "permanent_magnet"  # 永久磁石
    ELECTROMAGNETIC = "electromagnetic"    # 電磁石

class OperationMode(Enum):
    """動作モード"""
    STANDALONE = "standalone"      # 単独動作（回転数直接入力）
    ENGINE_DRIVEN = "engine_driven" # エンジン駆動（トルク入力）