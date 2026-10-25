"""One-shot fix: replace incorrect load_fmu (PyFMI) with FMPy API in generated component."""
from pathlib import Path

p = Path(__file__).parent.parent / "generated" / "FirstOrderLag.py"
t = p.read_text(encoding="utf-8")

# 1) import 行の置換
OLD_IMPORT = "from fmpy import load_fmu"
NEW_IMPORT = "from fmpy import read_model_description\nfrom fmpy.fmi2 import FMU2Slave\nfrom fmpy.simulation import apply_start_values"

if OLD_IMPORT not in t:
    print("ERROR: import line not found")
    raise SystemExit(1)

t = t.replace(OLD_IMPORT, NEW_IMPORT, 1)

# 2) load_fmu 呼び出し箇所を FMPy の低レベルAPIに置換
#    （現状は self.dahlquist = load_fmu(...) の1行のみと想定）
OLD_CALL = 'self.dahlquist = load_fmu(_psb_resolve_fmu("Dahlquist/source/Dahlquist.fmu"))'
NEW_CALL = (
    'fmu_path = _psb_resolve_fmu("Dahlquist/source/Dahlquist.fmu")\n'
    '        self.dahlquist = FMU2Slave(guid="dahlquist", '
    'model_description=read_model_description(fmu_path), '
    'unzipDirectory=_psb_temp_dir())'
)

if OLD_CALL not in t:
    print("WARN: load_fmu call pattern not matched; manual inspect needed")
    print("  Looking for:", OLD_CALL)
    # パターン不一致でも import 修正は活かす
else:
    t = t.replace(OLD_CALL, NEW_CALL, 1)

p.write_text(t, encoding="utf-8")
print("patched:", p)