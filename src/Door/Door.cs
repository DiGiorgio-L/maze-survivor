using Godot;

public partial class Door : Node3D
{
	// Método universal que responderá tanto si el collider es este nodo como si es un hijo
	public void interact(Node3D interactor)
	{
		GD.Print("🚪 ¡Puerta detectó la interacción del jugador!");

		if (interactor == null) return;

		bool hasKey = false;
		var keyProp = interactor.Get("HasKey");
		if (keyProp.VariantType != Variant.Type.Nil)
		{
			hasKey = (bool)keyProp;
		}

		GD.Print($"🚪 Estado de HasKey: {hasKey}");

		if (hasKey)
		{
			GD.Print("🎉 ¡VICTORIA! Abriendo puerta...");
			Input.MouseMode = Input.MouseModeEnum.Visible;
			GetTree().Quit();
		}
		else
		{
			GD.Print("🔒 Se necesita la llave.");
		}
	}
}
