# BlenderMCP Addon Socket Protocol

Reference for talking to the BlenderMCP addon directly over TCP.

## Connection

- Host: `127.0.0.1`
- Port: `9876` (addon default)
- Transport: raw TCP socket, one JSON request → one JSON response per connection.

## Request

```json
{
  "type": "execute_code",
  "params": {
    "code": "import bpy\nprint('hi')"
  }
}
```

`type` values handled by the addon include: `get_scene_info`, `get_object_info`, `execute_code`, `get_polyhaven_status`, `get_hyper3d_status`. For arbitrary work, use `execute_code`.

## Response

```json
{
  "status": "success",
  "result": {
    "executed": true,
    "result": "<stdout captured by print()>"
  }
}
```

Error shape:
```json
{
  "status": "error",
  "message": "exception text"
}
```

## execute_code semantics (addon source)

- Runs with namespace `{"bpy": bpy}` only.
- stdout is redirected into an in-memory buffer; `print(...)` output is returned as `result.result`.
- To return data, `print()` a string (e.g., `print(len(mesh.vertices))`).
- Exceptions abort and are returned as `status: "error"`.

## JSON escaping for inline code

When building the request as a single-line string (e.g. inside C# / shell), escape:
- `\` → `\\`
- `"` → `\"`
- newline → `\n`
- CR → strip (`\r`)

## Chunked responses

The addon may send the JSON response in multiple TCP chunks. The receiver should keep reading until the accumulated bytes parse as complete JSON, or the socket closes. Use a 15 s socket timeout to match the addon.

## Useful Blender snippets

### Create pointy-top hexagon (radius r, optional thickness)

```python
import bpy, math
r = 1.0
verts = []
for i in range(6):
    a = math.radians(90 - i * 60)
    verts.append((r * math.cos(a), r * math.sin(a), 0))
mesh = bpy.data.meshes.new('M')
mesh.from_pydata(verts, [], [list(range(6))])
mesh.update()
obj = bpy.data.objects.new('O', mesh)
bpy.context.collection.objects.link(obj)
# thickness:
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.extrude_region_move(TRANSFORM_OT_translate={'value': (0, 0, 0.05)})
bpy.ops.object.mode_set(mode='OBJECT')
obj.select_set(False)
```

### Export selection only (FBX)

```python
import bpy
for o in list(bpy.data.objects):
    if o.name != 'Target':
        bpy.data.objects.remove(o, do_unlink=True)
obj = bpy.data.objects['Target']
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.export_scene.fbx(filepath='C:/out/Target.fbx', use_selection=True, apply_unit_scale=True, object_types={'MESH'})
obj.select_set(False)
```

### Inspect an object

```python
import bpy
obj = bpy.data.objects.get('Target')
print(obj.name, obj.type, len(obj.data.vertices), len(obj.data.polygons))
for i, v in enumerate(obj.data.vertices[:12]):
    print(round(v.co.x, 3), round(v.co.y, 3), round(v.co.z, 3))
```
