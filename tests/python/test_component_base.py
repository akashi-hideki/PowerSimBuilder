"""Component 基底クラスの単体テスト。"""

from python_runtime.component_base import Component


def test_init_sets_id_and_type():
    c = Component("c1", "Dummy")
    assert c.id == "c1"
    assert c.type == "Dummy"


def test_init_containers_are_empty_and_independent():
    c1 = Component("c1", "Dummy")
    c2 = Component("c2", "Dummy")
    assert c1.parameters == {}
    assert c1.inputs == {}
    assert c1.outputs == {}
    c1.parameters["x"] = 1
    c1.inputs["a"] = 2
    c1.outputs["b"] = 3
    assert c2.parameters == {}
    assert c2.inputs == {}
    assert c2.outputs == {}


def test_update_default_is_noop():
    c = Component("c1", "Dummy")
    c.inputs["a"] = 1
    before = dict(c.outputs)
    result = c.update(0.0, 0.001, None)
    assert result is None
    assert c.outputs == before


def test_reset_clears_io_but_keeps_parameters():
    c = Component("c1", "Dummy")
    c.parameters["k"] = 10
    c.inputs["a"] = 1
    c.outputs["b"] = 2
    c.reset()
    assert c.inputs == {}
    assert c.outputs == {}
    assert c.parameters == {"k": 10}


def test_subclass_update_is_called():
    class Adder(Component):
        def update(self, t, dt, sim_data):
            self.outputs["y"] = self.inputs.get("x", 0) + 1

    c = Adder("c1", "Adder")
    c.inputs["x"] = 41
    c.update(0.0, 0.001, None)
    assert c.outputs["y"] == 42


def test_repr_contains_type_and_id():
    c = Component("c1", "Dummy")
    r = repr(c)
    assert "Dummy" in r
    assert "c1" in r
