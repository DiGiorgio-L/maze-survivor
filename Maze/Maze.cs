using Godot;
using System;
using System.Collections.Generic;

public partial class Maze : Node3D
{
	[Export] public int Width = 51;
	[Export] public int Height = 51;
	[Export] public float GridScale = 6.0f;
	[Export] public PackedScene PlayerScene;
	[Export] public PackedScene BossScene;
	[Export] public bool DebugSpawnPlayerNearBoss = false;
	[Export] public PackedScene palo_de_madera;
	[Export] public PackedScene HudScene;

	[Export] public Texture2D WallTexture;
	[Export] public Texture2D FloorTexture;

	public byte[,] Map;
	private Random _random = new Random();
	private NavigationRegion3D _navRegion;
	private Node3D _spawnedPlayer;

	public override void _Ready()
	{
		if (Width % 2 == 0) Width++;
		if (Height % 2 == 0) Height++;

		if (WallTexture == null) WallTexture = GD.Load<Texture2D>("res://src/Maze/paredes.jpg");
		if (FloorTexture == null) FloorTexture = GD.Load<Texture2D>("res://src/Maze/piso.jpg");

		InitializeMap();
		GenerateIterative(1, 1);
		CreateCentralRoom();

		_navRegion = new NavigationRegion3D();
		_navRegion.NavigationMesh = new NavigationMesh
		{
			AgentRadius = 0.6f,
			AgentHeight = 2.0f,
			AgentMaxClimb = 0.3f,
			AgentMaxSlope = 45.0f,
			CellSize = 0.25f,
			CellHeight = 0.25f
		};
		AddChild(_navRegion);

		CreateFloorWithCollision(); 
		DrawMapOptimized();
		
		_navRegion.BakeNavigationMesh(onThread: false);

		var spawner = new MazeSpawner();
		AddChild(spawner);
		spawner.SpawnEntities();

		SpawnHUD();
	}

	public void SetSpawnedPlayer(Node3D player)
	{
		_spawnedPlayer = player;
	}

	private void SpawnHUD()
	{
		if (HudScene == null)
		{
			HudScene = GD.Load<PackedScene>("res://src/ui/hud.tscn");
		}
		
		if (HudScene != null)
		{
			var hud = HudScene.Instantiate();
			AddChild(hud);
			
			if (_spawnedPlayer == null)
			{
				_spawnedPlayer = BuscarJugadorEnHijos();
			}

			if (_spawnedPlayer != null && hud.HasMethod("setup_player"))
			{
				hud.Call("setup_player", _spawnedPlayer);
			}

			// Instanciar el mapa dándole el nombre "Map"
			var mapUI = hud.GetNodeOrNull<Map>("Map"); 
			if (mapUI == null)
			{
				mapUI = new Map();
				mapUI.Name = "Map"; 
				hud.AddChild(mapUI);
			}
			
			mapUI.InitializeMapData(Map, GridScale);
			if (_spawnedPlayer != null)
			{
				mapUI.SetPlayer(_spawnedPlayer);
			}
		}
	}

