class_name WorldBackpack extends Node3D
## Mochila en el mundo (cofre). Genera loot aleatorio al spawnear.
## Al interactuar con E, abre el BackpackUI del jugador.

@export var loot_table: LootTable
@export var container_capacity: int = 10
@export var container_max_stack: int = 5

var container: ItemContainer = null
var _loot_generated: bool = false


func _ready() -> void:
	container = ItemContainer.new()
	container.capacity = container_capacity
	container.max_stack = container_max_stack
	add_child(container)
	call_deferred("_generate_loot")


func _generate_loot() -> void:
	if _loot_generated or loot_table == null:
		return
	_loot_generated = true
	loot_table.generate(container)


## Llamado por el RayCast del Player al presionar E
func interact(player: Node) -> void:
	var inv: Inventory = player.get_node_or_null("Inventory") as Inventory
	if inv == null:
		return
	for child: Node in player.get_children():
		if child is BackpackUI:
			child.open(container, inv)
			return
