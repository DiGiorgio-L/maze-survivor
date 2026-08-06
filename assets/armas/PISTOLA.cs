using Godot;
using System;
using System.Collections.Generic;

public partial class PISTOLA : Node3D
{
	[Export] public PackedScene EscenaBala { get; set; }
	[Export] public float CadenciaDisparo { get; set; } = 0.3f;
	[Export] public int CapacidadCargador { get; set; } = 10;
	[Export] public int BalasReserva { get; set; } = 20;
	[Export] public float TiempoRecarga { get; set; } = 0.7f;

	[Export] public float Damage { get; set; } = 1.0f;

	private Marker3D _puntaArma;
	private AnimationPlayer _animador;

	private AudioStreamPlayer3D _reproductorDisparo;
	private AudioStreamPlayer3D _reproductorRecarga;

	private Area3D _pickupArea;
	private Area3D _hitboxArea;

	private int _balasActuales;
	private bool _puedeDisparar = true;
	private bool _recargando = false;

	private Node3D _portador = null;

	private List<Node3D> _enemigosEnRango = new List<Node3D>();

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

	public override void _PhysicsProcess(double delta)
	{
		if (Input.IsActionJustPressed("recargar") && !_recargando && _balasActuales < CapacidadCargador)
		{
			IniciarRecarga();
			return;
		}

		if (Input.IsActionPressed("disparar") && _puedeDisparar && !_recargando)
		{
			IntentarDisparar();
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

		if (_animador != null && _animador.HasAnimation("recoil"))
		{
			_animador.Stop();
			_animador.Play("recoil");
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

		if (_animador != null && _animador.HasAnimation("reload"))
		{
			_animador.Stop();
			_animador.Play("reload");
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
	// HITBOX PARA ENEMIGOS
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
	// RECOGER EL ARMA
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
		}
	}

	// -------------------------
	// ANIMACIONES
	// -------------------------
	private void OnAnimadorAnimationFinished(StringName animName)
	{
		if (animName == "recoil" || animName == "Equipar")
			_puedeDisparar = true;

		else if (animName == "Desequipar")
		{
			Visible = false;
			_puedeDisparar = false;
		}
	}
}
