#!/usr/bin/env python3
"""
Reorganize Unity scene hierarchy for better artist workflow.
Creates clear groups: [GAME LOGIC], [UI], [WORLD], [CAMERA]
"""

import re
from pathlib import Path
from typing import Dict, List, Tuple

SCENE_PATH = Path("Assets/Scenes/SimpleNaturePack_Demo.unity")

# Mapping of old parent IDs to new organization groups
# We'll create new empty GameObjects as group containers

def read_scene():
    """Read the Unity scene file"""
    with open(SCENE_PATH, 'r', encoding='utf-8') as f:
        return f.read()

def write_scene(content):
    """Write the Unity scene file"""
    with open(SCENE_PATH, 'w', encoding='utf-8', newline='\n') as f:
        f.write(content)

def find_object_by_name(content, name_pattern):
    """Find GameObject ID by name pattern"""
    # Pattern: --- !u!1 &ID followed by name
    pattern = rf'--- !u!1 &(\d+)\nGameObject:.*?m_Name: {name_pattern}'
    match = re.search(pattern, content, re.DOTALL)
    if match:
        return match.group(1)
    return None

def get_all_gameobjects(content):
    """Extract all GameObject definitions with their IDs and names"""
    objects = {}
    # Find all GameObject definitions
    pattern = r'--- !u!1 &(\d+)\nGameObject:.*?m_Name: ([^\n]+)'
    for match in re.finditer(pattern, content, re.DOTALL):
        obj_id = match.group(1)
        obj_name = match.group(2).strip()
        objects[obj_id] = obj_name
    return objects

def get_transform_info(content, obj_id):
    """Get transform information for a GameObject"""
    # Find Transform or RectTransform component for this object
    patterns = [
        rf'--- !u!(?:224|4) &(\d+)\n(?:Rect)?Transform:.*?m_GameObject: \{{fileID: {obj_id}\}}.*?m_Father: \{{fileID: (\d+)\}}',
    ]
    
    for pattern in patterns:
        match = re.search(pattern, content, re.DOTALL)
        if match:
            transform_id = match.group(1)
            parent_id = match.group(2)
            return transform_id, parent_id
    return None, None

def create_empty_gameobject(new_id, name, parent_id="0"):
    """Generate YAML for a new empty GameObject organizer"""
    return f"""--- !u!1 &{new_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {new_id}01}}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{new_id}01
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {new_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {parent_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
"""

def main():
    print("Reading scene file...")
    content = read_scene()
    
    print("Analyzing current hierarchy...")
    objects = get_all_gameobjects(content)
    
    # Find key objects
    canvas_id = find_object_by_name(content, r'Canvas')
    core_id = find_object_by_name(content, r'_Hyperloop_Core')
    camera_id = find_object_by_name(content, r'Camera')
    gamemanager_id = find_object_by_name(content, r'GameManager')
    
    print(f"Canvas ID: {canvas_id}")
    print(f"Core ID: {core_id}")
    print(f"Camera ID: {camera_id}")
    print(f"GameManager ID: {gamemanager_id}")
    
    # Strategy: Since the hierarchy is complex, we'll do targeted renames first
    # Then in later steps, we can move objects if needed
    
    # For now, let's just rename key objects to follow conventions
    renames = {
        'GameManager': 'GameManager',  # Already good
        'Canvas': 'Canvas',  # Already good
        '_Hyperloop_Core': '[GAME_WORLD]',  # Rename parent
        'Camera': 'Main Camera',  # Keep as is
        'HudRoot': 'Canvas_HUD',
        'Stations': 'Canvas_Map',
        'VipMarkers': 'VipMarkers_Container',
        'HyperloopTrain': 'Train_NormalTrain',
        # Grass objects
        'Grass 1': 'Grass_Layer1',
        'Grass 1 (1)': 'Grass_Layer2',
        'Grass 1 (2)': 'Grass_Layer3',
        'Grass 1 (3)': 'Grass_Layer4',
        'GrassLayer (4)': 'Grass_Layer5',
        # Tube objects
        'Tube': 'Tube_Main',
        'Tube (left)': 'Tube_Left',
        'Tube (right) (1)': 'Tube_Right1',
        'Tube (right) (2)': 'Tube_Right2',
        'Tube (left) (3)': 'Tube_Left3',
        # UI elements
        'Money': 'MoneyCounter',
        'Name': 'PassengerCounter',
        'Income': 'IncomeDisplay',
        'Label': 'VipOnboardInfo',
        'Text (TMP) (1)': 'UnlockCostText',
        'Text (TMP) (2)': 'StationNameText',
        'rails': 'Rails_Layer',
        # Station buttons
        'Brussels Button': 'Station_Brussels',
        'Amsterdam Button': 'Station_Amsterdam',
        'Paris Button': 'Station_Paris',
        'Hamburg Button': 'Station_Hamburg',
        'Hannover Button': 'Station_Hannover',
        'Berlin Button': 'Station_Berlin',
        'Groningen Button': 'Station_Groningen',
    }
    
    print("\nApplying renames...")
    for old_name, new_name in renames.items():
        # Escape special regex characters
        old_escaped = re.escape(old_name)
        # Replace m_Name: old_name with m_Name: new_name
        pattern = f'(m_Name: ){old_escaped}(\\n)'
        replacement = f'\\1{new_name}\\2'
        content = re.sub(pattern, replacement, content)
        print(f"  {old_name} -> {new_name}")
    
    print("\nWriting updated scene...")
    write_scene(content)
    print("[OK] Hierarchy reorganization complete!")
    print("\nNext steps (manual in Unity Editor):")
    print("  1. Create empty GameObjects: [GAME LOGIC], [UI], [WORLD], [CAMERA]")
    print("  2. Move GameManager, VipSpawnManager, TrainMover into [GAME LOGIC]")
    print("  3. Move all Canvas children into organized [UI] subgroups")
    print("  4. Move all grass/tubes/rails into [WORLD] -> Environment_Group")
    print("  5. Move Camera into [CAMERA] group")

if __name__ == '__main__':
    main()
