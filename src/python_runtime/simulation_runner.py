"""SimulationRunner: main loop with connection propagation."""

from __future__ import annotations

from typing import Any

from .simulation_data import SimulationData


class SimulationRunner:
    """Runs the simulation loop.

    Components are registered by id. Connections propagate outputs to inputs
    after every update step, before logging.

    Attributes:
        components: dict of id -> Component.
        connections: list of {"from": "src.outputs.port",
                              "to": "dst.inputs.port"}.
        sim_data: SimulationData instance.
        dt: time step [s].
    """

    def __init__(self, dt: float = 0.001) -> None:
        self.components: dict[str, Any] = {}
        self.connections: list[dict[str, str]] = []
        self.sim_data = SimulationData(time_step=dt)
        self.dt = dt

    def add_component(self, comp: Any) -> None:
        self.components[comp.id] = comp

    def add_connection(self, src: str, dst: str) -> None:
        self.connections.append({"from": src, "to": dst})

    def step(self, t: float) -> None:
        """Perform one update cycle at time t."""
        self.sim_data.t = t
        self.sim_data.apply_stimulus(self.components, t)

        for comp in self.components.values():
            comp.update(t, self.dt, self.sim_data)

        self._propagate()

        self.sim_data.log(t, self.components)

    def run(self, t_end: float) -> SimulationData:
        """Run from t=0 to t_end inclusive (step = dt)."""
        t = 0.0
        n = int(round(t_end / self.dt))
        for i in range(n + 1):
            self.step(t)
            t = (i + 1) * self.dt
        return self.sim_data

    def _propagate(self) -> None:
        for conn in self.connections:
            src_path = conn["from"]
            dst_path = conn["to"]
            src_parts = src_path.split(".")
            dst_parts = dst_path.split(".")
            if len(src_parts) != 3 or len(dst_parts) != 3:
                continue
            src_id, src_sec, src_port = src_parts
            dst_id, dst_sec, dst_port = dst_parts
            src_comp = self.components.get(src_id)
            dst_comp = self.components.get(dst_id)
            if src_comp is None or dst_comp is None:
                continue
            if src_sec != "outputs" or dst_sec != "inputs":
                continue
            if src_port not in src_comp.outputs:
                continue
            dst_comp.inputs[dst_port] = src_comp.outputs[src_port]
