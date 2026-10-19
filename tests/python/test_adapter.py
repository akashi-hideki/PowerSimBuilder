from python_runtime.adapter import wrap_class


class _SimpleImpl:
    def __init__(self, gain: float = 1.0) -> None:
        self.gain = gain
        self.last = 0.0

    def update(self, t, dt, x=0.0):
        self.last = x * self.gain
        return {"y": self.last}


def test_wrap_class_basic():
    Adapter = wrap_class(
        "SimpleAdapter",
        _SimpleImpl,
        parameters={"gain": 2.0},
        inputs={"x": 0.0},
        outputs={"y": 0.0},
    )
    comp = Adapter("id1")
    assert comp.type == "SimpleAdapter"
    comp.inputs["x"] = 3.0
    comp.update(0.0, 0.1, None)
    assert comp.outputs["y"] == 6.0


def test_wrap_class_custom_call():
    def call(impl, inputs, dt):
        return {"y": inputs.get("x", 0.0) + 1.0}

    Adapter = wrap_class(
        "CustomCall",
        _SimpleImpl,
        inputs={"x": 5.0},
        outputs={"y": 0.0},
        call=call,
    )
    comp = Adapter("id2")
    comp.update(0.0, 0.1, None)
    assert comp.outputs["y"] == 6.0


def test_wrap_class_reset_clears_io_and_impl():
    Adapter = wrap_class("R", _SimpleImpl, parameters={"gain": 1.5})
    comp = Adapter("id3")
    comp.inputs["x"] = 1.0
    comp.outputs["y"] = 2.0
    comp.update(0.0, 0.1, None)
    assert comp._impl is not None
    comp.reset()
    assert comp._impl is None
    assert comp.inputs == {}
    assert comp.outputs == {}
    comp.update(0.0, 0.1, None)
    assert comp._impl.gain == 1.5


def test_wrap_class_ignores_unknown_output_keys():
    def call(impl, inputs, dt):
        return {"y": 1.0, "unknown": 999}

    Adapter = wrap_class(
        "SafeKeys",
        _SimpleImpl,
        inputs={"x": 0.0},
        outputs={"y": 0.0},
        call=call,
    )
    comp = Adapter("id4")
    comp.update(0.0, 0.1, None)
    assert comp.outputs == {"y": 1.0}
