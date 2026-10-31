"""Inspect .psbproj metadata + components."""
import zipfile, json, sys
p = sys.argv[1] if len(sys.argv) > 1 else "examples/demos/oven_control.psbproj"
d = json.loads(zipfile.ZipFile(p).read("project.json"))
m = d.get("metadata", {})
print(f"  tEnd={m.get('tEnd')} dt={m.get('dt')} components={len(d.get('components',[]))} connections={len(d.get('connections',[]))}")
for c in d.get("components", []):
    name = c.get("instanceName", "")
    params = c.get("parameters", {})
    print(f"  {name}: {params}")
for c in d.get("connections", []):
    print(f"  {c.get('from')} -> {c.get('to')}")