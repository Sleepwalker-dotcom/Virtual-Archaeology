# Virtual-Archaeology development guide

## Project brief

- Read [`Docs/Project-Brief.md`](Docs/Project-Brief.md) before planning or making
  Unity changes.
- Preserve the prototype's object-led, sound-led interaction loop and favour
  reusable systems over logic hardcoded to one artefact.
- Treat historical claims, especially the Black history narrative, carefully.
  Do not invent or dramatise unsupported details; label uncertainty clearly.
- Treat the UCL Whitechapel project page linked under "Authoritative sources" in
  the project brief as a primary source. Check it and its cited publications
  before adding historical narration, labels, or reconstruction details.
- Preserve distinctions such as "may", "possibly", and "could" when the source
  presents an interpretation rather than a confirmed fact.
- The target reconstruction is Georgian / late 18th-century East London, not a
  fantasy medieval tavern or a modern pub.
- Current priority is a stable first playable prototype rather than final visual
  polish.

## Project

- Unity version: 6000.0.58f2.
- Render pipeline: Universal Render Pipeline (URP) 17.0.4.
- Primary features include XR Interaction Toolkit 3.0.11, OpenXR 1.15.1, the Input System 1.14.2, FMOD, and Gaussian splat rendering.
- Treat `Assets/Plugin` and `Assets/Plugins` as third-party code unless the task explicitly targets a plugin.

## Safe editing

- Preserve `.meta` files and their GUIDs. Move assets together with their `.meta` files.
- Do not edit generated folders: `Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, or build output.
- Do not edit generated `.csproj`, `.sln`, or IDE cache files.
- Prefer Unity Editor APIs for scene, prefab, material, and serialized asset changes. Avoid hand-editing large Unity YAML files unless the change is small and verified.
- Before changing a scene or prefab, identify the exact asset and avoid unrelated serialization churn.
- Do not add, remove, or upgrade Unity packages without explaining compatibility and team impact.
- Do not commit, push, pull, switch branches, discard changes, or rewrite Git history unless the user explicitly asks.

## Code

- Follow the style and namespace of nearby code.
- Keep runtime code out of `Editor` folders and editor-only code inside an `Editor` folder or editor assembly.
- Avoid per-frame allocations and expensive object searches in `Update`, `LateUpdate`, and fixed-step loops.
- Use serialized private fields instead of public fields when a value only needs Inspector exposure.
- Consider XR device lifecycle, disabled objects, and missing references when changing interaction code.

## Verification

- After C# or package changes, run `powershell -ExecutionPolicy Bypass -File Tools/Invoke-Unity.ps1 -Task Compile`.
- For editor logic, run the relevant EditMode tests with `-Task EditMode`.
- For runtime or interaction behaviour, run relevant PlayMode tests with `-Task PlayMode` when practical.
- Read the generated log and report compiler errors, test failures, and meaningful warnings.
- Do not claim that scene or XR behaviour was visually verified unless it was actually exercised in Unity.
