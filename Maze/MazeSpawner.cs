using Godot;
using System;
using System.Collections.Generic;

public partial class MazeSpawner : Node
{
private Maze _maze;
private readonly Random _random = new Random();
private readonly HashSet<Vector2I> _occupiedPositions = new HashSet<Vector2I>();

public override void _Ready()
{
_maze = GetParent<Maze>();
if (_maze == null) GD.PrintErr("MazeSpawner: ¡No se encontró el nodo principal 'Maze'!");
}

public void SpawnEntities()
{
if (_maze == null) return;

_occupiedPositions.Clear();

Vector2I bossSpawnPos = SpawnBoss();
SpawnPlayer();
SpawnPalo();
SpawnKey(bossSpawnPos);   
SpawnDoorOnWall(); // Spawnea la puerta pegada a una pared
}

private Vector2I SpawnBoss()
{
Vector2I spawnPos = new Vector2I(_maze.Width / 2, _maze.Height / 2);

if (_maze.BossScene != null)
{
var boss = _maze.BossScene.Instantiate<Node3D>();
boss.Position = new Vector3(spawnPos.X * _maze.GridScale, 1.20f, spawnPos.Y * _maze.GridScale);
_maze.AddChild(boss);
_occupiedPositions.Add(spawnPos);
}

return spawnPos;
}

private void SpawnKey(Vector2I bossPosition)
{
if (_maze.KeyScene == null) return;

var key = _maze.KeyScene.Instantiate<Node3D>();
key.Position = new Vector3(bossPosition.X * _maze.GridScale, 0.5f, bossPosition.Y * _maze.GridScale);
_maze.AddChild(key);
}

private void SpawnDoorOnWall()
{
if (_maze.DoorScene == null) return;

// 1. Elegir un espacio libre que tenga una pared justo al lado
Vector2I freeSpace = ObtenerEspacioConParedAdyacente();
var door = _maze.DoorScene.Instantiate<Node3D>();
Vector3 basePos = new Vector3(freeSpace.X * _maze.GridScale, 0.0f, freeSpace.Y * _maze.GridScale);

float offset = _maze.GridScale * 0.45f; // Desplazamiento para pegarse a la pared sobresaliendo

// Revisar dónde está la pared para orientar y desplazar la puerta
if (_maze.Map[freeSpace.X - 1, freeSpace.Y] == 1) // Pared a la Izquierda
{
basePos.X -= offset;
door.RotationDegrees = new Vector3(0, 90, 0);
}
else if (_maze.Map[freeSpace.X + 1, freeSpace.Y] == 1) // Pared a la Derecha
{
basePos.X += offset;
door.RotationDegrees = new Vector3(0, -90, 0);
}
else if (_maze.Map[freeSpace.X, freeSpace.Y - 1] == 1) // Pared Arriba
{
basePos.Z -= offset;
door.RotationDegrees = new Vector3(0, 0, 0);
}
else if (_maze.Map[freeSpace.X, freeSpace.Y + 1] == 1) // Pared Abajo
{
basePos.Z += offset;
door.RotationDegrees = new Vector3(0, 180, 0);
}

door.Position = basePos;
_maze.AddChild(door);

_occupiedPositions.Add(freeSpace);
GD.Print($"Puerta pegada sobresaliendo a la pared en: {freeSpace}");
}

private Vector2I ObtenerEspacioConParedAdyacente()
{
List<Vector2I> candidatos = new List<Vector2I>();

for (int x = 1; x < _maze.Width - 1; x++)
{
for (int z = 1; z < _maze.Height - 1; z++)
{
if (_maze.Map[x, z] == 0 && !_occupiedPositions.Contains(new Vector2I(x, z)))
{
// Tiene al menos una pared vecina
if (_maze.Map[x - 1, z] == 1 || _maze.Map[x + 1, z] == 1 ||
_maze.Map[x, z - 1] == 1 || _maze.Map[x, z + 1] == 1)
{
candidatos.Add(new Vector2I(x, z));
}
}
}
}

if (candidatos.Count > 0)
{
return candidatos[_random.Next(candidatos.Count)];
}

return ObtenerEspacioVacioAleatorio();
}

private void SpawnPlayer()
{
if (_maze.PlayerScene == null) return;
Vector2I center = new Vector2I(_maze.Width / 2, _maze.Height / 2);
Vector2I spawnPos = _maze.DebugSpawnPlayerNearBoss
? center + new Vector2I(2, 0) 
: FindCornerSpace();

var player = _maze.PlayerScene.Instantiate<Node3D>();
player.Position = new Vector3(spawnPos.X * _maze.GridScale, 3.0f, spawnPos.Y * _maze.GridScale); 
_maze.AddChild(player);
_occupiedPositions.Add(spawnPos);

var cam = player.GetNodeOrNull<Camera3D>("Head/Camera3D");
if (cam != null) cam.Current = true;
}

private void SpawnPalo()
{
if (_maze.palo_de_madera == null) return;
int cantidadPalos = _random.Next(5, 16);
float alturaPalo = 1.0f; 
for (int i = 0; i < cantidadPalos; i++)
{
Vector2I spawnPos = ObtenerEspacioVacioAleatorio();
var palo = _maze.palo_de_madera.Instantiate<Node3D>();
palo.Position = new Vector3(spawnPos.X * _maze.GridScale, alturaPalo, spawnPos.Y * _maze.GridScale); 
_maze.AddChild(palo);
_occupiedPositions.Add(spawnPos);
}
}

private Vector2I ObtenerEspacioVacioAleatorio()
{
int intentos = 0;
while (intentos < 1000)
{
int x = _random.Next(1, _maze.Width - 1);
int z = _random.Next(1, _maze.Height - 1);
Vector2I pos = new Vector2I(x, z);
if (_maze.Map[x, z] == 0 && !_occupiedPositions.Contains(pos)) return pos;
intentos++;
}
return _maze.FindEmptySpace();
}

private Vector2I FindCornerSpace()
{
int esquinaElegida = _random.Next(0, 4);
int targetX = 1;
int targetZ = 1;

if (esquinaElegida == 1) { targetX = _maze.Width - 2; targetZ = 1; }
if (esquinaElegida == 2) { targetX = 1; targetZ = _maze.Height - 2; }
if (esquinaElegida == 3) { targetX = _maze.Width - 2; targetZ = _maze.Height - 2; }

int startX = Math.Max(1, targetX - 2);
int endX = Math.Min(_maze.Width - 2, targetX + 2);
int startZ = Math.Max(1, targetZ - 2);
int endZ = Math.Min(_maze.Height - 2, targetZ + 2);

for (int x = startX; x <= endX; x++)
{
for (int z = startZ; z <= endZ; z++)
{
Vector2I pos = new Vector2I(x, z);
if (_maze.Map[x, z] == 0 && !_occupiedPositions.Contains(pos)) return pos;
}
}
return _maze.FindEmptySpace();
}
}
