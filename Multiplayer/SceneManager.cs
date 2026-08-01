using Godot;
using System;

// Currently only used to handle the process of spawning players and asigning them their authority ID.
public partial class SceneManager : Node
{
	[Export]
	private PackedScene playerScene;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Multiplayer.PeerDisconnected += OnPeerDisconnected;

		int index = 0;
		var spawnPoints = GetTree().GetNodesInGroup("PlayerSpawnPoints");
		foreach (var item in GameManager.Players)
		{
			Player currentPlayer = playerScene.Instantiate<Player>();
			currentPlayer.Name = item.Id.ToString();
			currentPlayer.SetMultiplayerAuthority(item.Id);
			AddChild(currentPlayer);

			if (spawnPoints.Count > 0)
			{
				int targetIndex = index % spawnPoints.Count;
				foreach (Node3D spawnPoint in spawnPoints)
				{
					if (int.TryParse(spawnPoint.Name, out int spIndex) && spIndex == targetIndex)
					{
						currentPlayer.GlobalPosition = spawnPoint.GlobalPosition;
						break;
					}
				}
			}
			index++;
		}
	}

	public override void _ExitTree()
	{
		Multiplayer.PeerDisconnected -= OnPeerDisconnected;
	}

	private void OnPeerDisconnected(long id)
	{
		var playerNode = GetNodeOrNull(id.ToString());
		if (playerNode != null)
		{
			playerNode.QueueFree();
			GD.Print($"[SceneManager] Safely removed player node for disconnected peer {id}");
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
