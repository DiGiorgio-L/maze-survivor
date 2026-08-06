using Godot;
using System;
using System.Collections.Generic;

public partial class RIFLE : Node3D
{
	// -------------------------
	// CONFIGURACIÓN DEL ARMA
	// -------------------------
	[Export] public PackedScene EscenaBala { get; set; }
	[Export] public float CadenciaDisparo { get; set; } = 0.75f;
	[Export] public int CapacidadCargador { get; set; } = 10;
	[Export] public int BalasReserva { get; set; } = 15;
	[Export] public float TiempoRecarga { get; set; } = 1.0f;
	[Export] public float Damage { get; set; } = 1.0f;

	// -------------------------
	// NODOS
	// -------------------------
	private Marker3D _puntaArma;
	private AnimationPlayer _animador;
	private AudioStreamPlayer3D _reproductorDisparo;
	private AudioStreamPlayer3D _reproductorRecarga;

	private Area3D _pickupArea;
	private Area3D _hitboxArea;

	// -------------------------
	// ESTADOS
	// -------------------------
	private int _balasActuales;
	private bool _puedeDisparar = true;
	private bool _recargando = false;

	private bool _canAttack = false; // para melee

	private Node3D _portador = null;

	private List<Node3D> _enemigosEnRango = new List<Node3D>();

	// -------------------------
	// READY
	// -------------------------
	public override void _Ready()
	{
		_balasActuales = CapacidadCargador;

		_puntaArma = GetNodeOrNull<Marker3D>("Boca_canon");
		_animador = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		_reproductorDisparo = GetNodeOrNull<AudioStreamPlayer3D>("SonidoDisparo");
		_reproductorRecarga = GetNodeOrNull<AudioStreamPlayer3D>("SonidoRecarga");

		_pickupArea = GetNodeOrNull<Area3D>("PickupArea");
		_hitboxArea = GetNodeOrNull<Area3D>("Hitbox");

		if (_pickupArea != null)
			_pickupArea.BodyEntered += OnPickupAreaBodyEntered;

		if (_hitboxArea != null)
		{
			_hitboxArea.BodyEntered += OnHitboxBodyEntered;
			_hitboxArea.BodyExited += OnHitboxBodyExited;
		}

		if (_animador != null)
			_animador.AnimationFinished += OnAnimadorAnimationFinished;

		// Auto-equip si ya está en la mano
		if (GetParent() != null && GetParent().Name == "Hand")
		{
			_puedeDisparar = true;
			_canAttack = true;

			Node nodoActual = GetParent();
			while (nodoActual != null)
			{
				if (nodoActual is Node3D node3D && node3D.IsInGroup("player"))
				{
					_portador = node3D;
					break;
				}
				nodoActual = nodoActual.GetParent();
			}

			_pickupArea?.QueueFree();
		}
	}

	// -------------------------
	// PROCESS
	// -------------------------
	public override void _PhysicsProcess(double delta)
	{
		// Recargar
		if (Input.IsActionJustPressed("recargar") && !_recargando && _balasActuales < CapacidadCargador)
		{
			IniciarRecarga();
			return;
		}

		// Disparar
		if (Input.IsActionPressed("disparar") && _puedeDisparar && !_recargando)
		{
			IntentarDisparar();
		}

		// Golpe melee (opcional)
		if (Input.IsActionPressed("shoot") && _canAttack && !_animador.IsPlaying())
		{
			_animador.Play("Golpear");
			_canAttack = false;

			foreach (Node3D enemigo in _enemigosEnRango)
			{
				if (enemigo.HasMethod("hit"))
					enemigo.Call("hit", Damage);
			}
		}
	}

	// -------------------------
	// DISPARAR
	// -------------------------
	private void IntentarDisparar()
	{
		if (_balasActuales <= 0)
		{
			IniciarRecarga();
			return;
		}

		_balasActuales--;
		_puedeDisparar = false;

		GetTree().CreateTimer(CadenciaDisparo).Timeout += () => _puedeDisparar = true;

		if (EscenaBala != null)
		{
			Node nuevaBala = EscenaBala.Instantiate();
			GetTree().Root.AddChild(nuevaBala);

			if (nuevaBala is Node3D bala3D)
				bala3D.GlobalTransform = _puntaArma != null ? _puntaArma.GlobalTransform : GlobalTransform;

			nuevaBala.Set("damage", Damage);
			nuevaBala.Set("portador", _portador);
		}

		_reproductorDisparo?.Play();

		if (_animador != null && _animador.HasAnimation("recoil2"))
		{
			_animador.Stop();
			_animador.Play("recoil2");
		}
	}

	// -------------------------
	// RECARGA
	// -------------------------
	private void IniciarRecarga()
	{
		if (BalasReserva <= 0 || _recargando) return;

		_recargando = true;

		_reproductorRecarga?.Play();

		if (_animador != null && _animador.HasAnimation("reload2"))
		{
			_animador.Stop();
			_animador.Play("reload2");
		}

		GetTree().CreateTimer(TiempoRecarga).Timeout += TerminarRecarga;
	}

	private void TerminarRecarga()
	{
		int balasNecesarias = CapacidadCargador - _balasActuales;
		int balasATransferir = Mathf.Min(balasNecesarias, BalasReserva);

		_balasActuales += balasATransferir;
		BalasReserva -= balasATransferir;

		_recargando = false;
	}

	// -------------------------
	// HITBOX
	// -------------------------
	private void OnHitboxBodyEntered(Node3D body)
	{
		if (body.IsInGroup("player") && body != _portador && !_enemigosEnRango.Contains(body))
			_enemigosEnRango.Add(body);
	}

	private void OnHitboxBodyExited(Node3D body)
	{
		if (_enemigosEnRango.Contains(body))
			_enemigosEnRango.Remove(body);
	}

	// -------------------------
	// PICKUP
	// -------------------------
	private void OnPickupAreaBodyEntered(Node3D body)
	{
		if (!body.IsInGroup("player")) return;

		Node3D manoJugador = body.GetNodeOrNull<Node3D>("Head/Camera3D/Hand");

		if (manoJugador != null)
		{
			_portador = body;

			Reparent(manoJugador);

			Position = Vector3.Zero;
			Rotation = Vector3.Zero;

			_pickupArea?.QueueFree();

			_puedeDisparar = true;
			_canAttack = true;
		}
	}

	// -------------------------
	// ANIMACIONES
	// -------------------------
	private void OnAnimadorAnimationFinished(StringName animName)
	{
		if (animName == "Golpear")
			_canAttack = true;

		else if (animName == "Equipar")
			_canAttack = true;

		else if (animName == "Desequipar")
		{
			Visible = false;
			_canAttack = false;
		}
	}
}
