#!/usr/bin/env python3
"""
Copy all files and subdirectories from source to destination, excluding hidden files.
"""

import shutil
import os
from pathlib import Path

SOURCE = r"R:\layoutbak"
DESTINATION = r"R:\layouteditortop\layouteditor"


def is_hidden(path: Path) -> bool:
    """Check if a file or folder is hidden (Windows or Unix-style)."""
    # Check for Unix-style hidden (starts with .)
    if path.name.startswith('.'):
        return True
    # Check for Windows hidden attribute
    try:
        import ctypes
        attrs = ctypes.windll.kernel32.GetFileAttributesW(str(path))
        if attrs != -1 and (attrs & 2):  # FILE_ATTRIBUTE_HIDDEN = 2
            return True
    except:
        pass
    return False


def should_skip(path: Path, src: Path) -> bool:
    """Check if path should be skipped (hidden or in excluded folders)."""
    rel_path = path.relative_to(src)
    excluded_folders = {'obj', 'bin'}
    
    for i, part in enumerate(rel_path.parts):
        check_path = src / Path(*rel_path.parts[:i+1])
        # Skip hidden files/folders
        if is_hidden(check_path):
            return True
        # Skip excluded folders
        if part.lower() in excluded_folders:
            return True
    return False


def copy_tree(src: Path, dst: Path) -> None:
    """Recursively copy directory tree, skipping hidden files/folders and obj/bin."""
    
    # Create destination if it doesn't exist
    dst.mkdir(parents=True, exist_ok=True)
    
    copied_files = 0
    copied_dirs = 0
    skipped = 0
    
    for item in src.rglob('*'):
        # Skip hidden files and excluded folders
        if should_skip(item, src):
            skipped += 1
            continue
        
        rel_path = item.relative_to(src)
        
        dest_path = dst / rel_path
        
        if item.is_dir():
            dest_path.mkdir(parents=True, exist_ok=True)
            copied_dirs += 1
            print(f"Created folder: {rel_path}")
        elif item.is_file():
            dest_path.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(item, dest_path)
            copied_files += 1
            print(f"Copied: {rel_path}")
    
    print("\n" + "=" * 50)
    print(f"Done! Copied {copied_files} files and {copied_dirs} folders.")
    if skipped:
        print(f"Skipped {skipped} hidden items.")


def main():
    src = Path(SOURCE)
    dst = Path(DESTINATION)
    
    print(f"Source:      {src}")
    print(f"Destination: {dst}")
    print("=" * 50 + "\n")
    
    if not src.exists():
        print(f"Error: Source folder '{src}' does not exist.")
        return
    
    if not src.is_dir():
        print(f"Error: Source '{src}' is not a directory.")
        return
    
    copy_tree(src, dst)


if __name__ == "__main__":
    main()
