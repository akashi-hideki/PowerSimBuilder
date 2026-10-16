"""StaTable runtime support.

StaTable XML から生成された controller_box.py が利用する基底クラスと
ランタイムを提供する。
"""

from .controller_base import StaTableControllerBase
from .runtime import StaTableRuntime

__all__ = [
    "StaTableControllerBase",
    "StaTableRuntime",
]