	private Node3D BuscarJugadorEnHijos()
	{
		foreach (var child in GetChildren())
		{
			if (child is Node3D node && node.HasMethod("modify_stat"))
			{
				return node;
			}
		}
		return null;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (_spawnedPlayer == null)
			{
				_spawnedPlayer = BuscarJugadorEnHijos();
			}

			if (_spawnedPlayer != null)
			{
				switch (keyEvent.Keycode)
				{
					case Key.Key1:
						_spawnedPlayer.Call("modify_stat", 0, -20f);
						GD.Print("Debug UI: Tecla 1 -> -20 HP");
						break;
					case Key.Key2:
						_spawnedPlayer.Call("modify_stat", 0, 20f);
						GD.Print("Debug UI: Tecla 2 -> +20 HP");
						break;
					case Key.Key3:
						_spawnedPlayer.Call("modify_stat", 1, -25f);
						GD.Print("Debug UI: Tecla 3 -> -25 Estamina");
						break;
					case Key.Key4:
						_spawnedPlayer.Call("modify_stat", 1, 25f);
						GD.Print("Debug UI: Tecla 4 -> +25 Estamina");
						break;
					case Key.Key5:
						_spawnedPlayer.Call("modify_stat", 2, -30f);
						GD.Print("Debug UI: Tecla 5 -> -30 Hambre");
						break;
					case Key.Key6:
						_spawnedPlayer.Call("modify_stat", 2, 30f);
						GD.Print("Debug UI: Tecla 6 -> +30 Hambre");
						break;
					case Key.Key0:
						if (_spawnedPlayer.HasMethod("SetInputLocked"))
						{
							_spawnedPlayer.Call("SetInputLocked", false);
						}
						_spawnedPlayer.Call("modify_stat", 0, 100f);
						_spawnedPlayer.Call("modify_stat", 1, 100f);
						_spawnedPlayer.Call("modify_stat", 2, 100f);
						if (_spawnedPlayer.HasNode("StatusManager"))
						{
							_spawnedPlayer.GetNode("StatusManager").Call("clear_all");
						}
						GD.Print("Debug UI: Tecla 0 -> Resucitar y desbloquear jugador");
						break;
				}
			}
		}
	}

	private void CreateFloorWithCollision()
	{
		var staticBody = new StaticBody3D();
		staticBody.Position = new Vector3(((Width * GridScale) / 2) - (GridScale/2), 0, ((Height * GridScale) / 2) - (GridScale/2));
		
		var meshInstance = new MeshInstance3D();
		meshInstance.Mesh = new PlaneMesh() { Size = new Vector2(Width * GridScale, Height * GridScale) };
		
		var collisionShape = new CollisionShape3D();
		collisionShape.Shape = new BoxShape3D { Size = new Vector3(Width * GridScale, 0.2f, Height * GridScale) };
		
		staticBody.AddChild(meshInstance);
		staticBody.AddChild(collisionShape);
		
		var mat = new StandardMaterial3D();
		if (FloorTexture != null)
		{
			mat.AlbedoTexture = FloorTexture;
			mat.Uv1Scale = new Vector3(Width / 2.0f, Height / 2.0f, 1.0f);
			mat.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
		}
		else
		{
			mat.AlbedoColor = new Color(0.2f, 0.2f, 0.2f);
		}

		meshInstance.SetSurfaceOverrideMaterial(0, mat);
		_navRegion.AddChild(staticBody);
	}

	private void DrawMapOptimized()
	{
		var wallMaterial = new StandardMaterial3D();
		if (WallTexture != null)
		{
			wallMaterial.AlbedoTexture = WallTexture;
			wallMaterial.Uv1Scale = new Vector3(1.0f, 1.0f, 1.0f);
			wallMaterial.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
		}
		else
		{
			wallMaterial.AlbedoColor = new Color(0.2f, 0.6f, 0.8f);
		}

		var boxMesh = new BoxMesh() { Size = new Vector3(GridScale, GridScale, GridScale) };
		boxMesh.Material = wallMaterial;

		int wallCount = 0;
		for (int z = 0; z < Height; z++)
			for (int x = 0; x < Width; x++)
				if (Map[x, z] == 1) wallCount++;

		var multiMesh = new MultiMesh();
		multiMesh.Mesh = boxMesh;
		multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
		multiMesh.InstanceCount = wallCount;

		int index = 0;
		var staticBody = new StaticBody3D();

		for (int z = 0; z < Height; z++)
		{
			for (int x = 0; x < Width; x++)
			{
				if (Map[x, z] == 1)
				{
					Vector3 pos = new Vector3(x * GridScale, GridScale / 2, z * GridScale);
					Transform3D transform = new Transform3D(Basis.Identity, pos);
					multiMesh.SetInstanceTransform(index, transform);

					var colShape = new CollisionShape3D();
					colShape.Shape = new BoxShape3D { Size = new Vector3(GridScale, GridScale, GridScale) };
					colShape.Position = pos;
					staticBody.AddChild(colShape);

					index++;
				}
			}
		}

		var multiMeshInstance = new MultiMeshInstance3D();
		multiMeshInstance.Multimesh = multiMesh;
		
		_navRegion.AddChild(multiMeshInstance);
		_navRegion.AddChild(staticBody);
	}

	public Vector2I FindEmptySpace() { for (int x = 0; x < Width; x++) for (int z = 0; z < Height; z++) if (Map[x, z] == 0) return new Vector2I(x, z); return new Vector2I(1, 1); }
	private void InitializeMap() { Map = new byte[Width, Height]; for (int z = 0; z < Height; z++) for (int x = 0; x < Width; x++) Map[x, z] = 1; }
	private void GenerateIterative(int startX, int startZ) { 
		var stack = new Stack<Vector2I>();
		Map[startX, startZ] = 0;
		stack.Push(new Vector2I(startX, startZ));
		while (stack.Count > 0) {
			var current = stack.Peek();
			var neighbors = GetValidNeighbors(current.X, current.Y);
			if (neighbors.Count > 0) {
				var next = neighbors[_random.Next(neighbors.Count)];
				Map[current.X + (next.X - current.X) / 2, current.Y + (next.Y - current.Y) / 2] = 0;
				Map[next.X, next.Y] = 0;
				stack.Push(next);
			} else stack.Pop();
		}
	}
	private List<Vector2I> GetValidNeighbors(int x, int z) {
		var valid = new List<Vector2I>();
		var dirs = new Vector2I[] { new(2, 0), new(0, 2), new(-2, 0), new(0, -2) };
		foreach (var dir in dirs) {
			int nx = x + dir.X, nz = z + dir.Y;
			if (nx > 0 && nx < Width - 1 && nz > 0 && nz < Height - 1 && Map[nx, nz] == 1)
				valid.Add(new Vector2I(nx, nz));
		}
		return valid;
	}
	private void CreateCentralRoom() {
		int centerX = Width / 2;
		int centerZ = Height / 2;
		int radius = 3;
		for (int x = centerX - radius; x <= centerX + radius; x++)
			for (int z = centerZ - radius; z <= centerZ + radius; z++)
				Map[x, z] = 0;
	}
}
