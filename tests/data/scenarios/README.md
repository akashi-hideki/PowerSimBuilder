# Test Scenarios

End-to-end simulation scenarios used by pytest under tests/python/.

## Directory layout

Each scenario directory contains:

- system.json  -- serialized component/connection graph (schema below).
- README.md    -- description of what the scenario verifies.
- (optional) design.xml          -- original StaTable XML source.
- (optional) controller_box*.py  -- pre-generated Python code from design.xml.

## Scenarios

| Directory       | Components                                              | Verifies |
|-----------------|---------------------------------------------------------|----------|
| cooking_heater  | ControllerBox + HeaterBlock + Gain                      | 7-layer StaTable runtime, heater warm-up |
| motor_drive     | PWM3Phase + Gain + MechanicalInertia                    | duty -> torque -> speed response |
| heater_overheat | ThreePhaseSine + HeaterBlock + OverCurrentDetector      | thermal rise + over-current trip |

## system.json schema

    {
      "name": "scenario_id",
      "description": "human readable",
      "dt": 0.001,
      "t_end": 1.0,
      "components": [
        {
          "instanceName": "unique_id",
          "descriptorType": "ClassName",
          "parameters": { "key": "value" }
        }
      ],
      "connections": [
        { "from": "src_id.outputs.port", "to": "dst_id.inputs.port" }
      ]
    }

## 3-F Scenarios (SPEC_POWERSIM components)

| Directory            | Components                                  | Verifies |
|----------------------|---------------------------------------------|----------|
| generator_load       | Engine + Generator                          | torque -> voltage/current/power |
| rectifier_motor      | PowerSource + DiodeBridge + InductionMotor  | AC -> DC -> motor acceleration |
| fault_short_circuit  | Engine + Generator + FaultDevice            | fault trigger + power chain coexistence |
