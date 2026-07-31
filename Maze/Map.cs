using Godot;
using System;

public partial class Map : Control
{
	[Export] public Vector2 TabletSize = new Vector2(320, 320); // Tamaño reducido de la tablet
	[Export] public float BorderThickness = 30.0f;               // Bordes plateados ajustados
	[Export] public Color WallColor = new Color(0.15f, 0.15f, 0.2f);
	[Export] public Color PathColor = new Color(0.85f, 0.85f, 0.9f);
	[Export] public Color PlayerColor = new Color(0.9f, 0.2f, 0.2f);
	[Export] public Color UnexploredColor = new Color(0.02f, 0.02f, 0.05f);

	private byte[,] _mazeData;
	private bool[,] _exploredData;
	private int _gridWidth;
	private int _gridHeight;
	private float _gridScale;
	
	private Node3D _player;
	private Vector2I _lastPlayerGridPos = new Vector2I(-1, -1);
	
	private float _blinkTimer = 0f;
	private bool _showRecDot = true;

	public override void _Ready()
	{
		// Centrar la tablet compacta en la pantalla
		CustomMinimumSize = TabletSize;
		SetAnchorsPreset(LayoutPreset.Center);
		PivotOffset = TabletSize / 2.0f;
		Position = (GetViewportRect().Size - TabletSize) / 2.0f;
		
		Visible = false; // Inicia oculta hasta presionar M
	}

	public void InitializeMapData(byte[,] mazeData, float gridScale)
	{
		_mazeData = mazeData;
		_gridWidth = mazeData.GetLength(0);
		_gridHeight = mazeData.GetLength(1);
		_gridScale = gridScale;
		
		_exploredData = new bool[_gridWidth, _gridHeight];
		QueueRedraw();
	}

	public void SetPlayer(Node3D player)
	{
		_player = player;
	}

	public override void _Process(double delta)
	{
		// Animación luz REC (parpadeo)
		_blinkTimer += (float)delta;
		if (_blinkTimer >= 0.5f)
		{
			_blinkTimer = 0f;
			_showRecDot = !_showRecDot;
			if (Visible) QueueRedraw();
		}

		if (_player == null || _mazeData == null) return;

		int playerGridX = Mathf.RoundToInt(_player.GlobalPosition.X / _gridScale);
		int playerGridZ = Mathf.RoundToInt(_player.GlobalPosition.Z / _gridScale);

		Vector2I currentGridPos = new Vector2I(playerGridX, playerGridZ);

		// Revelar SOLAMENTE la casilla exactas por la que camina
		if (currentGridPos != _lastPlayerGridPos)
		{
			_lastPlayerGridPos = currentGridPos;
			
			if (playerGridX >= 0 && playerGridX < _gridWidth && playerGridZ >= 0 && playerGridZ < _gridHeight)
			{
				_exploredData[playerGridX, playerGridZ] = true;
			}
			
			if (Visible) QueueRedraw();
		}
	}

	public override void _Draw()
	{
		// 1. Marco metálico
		Rect2 outerRect = new Rect2(Vector2.Zero, TabletSize);
		DrawRect(outerRect, new Color(0.7f, 0.72f, 0.75f));
		
		Rect2 innerBezel = new Rect2(new Vector2(4, 4), TabletSize - new Vector2(8, 8));
		DrawRect(innerBezel, new Color(0.35f, 0.37f, 0.4f));

		// 2. Pantalla interna
		Vector2 screenPos = new Vector2(BorderThickness, BorderThickness);
		Vector2 screenSize = TabletSize - (screenPos * 2.0f);
		Rect2 screenRect = new Rect2(screenPos, screenSize);
		
		DrawRect(screenRect, UnexploredColor);

		// 3. Punto Rojo "REC" en el borde superior centrado
		if (_showRecDot)
		{
			Vector2 recDotPos = new Vector2(TabletSize.X / 2.0f, BorderThickness / 2.0f);
			DrawCircle(recDotPos, 5.0f, new Color(1f, 0.1f, 0.1f));
		}

		if (_mazeData == null || _player == null) return;

		// 4. Dibujar SOLAMENTE las casillas por donde pasó el jugador
		float cellWidth = screenRect.Size.X / _gridWidth;
		float cellHeight = screenRect.Size.Y / _gridHeight;

		for (int x = 0; x < _gridWidth; x++)
		{
			for (int z = 0; z < _gridHeight; z++)
			{
				if (_exploredData[x, z])
				{
					Vector2 cellPos = screenRect.Position + new Vector2(x * cellWidth, z * cellHeight);
					Rect2 cellRect = new Rect2(cellPos, new Vector2(cellWidth + 0.4f, cellHeight + 0.4f));
					
					Color colorToDraw = (_mazeData[x, z] == 1) ? WallColor : PathColor;
					DrawRect(cellRect, colorToDraw);
				}
			}
		}

		// 5. Indicador del jugador (Punto rojo más pequeño)
		float playerScreenX = screenRect.Position.X + ((_player.GlobalPosition.X / _gridScale) * cellWidth);
		float playerScreenY = screenRect.Position.Y + ((_player.GlobalPosition.Z / _gridScale) * cellHeight);
		
		Vector2 playerPosOnScreen = new Vector2(playerScreenX, playerScreenY);
		playerPosOnScreen.X = Mathf.Clamp(playerPosOnScreen.X, screenRect.Position.X, screenRect.End.X);
		playerPosOnScreen.Y = Mathf.Clamp(playerPosOnScreen.Y, screenRect.Position.Y, screenRect.End.Y);

		// Radio pequeño del punto rojo para no oscurecer el mapa
		float playerRadius = Mathf.Max(2.5f, cellWidth * 0.75f);
		DrawCircle(playerPosOnScreen, playerRadius, PlayerColor);
	}
}
