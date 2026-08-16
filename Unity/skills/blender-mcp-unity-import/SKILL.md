---
name: blender-mcp-unity-import
description: Connect to Blender via BlenderMCP socket protocol (port 9876), execute Blender Python code to create/modify meshes, export FBX into a Unity project, import the asset, and instantiate it across grid anchor points. Use when the user asks to control Blender from Codely, create 3D models in Blender for a Unity project, export Blender assets into the current Unity project, or fill hex/grid cells with Blender-made tiles.
---

# Blender MCP → Unity Import

End-to-end workflow: control Blender through its MCP addon socket, generate meshes, export FBX into the Unity project, import, and place instances on grid anchors.

## Prerequisites

- Blender running with the `BlenderMCP` addon installed (Edit → Preferences → Add-ons → "Interface: Blender MCP") and **Connect** pressed (3D View sidebar → BlenderMCP tab).
- The addon listens on TCP `127.0.0.1:9876`. Verify:
  ```powershell
  Get-NetTCPConnection -LocalPort 9876 | Select State, OwningProcess
  ```
- The MCP Python server (`uvx blender-mcp`) is **not** required for Codely to talk to Blender — Codely connects **directly** to the addon socket.

## Protocol (addon socket, port 9876)

Request (single-line JSON, newlines escaped as `\n`):
```json
{"type":"execute_code","params":{"code":"import bpy\nprint('hi')"}}
```

Response:
```json
{"status":"success","result":{"executed":true,"result":"<stdout captured by print()>"}}
```

- Return values go through `print()`; captured stdout is in `result.result`.
- The execution namespace only exposes `bpy`.
- JSON-escape the code payload: `\` → `\\`, `"` → `\"`, newline → `\n`.

## Workflow

### 1. Send Blender Python via TCP

From Unity's `execute_csharp_script` (or any TCP-capable runtime):

```csharp
var client = new System.Net.Sockets.TcpClient("127.0.0.1", 9876);
var stream = client.GetStream();
string code = "import bpy\nprint(bpy.app.version_string)";
var cmd = "{\"type\":\"execute_code\",\"params\":{\"code\":\"" +
          code.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n") +
          "\"}}";
var bytes = System.Text.Encoding.UTF8.GetBytes(cmd);
stream.Write(bytes, 0, bytes.Length);
var buf = new byte[16384];
int n = stream.Read(buf, 0, buf.Length);
Debug.Log(System.Text.Encoding.UTF8.GetString(buf, 0, n)); // {"status":"success",...}
client.Close();
```

### 2. Create a mesh in Blender

Example — pointy-top hexagon tile with thickness (radius 1.0, extrude 0.05):

```python
import bpy, math
for o in list(bpy.data.objects):
    if o.name.startswith('HexTile'):
        bpy.data.objects.remove(o, do_unlink=True)
verts = []
for i in range(6):
    a = math.radians(90 - i * 60)
    verts.append((math.cos(a), math.sin(a), 0))
mesh = bpy.data.meshes.new('HexTileMesh')
mesh.from_pydata(verts, [], [list(range(6))])
mesh.update()
obj = bpy.data.objects.new('HexTile', mesh)
bpy.context.collection.objects.link(obj)
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.extrude_region_move(TRANSFORM_OT_translate={'value': (0, 0, 0.05)})
bpy.ops.object.mode_set(mode='OBJECT')
obj.select_set(False)
print('HexTile created: ' + str(len(mesh.vertices)) + ' verts')
```

### 3. Clean scene & export FBX (selection only)

**Critical:** delete every non-target object first, otherwise the default Cube/Camera/Light get exported too:

```python
import bpy
for o in list(bpy.data.objects):
    if o.name != 'HexTile':
        bpy.data.objects.remove(o, do_unlink=True)
obj = bpy.data.objects.get('HexTile')
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.export_scene.fbx(
    filepath='D:/path/to/Unity/Project/Assets/SCR/Prefab/HexTile.fbx',
    use_selection=True,
    apply_unit_scale=True,
    object_types={'MESH'})
obj.select_set(False)
print('Exported')
```

### 4. Import into Unity

```csharp
UnityEditor.AssetDatabase.ImportAsset("Assets/SCR/Prefab/HexTile.fbx");
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SCR/Prefab/HexTile.fbx");
```

Verify geometry is correct (dedup XY positions — a hexagon should give exactly 6 distinct points):

```csharp
var m = prefab.GetComponent<MeshFilter>().sharedMesh;
var seen = new System.Collections.Generic.HashSet<string>();
foreach (var v in m.vertices) seen.Add($"{v.x:F3},{v.y:F3}");
Debug.Log($"distinct XY positions: {seen.Count} (expect 6 for hexagon)");
```

### 5. Unit-scale note

Blender exports in centimeters; Unity FBX import may scale 1 Blender unit → 0.01 Unity units (mesh radius 1.0 becomes 0.01). Handle by **auto-fitting** instances: read renderer world bounds, divide by current scale to get model-local size, then set `localScale = targetDiameter / modelLocalSize`.

## Pitfalls

- **Default Cube exported**: always `use_selection=True` AND delete unrelated objects first.
- **Old import cache**: after re-exporting, re-run `AssetDatabase.ImportAsset` before inspecting; `LoadAllAssetsAtPath` avoids stale sub-assets.
- **Winding/backface**: Unity `Cull Back` hides reversed triangles. Verify average triangle normal z-sign matches a known-visible mesh; for flat meshes at z=0 viewed from -z, keep winding so normal z > 0.
- **No light → Lit materials look black**: add a Directional Light when using URP Lit.
- **Material cache in [ExecuteAlways]**: cached `Material` fields survive rebuilds; destroy them in `Rebuild()` or the new shader/cull settings never apply.
