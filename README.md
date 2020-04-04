# AllodsOnlineEditorTools
Allods Online editor tools for 1.1.02.0



# Setup

## Requirements

To correctly use these tools you'll need:

- Allods 1.1.02.0 server and client files
- Allods 1.1.02.0 extracted client files, that you can get with the unpacker (https://github.com/paulus-allods/AllodsOnlineDatabaseUnpacker)
- Allods 1.1.02.0 helper archive (https://community.allods-developers.eu/resources/v1-helper-folder.24/)

## Setup folders

- Create a folder with nested `bin`, `data` and `Editor` folder
- Copy the `data` folder from this repository in your `data` folder
- Copy the `Editor` folder from this repository in your `Editor` folder
- Copy `Release_bin` folder from Helper Archive to `bin` folder
- Extract and copy `Tests` to `data` from `Tests.pak` in Helper Archive
- Copy all xdb from server `data` folder to your `data` folder
- Extract all `.pak` archives from client and move all folders to your `data` folder
- Copy all xdb from the extracted database to your `data` folder


You should have the following folder architecture :

```
.
+-- bin
|   +-- All files from Release_bin
+-- data
|   +-- Characters
|   +-- Client
|   +-- Creatures
|   +-- Editor
|   +-- Interface
|   +-- ItemMall
|   +-- Items
|   +-- ...
+-- Editor
|   +-- CommonFolder
|   +-- ...
```

You can now start tools from your `bin` folder

## Change language

You can change language in `Editor/ConstData.xml`. Available languages are russian and english.

For english (default): 

```
<Language>en</Language>
```

For russian:

```
<Language>ru</Language>
```

# Tools

## Core Design

Start with `CoreDesign.bat`

![](img/CoreDesign.JPG)

### Abilities

![](img/CoreDesign-Ability.JPG)

### Item Mall

![](img/CoreDesign-ItemMall.JPG)

### Recipes

![](img/CoreDesign-Recipe.JPG)

### Spell

![](img/CoreDesign-Spell.JPG)

### Vendors

![](img/CoreDesign-Vendors.JPG)

### Loot

![](img/CoreDesign-Loot.JPG)

### VisScript

![](img/Core-VisScript.JPG)

### Item Generator

![](img/CoreDesign-ItemGenerator.JPG)

## EditorGui aka MapEditor

Start with `EditorGUI.exe`

![](img/EditorGUI.JPG)

## MobEditor

Start with `EditorEditor.bat`

![](img/MobEditor.JPG)

## ModelViewer

Start with `ModelViewer.bat`

![](img/ModelViewer.JPG)

## Properties and WPFProperties

Start with `Properties.bat`

![](img/Properties.JPG)

Start with `WPFProperties.bat`

![](img/WPFProperties.JPG)

## Table Editor

Start with `TableEditor.bat`

![](img/TableEditor.JPG)

## The Browser

Start with `TheBrowser.bat`

![](img/TheBrowser.JPG)

## UIEditor

Start with `UIEditor.bat`

![](img/UIEditor.JPG)
