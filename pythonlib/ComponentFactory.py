# component_factory.py
from typing import Dict, Any
import xml.etree.ElementTree as ET

class ComponentFactory:
    @staticmethod
    def create_component(comp_type: str, comp_id: str) -> Component:
        if comp_type == "LCFilter":
            return LCFilter(comp_id)
        elif comp_type == "DiodeBridge":
            return DiodeBridge(comp_id)
        elif comp_type == "IGBTRectifier":
            return IGBTRectifier(comp_id)
        elif comp_type == "PWMChopper":
            return PWMChopper(comp_id)
        else:
            raise ValueError(f"Unknown component type: {comp_type}")

    @staticmethod
    def load_configuration(xml_file: str) -> Dict[str, Component]:
        tree = ET.parse(xml_file)
        root = tree.getroot()
        
        components = {}
        
        for comp_elem in root.findall('.//Components/*'):
            comp_type = comp_elem.tag
            comp_id = comp_elem.get('id')
            
            # コンポーネント作成
            component = ComponentFactory.create_component(comp_type, comp_id)
            
            # パラメータ設定
            for param_elem in comp_elem.findall('Parameters/*'):
                param_name = param_elem.tag
                param_value = float(param_elem.text) if param_elem.text.replace('.', '').isdigit() else param_elem.text
                component.parameters[param_name] = param_value
            
            components[comp_id] = component
        
        return components