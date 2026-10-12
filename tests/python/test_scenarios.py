"""Integration tests for example scenarios."""

from examples import scenario_a, scenario_b, scenario_c


def test_scenario_a_button_timer_led():
    runner = scenario_a.build()
    runner.run(t_end=1.2)

    # Before t=0.6: LED off
    entry_05 = runner.sim_data.logs[50]
    assert entry_05["components"]["led"]["outputs"]["state"] == 0

    # After timer fires (accounting for one-step propagation delay):
    # timer.done becomes True at t=0.61, LED turns on at t=0.62.
    entry_06 = runner.sim_data.logs[62]
    assert entry_06["components"]["timer"]["outputs"]["done"] is True
    assert entry_06["components"]["led"]["outputs"]["state"] == 1


def test_scenario_b_filter_and_pid():
    runner = scenario_b.build()
    runner.run(t_end=0.5)

    # After step, filtered should rise
    entry_20 = runner.sim_data.logs[20]
    filtered = entry_20["components"]["ain"]["outputs"]["filtered"]
    assert 0.0 < filtered <= 1.0

    # PID output clamped to [0, 1]
    ctrl = entry_20["components"]["pid"]["outputs"]["control"]
    assert 0.0 <= ctrl <= 1.0


def test_scenario_c_counter_and_state():
    runner = scenario_c.build()
    runner.run(t_end=1.0)

    # State machine should eventually transition
    final_state = runner.sim_data.logs[-1]["components"]["sm"]["outputs"]["current_state"]
    assert final_state in ("IDLE", "DONE")
