#!/usr/bin/env python3
"""为 LingyanUnity/Assets 生成确定性的 .meta 文件。

GUID = md5(相对工程根的 POSIX 路径)，因此对同一路径永远稳定，
EditorBuildSettings.asset 等引用可以在写场景文件时预先算出。

用法:
    python3 tools/gen_meta.py          # 补齐缺失的 .meta
    python3 tools/gen_meta.py --check  # 只检查，缺失则退出码 1（供冒烟脚本用）

.ttf/.otf 交给 Unity 首次导入时自建（TrueTypeFontImporter 字段多，不硬造），
本脚本对它们只提示、不生成、不算失败。
"""
import hashlib
import os
import sys

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECT = os.path.join(REPO, "LingyanUnity")
ASSETS = os.path.join(PROJECT, "Assets")

HEADER = "fileFormatVersion: 2\nguid: {guid}\n"

FOLDER_BODY = (
    "folderAsset: yes\n"
    "DefaultImporter:\n"
    "  externalObjects: {}\n"
    "  userData: \n"
    "  assetBundleName: \n"
    "  assetBundleVariant: \n"
)

DEFAULT_BODY = (
    "DefaultImporter:\n"
    "  externalObjects: {}\n"
    "  userData: \n"
    "  assetBundleName: \n"
    "  assetBundleVariant: \n"
)

MONO_BODY = (
    "MonoImporter:\n"
    "  externalObjects: {}\n"
    "  serializedVersion: 2\n"
    "  defaultReferences: []\n"
    "  executionOrder: 0\n"
    "  icon: {instanceID: 0}\n"
    "  userData: \n"
    "  assetBundleName: \n"
    "  assetBundleVariant: \n"
)

TEXT_BODY = (
    "TextScriptImporter:\n"
    "  externalObjects: {}\n"
    "  userData: \n"
    "  assetBundleName: \n"
    "  assetBundleVariant: \n"
)

ASMDEF_BODY = (
    "AssemblyDefinitionImporter:\n"
    "  externalObjects: {}\n"
    "  userData: \n"
    "  assetBundleName: \n"
    "  assetBundleVariant: \n"
)

SKIP_EXTENSIONS = {".ttf", ".otf"}

BODY_BY_EXTENSION = {
    ".cs": MONO_BODY,
    ".asmdef": ASMDEF_BODY,
    ".json": TEXT_BODY,
    ".txt": TEXT_BODY,
    ".md": TEXT_BODY,
    ".unity": DEFAULT_BODY,
    ".asset": DEFAULT_BODY,
}


def guid_for(rel_posix: str) -> str:
    return hashlib.md5(rel_posix.encode("utf-8")).hexdigest()


def body_for(path: str, is_dir: bool):
    if is_dir:
        return FOLDER_BODY
    ext = os.path.splitext(path)[1].lower()
    if ext in SKIP_EXTENSIONS:
        return None
    return BODY_BY_EXTENSION.get(ext, DEFAULT_BODY)


def main() -> int:
    check_only = "--check" in sys.argv
    missing, created, skipped = [], [], []

    entries = []
    for root, dirs, files in os.walk(ASSETS):
        dirs.sort()
        for d in dirs:
            entries.append((os.path.join(root, d), True))
        for f in sorted(files):
            if f.endswith(".meta"):
                continue
            entries.append((os.path.join(root, f), False))

    for path, is_dir in entries:
        meta_path = path + ".meta"
        rel = os.path.relpath(path, PROJECT).replace(os.sep, "/")
        body = body_for(path, is_dir)
        if body is None:
            if not os.path.exists(meta_path):
                skipped.append(rel)
            continue
        if os.path.exists(meta_path):
            continue
        if check_only:
            missing.append(rel)
            continue
        with open(meta_path, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(HEADER.format(guid=guid_for(rel)) + body)
        created.append(rel)

    for rel in skipped:
        print(f"[gen_meta] 跳过（Unity 首次导入自建）: {rel}")
    if check_only:
        if missing:
            print("[gen_meta] 缺 .meta：")
            for rel in missing:
                print("  " + rel)
            return 1
        print("[gen_meta] .meta 齐备")
        return 0
    for rel in created:
        print(f"[gen_meta] 生成: {rel}.meta")
    print(f"[gen_meta] 新建 {len(created)} 个 .meta")
    return 0


if __name__ == "__main__":
    sys.exit(main())
