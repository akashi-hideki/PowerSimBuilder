"""Tests for MechanicalInertia."""

from __future__ import annotations

import math

from python_runtime.components import MechanicalInertia


def test_initial_speed():
    mi = MechanicalInertia("mi")
    mi.parameters["initial_speed"] = 10.0
    mi.reset()
    assert mi.outputs["speed"] == 10.0


def test_accelerates_with_torque():
    mi = MechanicalInertia("mi")
    mi.parameters["inertia"] = 0.01
    mi.inputs["torque_drive"] = 1.0
    mi.update(0.0, 0.01, None)
    # a = 1 / 0.01 = 100 rad/s^2, w = 100 * 0.01 = 1
    assert abs(mi.outputs["speed"] - 1.0) < 1e-6


def test_viscous_friction_damping():
    mi = MechanicalInertia("mi")
    mi.parameters["inertia"] = 0.01
    mi.parameters["viscous_friction"] = 1.0
    mi.parameters["initial_speed"] = 10.0
    mi.reset()
    mi.update(0.0, 0.001, None)
    # No drive torque, but friction damping
    assert mi.outputs["speed"] < 10.0


def test_rpm_conversion():
    mi = MechanicalInertia("mi")
    mi.parameters["initial_speed"] = 0.0
    mi.reset()
    # 60 rpm = 2*pi rad/s
    mi.outputs["speed"] = 2.0 * math.pi
    mi.inputs["torque_drive"] = 0.0
    mi.inputs["torque_load"] = 0.0
    mi.update(0.0, 0.0001, None)
    # speed_rpm should be very close to 60
    assert abs(mi.outputs["speed_rpm"] - 60.0) < 1.0
