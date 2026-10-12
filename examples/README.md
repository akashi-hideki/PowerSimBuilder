# Example Scenarios

Three small scenarios demonstrate the Python runtime.

| Scenario | File | Demonstrates |
|----------|------|--------------|
| A | scenario_a.py | Digital input -> delay timer -> digital output |
| B | scenario_b.py | Analog scaling + first-order filter + PID |
| C | scenario_c.py | Rising-edge counter + state transition |

## Run

    python -m examples.scenario_a
    python -m examples.scenario_b
    python -m examples.scenario_c

## Use as a library

Each scenario exports:

    build() -> SimulationRunner    # configured, not yet run
    run()   -> None                # builds, runs, prints summary

You can import and extend them:

    from examples import scenario_a
    runner = scenario_a.build()
    runner.run(t_end=2.0)
    for entry in runner.sim_data.logs:
        ...

## Dump to CSV or PNG

    from python_runtime import dump_csv, plot

    dump_csv(runner.sim_data, "scenario_a.csv")
    plot(runner.sim_data, keys=["led.outputs.state"], output_path="scenario_a.png")
