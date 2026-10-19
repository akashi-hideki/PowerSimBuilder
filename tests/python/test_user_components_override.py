import textwrap

import pytest

from python_runtime import components


@pytest.fixture
def project(tmp_path, monkeypatch):
    monkeypatch.chdir(tmp_path)
    return tmp_path


def test_user_component_overrides_bundled(project):
    (project / "user_components").mkdir()
    (project / "user_components" / "Gain.py").write_text(
        textwrap.dedent(
            """
            class Gain:
                source = "user"
            """
        ),
        encoding="utf-8",
    )
    impl = components.load("Gain")
    assert impl.source == "user"


def test_fallback_to_bundled_when_no_override(project):
    impl = components.load("Gain")
    assert impl is components.Gain


def test_load_unknown_name_raises(project):
    with pytest.raises(AttributeError):
        components.load("NoSuchComponent")


def test_scaffold_override_comments_out_all_lines(project):
    dst = components.scaffold_override("Gain")
    content = dst.read_text(encoding="utf-8")
    body = content.splitlines()
    body = [line for line in body if line.strip() and not line.startswith("# override:")]
    assert all(line.startswith("#") for line in body)


def test_scaffold_override_refuses_overwrite(project):
    components.scaffold_override("Gain")
    with pytest.raises(FileExistsError):
        components.scaffold_override("Gain")


def test_scaffold_override_then_load_uses_commented_stub(project):
    dst = components.scaffold_override("Gain")
    dst.write_text(
        "class Gain:\n    source = 'override'\n",
        encoding="utf-8",
    )
    impl = components.load("Gain")
    assert impl.source == "override"
