#!/usr/bin/env python3
"""打包：.cs 源码原样放入（启动器会自己编译），不带 bin/ obj/。"""
import json, os, zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
manifest = json.load(open(os.path.join(ROOT, 'manifest.json'), encoding='utf-8'))
out_dir = os.path.join(ROOT, 'dist')
os.makedirs(out_dir, exist_ok=True)
out = os.path.join(out_dir, f"deepfishai-{manifest['version']}.bmod")

SKIP_DIRS = {'bin', 'obj', 'dist', 'tools', '.git'}

with zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED) as pack:
    for dirpath, dirnames, files in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
        for f in files:
            if f.endswith(('.csproj', '.user', '.sln')):
                continue
            full = os.path.join(dirpath, f)
            pack.write(full, os.path.relpath(full, ROOT))

print(f'{os.path.relpath(out, ROOT)}  {os.path.getsize(out) / 1024:.1f} KB')
