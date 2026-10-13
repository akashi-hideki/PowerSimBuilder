"""Integration tests: example scenarios as regression suite.

Each scenario is run and key observable outputs are asserted. These
tests protect against regressions in the Python runtime while the
C#-generated code depends on it.
"""
from __future__ import annotations

import pytest

from examples import scenario_a, scenario_b, scenario_c


def _led_states(logs):
    return [e["components"]["led"]["outputs"]["state"] for e in logs]


class TestScenarioA:
    """button -> delay timer -> LED."""

    def test_build_returns_runner(self):
        runner = scenario_a.build()
        assert runner is not None
        assert "button" in runner.components
        assert "timer" in runner.components
        assert "led" in runner.components

    def test_led_turns_on_at_some_point(self):
        """LED should turn ON at least once during the run."""
        runner = scenario_a.build()
        runner.run(t_end=1.2)

        logs = runner.sim_data.logs
        assert len(logs) > 0

        states = _led_states(logs)
        assert 1 in states, f"LED never turned ON. States seen: {set(states)}"

    def test_timer_fires(self):
        """Timer 'done' should become True at least once."""
        runner = scenario_a.build()
        runner.run(t_end=1.2)

        logs = runner.sim_data.logs
        done_states = [e["components"]["timer"]["outputs"]["done"] for e in logs]
        assert True in done_states, "Timer never fired"


class TestScenarioB:
    """Analog scaling + first-order filter + PID."""

    def test_build_returns_runner(self):
        runner = scenario_b.build()
        assert runner is not None
        assert len(runner.components) >= 3

    def test_no_nan_in_outputs(self):
        runner = scenario_b.build()
        runner.run(t_end=1.0)

        logs = runner.sim_data.logs
        assert len(logs) > 0

        last = logs[-1]
        for comp_id, comp in last["components"].items():
            for key, value in comp["outputs"].items():
                if isinstance(value, (int, float)):
                    assert value == value, f"NaN in {comp_id}.{key}"


class TestScenarioC:
    """Rising-edge counter + state transition."""

    def test_build_returns_runner(self):
        runner = scenario_c.build()
        assert runner is not None

    def test_counter_increments(self):
        runner = scenario_c.build()
        runner.run(t_end=1.5)

        logs = runner.sim_data.logs
        last = logs[-1]

        counter_values = [
            comp["outputs"].get("count")
            for comp in last["components"].values()
            if "count" in comp["outputs"]
        ]
        assert len(counter_values) == 1, "Expected exactly one counter component"
        assert counter_values[0] >= 3, f"Counter did not increment: {counter_values[0]}"

    def test_state_machine_reaches_done(self):
        """The state machine output 'current_state' should reach DONE."""
        runner = scenario_c.build()
        runner.run(t_end=1.5)

        logs = runner.sim_data.logs

        # StateMachine exposes 'current_state' in its outputs
        sm_states_by_step = []
        for entry in logs:
            for comp in entry["components"].values():
                if "current_state" in comp["outputs"]:
                    sm_states_by_step.append(comp["outputs"]["current_state"])

        assert len(sm_states_by_step) > 0, "No state machine found"
        assert "DONE" in sm_states_by_step, (
            f"State machine never reached DONE. States seen: {set(sm_states_by_step)}"
        )
