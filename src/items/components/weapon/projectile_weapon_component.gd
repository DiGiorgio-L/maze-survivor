class_name ProjectileWeaponComponent extends WeaponComponent
## Datos de un arma a distancia. La munición vive en slot.instance_data.
## La lógica de disparo (instanciar proyectil, trayectoria) la maneja la escena view_model.

@export var max_ammo: int = 10
@export var projectile_scene: PackedScene

func on_used(slot: InventorySlot) -> void:
	if slot == null:
		return
	if not slot.instance_data.has("ammo"):
		return
	slot.instance_data["ammo"] = maxi(slot.instance_data["ammo"] - 1, 0)

static func create_instance_data(ammo: int) -> Dictionary:
	return {"ammo": ammo}
