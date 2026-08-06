extends Node
## Autoload "ItemRegistry". Cachea bajo demanda (lazy loading).
## Busca recursivamente en las carpetas de ítems.

var _scenes: Dictionary = {}
var _components: Dictionary = {}
var _data: Dictionary = {}

const ITEM_FOLDERS: PackedStringArray = [
	"res://src/items/consumables/",
	"res://src/items/weapons/",
]


func get_component(id: String) -> ComponentBase:
	_ensure_loaded(id)
	return _components.get(id, null)


func get_data(id: String) -> ItemData:
	_ensure_loaded(id)
	return _data.get(id, null)


func get_scene(id: String) -> PackedScene:
	_ensure_loaded(id)
	return _scenes.get(id, null)


func _ensure_loaded(id: String) -> void:
	if _data.has(id):
		return
	for folder: String in ITEM_FOLDERS:
		if _search_recursive(id, folder):
			return


func _search_recursive(id: String, path: String) -> bool:
	var dir := DirAccess.open(path)
	if dir == null:
		return false
	dir.list_dir_begin()
	var file_name: String = dir.get_next()
	while file_name != "":
		var full_path: String = path + file_name
		if dir.current_is_dir():
			if _search_recursive(id, full_path + "/"):
				dir.list_dir_end()
				return true
		elif file_name == id + ".tscn":
			_register(id, full_path)
			dir.list_dir_end()
			return true
		file_name = dir.get_next()
	dir.list_dir_end()
	return false


func _register(id: String, scene_path: String) -> void:
	var scene: PackedScene = load(scene_path) as PackedScene
	if scene == null:
		return
	var instance: Node = scene.instantiate()
	if not (instance is ItemEntity):
		instance.queue_free()
		return
	var entity: ItemEntity = instance as ItemEntity
	_scenes[id] = scene
	_data[id] = entity.data
	if entity.component != null:
		_components[id] = entity.component
		entity.remove_child(entity.component)
	entity.queue_free()
