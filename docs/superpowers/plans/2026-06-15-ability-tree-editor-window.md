# Ability Tree Editor Window Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Unity EditorWindow for visually arranging and editing `AbilityTreeConfig_SO` nodes.

**Architecture:** Keep `AbilityTreeConfig_SO` as the only data source. Put testable mutation helpers in `AbilityTreeEditorOperations`, use `AbilityTreeEditorWindow` for graph drawing and interaction, and add an Inspector button to open the selected config.

**Tech Stack:** Unity Editor IMGUI, ScriptableObject serialization, NUnit EditMode tests.

---

### Task 1: Editor Operations

**Files:**
- Create: `Assets/Scripts/Editor/AbilityTreeEditorOperations.cs`
- Create: `Assets/Tests/EditMode/Editor/AbilityTreeEditorOperationsTests.cs`

- [ ] Write failing EditMode tests for add node, delete node with prerequisite cleanup, generate missing IDs, and remove invalid prerequisites.
- [ ] Implement operations with Undo-friendly data mutation call sites.
- [ ] Run targeted tests.

### Task 2: Graph Editor Window

**Files:**
- Create: `Assets/Scripts/Editor/AbilityTreeEditorWindow.cs`

- [ ] Add `Tools/Ability Tree Editor` menu entry.
- [ ] Let the window select or auto-load an `AbilityTreeConfig_SO`.
- [ ] Draw draggable nodes from `node.position`.
- [ ] Draw prerequisite lines.
- [ ] Add a right-side detail panel for selected node fields, prerequisites, and effects.
- [ ] Add toolbar actions for add, delete, cleanup, validate, save, and frame all.

### Task 3: Inspector Integration

**Files:**
- Modify: `Assets/Scripts/Editor/AbilityTreeConfigEditor.cs`

- [ ] Add `打开节点编辑器` button.
- [ ] Reuse `AbilityTreeEditorOperations` for existing inspector helper buttons.
- [ ] Run console check and targeted tests.
