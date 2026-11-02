"""Package the tested standalone folder with file hashes, excluding debug-only symbols."""
from pathlib import Path
import hashlib,json,zipfile,subprocess

ROOT=Path(__file__).resolve().parents[1]
BUILD=Path('D:/indiegameDev/builds/DarkBrine_Rot-20261101/Windows64-Final')
ARCHIVE=BUILD.parent/'DarkBrine_Rot-Windows64-20261101.zip'
if __name__=='__main__':
    if ARCHIVE.exists():raise RuntimeError('Preserve existing release archive')
    source=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT).decode().strip()
    (BUILD/'README.txt').write_text('''Dark Brine_Rot — Windows 64位 / 2026-11-01

完整解压此文件夹后运行 DarkBrine_Rot.exe。
请保留同目录的 DarkBrine_Rot_Data、UnityPlayer.dll、MonoBleedingEdge 等文件。
此版本为普通非 Development 版本，包含8个正式构建场景。

已检查主菜单、设置、开场加载、第一岛海岸/森林/Boss区域，
以及旧版第二岛存档的继续游戏、回血提示和攻击输入。
当前是功能冒烟测试，未完成全流程通关或多硬件/实体手柄认证。
已有的3个植物Prefab资源缺失仍保留在源场景中；详见项目验证记录。
''',encoding='utf-8-sig')
    files=sorted(p for p in BUILD.rglob('*') if p.is_file() and not any('DoNotShip' in part for part in p.relative_to(BUILD).parts) and p.name!='release-manifest.json')
    hashes={p.relative_to(BUILD).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in files}
    (BUILD/'release-manifest.json').write_text(json.dumps({'git':source,'platform':'Windows64','development':False,'files':hashes},indent=2)+'\n',encoding='utf-8')
    files.append(BUILD/'release-manifest.json')
    with zipfile.ZipFile(ARCHIVE,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=3,allowZip64=True) as archive:
        for p in files:archive.write(p,'DarkBrine_Rot/'+p.relative_to(BUILD).as_posix())
    with zipfile.ZipFile(ARCHIVE) as archive:
        bad=archive.testzip()
        if bad:raise RuntimeError('Archive CRC failed: '+bad)
    receipt={'archive':str(ARCHIVE),'bytes':ARCHIVE.stat().st_size,'sha256':hashlib.sha256(ARCHIVE.read_bytes()).hexdigest(),'files':len(files),'crcPassed':True,'source':source}
    (ROOT/'.codex/release051/archive.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(receipt,indent=2))
