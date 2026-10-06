import os
import runpy
import sys
import traceback
root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
log = os.path.join(root, '.codex/ship-wreck-blender/build.log')
os.makedirs(os.path.dirname(log), exist_ok=True)
with open(log, 'w', buffering=1) as output:
    sys.stdout = output
    sys.stderr = output
    try:
        runpy.run_path(os.path.join(root, 'Tools/BlenderShipWreck.py'), run_name='__main__')
    except Exception:
        traceback.print_exc()
        raise
