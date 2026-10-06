import os, runpy, sys, traceback
root=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
folder=os.path.join(root,'.codex','shark-blender');os.makedirs(folder,exist_ok=True)
with open(os.path.join(folder,'build.log'),'w',buffering=1) as log:
    sys.stdout=sys.stderr=log
    try:runpy.run_path(os.path.join(root,'Tools','BlenderSharkAnimation.py'),run_name='__main__')
    except Exception:traceback.print_exc();raise
