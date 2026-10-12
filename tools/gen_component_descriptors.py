"""Generate new ComponentDescriptors.xml (control-board I/O palette)."""
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TARGETS = [
    ROOT / "bin" / "Debug" / "Pallets" / "ComponentDescriptors.xml",
    ROOT / "Pallets" / "ComponentDescriptors.xml",
]

XML = '''<?xml version="1.0"?>
<ArrayOfComponentDescriptor xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <ComponentDescriptor>
    <Name>Digital Input</Name>
    <Type>DigitalInput</Type>
    <Category>Digital I/O</Category>
    <Parameters>
      <ParameterDescriptor>
        <Name>debounce_ms</Name>
        <Type>double</Type>
        <DefaultValue Type="System.Double">0</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>initial</Name>
        <Type>int</Type>
        <DefaultValue Type="System.Int32">0</DefaultValue>
      </ParameterDescriptor>
    </Parameters>
    <InputPorts>
      <string>state</string>
    </InputPorts>
    <OutputPorts>
      <string>state</string>
      <string>edge_rising</string>
      <string>edge_falling</string>
    </OutputPorts>
  </ComponentDescriptor>
  <ComponentDescriptor>
    <Name>Digital Output</Name>
    <Type>DigitalOutput</Type>
    <Category>Digital I/O</Category>
    <Parameters>
      <ParameterDescriptor>
        <Name>active_low</Name>
        <Type>bool</Type>
        <DefaultValue Type="System.Boolean">false</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>on_current_ma</Name>
        <Type>double</Type>
        <DefaultValue Type="System.Double">20</DefaultValue>
      </ParameterDescriptor>
    </Parameters>
    <InputPorts>
      <string>command</string>
    </InputPorts>
    <OutputPorts>
      <string>state</string>
      <string>current_draw</string>
    </OutputPorts>
  </ComponentDescriptor>
  <ComponentDescriptor>
    <Name>Analog Input</Name>
    <Type>AnalogInput</Type>
    <Category>Analog I/O</Category>
    <Parameters>
      <ParameterDescriptor>
        <Name>v_min</Name>
        <Type>double</Type>
        <DefaultValue Type="System.Double">0</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>v_max</Name>
        <Type>double</Type>
        <DefaultValue Type="System.Double">5</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>resolution</Name>
        <Type>int</Type>
        <DefaultValue Type="System.Int32">12</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>filter_alpha</Name>
        <Type>double</Type>
        <DefaultValue Type="System.Double">0</DefaultValue>
      </ParameterDescriptor>
    </Parameters>
    <InputPorts>
      <string>raw</string>
    </InputPorts>
    <OutputPorts>
      <string>raw</string>
      <string>scaled</string>
      <string>filtered</string>
    </OutputPorts>
  </ComponentDescriptor>
  <ComponentDescriptor>
    <Name>Analog Output</Name>
    <Type>AnalogOutput</Type>
    <Category>Analog I/O</Category>
    <Parameters>
      <ParameterDescriptor>
        <Name>v_ref</Name>
        <Type>double</Type>
        <DefaultValue Type="System.Double">5</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>pwm_freq</Name>
        <Type>double</Type>
        <DefaultValue Type="System.Double">1000</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>resolution</Name>
        <Type>int</Type>
        <DefaultValue Type="System.Int32">10</DefaultValue>
      </ParameterDescriptor>
    </Parameters>
    <InputPorts>
      <string>value</string>
    </InputPorts>
    <OutputPorts>
      <string>duty</string>
      <string>voltage</string>
      <string>active</string>
    </OutputPorts>
  </ComponentDescriptor>
  <ComponentDescriptor>
    <Name>PID Controller</Name>
    <Type>PIDController</Type>
    <Category>Control</Category>
    <Parameters>
      <ParameterDescriptor>
        <Name>kp</Name>
        <Type>double</Type>
        <DefaultValue Type="System.Double">1</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>ki</Name>
        <Type>double</Type>
        <DefaultValue Type="System.Double">0</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>kd</Name>
        <Type>double</Type>
        <DefaultValue Type="System.Double">0</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>out_min</Name>
        <Type>double</Type>
        <DefaultValue Type="System.Double">-1e9</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>out_max</Name>
        <Type>double</Type>
        <DefaultValue Type="System.Double">1e9</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>setpoint</Name>
        <Type>double</Type>
        <DefaultValue Type="System.Double">0</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>anti_windup</Name>
        <Type>bool</Type>
        <DefaultValue Type="System.Boolean">false</DefaultValue>
      </ParameterDescriptor>
    </Parameters>
    <InputPorts>
      <string>measured</string>
      <string>setpoint</string>
    </InputPorts>
    <OutputPorts>
      <string>control</string>
      <string>error</string>
      <string>p_term</string>
      <string>i_term</string>
      <string>d_term</string>
      <string>saturated</string>
    </OutputPorts>
  </ComponentDescriptor>
  <ComponentDescriptor>
    <Name>Timer</Name>
    <Type>Timer</Type>
    <Category>Control</Category>
    <Parameters>
      <ParameterDescriptor>
        <Name>mode</Name>
        <Type>string</Type>
        <DefaultValue Type="System.String">delay</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>duration</Name>
        <Type>double</Type>
        <DefaultValue Type="System.Double">1</DefaultValue>
      </ParameterDescriptor>
    </Parameters>
    <InputPorts>
      <string>trigger</string>
      <string>reset</string>
    </InputPorts>
    <OutputPorts>
      <string>done</string>
      <string>running</string>
      <string>elapsed</string>
    </OutputPorts>
  </ComponentDescriptor>
  <ComponentDescriptor>
    <Name>Counter</Name>
    <Type>Counter</Type>
    <Category>Control</Category>
    <Parameters>
      <ParameterDescriptor>
        <Name>min_count</Name>
        <Type>int</Type>
        <DefaultValue Type="System.Int32">0</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>max_count</Name>
        <Type>int</Type>
        <DefaultValue Type="System.Int32">10</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>initial</Name>
        <Type>int</Type>
        <DefaultValue Type="System.Int32">0</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>direction</Name>
        <Type>string</Type>
        <DefaultValue Type="System.String">up</DefaultValue>
      </ParameterDescriptor>
      <ParameterDescriptor>
        <Name>wrap</Name>
        <Type>bool</Type>
        <DefaultValue Type="System.Boolean">false</DefaultValue>
      </ParameterDescriptor>
    </Parameters>
    <InputPorts>
      <string>clock</string>
      <string>reset</string>
    </InputPorts>
    <OutputPorts>
      <string>count</string>
      <string>overflow</string>
      <string>underflow</string>
    </OutputPorts>
  </ComponentDescriptor>
  <ComponentDescriptor>
    <Name>State Machine</Name>
    <Type>StateMachine</Type>
    <Category>Control</Category>
    <Parameters>
      <ParameterDescriptor>
        <Name>initial_state</Name>
        <Type>string</Type>
        <DefaultValue Type="System.String">IDLE</DefaultValue>
      </ParameterDescriptor>
    </Parameters>
    <InputPorts>
      <string>event</string>
      <string>reset</string>
    </InputPorts>
    <OutputPorts>
      <string>current_state</string>
      <string>prev_state</string>
      <string>transitioned</string>
    </OutputPorts>
  </ComponentDescriptor>
</ArrayOfComponentDescriptor>
'''

# Backup existing
for target in TARGETS:
    if target.exists():
        backup = target.with_suffix(".xml.old")
        if not backup.exists():
            backup.write_text(target.read_text(encoding="utf-8"), encoding="utf-8")
            print(f"  backup: {backup}")

# Write new
for target in TARGETS:
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(XML, encoding="utf-8")
    print(f"  + {target} ({len(XML)} chars)")